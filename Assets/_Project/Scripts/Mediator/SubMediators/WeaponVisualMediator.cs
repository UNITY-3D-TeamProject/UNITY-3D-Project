using UnityEngine;
using Visual;

namespace Mediator.SubMediators
{
    /// <summary>
    /// 열 어트리뷰트 변경을 열 비율로 바꿔 EmissionHueChanger 로 전달하는 접착 컴포넌트.
    /// </summary>
    public class WeaponVisualMediator : MediatorBase
    {
        #region Serialized Fields
        [Header("References")]
        [SerializeField] private EmissionHueChanger _hueChanger;
        [Header("Settings")]
        [Tooltip("열로 사용할 어트리뷰트 이름")]
        [SerializeField] private string _heatKey = "CurrentHeat";
        [Tooltip("열 최댓값으로 사용할 어트리뷰트 이름")]
        [SerializeField] private string _maxHeatKey = "MaxHeat";
        #endregion

        #region Unity Lifecycle
        protected override void Awake()
        {
            base.Awake();
            ResolveComponent(ref _hueChanger);
        }
        #endregion

        #region Protected Methods
        /// <inheritdoc />
        protected override void InitAttributeCallback()
        {
            AttributeCallback.TryAdd(_heatKey, _ => InitValue());
            AttributeCallback.TryAdd(_maxHeatKey, _ => InitValue());
        }

        /// <inheritdoc />
        protected override void InitValue()
        {
            if (_hueChanger == null || AttributeGetter == null) return;

            float maxHeat = AttributeGetter.Invoke(_maxHeatKey);
            float ratio = maxHeat > 0.0f ? AttributeGetter.Invoke(_heatKey) / maxHeat : 0.0f;

            _hueChanger.SetRatio(ratio);
        }
        #endregion
    }
}
