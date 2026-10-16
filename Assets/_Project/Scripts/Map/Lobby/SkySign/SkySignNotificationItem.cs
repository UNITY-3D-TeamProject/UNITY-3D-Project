using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Map.Lobby.SkySign
{
    /// <summary>
    /// 알림 카드 한 장. 나타날 때 높이가 0 에서 자라 아래 카드들을 부드럽게 밀어내고, 내용은 위에서 튀어나온다.
    /// </summary>
    public class SkySignNotificationItem : MonoBehaviour
    {
        #region Constants
        private const float APPEAR_DURATION = 0.55f;
        #endregion

        #region Serialized Fields
        [Header("References")]
        [SerializeField] private LayoutElement _layoutElement;
        [SerializeField] private RectTransform _content;
        [SerializeField] private CanvasGroup _contentGroup;
        [SerializeField] private Image _iconBackground;
        [SerializeField] private Image _icon;
        [SerializeField] private TMP_Text _appNameText;
        [SerializeField] private TMP_Text _timeText;
        [SerializeField] private TMP_Text _messageText;
        #endregion

        #region Private Fields
        private float _elapsed;
        #endregion

        #region Unity Lifecycle
        private void Update()
        {
            // 글자 줄바꿈은 카드 폭이 정해진 뒤에 확정되므로 매 프레임 실제 높이를 따른다.
            float contentHeight = _content.rect.height;
            if (_elapsed >= APPEAR_DURATION)
            {
                _layoutElement.preferredHeight = contentHeight;
                return;
            }

            _elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(_elapsed / APPEAR_DURATION);
            float easeOut = 1.0f - Mathf.Pow(1.0f - t, 3.0f);
            // 살짝 넘쳤다 돌아오는 튀는 느낌
            float overshoot = 1.0f + 0.08f * Mathf.Sin(t * Mathf.PI);

            _layoutElement.preferredHeight = contentHeight * easeOut;
            _contentGroup.alpha = easeOut;
            _content.localScale = Vector3.one * Mathf.Lerp(0.9f, 1.0f, easeOut) * (t < 1.0f ? overshoot : 1.0f);
        }
        #endregion

        #region Public Methods
        public void Setup(SNotificationEntry entry)
        {
            _iconBackground.color = entry.Color;
            _icon.sprite = entry.Icon;
            _appNameText.text = entry.AppName;
            _messageText.text = entry.Message;

            _layoutElement.preferredHeight = 0.0f;
            _contentGroup.alpha = 0.0f;
            _elapsed = 0.0f;
        }

        public void SetTimeLabel(string label)
        {
            _timeText.text = label;
        }
        #endregion
    }
}
