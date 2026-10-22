using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UI;

namespace UIandCinematic
{
    public class PetGuideView : BaseView
    {
        [Header("Content")]
        [SerializeField] private TMP_Text _messageText;
        [SerializeField] private TMP_Text _nameText;
        [SerializeField] private Image _portraitImage;
        [Tooltip("대화 중에만 보이는 넘기기 안내 (예: E ▶ 다음)")]
        [SerializeField] private TMP_Text _nextLabelText;
        [Tooltip("독백 중에만 보이는 남은 시간 게이지. Image Type을 Filled로 둔다")]
        [SerializeField] private Image _gaugeImage;

        public void Show(string message)
        {
            if (_messageText != null)
                _messageText.text = message;

            Show();
        }

        // 대화 한 줄을 띄운다. 초상화가 없으면 초상화 칸을 숨긴다.
        public void ShowLine(string speakerName, Sprite portrait, string message)
        {
            if (_nameText != null)
                _nameText.text = speakerName;

            if (_portraitImage != null)
            {
                _portraitImage.sprite = portrait;
                _portraitImage.enabled = portrait != null;
            }

            Show(message);
        }

        // 패널 아래쪽에 넘기기 안내를 띄운다.
        public void ShowNextLabel(string text)
        {
            if (_nextLabelText == null) return;

            _nextLabelText.text = text;
            _nextLabelText.gameObject.SetActive(true);
        }

        public void HideNextLabel()
        {
            if (_nextLabelText != null)
                _nextLabelText.gameObject.SetActive(false);
        }

        // 패널 아래쪽 게이지를 띄우고 남은 비율(1 → 0)을 채운다.
        public void ShowGauge(float ratio)
        {
            if (_gaugeImage == null) return;

            _gaugeImage.fillAmount = Mathf.Clamp01(ratio);
            _gaugeImage.gameObject.SetActive(true);
        }

        public void HideGauge()
        {
            if (_gaugeImage != null)
                _gaugeImage.gameObject.SetActive(false);
        }
    }
}
