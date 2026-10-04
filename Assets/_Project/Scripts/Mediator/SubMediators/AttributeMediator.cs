using System;
using UnityEngine;
using Attribute.Core;

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
        }

        protected override void OnEnable()
        {
            if (_attributeSet) _attributeSet.AddOnAttributeChangedCallback(RelayAttributeChanged);
            base.OnEnable();
        }

        private void OnDisable()
        {
            if (_attributeSet) _attributeSet.RemoveOnAttributeChangedCallback(RelayAttributeChanged);
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
        #endregion
    }
}
