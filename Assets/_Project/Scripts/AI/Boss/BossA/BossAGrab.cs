using UnityEngine;
using Attribute.Effect;
using Movement;
using Skill.Core;
using AI.Boss.Arena;
using AI.Boss.Logic;

namespace AI.Boss.BossA
{
    /// <summary>
    /// A4. 블리츠크랭크 Q 식 그랩. 한 손이 직선 경로를 예고한 뒤 판 반대편 끝까지 곧장 날아가고, 경로에서 처음 닿은 플레이어를 잡는다.
    /// 잡힌 플레이어는 손에 매달린 채(중력 없음) 손이 끝까지 가는 만큼 끌려갔다가, 같은 속도로 돌아오는 손에 끌려
    /// 손이 떠난 쪽에서 가장 가까운 멀쩡한 칸의 중심에 놓인다. 놓인 칸은 예고가 뜨고, 반대쪽 손이 내려쳐 그 칸 하나만 무너뜨린다.
    /// 빗나가면 손만 돌아오고 끝난다.
    /// 잡힌 순간 화면이 흔들리고, 매달린 동안과 놓인 뒤 잠시 플레이어는 움직일 수 없다(남은 예고 시간에는 피할 수 있다).
    /// 그랩 손이 날아갔다 돌아오는 동안은 손과 플레이어의 충돌을 끈다. 켜 두면 손이 플레이어를 밀어내고 끌고 가는 길을 막는다.
    /// 매달린 플레이어를 모터 외부 변위로 옮기므로 이번 프레임 이동에 합쳐지도록 실행 순서를 앞당긴다.
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
            Returning,
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
        [Tooltip("임시값. 손이 날아가는 속도(m/s). 플레이어를 끌고 돌아올 때도 같은 속도다")]
        [SerializeField, Min(0.1f)] private float _hookSpeed = 60.0f;
        [Tooltip("임시값. 손이 날아갈 수 있는 최대 거리(m). 판 반대편 끝이 더 가까우면 거기까지만 간다. 보스가 판 밖에 있으므로 판의 맨 끝 칸까지 닿아야 한다")]
        [SerializeField, Min(0.0f)] private float _hookRange = 100.0f;
        [Tooltip("임시값. 손이 날아갈 때 아레나 바닥에서 손 오브젝트까지의 높이(m)")]
        [SerializeField, Min(0.0f)] private float _hookHeight = 1.0f;
        [Tooltip("임시값. 손 중심에서 이 반경(m) 안에 들어온 플레이어를 잡는다. 직선 예고의 폭은 이 값의 두 배")]
        [SerializeField, Min(0.0f)] private float _hookRadius = 2.5f;
        [Tooltip("임시값. 플레이어를 칸에 놓은 순간부터 반대쪽 손이 내려치기까지의 예고 시간(초). TelegraphScale 을 곱한다")]
        [SerializeField, Min(0.0f)] private float _slamTelegraphSeconds = 1.5f;
        [Tooltip("임시값. 칸에 놓인 뒤에도 플레이어가 움직이지 못하는 시간(초). 매달린 동안은 항상 막는다")]
        [SerializeField, Min(0.0f)] private float _holdSeconds = 0.7f;
        [Tooltip("임시값. 잡는 순간 카메라 흔들기 세기(플레이어 차지샷 쉐이크 = 1). 0 이면 흔들지 않는다")]
        [SerializeField, Min(0.0f)] private float _captureShakeForce = 2.0f;
        #endregion

