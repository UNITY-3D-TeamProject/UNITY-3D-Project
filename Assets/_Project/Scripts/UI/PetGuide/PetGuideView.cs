using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.PetGuide
{
    public class PetGuideView : MonoBehaviour
    {
        [Header("Root")]
        [SerializeField] private GameObject _root;

        [Header("Content")]
        [SerializeField] private TMP_Text _messageText;
        [SerializeField] private TMP_Text _nameText;
        [SerializeField] private Image _portraitImage;
        [Tooltip("대화 중에만 보이는 넘기기 안내 (예: Space ▶ 다음)")]
        [SerializeField] private TMP_Text _nextLabelText;

        public bool IsVisible => _root != null && _root.activeSelf;

        public void Show(string message)
        {
            if (_messageText != null)
                _messageText.text = message;

            SetVisible(true);
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

        public void Hide()
        {
            SetVisible(false);
        }

        private void SetVisible(bool isVisible)
        {
            if (_root != null)
                _root.SetActive(isVisible);
        }
    }
}
