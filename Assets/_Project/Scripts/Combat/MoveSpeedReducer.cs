using System;
using UnityEngine;

namespace Combat
{
    /// <summary>
    /// 차지·조준 상태로 이동속도 감속량을 결정하는 컴포넌트.
    /// 활성 상태의 감속량 중 가장 큰 값 하나만 적용하며(합산하지 않음), 값이 바뀔 때만 이벤트로 알린다.
    /// UnityEngine 외 어떤 시스템도 참조하지 않는다.
    /// </summary>
    public class MoveSpeedReducer : MonoBehaviour
    {
        #region Serialized Fields
        [Header("Reduction")]
        [Tooltip("차지 중 이동속도에서 뺄 값")]
        [SerializeField, Min(0.0f)] private float _chargeReduction = 1.5f;
        [Tooltip("조준 중 이동속도에서 뺄 값")]
        [SerializeField, Min(0.0f)] private float _aimReduction = 3.0f;
        #endregion

        #region Private Fields
        private bool _isCharging;
        private bool _isAiming;
        #endregion

        #region Properties
        /// <summary>현재 적용 중인 감속량.</summary>
        public float Reduction { get; private set; }
        #endregion

        #region Events
        /// <summary>감속량이 바뀌었을 때 (변경 후, 변경 전) 감속량을 알린다.</summary>
        public event Action<float, float> OnReductionChanged;
        #endregion

        #region Public Methods
        /// <summary>
        /// 차지 상태를 설정한다.
        /// </summary>
        /// <param name="isCharging">차지 중이면 true</param>
        public void SetCharging(bool isCharging)
        {
            _isCharging = isCharging;
            UpdateReduction();
        }

        /// <summary>
        /// 조준 상태를 설정한다.
        /// </summary>
        /// <param name="isAiming">조준 중이면 true</param>
        public void SetAiming(bool isAiming)
        {
            _isAiming = isAiming;
            UpdateReduction();
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// 활성 상태 중 가장 큰 감속량을 골라 적용하고, 바뀌었으면 알린다.
        /// </summary>
        private void UpdateReduction()
        {
            float chargeReduction = _isCharging ? _chargeReduction : 0.0f;
            float aimReduction = _isAiming ? _aimReduction : 0.0f;
            float newReduction = Mathf.Max(chargeReduction, aimReduction);
            if (Mathf.Approximately(newReduction, Reduction)) return;

            float oldReduction = Reduction;
            Reduction = newReduction;
            OnReductionChanged?.Invoke(newReduction, oldReduction);
        }
        #endregion
    }
}
