using UnityEngine;

namespace Map.Lobby.SkySign
{
    /// <summary>
    /// 작은 이퀄라이저 막대들의 높이를 막대마다 다른 속도로 출렁이게 한다.
    /// </summary>
    public class SkySignEqualizer : MonoBehaviour
    {
        #region Serialized Fields
        [SerializeField] private RectTransform[] _bars;

        [Tooltip("가장 낮을 때의 높이 비율")]
        [SerializeField] private float _minScale = 0.15f;

        [Tooltip("출렁이는 빠르기 범위 (Hz)")]
        [SerializeField] private Vector2 _speedRange = new Vector2(0.8f, 1.8f);
        #endregion

        #region Private Fields
        private float[] _speeds;
        private float[] _phases;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            _speeds = new float[_bars.Length];
            _phases = new float[_bars.Length];
            for (int i = 0; i < _bars.Length; i++)
            {
                _speeds[i] = Random.Range(_speedRange.x, _speedRange.y);
                _phases[i] = Random.value;
            }
        }

        private void Update()
        {
            for (int i = 0; i < _bars.Length; i++)
            {
                float wave = 0.5f + 0.5f * Mathf.Sin((Time.time * _speeds[i] + _phases[i]) * Mathf.PI * 2.0f);
                _bars[i].localScale = new Vector3(1.0f, Mathf.Lerp(_minScale, 1.0f, wave), 1.0f);
            }
        }
        #endregion
    }
}
