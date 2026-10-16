using UnityEngine;
using UnityEngine.UI;

namespace Map.Lobby.SkySign
{
    /// <summary>
    /// 날씨 전광판 화면 넘김. 일정 시간마다 다음 화면이 아래에서 떠오르며 나타나고,
    /// 같은 순서의 배경이 서서히 바뀌며, 아래 점 표시가 현재 화면을 길게 보여 준다.
    /// </summary>
    public class SkySignSlideShow : MonoBehaviour
    {
        #region Serialized Fields
        [Header("References")]
        [SerializeField] private CanvasGroup[] _slides;

        [Tooltip("화면마다 하나씩, 같은 순서의 배경 (없으면 비워 둔다)")]
        [SerializeField] private CanvasGroup[] _backgrounds;

        [Tooltip("화면 위치 점 (화면 수와 같은 개수)")]
        [SerializeField] private Image[] _dots;

        [Header("Settings")]
        [Tooltip("한 화면을 보여 주는 시간 (초)")]
        [SerializeField] private float _interval = 4.5f;

        [Tooltip("화면이 바뀌는 시간 (초)")]
        [SerializeField] private float _fadeSeconds = 0.6f;

        [Tooltip("나타날 때 아래에서 올라오는 거리 (UI 픽셀)")]
        [SerializeField] private float _riseDistance = 40.0f;

        [Tooltip("배경이 바뀌는 시간 (초)")]
        [SerializeField] private float _backgroundFadeSeconds = 1.2f;

        [SerializeField] private float _dotWidth = 14.0f;
        [SerializeField] private float _activeDotWidth = 40.0f;
        [SerializeField] private Color _dotColor = new Color(1.0f, 1.0f, 1.0f, 0.4f);
        [SerializeField] private Color _activeDotColor = Color.white;
        #endregion

        #region Private Fields
        private int _current;
        private float _timer;
        private Vector2[] _slidePositions;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            _slidePositions = new Vector2[_slides.Length];
            for (int i = 0; i < _slides.Length; i++)
            {
                _slidePositions[i] = ((RectTransform)_slides[i].transform).anchoredPosition;
            }
        }

        private void OnEnable()
        {
            _current = 0;
            _timer = _fadeSeconds;  // 첫 화면은 바로 보이게
            for (int i = 0; i < _backgrounds.Length; i++)
            {
                _backgrounds[i].alpha = i == 0 ? 1.0f : 0.0f;
            }
        }

        private void Update()
        {
            _timer += Time.deltaTime;
            if (_timer >= _interval)
            {
                _timer = 0.0f;
                _current = (_current + 1) % _slides.Length;
            }

            float slideT = Mathf.Clamp01(_timer / _fadeSeconds);
            float easeOut = 1.0f - Mathf.Pow(1.0f - slideT, 3.0f);
            for (int i = 0; i < _slides.Length; i++)
            {
                bool isCurrent = i == _current;
                _slides[i].alpha = isCurrent ? easeOut : Mathf.Min(_slides[i].alpha, 1.0f - easeOut);
                float rise = isCurrent ? (1.0f - easeOut) * _riseDistance : 0.0f;
                ((RectTransform)_slides[i].transform).anchoredPosition = _slidePositions[i] - new Vector2(0.0f, rise);
            }

            float backgroundStep = Time.deltaTime / _backgroundFadeSeconds;
            for (int i = 0; i < _backgrounds.Length; i++)
            {
                float target = i == _current ? 1.0f : 0.0f;
                _backgrounds[i].alpha = Mathf.MoveTowards(_backgrounds[i].alpha, target, backgroundStep);
            }

            for (int i = 0; i < _dots.Length; i++)
            {
                bool isCurrent = i == _current;
                RectTransform dotRect = _dots[i].rectTransform;
                float width = Mathf.Lerp(dotRect.sizeDelta.x, isCurrent ? _activeDotWidth : _dotWidth, slideT);
                dotRect.sizeDelta = new Vector2(width, dotRect.sizeDelta.y);
                _dots[i].color = isCurrent ? _activeDotColor : _dotColor;
            }
        }
        #endregion
    }
}
