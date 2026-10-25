using UnityEngine;
using AI.Boss.Arena;
using Attribute.Core;
using Attribute.Effect;
using Movement;
using Skill.Core;

namespace AI.Boss.BossB
{
    /// <summary>
    /// B 스킬의 공통 기반. 발동하면 몸을 붙잡고(AIController.Hold) 매 프레임 Tick 으로 진행하다가 끝나면 붙잡힘을 푼다.
    /// BT 는 AIUseSkillAndWaitAction 으로 IsHolding 이 풀릴 때까지 기다린다. 쿨타임은 스킬이 아니라 BT 블랙보드가 관리한다.
    /// 발동 중에는 몸을 직접 옮기므로 AIController 의 Agent 위치 동기화를 멈춰 둔다(SuspendAgentSync).
    /// 플레이어를 밀어내는 호출 측이므로 이번 프레임 이동에 합쳐지도록 실행 순서를 앞당긴다.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public abstract class BossBSkillBase : SkillBase
    {
        #region Constants
        /// <summary>정상 종료는 EndRun 이 맡으므로, 붙잡힘 안전 시간에 더하는 여유(초).</summary>
        private const float HOLD_MARGIN = 0.5f;
        #endregion

        #region Serialized Fields
        [Header("Hit")]
        [Tooltip("피격 대상에 적용할 효과. 비면 피해 없이 넉백만 한다")]
        [SerializeField] private SOAttributeEffect _hitEffect;
        #endregion

        #region Private Fields
        private readonly BossKnockback _knockback = new BossKnockback();
        private BossBController _boss;
        private AIController _controller;
        private CharacterMotor _selfMotor;
        private IEffectTarget _cursor;
        private Transform _target;
        private CharacterMotor _targetMotor;
        private IEffectTarget _targetEffect;
        private bool _isTargetRequired;
        #endregion

        #region Properties
        /// <summary>발동해서 아직 끝나지 않았는지.</summary>
        public bool IsRunning { get; private set; }

        /// <summary>회피 도약인지. 안전망이 진행 중 스킬을 끊을 때 회피 도약은 제외한다.</summary>
        public virtual bool IsEvade => false;

        /// <summary>
        /// 이 스킬이 몸을 띄워 목표 지점으로 옮기는 중인지. 안전망이 끼어들면 엉뚱한 자리에 떨어지는 구간이다.
        /// </summary>
        public virtual bool IsBodyInFlight => false;

        protected AIController Controller => _controller;
        protected BossBController Boss => _boss;
        protected IArenaQuery Arena => _boss ? _boss.Arena : null;
        protected CharacterMotor SelfMotor => _selfMotor;
        protected Transform Target => _target;
        #endregion

        #region Unity Lifecycle
        protected override void Awake()
        {
            base.Awake();
            _boss = GetComponentInParent<BossBController>();
            _controller = GetComponentInParent<AIController>();
            _cursor = GetComponentInParent<IEffectTarget>();
            if (_controller) _selfMotor = _controller.GetComponentInChildren<CharacterMotor>();

            Debug.Assert(_boss != null, $"[{name}] BossBController 를 찾을 수 없습니다.");
            Debug.Assert(_controller != null, $"[{name}] AIController 를 찾을 수 없습니다.");
            Debug.Assert(_selfMotor != null, $"[{name}] CharacterMotor 를 찾을 수 없습니다.");
        }

        private void Update()
        {
            _knockback.Tick(Time.deltaTime);

            if (!IsRunning) return;

            // 대상이 파괴되었으면(플레이어 사망·재생성) 그 프레임에 끝낸다
            if (_isTargetRequired && (!_target || !_targetMotor))
            {
                EndRun();
                return;
            }

            Tick(Time.deltaTime);
        }

        private void OnDisable()
        {
            Stop();
            _knockback.Cancel();
        }
        #endregion

        #region Public Methods
        public override void Stop()
        {
            EndRun();
        }
        #endregion

        #region Protected Methods
        /// <summary>
        /// 스킬이 시작될 때 Execute 에서 부른다. 아레나·모터가 준비되지 않았거나 대상이 필요한데 없으면 시작하지 않는다.
        /// B 의 다른 스킬이 진행 중이어도 시작하지 않는다. 겹쳐 시작하면 먼저 끝난 스킬이 남의 붙잡힘까지 풀어 BT 가 다음 스킬을 또 시작한다.
        /// </summary>
        /// <param name="isTargetRequired">보이는 대상(플레이어)이 있어야 시작하는지</param>
        /// <param name="holdSeconds">붙잡아 둘 최대 시간(초). 정상 종료는 EndRun 이 풀어 준다</param>
        /// <param name="isArenaRequired">아레나가 연결돼 있어야 시작하는지. 보스전 시작 전에 쓰는 스킬(등장 연출)만 false 로 부른다</param>
        /// <returns>시작했으면 true</returns>
        protected bool BeginRun(bool isTargetRequired, float holdSeconds, bool isArenaRequired = true)
        {
            if (IsRunning) return false;
            if (!_controller || !_selfMotor || (isArenaRequired && (Arena == null))) return false;
            if (_boss.IsSkillRunning) return false;

            _isTargetRequired = isTargetRequired;
            _target = _controller.VisibleTarget;
            _targetMotor = _target ? _target.GetComponentInChildren<CharacterMotor>() : null;
            _targetEffect = _target ? FindEffectTarget(_target) : null;
            if (isTargetRequired && (!_target || !_targetMotor)) return false;

            IsRunning = true;
            // 발동 중에는 이 스킬이 몸을 직접 옮긴다. 공중·구멍 위 좌표를 Agent 에 밀어 넣으면 NavMesh 밖으로 떨어진다
            _controller.SuspendAgentSync();
            _controller.Hold(GetDirectionToTarget(), holdSeconds + HOLD_MARGIN);
            return true;
        }

        /// <summary>
        /// 스킬을 끝내고 붙잡힘을 푼다. 진행 중이 아니면 아무것도 하지 않는다.
        /// </summary>
        protected void EndRun()
        {
            if (!IsRunning) return;

            IsRunning = false;
            OnRunEnded();

            _target = null;
            _targetMotor = null;
            _targetEffect = null;
            if (_controller)
            {
                // 몸을 옮겨 둔 자리의 NavMesh 로 Agent 를 되붙인 뒤 경로 추종을 넘긴다
                _controller.ResumeAgentSync();
                _controller.ReleaseHold();
            }
        }

        /// <summary>
        /// 몸을 붙잡는 시간과 바라보는 방향을 다시 정한다. 방향은 대상 쪽(대상이 없으면 정면)이다.
        /// </summary>
        protected void RefreshHold(float holdSeconds)
        {
            _controller.Hold(GetDirectionToTarget(), holdSeconds + HOLD_MARGIN);
        }

        /// <summary>
        /// 발동 중 매 프레임 불린다.
        /// </summary>
        protected abstract void Tick(float deltaTime);

        /// <summary>
        /// 스킬이 끝날 때(정상·Stop·비활성화) 한 번 불린다. 붙잡힘을 풀기 전이다.
        /// </summary>
        protected virtual void OnRunEnded()
        {
        }

        /// <summary>
        /// B 에서 대상 쪽 수평 방향(정규화). 대상이 없거나 겹쳐 있으면 정면.
        /// </summary>
        protected Vector3 GetDirectionToTarget()
        {
            if (!_target) return _controller.transform.forward;

            Vector3 flat = Vector3.ProjectOnPlane(_target.position - _controller.Position, Vector3.up);
            return (flat.sqrMagnitude > Mathf.Epsilon) ? flat.normalized : _controller.transform.forward;
        }

        /// <summary>
        /// 지점에서 대상까지 수평 거리(m). 높이는 보지 않는다. 대상이 없으면 무한대.
        /// </summary>
        protected float GetFlatDistanceToTarget(Vector3 point)
        {
            if (!_target) return float.PositiveInfinity;

            return Vector3.ProjectOnPlane(_target.position - point, Vector3.up).magnitude;
        }

        /// <summary>
        /// 대상이 중심에서 반경 안(수평)이면 피해 Effect 를 적용한다.
        /// </summary>
        /// <returns>반경 안이어서 맞았으면 true</returns>
        protected bool TryHitTarget(Vector3 center, float radius)
        {
            if (GetFlatDistanceToTarget(center) > radius) return false;

            if (_hitEffect && (_targetEffect != null) && _targetEffect.IsValidTarget(_hitEffect.TargetAttribute))
            {
                _hitEffect.Apply(_targetEffect, new SEffectContext(_cursor));
            }
            return true;
        }

        /// <summary>
        /// 대상을 수평 방향으로 밀어낸다. 진행 중인 넉백은 새 넉백으로 바뀐다.
        /// </summary>
        protected void KnockbackTarget(Vector3 direction, float distance, float seconds)
        {
            if (!_targetMotor) return;

            _knockback.Start(_targetMotor, direction * distance, seconds);
        }
        #endregion

        #region Private Methods
        // 충돌체가 루트에 있고 AttributeSet 이 자식에 있는 캐릭터도 있어서 부모 → 자식 순으로 찾는다 (MeleeHitController 와 같다)
        private static IEffectTarget FindEffectTarget(Transform target)
        {
            IEffectTarget effectTarget = target.GetComponentInParent<IEffectTarget>();
            return effectTarget ?? target.GetComponentInChildren<IEffectTarget>();
        }
        #endregion
    }
}
