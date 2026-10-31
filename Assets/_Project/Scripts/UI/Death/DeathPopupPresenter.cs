using System;
using UnityEngine;

namespace UI.Death
{
    // 사망 팝업의 버튼 입력을 UIManager가 처리할 요청으로 바꿔 전달한다.
    public class DeathPopupPresenter : MonoBehaviour
    {
        [SerializeField] private DeathPopupView _view;

        public event Action RetryRequested;
        public event Action MainMenuRequested;

        private void OnEnable()
        {
            if (_view != null)
            {
                _view.RetryClicked += HandleRetryClicked;
                _view.MainMenuClicked += HandleMainMenuClicked;
            }
        }

        private void OnDisable()
        {
            if (_view != null)
            {
                _view.RetryClicked -= HandleRetryClicked;
                _view.MainMenuClicked -= HandleMainMenuClicked;
            }
        }

        public void Open()
        {
            _view?.Show();
        }

        public void Close()
        {
            _view?.Hide();
        }

        private void HandleRetryClicked()
        {
            RetryRequested?.Invoke();
        }

        private void HandleMainMenuClicked()
        {
            MainMenuRequested?.Invoke();
        }
    }
}
