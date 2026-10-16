using System.Collections;
using UnityEngine;
using Skill.Core;

namespace Skill.Skills
{
    /// <summary>
    /// Fire 의 진행 단계. 발동 알림을 받은 외부가 이 값으로 알림 종류를 구분한다.
    /// </summary>
    public enum EFirePhase
    {
        /// <summary>차지를 시작했다.</summary>
        ChargeStarted,
        /// <summary>한 발을 쐈다. IsCharged 로 차지샷 여부를 구분한다.</summary>
        Fired,
        /// <summary>쏘지 않고 차지를 끝냈다 (비활성화, 발사 시점 코스트 지불 실패).</summary>
        Canceled,
    }

    /// <summary>
    /// 누르고 있는 동안 차지하고 한 발을 쏘는 차지샷 스킬.
    /// 발동(TryExecute)하면 조건·코스트 판정만 하고 차지를 시작한다.
    /// 차지 시간이 차면 차지샷을 자동 발사하고, 그 전에 Stop 이 오면 비차지샷을 쏜다.
    /// 자동 발사 후에도 Stop 이 오지 않으면(누르고 있으면) 잠시 기다렸다가 다시 차지한다.
    /// 코스트는 발사하는 순간 지불하며, 지불에 실패하면 쏘지 않고 차지만 끝낸다.
    /// 차지 시작·발사·취소마다 발동 알림을 보내며, 외부는 Phase, IsCharged, FirePosition 을 읽어 처리한다.
    /// </summary>
    public class Fire : SkillBase
    {
        #region Serialized Fields
        [Header("Fire")]
        [Tooltip("차지샷이 자동 발사되기까지 누르고 있어야 하는 시간(초)")]
        [SerializeField, Min(0.0f)] private float _chargeTime = 1.0f;
        [Tooltip("차지샷 발사 시 배수를 적용할 수 있는 코스트(IScalableCost)에 곱할 배수")]
        [SerializeField, Min(0.0f)] private float _chargedCostScale = 2.0f;
        [Tooltip("차지샷 자동 발사 후 계속 누르고 있을 때 다시 차지를 시작하기까지 기다리는 시간(초)")]
        [SerializeField, Min(0.0f)] private float _rechargeDelay = 0.1f;

        [Header("References")]
        [Tooltip("총알이 생성될 위치. 비어 있을 때 발동을 막으려면 NotNullCondition 에 연결한다.")]
        [SerializeField] private Transform _firePoint;
        #endregion

        #region Private Fields
        private WaitForSeconds _waitChargeTime;
        private WaitForSeconds _waitRechargeDelay;
        private Coroutine _chargeCoroutine;
        private Coroutine _rechargeCoroutine;
        // 발동(누름) ~ Stop(뗌). 누르고 있는 동안 자동 발사 후 다시 차지한다
        private bool _isHeld;
        // 현재 차지를 시작한 시각(Time.time). 차지 진행도 계산에 쓴다
        private float _chargeStartTime;
        #endregion

        #region Properties
        /// <summary>총알이 생성될 월드 위치.</summary>
        public Vector3 FirePosition => _firePoint.position;

        /// <summary>마지막 발동 알림의 진행 단계.</summary>
        public EFirePhase Phase { get; private set; }

        /// <summary>마지막 발사가 차지샷인지 여부. Phase 가 Fired 일 때 읽는다.</summary>
        public bool IsCharged { get; private set; }

        /// <summary>
        /// 차지 진행도(0~1). 차지 중이 아니면(발사·취소 후, 재차지 대기 중) 0.
        /// </summary>
        public float ChargeRatio
        {
            get
            {
                if (!IsCharging) return 0.0f;
                if (_chargeTime <= 0.0f) return 1.0f;

                return Mathf.Clamp01((Time.time - _chargeStartTime) / _chargeTime);
            }
        }

        protected override bool IsPayOnExecute => false;

        private bool IsCharging => _chargeCoroutine != null;
        #endregion

