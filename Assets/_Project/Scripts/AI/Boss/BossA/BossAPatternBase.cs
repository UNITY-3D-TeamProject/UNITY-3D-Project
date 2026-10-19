using UnityEngine;
using Attribute.Effect;
using Skill.Core;
using AI.Boss.Arena;
using AI.Boss.Logic;

namespace AI.Boss.BossA
{
    /// <summary>
    /// A 손 패턴 스킬의 공통 흐름.
    /// 예고(시간 × TelegraphScale) → 손 이동 → 내려침 → 피해·붕괴 → 머묾 → 회수 → EndPattern.
    /// StrikeCount 가 2 이상이면 내려침을 사이 간격을 두고 반복하고, UsesCannon 이면 내려친 직후 어깨 포격이 붙어 포격이 끝나야 패턴이 끝난다.
    /// 코루틴 없이 Update 상태 머신으로 돌아서, Stop 한 번으로 모든 단계를 정리할 수 있다.
    /// </summary>
    public abstract class BossAPatternBase : SkillBase
    {
        #region Types
        private enum EPatternState
        {
            Idle,
            Telegraph,
            Moving,
            Slamming,
            Staying,
            Recalling,
            WaitingCannon,
        }
        #endregion

        #region Serialized Fields
        [Header("References")]
        [SerializeField] private BossController _controller;

        [Header("Settings")]
        [Tooltip("내려치는 순간 표적 칸 위의 플레이어에게 줄 피해. 비워 두면 피해 없음")]
        [SerializeField] private SOAttributeEffect _hitEffect;
        #endregion

        #region Private Fields
        private readonly BossHand[] _hands = new BossHand[2];
        private readonly Vector3[] _points = new Vector3[2];
        private int _handCount;
        private int _pendingCount;
        private EPatternState _state = EPatternState.Idle;
        private float _remaining;
        private bool _isHoverStarted;
        private bool _isTelegraphOn;
        private bool _isCannonPending;
        private int _strikeIndex;
        private int[] _tiles;
        #endregion

        #region Properties
        protected BossController Controller => _controller;
        protected IBossArena Arena => _controller.Arena;
        protected IBossSituation Situation => _controller.Situation;
        protected int HandCount => _handCount;

        /// <summary>지금 몇 번째 내려침인지(0부터). OnImpact·RetargetHands 가 읽는다.</summary>
        protected int StrikeIndex => _strikeIndex;

        /// <summary>예고 시간(초). TelegraphScale 곱하기 전 값.</summary>
        protected abstract float TelegraphSeconds { get; }

        /// <summary>내려친 자리에 머무는 시간(초). 기본은 현재 단계의 손 머묾 시간.</summary>
        protected virtual float StaySeconds => _controller.HandStayDuration;

        /// <summary>true 면 예고가 시작될 때 손이 미리 떠오른다. 기본은 예고가 끝난 뒤 움직인다.</summary>
        protected virtual bool IsHoverDuringTelegraph => false;

        /// <summary>손이 내려치는 횟수. 2 이상이면 내려칠 때마다 RetargetHands 로 다음 칸을 정한다.</summary>
        protected virtual int StrikeCount => 1;

        /// <summary>한 번 내려친 충돌에서 다음 충돌까지의 간격(초). 손 이동 시간을 포함하며 TelegraphScale 을 곱하지 않는다. 손 이동이 더 오래 걸리면 도착 즉시 친다.</summary>
        protected virtual float NextStrikeDelay => 0.0f;

        /// <summary>true 면 마지막으로 내려친 직후 플레이어에게 가까운 손 자리에 어깨 포격을 쏜다. 손이 둘일 때만 쏜다.</summary>
        protected virtual bool UsesCannon => false;
        #endregion

        #region Unity Lifecycle
        protected override void Awake()
        {
            base.Awake();

            if (!_controller) _controller = GetComponentInParent<BossController>();
        }

        protected virtual void OnDisable()
        {
            Stop();
        }

