using Attribute.Core;
using UnityEngine;
using Core;
using UnityEngine.InputSystem;

namespace UI
{
    public class UIManager : MonoBehaviour
    {
        // 플레이어 데이터를 UI에 보여줘야하는데 그 데이터를 조작하는 Presenter를 갖고있는다.
        [Header("References")]
        [SerializeField] private PlayerHudPresenter _playerHudPresenter;
        [SerializeField] private SettingsPresenter _settingsPresenter;

        private SettingsPresenter _activeSettingsPresenter;
        private UnityEngine.InputSystem.PlayerInput _playerInput;
        private InputAction _openSettingsAction;
        private InputAction _closeSettingsAction;

        // 이벤트 구독용
        private GameManager _gameManager;

        private void OnEnable()
        {
            // 1. 플레이어를 연결할 HUD가 있는지 확인한다.
            if (_playerHudPresenter == null)
            {
                Debug.LogError(
                    "PlayerHudPresenter가 연결되지 않았습니다.", this);
                return;
            }

            // 2. 플레이어 등록을 담당하는 GameManager를 가져온다.
            _gameManager = GameManager.Instance;

            if (_gameManager == null)
            {
                Debug.LogError("씬에 GameManager가 없습니다.", this);
                return;
            }

            // 3. 앞으로 플레이어가 스폰되면 
            // HandlePlayerSpawned를 실행해 달라고 등록한다. HandlePlayerSpwned 함수는 => UI를 스폰된 플레이어와 동기화 시키는 함수
            _gameManager.OnPlayerSpawned += HandlePlayerSpawned;

            // 4. UI가 켜지기 전에 이미 등록된 플레이어가 있다면
            //    그 플레이어를 지금 바로 HUD에 연결한다.
            //    아직 플레이어가 없다면 null이 전달된다.
            //_playerHudPresenter.SetPlayerAttributes(_gameManager.CurrentPlayerState);
            HandlePlayerSpawned(_gameManager.CurrentPlayerState);
        }
        private void OnDisable()
        {
            CloseSettings();
            UnbindPlayerInput();
            // 1. 알림을 신청했던 GameManager에서 구독을 해제한다.
            if (_gameManager != null)
            {
                _gameManager.OnPlayerSpawned -= HandlePlayerSpawned;
            }

            // 2. 기억해 둔 GameManager 참조를 비운다.
            _gameManager = null;

            // 3. HUD와 플레이어의 연결도 해제한다.
            if (_playerHudPresenter != null)
            {
                _playerHudPresenter.SetPlayerAttributes(null);
            }
        }

        private void HandlePlayerSpawned(AttributeSet attributeSet)
        {
            // 전달받은 플레이어를 HUD에 연결한다.
            _playerHudPresenter.SetPlayerAttributes(attributeSet);
            BindPlayerInput(attributeSet);
        }

        public void ShowPlayerHud() => _playerHudPresenter.Show();
        public void HidePlayerHud() => _playerHudPresenter.Hide();

        private void BindPlayerInput(AttributeSet attributeSet)
        {
            UnbindPlayerInput();

            if (attributeSet == null) return;

            _playerInput =
                attributeSet.GetComponentInParent<UnityEngine.InputSystem.PlayerInput>();

            if (_playerInput == null) return;

            _openSettingsAction =
                _playerInput.actions.FindAction("Player/OpenSettings");
            _closeSettingsAction =
                _playerInput.actions.FindAction("UI/CloseSettings");

            if (_openSettingsAction != null)
                _openSettingsAction.performed += HandleOpenSettings;

            if (_closeSettingsAction != null)
                _closeSettingsAction.performed += HandleCloseSettings;

            // 설정창이 열린 상태에서 플레이어가 교체된 경우에도 UI 입력 유지.
            if (_activeSettingsPresenter != null)
            {
                _playerInput.SwitchCurrentActionMap("UI");
            }
        }

        private void UnbindPlayerInput()
        {
            if (_openSettingsAction != null)
                _openSettingsAction.performed -= HandleOpenSettings;

            if (_closeSettingsAction != null)
                _closeSettingsAction.performed -= HandleCloseSettings;

            _openSettingsAction = null;
            _closeSettingsAction = null;
            _playerInput = null;
        }

        private void HandleOpenSettings(InputAction.CallbackContext context)
        {
            OpenSettings();
        }

        private void HandleCloseSettings(InputAction.CallbackContext context)
        {
            CloseSettings();
        }

        public void OpenSettings()
        {
            if (_activeSettingsPresenter != null ||
                _settingsPresenter == null ||
                _playerInput == null ||
                _gameManager == null)
                return;

            if (_gameManager.CurrentState != GameManager.GameState.Playing)
                return;

            // 현재 UI 지정 및 닫기 요청 연결.
            _activeSettingsPresenter = _settingsPresenter;
            _activeSettingsPresenter.CloseRequested += CloseSettings;

            // 게임 상태와 입력 전환.
            _gameManager.PauseGame();
            _playerInput.SwitchCurrentActionMap("UI");

            // Presenter와 View를 활성화한 뒤 화면 표시.
            _activeSettingsPresenter.gameObject.SetActive(true);
            _activeSettingsPresenter.Open();
        }

        public void CloseSettings()
        {
            if (_activeSettingsPresenter == null) return;

            SettingsPresenter presenter = _activeSettingsPresenter;
            _activeSettingsPresenter = null;

            // 닫기 요청 해제 후 화면과 컴포넌트 비활성화.
            presenter.CloseRequested -= CloseSettings;
            presenter.Close();
            presenter.gameObject.SetActive(false);

            if (_playerInput != null)
                _playerInput.SwitchCurrentActionMap("Player");


            if (_gameManager != null &&
                _gameManager.CurrentState == GameManager.GameState.Pause)
            {
                _gameManager.ResumeGame();
            }
        }

    }
}
