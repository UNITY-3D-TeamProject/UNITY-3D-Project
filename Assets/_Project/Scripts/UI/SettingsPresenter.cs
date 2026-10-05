using System;
using UnityEngine;

namespace UI
{
    public class SettingsPresenter : MonoBehaviour
    {
        [SerializeField] private SettingsView _view;

        public event Action CloseRequested;

        private void OnEnable()
        {
            if (_view != null)
                _view.CloseClicked += HandleCloseClicked;
        }

        private void OnDisable()
        {
            if (_view != null)
                _view.CloseClicked -= HandleCloseClicked;
        }

        public void Open()
        {
            _view?.Show();
        }

        public void Close()
        {
            _view?.Hide();
        }

        private void HandleCloseClicked()
        {
            CloseRequested?.Invoke();
        }
    }
}
