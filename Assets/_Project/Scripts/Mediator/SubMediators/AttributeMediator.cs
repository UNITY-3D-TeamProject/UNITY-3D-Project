using System;
using UnityEngine;
using Attribute.Core;
using Attribute.ProjectSpecific;

namespace Mediator.SubMediators
{
    /// <summary>
    /// AttributeSet 의 값 조회/수정과 값 변경 알림을 중계하는 접착 컴포넌트.
    /// </summary>
    public class AttributeMediator : MediatorBase
    {
        #region Serialized Fields
        [Header("References")]
        [SerializeField] private AttributeSet _attributeSet;
        [SerializeField] private AttributeClamper _clamper;
        [SerializeField] private AttributeRegenerator _regenerator;
        [Header("Settings")]
        [Tooltip("사격 시 회복을 정지할 어트리뷰트 이름")]
        [SerializeField] private string _firePauseKey = "CurrentHeat";
        #endregion

        #region Properties
        /// <summary>효과 적용 대상/주체로 사용할 AttributeSet.</summary>
        public IEffectTarget EffectTarget => _attributeSet;
        #endregion

        #region Events
        /// <summary>AttributeSet 의 값이 변경될 때 (이름, 변경 후 값, 변경 전 값)을 알린다.</summary>
        public event Action<string, float, float> OnAttributeChanged;
        #endregion

        #region Unity Lifecycle
        protected override void Awake()
        {
            base.Awake();
            ResolveComponent(ref _attributeSet);
            ResolveComponent(ref _clamper);
            ResolveComponent(ref _regenerator);
        }

        protected override void OnEnable()
        {
            if (_attributeSet) _attributeSet.AddOnAttributeChangedCallback(RelayAttributeChanged);
            if (_attributeSet && _clamper)
            {
                _attributeSet.SetPreAttributeChangedCallback(ClampAttribute);
            }
            if (_attributeSet && _regenerator) _regenerator.SetTarget(_attributeSet);
            base.OnEnable();
        }

        private void OnDisable()
        {
            if (_attributeSet) _attributeSet.RemoveOnAttributeChangedCallback(RelayAttributeChanged);
            if (_attributeSet && _clamper)
            {
                _attributeSet.ClearPreAttributeChangedCallback();
            }
            if (_regenerator) _regenerator.ClearTarget();
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// 어트리뷰트 존재 여부를 반환한다.
        /// </summary>
        /// <param name="key">어트리뷰트 이름</param>
        /// <returns>AttributeSet 이 있고 해당 어트리뷰트가 정의되어 있으면 true</returns>
        public bool IsValidTarget(string key)
        {
            if (!_attributeSet) return false;

            return _attributeSet.IsValidTarget(key);
        }

        /// <summary>
        /// 어트리뷰트 값을 반환한다.
        /// </summary>
        /// <param name="key">어트리뷰트 이름</param>
        /// <returns>어트리뷰트 값. AttributeSet 이 없으면 0</returns>
        public float GetValue(string key)
        {
            if (!_attributeSet) return 0.0f;

            return _attributeSet.GetValue(key);
        }

        /// <summary>
        /// 어트리뷰트 값에서 amount 만큼 차감한다.
        /// </summary>
        /// <param name="key">어트리뷰트 이름</param>
        /// <param name="amount">차감량</param>
        public void SetValue(string key, float amount)
        {
            if (!_attributeSet) return;

            _attributeSet.SetValue(key, _attributeSet.GetValue(key) - amount);
        }

        /// <summary>
        /// 사격 시 _firePauseKey 의 회복을 정지한다.
        /// </summary>
        public void PauseRegenOnFire()
        {
            if (!_regenerator) return;

            _regenerator.Pause(_firePauseKey);
        }
        #endregion

        #region Protected Methods
        /// <inheritdoc />
        protected override void InitAttributeCallback()
        {
        }

        /// <inheritdoc />
        protected override void InitValue()
        {
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// AttributeSet 의 값 변경을 OnAttributeChanged 로 중계한다.
        /// </summary>
        private void RelayAttributeChanged(string attributeName, float newValue, float oldValue)
        {
            OnAttributeChanged?.Invoke(attributeName, newValue, oldValue);
        }

        /// <summary>
        /// 값이 반영되기 전에 AttributeClamper 규칙으로 보정한다.
        /// </summary>
        private void ClampAttribute(string attributeName, ref float newValue, float oldValue)
        {
            _clamper.Clamp(_attributeSet, attributeName, ref newValue);
        }
        #endregion
    }
}
