using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;
using SceneTransition;
using UI.Settings;

namespace UI.MainMenu
{
    public class MainMenuPresenter : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private MainMenuView _view;
        [SerializeField] private SettingsPresenter _settingsPresenter;

        // 설정창 닫기 입력 (FID_InputSystem의 UI/CloseSettings).
        // 메인 메뉴 씬에는 PlayerInput이 없으므로 여기서 직접 켜고 끈다.
        [Header("Input")]
        [SerializeField] private InputActionReference _closeSettingsAction;

        // 스플래시에서 아무 버튼 입력을 한 번 기다리는 구독. 입력을 받으면 null.
        private IDisposable _anyKeyListener;
        private bool _isSettingsOpen;

        private void OnEnable()
        {
            if (_view == null)
            {
                Debug.LogError("MainMenuView가 연결되지 않았습니다.", this);
                return;
            }

            _view.StartClicked += HandleStartClicked;
            _view.SettingsClicked += OpenSettings;
            _view.ExitClicked += HandleExitClicked;

            // 스플래시부터 보여주고, 아무 버튼(키보드·마우스·게임패드)을 한 번 누르면 메뉴로 넘어간다.
            // 액션 맵과 상관없이 장치 입력을 직접 받는다.
            _view.ShowSplash();
            _anyKeyListener = InputSystem.onAnyButtonPress.CallOnce(HandleAnyKeyPressed);

            if (_closeSettingsAction != null)
            {
                _closeSettingsAction.action.performed += HandleCloseSettingsPerformed;
                _closeSettingsAction.action.Enable();
            }
        }

        private void OnDisable()
        {
            CloseSettings();

            if (_closeSettingsAction != null)
            {
                _closeSettingsAction.action.performed -= HandleCloseSettingsPerformed;
                _closeSettingsAction.action.Disable();
            }

            _anyKeyListener?.Dispose();
            _anyKeyListener = null;

            if (_view != null)
            {
                _view.StartClicked -= HandleStartClicked;
                _view.SettingsClicked -= OpenSettings;
                _view.ExitClicked -= HandleExitClicked;
            }
        }

        public void OpenSettings()
        {
            if (_isSettingsOpen || _settingsPresenter == null) return;

            _isSettingsOpen = true;
            _settingsPresenter.CloseRequested += CloseSettings;

            _settingsPresenter.Open();
        }

        public void CloseSettings()
        {
            if (!_isSettingsOpen) return;

            _isSettingsOpen = false;

            // 닫기 요청 해제 후 설정창 닫기.
            _settingsPresenter.CloseRequested -= CloseSettings;
            _settingsPresenter.Close();
        }

        private void HandleAnyKeyPressed(InputControl control)
        {
            _anyKeyListener = null;
            _view.ShowMenu();
        }

        private void HandleCloseSettingsPerformed(InputAction.CallbackContext context)
        {
            CloseSettings();
        }

        private void HandleStartClicked()
        {
            // 저장 기능이 없으므로 항상 새 게임(튜토리얼)부터 시작한다.
            SceneLoader.TryStartNewGame();
        }

        private void HandleExitClicked()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
