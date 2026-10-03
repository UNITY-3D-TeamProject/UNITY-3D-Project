using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class MonologueView : MonoBehaviour
    {
        [Header("Root")]
        [SerializeField] private GameObject _root;

        [Header("Content")]
        [SerializeField] private Text _messageText;

        public bool IsVisible => _root != null && _root.activeSelf;

        public void Show(string message)
        {
            if (_messageText != null)
                _messageText.text = message;

            SetVisible(true);
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