using System;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class SettingsView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameObject _root;
        [SerializeField] private Button _closeButton;

        public event Action CloseClicked;

        private void OnEnable()
        {
            if (_closeButton != null)
                _closeButton.onClick.AddListener(HandleCloseClicked);
        }

        private void OnDisable()
        {
            if (_closeButton != null)
                _closeButton.onClick.RemoveListener(HandleCloseClicked);
        }

        public void Show()
        {
            if (_root != null) _root.SetActive(true);
        }

        public void Hide()
        {
            if (_root != null) _root.SetActive(false);
        }

        private void HandleCloseClicked()
        {
            CloseClicked?.Invoke();
        }
    }
}
