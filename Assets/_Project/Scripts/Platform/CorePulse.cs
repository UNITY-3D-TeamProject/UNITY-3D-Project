using UnityEngine;

namespace Platform
{
    /// <summary>
    /// 소각로 코어를 심장처럼 맥동시킨다. 크기와 발광 세기(Emission)가 함께 변한다.
    /// 머티리얼에 Emission이 켜져 있어야 한다.
    /// </summary>
    [RequireComponent(typeof(Renderer))]
    public class CorePulse : MonoBehaviour
    {
        #region Constants
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
        #endregion

        #region Serialized Fields
        [Header("Pulse")]
        [Tooltip("맥동 속도 (라디안/초). 클수록 빨리 뛴다.")]
        [SerializeField, Min(0.0f)] private float _pulseSpeed = 3.0f;

        [Tooltip("크기 변화 폭 (0.1 = 기본 크기 기준 ±10%)")]
        [SerializeField, Range(0.0f, 0.5f)] private float _scaleAmplitude = 0.1f;

        [Header("Emission")]
        [ColorUsage(false, true)]
        [SerializeField] private Color _emissionColor = new Color(1.0f, 0.4f, 0.1f, 1.0f);
        [SerializeField, Min(0.0f)] private float _minIntensity = 2.0f;
        [SerializeField, Min(0.0f)] private float _maxIntensity = 6.0f;
        #endregion

        #region Private Fields
        private Renderer _renderer;
        private MaterialPropertyBlock _propertyBlock;
        private Vector3 _baseScale;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            _renderer = GetComponent<Renderer>();
            _propertyBlock = new MaterialPropertyBlock();
            _baseScale = transform.localScale;
        }

        private void Update()
        {
            float wave = (Mathf.Sin(Time.time * _pulseSpeed) + 1.0f) * 0.5f;

            transform.localScale = _baseScale * Mathf.Lerp(1.0f - _scaleAmplitude, 1.0f + _scaleAmplitude, wave);

            _renderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetColor(EmissionColorId, _emissionColor * Mathf.Lerp(_minIntensity, _maxIntensity, wave));
            _renderer.SetPropertyBlock(_propertyBlock);
        }
        #endregion
    }
}
