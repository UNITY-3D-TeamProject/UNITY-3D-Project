using System;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Death
{
    // 사망 애니메이션이 끝나면 뜨는 팝업. 다시하기·메인 메뉴 버튼 클릭만 DeathPopupPresenter에 알린다.
    public class DeathPopupView : BaseView
    {
        [Header("Buttons")]
        [SerializeField] private Button _retryButton;
        [SerializeField] private Button _mainMenuButton;

        public event Action RetryClicked;
        public event Action MainMenuClicked;

        private void OnEnable()
        {
            if (_retryButton != null)
                _retryButton.onClick.AddListener(HandleRetryClicked);
            if (_mainMenuButton != null)
                _mainMenuButton.onClick.AddListener(HandleMainMenuClicked);
        }

        private void OnDisable()
        {
            if (_retryButton != null)
                _retryButton.onClick.RemoveListener(HandleRetryClicked);
            if (_mainMenuButton != null)
                _mainMenuButton.onClick.RemoveListener(HandleMainMenuClicked);
        }

        private void HandleRetryClicked()
        {
            RetryClicked?.Invoke();
        }

        private void HandleMainMenuClicked()
        {
            MainMenuClicked?.Invoke();
        }
    }
}
