using TMPro;
using UnityEngine;

namespace Map.Lobby.SkySign
{
    /// <summary>
    /// 글자가 칸보다 길면 왼쪽으로 끊김 없이 흘려 보낸다. 짧으면 멈춰 둔다.
    /// 칸(RectMask2D) 안에 같은 글자 두 개를 나란히 두고 이어 붙여 돌린다.
    /// </summary>
    public class SkySignMarquee : MonoBehaviour
    {
        #region Serialized Fields
        [Header("References")]
        [Tooltip("글자가 보이는 칸 (RectMask2D)")]
        [SerializeField] private RectTransform _viewport;
        [SerializeField] private TMP_Text _first;
        [SerializeField] private TMP_Text _second;

        [Header("Settings")]
        [Tooltip("흐르는 속도 (UI 픽셀/초)")]
        [SerializeField] private float _speed = 70.0f;

        [Tooltip("글자 사이 간격 (UI 픽셀)")]
        [SerializeField] private float _gap = 90.0f;
        #endregion

        #region Private Fields
        private float _textWidth;
        private float _offset;
        private bool _isScrolling;
        #endregion

        #region Unity Lifecycle
        private void Update()
        {
            if (!_isScrolling) return;

            float loop = _textWidth + _gap;
            _offset = (_offset + _speed * Time.deltaTime) % loop;
            SetPositions(-_offset, loop);
        }
        #endregion

        #region Public Methods
        public void SetText(string text)
        {
            _first.text = text;
            _second.text = text;
            _textWidth = _first.GetPreferredValues(text).x;
            _isScrolling = _textWidth > _viewport.rect.width;
            _second.gameObject.SetActive(_isScrolling);
            _offset = 0.0f;
            SetPositions(0.0f, _textWidth + _gap);
        }
        #endregion

        #region Private Methods
        private void SetPositions(float x, float loop)
        {
            _first.rectTransform.anchoredPosition = new Vector2(x, 0.0f);
            _second.rectTransform.anchoredPosition = new Vector2(x + loop, 0.0f);
        }
        #endregion
    }
}
