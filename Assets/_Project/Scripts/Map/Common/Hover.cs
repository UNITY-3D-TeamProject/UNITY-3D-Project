using UnityEngine;

namespace Map.Common
{
    /// <summary>
    /// 시작 위치를 기준으로 위아래로 천천히 떠다니게 한다. (홀로 드론·떠 있는 장식용, 판정 없음)
    /// </summary>
    public class Hover : MonoBehaviour
    {
        #region Serialized Fields
        [Tooltip("위아래로 움직이는 거리(m)")]
        [SerializeField, Min(0.0f)] private float _amplitude = 0.6f;

        [Tooltip("초당 왕복 횟수")]
        [SerializeField, Min(0.0f)] private float _frequency = 0.25f;

        [Tooltip("여러 개가 같이 움직이지 않도록 어긋나게 하는 값 (0~1)")]
        [SerializeField, Range(0.0f, 1.0f)] private float _phase;
        #endregion

        #region Private Fields
        private Vector3 _origin;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            _origin = transform.localPosition;
        }

        private void Update()
        {
            float offset = Mathf.Sin(((Time.time * _frequency) + _phase) * Mathf.PI * 2.0f) * _amplitude;
            transform.localPosition = _origin + (Vector3.up * offset);
        }
        #endregion
    }
}
