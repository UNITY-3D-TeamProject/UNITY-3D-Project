using System;
using UnityEngine;
using UnityEngine.UI;

namespace UI.MainMenu
{
    public class MainMenuView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameObject _splashRoot;
        [SerializeField] private GameObject _menuRoot;
        [SerializeField] private Button _startButton;
        [SerializeField] private Button _continueButton;
        [SerializeField] private Button _settingsButton;
        [SerializeField] private Button _exitButton;

        public event Action StartClicked;
        public event Action ContinueClicked;
        public event Action SettingsClicked;
        public event Action ExitClicked;

        private void OnEnable()
        {
            if (_startButton != null)
                _startButton.onClick.AddListener(HandleStartClicked);
            if (_continueButton != null)
                _continueButton.onClick.AddListener(HandleContinueClicked);
            if (_settingsButton != null)
                _settingsButton.onClick.AddListener(HandleSettingsClicked);
            if (_exitButton != null)
                _exitButton.onClick.AddListener(HandleExitClicked);
        }

        private void OnDisable()
        {
            if (_startButton != null)
                _startButton.onClick.RemoveListener(HandleStartClicked);
            if (_continueButton != null)
                _continueButton.onClick.RemoveListener(HandleContinueClicked);
            if (_settingsButton != null)
                _settingsButton.onClick.RemoveListener(HandleSettingsClicked);
            if (_exitButton != null)
                _exitButton.onClick.RemoveListener(HandleExitClicked);
        }

        // 스플래시를 메뉴 위에 보여준다.
        public void ShowSplash()
        {
            if (_splashRoot != null) _splashRoot.SetActive(true);
            if (_menuRoot != null) _menuRoot.SetActive(false);
        }

        // 스플래시를 끄고 메뉴를 보여준다.
        public void ShowMenu()
        {
            if (_splashRoot != null) _splashRoot.SetActive(false);
            if (_menuRoot != null) _menuRoot.SetActive(true);
        }

        public void SetContinueInteractable(bool isInteractable)
        {
            if (_continueButton != null)
                _continueButton.interactable = isInteractable;
        }

        private void HandleStartClicked()
        {
            StartClicked?.Invoke();
        }

        private void HandleContinueClicked()
        {
            ContinueClicked?.Invoke();
        }

        private void HandleSettingsClicked()
        {
            SettingsClicked?.Invoke();
        }

        private void HandleExitClicked()
        {
            ExitClicked?.Invoke();
        }
    }
}
