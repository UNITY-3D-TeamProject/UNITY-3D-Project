using UnityEngine;

namespace Map.Lobby.SkySign
{
    /// <summary>
    /// 화면 배경의 흐린 색 덩어리를 천천히 오가며 커졌다 작아지게 한다.
    /// </summary>
    public class SkySignDrift : MonoBehaviour
    {
        #region Serialized Fields
        [Tooltip("시작 위치에서 가장 멀리 가는 거리 (UI 픽셀)")]
        [SerializeField] private Vector2 _offset = new Vector2(90.0f, 70.0f);

        [Tooltip("가장 커질 때의 크기 증가량 (0.2 = 20%)")]
        [SerializeField] private float _scaleAmount = 0.18f;

        [Tooltip("한 번 갔다 오는 데 걸리는 시간 (초)")]
        [SerializeField] private float _period = 28.0f;

        [Tooltip("시작 위상 (0 ~ 1). 덩어리마다 다르게 주면 따로 움직인다")]
        [SerializeField] private float _phase;
        #endregion

        #region Private Fields
        private RectTransform _rectTransform;
        private Vector2 _startPosition;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            _rectTransform = (RectTransform)transform;
            _startPosition = _rectTransform.anchoredPosition;
        }

        private void Update()
        {
            // 0 → 1 → 0 을 부드럽게 오간다.
            float t = 0.5f - 0.5f * Mathf.Cos((Time.time / _period + _phase) * Mathf.PI * 2.0f);
            _rectTransform.anchoredPosition = _startPosition + _offset * t;
            _rectTransform.localScale = Vector3.one * (1.0f + _scaleAmount * t);
        }
        #endregion
    }
}