        private void Update()
        {
            float deltaTime = Time.deltaTime;
            OnTick(deltaTime);

            switch (_state)
            {
                case EPatternState.Telegraph:
                    _remaining -= deltaTime;
                    if (_remaining > 0.0f) break;

                    if (!_isHoverStarted) StartHover();
                    _state = EPatternState.Moving;
                    break;

                case EPatternState.Moving:
                    // 다음 내려침은 간격이 끝나는 순간 손이 닿도록 내려치는 시간만큼 앞서 시작한다
                    _remaining -= deltaTime;
                    if (AreAllHovering() && (_remaining <= GetSlamLead())) StartSlam();
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
            if (_state == EPatternState.Idle) return;

            _state = EPatternState.Idle;
            _isCannonPending = false;
            if (_controller) _controller.CancelCannons();
            ClearTelegraph();

            for (int i = 0; i < _handCount; i++)
            {
                if (_hands[i]) _hands[i].CancelAndRecall();
            }
            _handCount = 0;

            if (_controller) _controller.EndPattern(this, false);
        }
        #endregion

        #region Protected Methods
        protected override void Execute()
        {
            if (_state != EPatternState.Idle) return;

            bool isReady = _controller && (_controller.Arena != null) && (_controller.Situation != null);
            if (!isReady)
            {
                Debug.LogError($"[{name}] BossController 가 없거나 Initialize 되지 않았습니다.");
                return;
            }

            base.Execute();
            _controller.BeginPattern(this);

            _handCount = 0;
            BuildTargets();

            _tiles = GetTargetTiles();
            ShowTelegraph();

            _isHoverStarted = false;
            _strikeIndex = 0;
            _isCannonPending = false;
            _remaining = TelegraphSeconds * _controller.TelegraphScale;
            _state = EPatternState.Telegraph;

            if (IsHoverDuringTelegraph) StartHover();
        }

        /// <summary>
        /// 이번 패턴에서 쓸 손과 내려칠 칸을 AddHand 로 등록한다. 기본은 선택 결과의 왼손·오른손 칸.
        /// </summary>
        protected virtual void BuildTargets()
        {
            SBossAPatternChoice choice = _controller.CurrentChoice;
            AddHand(true, choice.HandTileLeft);
            AddHand(false, choice.HandTileRight);
        }

        /// <summary>
        /// 두 번째 이후 내려침의 손과 칸을 AddHand 로 다시 등록한다. StrikeCount 가 2 이상일 때만 불린다.
        /// </summary>
        protected virtual void RetargetHands()
        {
        }

        /// <summary>
        /// 예고를 띄우고 피해를 줄 칸. 기본은 선택 결과의 칸 전부.
        /// </summary>
        protected virtual int[] GetTargetTiles()
        {
            return _controller.CurrentChoice.Tiles;
        }

        /// <summary>
        /// 모든 손이 내려친 순간의 효과. 기본은 표적 칸 위 플레이어 피해 + 표적 칸 붕괴.
        /// </summary>
        protected virtual void OnImpact()
        {
            if (_hitEffect && IsPlayerOnTargetTile()) Situation.TryApplyToPlayer(_hitEffect);

            if (_tiles == null) return;

            foreach (int tile in _tiles)
            {
                Arena.Collapse(tile);
            }
        }

        /// <summary>
        /// 매 프레임 Update 맨 앞에서 불린다. 패턴이 끝난 뒤에도 이어지는 동작(넉백 등)이 쓴다.
        /// </summary>
        protected virtual void OnTick(float deltaTime)
        {
        }

        /// <summary>
        /// 표적 칸 위 플레이어에게 피해를 준다. 피해 Effect 가 비어 있으면 아무것도 하지 않는다.
        /// </summary>
        protected void ApplyHitToPlayer()
        {
            if (_hitEffect) Situation.TryApplyToPlayer(_hitEffect);
        }

        protected void AddHand(bool isLeft, int tile)
        {
            if (tile < 0) return;

            BossHand hand = _controller.GetHand(isLeft);
            if (!hand) return;

            _hands[_handCount] = hand;
            _points[_handCount] = Arena.GetTileCenter(tile);
            _handCount++;
        }

        protected Vector3 GetPoint(int handIndex)
        {
            return _points[handIndex];
        }

        protected bool IsPlayerOnTargetTile()
        {
            if (_tiles == null) return false;

            Transform body = Situation.PlayerBody;
            if (!body) return false;

            // 높이는 보지 않는다. 점프 중이어도 칸 위면 맞는다
            if (!Arena.TryGetTile(body.position, out int index)) return false;

            foreach (int tile in _tiles)
            {
                if (tile == index) return true;
            }
            return false;
        }

        /// <summary>
        /// 플레이어가 해당 칸 위에 있는지. 높이는 보지 않는다.
        /// </summary>
        protected bool IsPlayerOnTile(int tile)
        {
            Transform body = Situation.PlayerBody;
            if (!body) return false;

            return Arena.TryGetTile(body.position, out int index) && (index == tile);
        }
        #endregion

        #region Private Methods
        private void ShowTelegraph()
        {
            if (_tiles == null) return;

            foreach (int tile in _tiles)
            {
                Arena.SetTelegraph(tile, true);
            }
            _isTelegraphOn = true;
        }

        private void ClearTelegraph()
        {
            if (!_isTelegraphOn) return;

            _isTelegraphOn = false;
            foreach (int tile in _tiles)
            {
                Arena.SetTelegraph(tile, false);
            }
        }

        private void StartHover()
        {
            _isHoverStarted = true;
            for (int i = 0; i < _handCount; i++)
            {
                _hands[i].MoveToHover(_points[i]);
            }
        }

        private float GetSlamLead()
        {
            return (_handCount > 0) ? _hands[0].SlamSeconds : 0.0f;
        }

        private bool AreAllHovering()
        {
            for (int i = 0; i < _handCount; i++)
            {
                if (!_hands[i].IsHovering) return false;
            }
            return true;
        }

        private void StartSlam()
        {
            _state = EPatternState.Slamming;
            _pendingCount = _handCount;
            if (_pendingCount == 0)
            {
                HandleImpact();
                return;
            }

            for (int i = 0; i < _handCount; i++)
            {
                _hands[i].Slam(OnHandImpact);
            }
        }

        private void OnHandImpact()
        {
            _pendingCount--;
            if (_pendingCount > 0) return;

            HandleImpact();
        }

        private void HandleImpact()
        {
            OnImpact();

            if ((_strikeIndex + 1) < StrikeCount)
            {
                _strikeIndex++;
                _remaining = NextStrikeDelay;
                _handCount = 0;
                RetargetHands();
                StartHover();
                _state = EPatternState.Moving;
                return;
            }

            ClearTelegraph();
            if (UsesCannon) TryFireCannon();
            StartStay();
        }

        private void TryFireCannon()
        {
            if (_handCount < 2) return;

            Transform body = Situation.PlayerBody;
            if (!body) return;

            // 플레이어에게 가까운 손이 친 자리를 쏜다. 왼쪽 포는 왼손, 오른쪽 포는 오른손 자리
            int nearest = 0;
            float nearestDistance = float.MaxValue;
            for (int i = 0; i < _handCount; i++)
            {
                Vector3 offset = body.position - _points[i];
                offset.y = 0.0f;
                if (offset.sqrMagnitude >= nearestDistance) continue;

                nearestDistance = offset.sqrMagnitude;
                nearest = i;
            }

            _isCannonPending = true;
            _controller.FireCannon(_hands[nearest] == _controller.GetHand(true), _points[nearest], OnCannonDone);
        }

        private void OnCannonDone()
        {
            _isCannonPending = false;
            if (_state == EPatternState.WaitingCannon) Finish();
        }

        private void StartStay()
        {
            _state = EPatternState.Staying;
            _pendingCount = _handCount;
            if (_pendingCount == 0)
            {
                StartRecall();
                return;
            }

            float seconds = StaySeconds;
            for (int i = 0; i < _handCount; i++)
            {
                _hands[i].Stay(seconds, OnHandStayDone);
            }
        }

        private void OnHandStayDone()
        {
            _pendingCount--;
            if (_pendingCount > 0) return;

            StartRecall();
        }

        private void StartRecall()
        {
            _state = EPatternState.Recalling;
            _pendingCount = _handCount;
            if (_pendingCount == 0)
            {
                Finish();
                return;
            }

            for (int i = 0; i < _handCount; i++)
            {
                _hands[i].Recall(OnHandRecallDone);
            }
        }

        private void OnHandRecallDone()
        {
            _pendingCount--;
            if (_pendingCount > 0) return;

            Finish();
        }

        private void Finish()
        {
            if (_isCannonPending)
            {
                _state = EPatternState.WaitingCannon;
                return;
            }

            _state = EPatternState.Idle;
            _handCount = 0;
            _controller.EndPattern(this, true);
        }
        #endregion
    }
}
