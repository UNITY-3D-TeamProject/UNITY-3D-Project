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

        // Presenter 오브젝트를 켠 뒤 화면 표시. 호출자는 Open만 부르면 된다.
        public void Open()
        {
            gameObject.SetActive(true);
            _view?.Show();
        }

        // 화면을 숨긴 뒤 Presenter 오브젝트 비활성화.
        public void Close()
        {
            _view?.Hide();
            gameObject.SetActive(false);
        }

        private void HandleCloseClicked()
        {
            CloseRequested?.Invoke();
        }
    }
}
