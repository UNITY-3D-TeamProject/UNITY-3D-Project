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
    /// 코스트는 발사하는 순간 지불하며, 지불에 실패하면 쏘지 않고 차지만 끝낸다.
    /// 차지 시작·발사·취소마다 발동 알림을 보내며, 외부는 Phase, IsCharged, FirePosition 을 읽어 처리한다.
    /// </summary>
    public class Fire : SkillBase
    {
        #region Serialized Fields
        [Header("Fire")]
        [Tooltip("차지샷이 자동 발사되기까지 누르고 있어야 하는 시간(초)")]
        [SerializeField, Min(0.0f)] private float _chargeTime = 1.0f;

        [Header("References")]
        [Tooltip("총알이 생성될 위치. 비어 있을 때 발동을 막으려면 NotNullCondition 에 연결한다.")]
        [SerializeField] private Transform _firePoint;
        #endregion

        #region Private Fields
        private WaitForSeconds _waitChargeTime;
        private Coroutine _chargeCoroutine;
        #endregion

        #region Properties
        /// <summary>총알이 생성될 월드 위치.</summary>
        public Vector3 FirePosition => _firePoint.position;

        /// <summary>마지막 발동 알림의 진행 단계.</summary>
        public EFirePhase Phase { get; private set; }

        /// <summary>마지막 발사가 차지샷인지 여부. Phase 가 Fired 일 때 읽는다.</summary>
        public bool IsCharged { get; private set; }

        protected override bool IsPayOnExecute => false;

        private bool IsCharging => _chargeCoroutine != null;
        #endregion

        #region Unity Lifecycle
        protected override void Awake()
        {
            base.Awake();
            _waitChargeTime = new WaitForSeconds(_chargeTime);

            if (_firePoint == null)
            {
                Debug.LogError($"[{name}] 발사 지점이 연결되지 않았습니다.", this);
            }
        }

        private void OnDisable()
        {
            Cancel();
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// 차지 시간 전이면 비차지샷을 쏜다. 이미 자동 발사했거나 차지 중이 아니면 아무것도 하지 않는다.
        /// </summary>
        public override void Stop()
        {
            if (!IsCharging) return;

            Shoot(false);
        }
        #endregion

        #region Protected Methods
        protected override void Execute()
        {
            if (IsCharging) return;

            IsCharged = false;
            Phase = EFirePhase.ChargeStarted;
            _chargeCoroutine = StartCoroutine(CoCharge());

            base.Execute();
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// 차지를 끝내고 코스트를 지불한 뒤 한 발을 쏜다. 지불에 실패하면 쏘지 않고 취소로 알린다.
        /// </summary>
        /// <param name="isCharged">차지샷이면 true</param>
        private void Shoot(bool isCharged)
        {
            EndCharge();
            IsCharged = isCharged;
            Phase = PayCosts() ? EFirePhase.Fired : EFirePhase.Canceled;

            base.Execute();
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
        #endregion

        #region Coroutines
        private IEnumerator CoCharge()
        {
            yield return _waitChargeTime;

            // 실행 중인 자기 자신을 StopCoroutine 하지 않도록 먼저 비운다
            _chargeCoroutine = null;
            Shoot(true);
        }
        #endregion
    }
}