        #region Private Fields
        private EGrabState _state = EGrabState.Idle;
        private BossHand _grabHand;
        private BossHand _slamHand;
        private Vector3 _origin;
        private Vector3 _direction;
        private float _hookDistance;
        private float _floorY;
        private float _heldHeight;
        private float _remaining;
        private float _holdRemaining;
        private int _lineHandle = -1;
        private int _slamTile = -1;
        private Collider[] _passThroughHandColliders;
        private Collider _passThroughPlayerCollider;
        private bool _isGrabHandBusy;
        private bool _isTelegraphOn;
        private bool _isCaptured;
        private bool _isPlayerHeld;
        private bool _isPlayerLocked;
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
        }

        private void Update()
        {
            float deltaTime = Time.deltaTime;

            // 패턴 단계와 따로 센다. 구속 시간을 늘려도 패턴이 먼저 끝나 풀리지 않는 일이 없다
            if (_isPlayerLocked)
            {
                _holdRemaining -= deltaTime;
                if (_holdRemaining <= 0.0f) ReleasePlayer();
            }

            switch (_state)
            {
                case EGrabState.Aiming:
                    _remaining -= deltaTime;
                    if (_remaining <= 0.0f) StartFly();
                    break;

                case EGrabState.Flying:
                    if (_isCaptured) DragPlayer();
                    else TryCapture();
                    break;

                case EGrabState.Returning:
                    DragPlayer();
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
            ReleasePlayer();
            ReleaseHeldPlayer();
            SetGrabHandPassThrough(false);
            if (_state == EGrabState.Idle) return;

            _state = EGrabState.Idle;
            _isGrabHandBusy = false;
            _isCaptured = false;
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
            _floorY = Arena.GetTileCenter(ArenaGrid.CENTER).y;
            Vector3 handPosition = _grabHand.transform.position;
            _origin = new Vector3(handPosition.x, _floorY, handPosition.z);

            // 방향은 예고를 켜는 순간의 플레이어 위치로 고정된다. 비켜서 피한다
            Vector3 toPlayer = body.position - _origin;
            toPlayer.y = 0.0f;
            if (toPlayer.sqrMagnitude < 0.0001f) toPlayer = Vector3.ProjectOnPlane(_controller.transform.forward, Vector3.up);
            _direction = toPlayer.normalized;
            _hookDistance = GetHookDistance();

            _remaining = _aimSeconds * _controller.TelegraphScale;
            _lineHandle = Arena.ShowLine(_origin, _origin + (_direction * _hookDistance), _hookRadius * 2.0f, _remaining);
            _isGrabHandBusy = false;
            _isCaptured = false;
            _slamTile = -1;
            _state = EGrabState.Aiming;
        }
        #endregion

        #region Private Methods
        private void StartFly()
        {
            HideLine();
            SetGrabHandPassThrough(true);

            _state = EGrabState.Flying;
            Vector3 destination = _origin + (_direction * _hookDistance) + (Vector3.up * _hookHeight);
            _grabHand.Fly(destination, _hookDistance / _hookSpeed, OnFlyArrived);
        }

        private void TryCapture()
        {
            Transform body = Situation.PlayerBody;
            if (!body) return;

            Vector3 offset = body.position - _grabHand.transform.position;
            offset.y = 0.0f;
            if (offset.sqrMagnitude > (_hookRadius * _hookRadius)) return;

            // 모터가 없으면 끌고 갈 수 없으므로 잡지 않고 지나간다
            if (!Situation.PlayerMotor) return;

            // 잡은 순간부터 손에 매달린다. 손은 멈추지 않고 끝까지 날아간다. 놓을 때 구속 시간을 _holdSeconds 로 바꾼다
            _isCaptured = true;
            _heldHeight = Mathf.Max(0.0f, body.position.y - _floorY);
            LockPlayer(float.PositiveInfinity);
            _isPlayerHeld = true;
            Situation.SetPlayerHeld(true);
            if (_captureShakeForce > 0.0f) Situation.ShakePlayerCamera(_captureShakeForce);

            DragPlayer();
        }

        /// <summary>
        /// 매달린 플레이어를 손 아래(잡을 때의 발 높이)로 옮긴다. 손은 이 뒤에 움직이므로 한 프레임 뒤따라간다.
        /// </summary>
        private void DragPlayer()
        {
            Vector3 hand = _grabHand.transform.position;
            MovePlayerTo(new Vector3(hand.x, _floorY, hand.z));
        }

        /// <param name="floorPoint">옮길 바닥 높이의 위치. 잡을 때의 발 높이만큼 위로 올려 둔다</param>
        private void MovePlayerTo(Vector3 floorPoint)
        {
            Transform body = Situation.PlayerBody;
            CharacterMotor motor = Situation.PlayerMotor;
            if (!body || !motor) return;

            Vector3 target = floorPoint + (Vector3.up * _heldHeight);
            motor.AddExternalDisplacement(target - body.position);
        }

        private void OnFlyArrived()
        {
            if (!_isCaptured)
            {
                Miss();
                return;
            }

            // 손이 떠난 쪽에서 가장 가까운 멀쩡한 칸까지 같은 속도로 끌고 돌아온다
            _slamTile = Arena.FindNearestSafe(_origin);
            if (_slamTile < 0)
            {
                // 놓을 칸이 없으면 그 자리에서 놓고 손만 돌아온다
                DropPlayer();
                Miss();
                return;
            }

            Vector3 destination = Arena.GetTileCenter(_slamTile) + (Vector3.up * _hookHeight);
            float distance = Vector3.Distance(_grabHand.transform.position, destination);
            _state = EGrabState.Returning;
            _grabHand.Fly(destination, distance / _hookSpeed, OnReturnArrived);
        }

        private void OnReturnArrived()
        {
            // 칸 중심에 놓으면 손은 돌아가고 반대 손이 그 칸을 향해 떠오른다
            Vector3 destination = Arena.GetTileCenter(_slamTile);
            MovePlayerTo(destination);
            DropPlayer();

            _isGrabHandBusy = true;
            _grabHand.Recall(OnGrabHandBack);

            _remaining = _slamTelegraphSeconds * _controller.TelegraphScale;
            Arena.SetTelegraph(_slamTile, true, _remaining);
            _isTelegraphOn = true;
            _slamHand.MoveToHover(destination);
            _state = EGrabState.Telegraph;
        }

        private void Miss()
        {
            _state = EGrabState.Missed;
            _isGrabHandBusy = true;
            _grabHand.Recall(OnGrabHandBack);
        }

        /// <summary>
        /// 매달린 플레이어를 놓는다. 중력이 돌아오고, 이동 구속은 _holdSeconds 뒤에 풀린다.
        /// </summary>
        private void DropPlayer()
        {
            _isCaptured = false;
            ReleaseHeldPlayer();
            if (_isPlayerLocked) _holdRemaining = _holdSeconds;
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
            SetGrabHandPassThrough(false);
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

        private void LockPlayer(float seconds)
        {
            _holdRemaining = seconds;
            _isPlayerLocked = true;
            Situation.SetPlayerMoveLocked(true);
        }

        private void ReleasePlayer()
        {
            if (!_isPlayerLocked) return;

            _isPlayerLocked = false;
            if (_controller && (_controller.Situation != null)) _controller.Situation.SetPlayerMoveLocked(false);
        }

        private void ReleaseHeldPlayer()
        {
            if (!_isPlayerHeld) return;

            _isPlayerHeld = false;
            if (_controller && (_controller.Situation != null)) _controller.Situation.SetPlayerHeld(false);
        }

        /// <returns>_origin 에서 _direction 으로 판 반대편 끝까지의 거리. 판을 비껴가거나 _hookRange 보다 멀면 _hookRange</returns>
        private float GetHookDistance()
        {
            Vector3 corner = Arena.GetTileCenter(0);
            Vector3 colStep = Arena.GetTileCenter(1) - corner;
            Vector3 rowStep = Arena.GetTileCenter(ArenaGrid.SIZE) - corner;
            float halfExtent = colStep.magnitude * ArenaGrid.SIZE * 0.5f;
            Vector3 offset = _origin - Arena.GetTileCenter(ArenaGrid.CENTER);

            float exit = Mathf.Min(
                GetSlabExit(offset, colStep.normalized, halfExtent),
                GetSlabExit(offset, rowStep.normalized, halfExtent));

            return (exit > 0.0f) ? Mathf.Min(exit, _hookRange) : _hookRange;
        }

        /// <returns>판 중심에서 한 축으로 ±halfExtent 인 두 면 사이를 _direction 으로 지나 빠져나가는 거리. 축과 수직이면 무한대</returns>
        private float GetSlabExit(Vector3 offset, Vector3 axis, float halfExtent)
        {
            float start = Vector3.Dot(offset, axis);
            float speed = Vector3.Dot(_direction, axis);
            if (Mathf.Abs(speed) < 0.0001f) return float.PositiveInfinity;

            float side = (speed > 0.0f) ? halfExtent : -halfExtent;
            return (side - start) / speed;
        }

        /// <summary>
        /// 그랩 손의 콜라이더와 플레이어 콜라이더 사이 충돌을 끄거나 되돌린다. 다른 대상(총알 등)과의 충돌은 그대로다.
        /// </summary>
        private void SetGrabHandPassThrough(bool isOn)
        {
            if (isOn)
            {
                Transform body = Situation.PlayerBody;
                _passThroughPlayerCollider = body ? body.GetComponent<Collider>() : null;
                _passThroughHandColliders = _grabHand.GetComponentsInChildren<Collider>();
            }

            if (!_passThroughPlayerCollider || (_passThroughHandColliders == null)) return;

            foreach (Collider handCollider in _passThroughHandColliders)
            {
                if (handCollider) Physics.IgnoreCollision(handCollider, _passThroughPlayerCollider, isOn);
            }

            if (isOn) return;

            _passThroughHandColliders = null;
            _passThroughPlayerCollider = null;
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
