using UnityEngine;

namespace Visual
{
    /// <summary>
    /// 0~1 비율에 맞는 Gradient 색의 Hue 를 렌더러 발광 HueShift 속성에 적용하는 컴포넌트.
    /// 발광 색을 HueShift 로 돌리는 셰이더용이며, Gradient 의 밝기·채도·알파는 쓰지 않는다.
    /// 머티리얼 에셋은 건드리지 않고 MaterialPropertyBlock 으로만 바꾼다.
    /// </summary>
    public class EmissionHueChanger : MonoBehaviour
    {
        #region Constants
        private const float FULL_DEGREE = 360.0f;
        #endregion

        #region Serialized Fields
        [Header("References")]
        [Tooltip("발광 색을 바꿀 렌더러")]
        [SerializeField] private Renderer[] _renderers;
        [Header("Settings")]
        [Tooltip("비율(0~1)에 따른 발광 색. Hue 만 사용한다.")]
        [SerializeField] private Gradient _gradient = new()
        {
            colorKeys = new[]
            {
                new GradientColorKey(Color.HSVToRGB(245.5f / FULL_DEGREE, 1.0f, 1.0f), 0.0f),
                new GradientColorKey(Color.red, 1.0f),
            },
        };
        [Tooltip("발광 텍스처 원래 색의 Hue (도)")]
        [SerializeField] private float _sourceHue = 12.0f;
        [Tooltip("HueShift 로 사용할 셰이더 속성 이름")]
        [SerializeField] private string _hueShiftProperty = "_Emissive_HueShift";
        #endregion

        #region Private Fields
        private MaterialPropertyBlock _propertyBlock;
        private int _hueShiftId;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            _propertyBlock = new MaterialPropertyBlock();
            _hueShiftId = Shader.PropertyToID(_hueShiftProperty);
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// 비율에 맞는 Gradient 색으로 렌더러 발광 색을 바꾼다.
        /// </summary>
        /// <param name="ratio">0~1 비율. 범위를 벗어나면 Clamp 한다.</param>
        public void SetRatio(float ratio)
        {
            if (_renderers == null || _gradient == null) return;

            Color.RGBToHSV(_gradient.Evaluate(Mathf.Clamp01(ratio)), out float hue, out _, out _);
            float hueShift = hue * FULL_DEGREE - _sourceHue;

            foreach (Renderer target in _renderers)
            {
                if (!target) continue;

                target.GetPropertyBlock(_propertyBlock);
                _propertyBlock.SetFloat(_hueShiftId, hueShift);
                target.SetPropertyBlock(_propertyBlock);
            }
        }
        #endregion
    }
}
