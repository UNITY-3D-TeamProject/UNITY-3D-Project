using UnityEngine;

namespace Map.Common
{
    /// <summary>
    /// 조명의 밝기를 불규칙하게 흔든다. (불, 전기 주변의 일렁이는 빛)
    /// </summary>
    [RequireComponent(typeof(Light))]
    public class LightFlicker : MonoBehaviour
    {
        #region Serialized Fields
        [Tooltip("흔들리는 폭 (원래 밝기에 대한 비율). 0.3 이면 70% ~ 130% 사이에서 변한다.")]
        [SerializeField, Range(0.0f, 1.0f)] private float _amount = 0.3f;

        [Tooltip("흔들리는 빠르기")]
        [SerializeField, Min(0.0f)] private float _speed = 8.0f;
        #endregion

        #region Private Fields
        private Light _light;
        private float _baseIntensity;
        private float _seed;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            _light = GetComponent<Light>();
            _baseIntensity = _light.intensity;

            // 여러 개가 같은 박자로 깜빡이지 않게 한다
            _seed = Random.value * 100.0f;
        }

        private void Update()
        {
            float noise = (Mathf.PerlinNoise(_seed, Time.time * _speed) * 2.0f) - 1.0f;
            _light.intensity = _baseIntensity * (1.0f + (noise * _amount));
        }
        #endregion
    }
}
