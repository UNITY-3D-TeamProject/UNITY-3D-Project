using UnityEngine;
using Attribute.Effect;
using Movement;
using Skill.Core;
using AI.Boss.Arena;
using AI.Boss.Logic;

namespace AI.Boss.BossA
{
    /// <summary>
    /// A4. 블리츠크랭크 Q 식 그랩. 한 손이 직선 경로를 예고한 뒤 곧장 날아가 경로에서 처음 닿은 플레이어를 잡고,
    /// 잡힌 플레이어는 손이 떠난 쪽에서 가장 가까운 멀쩡한 칸의 중심으로 끌려온다.
    /// 끌려온 칸은 예고가 뜨고, 반대쪽 손이 내려쳐 그 칸 하나만 무너뜨린다. 빗나가면 손만 돌아오고 끝난다.
    /// BossKnockback 호출 측이므로 이번 프레임 이동에 합쳐지도록 실행 순서를 앞당긴다.
    /// 코루틴 없이 Update 상태 머신으로 돌아서, Stop 한 번으로 모든 단계를 정리할 수 있다.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public class BossAGrab : SkillBase
    {
        #region Types
        private enum EGrabState
        {
            Idle,
            Aiming,
            Flying,
            Missed,
            Telegraph,
            Slamming,
            Staying,
            Recalling,
        }
        #endregion

        #region Serialized Fields
        [Header("References")]
        [SerializeField] private BossController _controller;

        [Header("Settings")]
        [Tooltip("내려치는 순간 끌려온 칸 위의 플레이어에게 줄 피해. 비워 두면 피해 없음")]
        [SerializeField] private SOAttributeEffect _hitEffect;
        [Tooltip("임시값. 직선 경로를 예고하는 시간(초). TelegraphScale 을 곱한다")]
        [SerializeField, Min(0.0f)] private float _aimSeconds = 1.0f;
        [Tooltip("임시값. 손이 날아가는 속도(m/s)")]
        [SerializeField, Min(0.1f)] private float _hookSpeed = 60.0f;
        [Tooltip("임시값. 손이 날아갈 수 있는 최대 거리(m). 보스가 판 밖에 있으므로 판의 맨 끝 칸까지 닿아야 한다")]
        [SerializeField, Min(0.0f)] private float _hookRange = 100.0f;
        [Tooltip("임시값. 손이 날아갈 때 아레나 바닥에서 손 오브젝트까지의 높이(m)")]
        [SerializeField, Min(0.0f)] private float _hookHeight = 1.0f;
        [Tooltip("임시값. 손 중심에서 이 반경(m) 안에 들어온 플레이어를 잡는다. 직선 예고의 폭은 이 값의 두 배")]
        [SerializeField, Min(0.0f)] private float _hookRadius = 2.5f;
        [Tooltip("임시값. 잡은 플레이어를 끌려온 칸 중심까지 끌어당기는 시간(초)")]
        [SerializeField, Min(0.0f)] private float _pullSeconds = 0.4f;
        [Tooltip("임시값. 잡은 순간부터 반대쪽 손이 내려치기까지의 예고 시간(초). TelegraphScale 을 곱한다")]
        [SerializeField, Min(0.0f)] private float _slamTelegraphSeconds = 1.5f;
        #endregion

        #region Private Fields
        private readonly BossKnockback _knockback = new BossKnockback();
        private EGrabState _state = EGrabState.Idle;
        private BossHand _grabHand;
        private BossHand _slamHand;
        private Vector3 _origin;
        private Vector3 _direction;
        private float _remaining;
        private int _lineHandle = -1;
        private int _slamTile = -1;
        private bool _isGrabHandBusy;
        private bool _isTelegraphOn;
        #endregion

        #region Properties
        private IBossArena Arena => _controller.Arena;
        private IBossSituation Situation => _controller.Situation;
        #endregion

        #region Unity Lifecycle
        protected override void Awake()
        {
            base.Awake();

            if (!_controller) _controller = GetComponentInParent<BossController>();
        }

        private void OnDisable()
        {
            Stop();
            _knockback.Cancel();
        }

        private void Update()
        {
            float deltaTime = Time.deltaTime;
            _knockback.Tick(deltaTime);

            switch (_state)
            {
                case EGrabState.Aiming:
                    _remaining -= deltaTime;
                    if (_remaining <= 0.0f) StartFly();
                    break;

                case EGrabState.Flying:
                    TryCapture();
                    break;

                case EGrabState.Telegraph:
                    // 내려치는 시간만큼 앞서 시작해 예고가 끝나는 순간 손이 닿게 한다
                    _remaining -= deltaTime;
                    if (_slamHand.IsHovering && (_remaining <= _slamHand.SlamSeconds)) StartSlam();
                    break;
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// 진행 중인 패턴을 끊는다. 예고를 끄고 손을 회수하며 취소로 알린다.
        /// </summary>
        public override void Stop()
        {
            if (_state == EGrabState.Idle) return;

            _state = EGrabState.Idle;
            _isGrabHandBusy = false;
            _knockback.Cancel();
            HideLine();
            ClearTelegraph();

            if (_grabHand) _grabHand.CancelAndRecall();
            if (_slamHand) _slamHand.CancelAndRecall();

            if (_controller) _controller.EndPattern(this, false);
        }
        #endregion

        #region Protected Methods
        protected override void Execute()
        {
            if (_state != EGrabState.Idle) return;

            bool isReady = _controller && (_controller.Arena != null) && (_controller.Situation != null);
            if (!isReady)
            {
                Debug.LogError($"[{name}] BossController 가 없거나 Initialize 되지 않았습니다.");
                return;
            }

            // 선택 결과의 손이 그랩, 반대 손이 내려치기
            bool isLeftGrab = _controller.CurrentChoice.HandTileLeft >= 0;
            _grabHand = _controller.GetHand(isLeftGrab);
            _slamHand = _controller.GetHand(!isLeftGrab);
            Transform body = Situation.PlayerBody;
            if (!_grabHand || !_slamHand || !body)
            {
                Debug.LogError($"[{name}] 양손 또는 플레이어를 찾지 못했습니다.");
                return;
            }

            base.Execute();
            _controller.BeginPattern(this);

            // 손이 날아가는 선과 예고는 아레나 바닥 높이에 둔다
            float floorY = Arena.GetTileCenter(ArenaGrid.CENTER).y;
            Vector3 handPosition = _grabHand.transform.position;
            _origin = new Vector3(handPosition.x, floorY, handPosition.z);

            // 방향은 예고를 켜는 순간의 플레이어 위치로 고정된다. 비켜서 피한다
            Vector3 toPlayer = body.position - _origin;
            toPlayer.y = 0.0f;
            if (toPlayer.sqrMagnitude < 0.0001f) toPlayer = Vector3.ProjectOnPlane(_controller.transform.forward, Vector3.up);
            _direction = toPlayer.normalized;

            _lineHandle = Arena.ShowLine(_origin, _origin + (_direction * _hookRange), _hookRadius * 2.0f);
            _remaining = _aimSeconds * _controller.TelegraphScale;
            _isGrabHandBusy = false;
            _slamTile = -1;
            _state = EGrabState.Aiming;
        }
        #endregion

        #region Private Methods
        private void StartFly()
        {
            HideLine();

            _state = EGrabState.Flying;
            Vector3 destination = _origin + (_direction * _hookRange) + (Vector3.up * _hookHeight);
            _grabHand.Fly(destination, _hookRange / _hookSpeed, OnFlyArrived);
        }

        private void TryCapture()
        {
            Transform body = Situation.PlayerBody;
            if (!body) return;

            Vector3 offset = body.position - _grabHand.transform.position;
            offset.y = 0.0f;
            if (offset.sqrMagnitude > (_hookRadius * _hookRadius)) return;

            CharacterMotor motor = Situation.PlayerMotor;
            int tile = Arena.FindNearestSafe(_origin);
            if (!motor || (tile < 0))
            {
                OnFlyArrived();
                return;
            }

            // 잡은 순간 손이 돌아가고 플레이어는 끌려온다. 반대 손은 끌려온 칸을 향해 떠오른다
            _slamTile = tile;
            Vector3 destination = Arena.GetTileCenter(tile);
            Vector3 displacement = destination - body.position;
            displacement.y = 0.0f;
            _knockback.Start(motor, displacement, _pullSeconds);

            _isGrabHandBusy = true;
            _grabHand.Recall(OnGrabHandBack);

            _remaining = _slamTelegraphSeconds * _controller.TelegraphScale;
            Arena.SetTelegraph(tile, true, _remaining);
            _isTelegraphOn = true;
            _slamHand.MoveToHover(destination);
            _state = EGrabState.Telegraph;
        }

        private void OnFlyArrived()
        {
            _state = EGrabState.Missed;
            _isGrabHandBusy = true;
            _grabHand.Recall(OnGrabHandBack);
        }

        private void StartSlam()
        {
            _state = EGrabState.Slamming;
            _slamHand.Slam(OnSlamImpact);
        }

        private void OnSlamImpact()
        {
            if (_hitEffect && IsPlayerOnTile(_slamTile)) Situation.TryApplyToPlayer(_hitEffect);

            Arena.Collapse(_slamTile);
            ClearTelegraph();

            _state = EGrabState.Staying;
            _slamHand.Stay(_controller.HandStayDuration, OnStayDone);
        }

        private void OnStayDone()
        {
            _state = EGrabState.Recalling;
            _slamHand.Recall(OnSlamHandBack);
        }

        private void OnGrabHandBack()
        {
            _isGrabHandBusy = false;
            TryFinish();
        }

        private void OnSlamHandBack()
        {
            TryFinish();
        }

        /// <summary>
        /// 그랩 손이 돌아왔고, 빗나갔거나 내려치는 손까지 돌아왔을 때 패턴을 끝낸다.
        /// </summary>
        private void TryFinish()
        {
            if (_isGrabHandBusy) return;

            bool isDone = (_state == EGrabState.Missed) || (_state == EGrabState.Recalling);
            if (!isDone) return;

            _state = EGrabState.Idle;
            _controller.EndPattern(this, true);
        }

        private bool IsPlayerOnTile(int tile)
        {
            Transform body = Situation.PlayerBody;
            if (!body) return false;

            return Arena.TryGetTile(body.position, out int index) && (index == tile);
        }

        private void HideLine()
        {
            if (_lineHandle < 0) return;

            if (_controller && (_controller.Arena != null)) _controller.Arena.HideLine(_lineHandle);
            _lineHandle = -1;
        }

        private void ClearTelegraph()
        {
            if (!_isTelegraphOn) return;

            _isTelegraphOn = false;
            if (_controller && (_controller.Arena != null)) _controller.Arena.SetTelegraph(_slamTile, false, 0.0f);
        }
        #endregion
    }
}