        #region Unity Lifecycle
        protected override void Awake()
        {
            base.Awake();
            _waitChargeTime = new WaitForSeconds(_chargeTime);
            _waitRechargeDelay = new WaitForSeconds(_rechargeDelay);

            if (_firePoint == null)
            {
                Debug.LogError($"[{name}] 발사 지점이 연결되지 않았습니다.", this);
            }
        }

        private void OnEnable()
        {
            // 비활성화된 동안 계속 누르고 있었으면 바로 다시 차지한다. 실패하면 다시 눌러야 한다
            if (!_isHeld) return;

            if (!TryExecute()) _isHeld = false;
        }

        private void OnDisable()
        {
            // 누름 상태는 유지해 다시 켜질 때 차지를 이어간다
            EndRecharge();
            Cancel();
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// 누름이 끝났음을 알린다. 차지 시간 전이면 비차지샷을 쏜다.
        /// 이미 자동 발사했거나 재차지 대기 중이면 재차지만 멈추고 쏘지 않는다.
        /// </summary>
        public override void Stop()
        {
            _isHeld = false;
            EndRecharge();
            if (!IsCharging) return;

            Shoot(false);
        }
        #endregion

        #region Protected Methods
        protected override void Execute()
        {
            if (IsCharging) return;

            _isHeld = true;
            EndRecharge();
            IsCharged = false;
            Phase = EFirePhase.ChargeStarted;
            _chargeStartTime = Time.time;
            _chargeCoroutine = StartCoroutine(CoCharge());

            base.Execute();
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// 차지를 끝내고 코스트를 지불한 뒤 한 발을 쏜다. 지불에 실패하면 쏘지 않고 취소로 알린다.
        /// 쏜 뒤에도 누르고 있으면 대기 후 다시 차지하고, 지불에 실패하면 재차지하지 않는다.
        /// </summary>
        /// <param name="isCharged">차지샷이면 true</param>
        private void Shoot(bool isCharged)
        {
            EndCharge();
            IsCharged = isCharged;
            float costScale = isCharged ? _chargedCostScale : 1.0f;
            Phase = PayCosts(costScale) ? EFirePhase.Fired : EFirePhase.Canceled;

            base.Execute();

            if (Phase == EFirePhase.Canceled)
            {
                _isHeld = false;
                return;
            }

            if (_isHeld && isActiveAndEnabled) _rechargeCoroutine = StartCoroutine(CoRecharge());
        }

        /// <summary>
        /// 진행 중인 차지를 쏘지 않고 끝내고 취소로 알린다. 차지 중이 아니면 아무것도 하지 않는다.
        /// </summary>
        private void Cancel()
        {
            if (!IsCharging) return;

            EndCharge();
            Phase = EFirePhase.Canceled;

            base.Execute();
        }

        /// <summary>
        /// 차지 코루틴을 멈춘다.
        /// </summary>
        private void EndCharge()
        {
            if (_chargeCoroutine == null) return;

            StopCoroutine(_chargeCoroutine);
            _chargeCoroutine = null;
        }

        /// <summary>
        /// 재차지 대기 코루틴을 멈춘다.
        /// </summary>
        private void EndRecharge()
        {
            if (_rechargeCoroutine == null) return;

            StopCoroutine(_rechargeCoroutine);
            _rechargeCoroutine = null;
        }
        #endregion

        #region Coroutines
        private IEnumerator CoCharge()
        {
            yield return _waitChargeTime;

            // 실행 중인 자기 자신을 StopCoroutine 하지 않도록 먼저 비운다
            _chargeCoroutine = null;
            Shoot(true);
        }

        /// <summary>
        /// 대기 시간 뒤 조건·코스트 판정을 거쳐 다시 차지한다. 실패하면 재차지를 멈추고 다시 눌러야 한다.
        /// </summary>
        private IEnumerator CoRecharge()
        {
            yield return _waitRechargeDelay;

            // Execute 의 EndRecharge 가 실행 중인 자기 자신을 StopCoroutine 하지 않도록 먼저 비운다
            _rechargeCoroutine = null;
            if (!TryExecute()) _isHeld = false;
        }
        #endregion
    }
}
