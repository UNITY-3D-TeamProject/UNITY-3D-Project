using System;
using UnityEngine;
using UnityEngine.InputSystem;
using Core;
using Facade.Player;
using UI.Dialogue;
using UI.PetGuide;
using UI.PlayerHud;
using UI.Settings;

namespace UI
{
    public class UIManager : MonoBehaviour
    {
        private const string ACTION_MAP_DIALOGUE = "Dialogue";
        private const string ACTION_ADVANCE_DIALOGUE = "Dialogue/Advance";
        private const string ACTION_OPEN_SETTINGS_IN_DIALOGUE = "Dialogue/OpenSettings";

        // 플레이어 데이터를 UI에 보여줘야하는데 그 데이터를 조작하는 Presenter를 갖고있는다.
        [Header("References")]
        [SerializeField] private PlayerHudPresenter _playerHudPresenter;
        [SerializeField] private SettingsPresenter _settingsPresenter;
        [SerializeField] private PetGuidePresenter _petGuidePresenter;
        [SerializeField] private DialoguePresenter _dialoguePresenter;

        private SettingsPresenter _activeSettingsPresenter;
        // 대화를 시작한 쪽에 돌려줄 콜백. 대화 중일 때만 값이 있다.
        private Action<SDialogueLine> _dialogueLineChanged;
        private Action _dialogueFinished;
        // 현재 플레이어의 PlayerFacade. 설정창 입력 이벤트와 입력 맵 전환에 사용한다.
        private PlayerFacade _playerFacade;
        // 현재 플레이어의 PlayerInput. 대화 입력 맵(Dialogue)은 PlayerFacade를 거치지 않고 여기서 직접 전환·구독한다.
        private UnityEngine.InputSystem.PlayerInput _playerInput;
        private InputAction _advanceDialogueAction;
        private InputAction _openSettingsInDialogueAction;

        // 이벤트 구독용
        private GameManager _gameManager;

        // 지금 씬의 UIManager. 배치하지 않은 씬이거나 Awake 전이면 null이다.
        // DontDestroyOnLoad를 쓰지 않으므로 공용 Singleton<T>을 상속하지 않는다.
        public static UIManager Instance { get; private set; }

        public bool IsDialoguePlaying => (_dialoguePresenter != null) && _dialoguePresenter.IsDialoguePlaying;

        private void Awake()
        {
            // 같은 씬에 둘이면 배치 실수다. (이전 씬의 UIManager가 아직 남아 있는 경우는 씬으로 구분한다)
            if ((Instance != null) && (Instance.gameObject.scene == gameObject.scene))
                Debug.LogError("[UIManager] 한 씬에 UIManager가 두 개 있습니다. 나중에 Awake된 쪽이 Instance가 됩니다.", this);

            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

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
            HandlePlayerSpawned(_gameManager.CurrentPlayerFacade);
        }
        private void OnDisable()
        {
            CloseSettings();
            StopDialogue();
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
                _playerHudPresenter.SetPlayer(null);
            }
        }

        private void HandlePlayerSpawned(PlayerFacade playerFacade)
        {
            // 전달받은 플레이어를 HUD에 연결한다.
            _playerHudPresenter.SetPlayer(playerFacade);
            BindPlayerInput(playerFacade);
        }

        public void ShowPlayerHud() => _playerHudPresenter.Show();
        public void HidePlayerHud() => _playerHudPresenter.Hide();

        // 번호에 맞는 펫 안내 문구를 띄운다. 번호와 문구는 SOPetGuideTable에서 정한다.
        public void ShowPetGuide(int id) => _petGuidePresenter?.Show(id);
        public void HidePetGuide() => _petGuidePresenter?.Hide();

        /// <summary>
        /// 대화를 시작한다. 끝날 때까지 Dialogue 입력 맵으로 바꿔 플레이어 조작을 막는다.
        /// Dialogue 맵에서는 Space로 대사를 넘기고, ESC로 설정창을 연다.
        /// </summary>
        /// <param name="sequence">재생할 대화</param>
        /// <param name="onLineChanged">줄이 바뀔 때 그 줄(말하는 사람, 카메라 번호 등)을 받는다</param>
        /// <param name="onFinished">대화가 끝나 조작이 돌아온 뒤 호출된다</param>
        /// <returns>시작했으면 true. 이미 대화 중이거나 설정창이 열려 있거나 플레이어가 없으면 false</returns>
        public bool PlayDialogue(SODialogueSequence sequence, Action<SDialogueLine> onLineChanged, Action onFinished)
        {
            if ((_dialoguePresenter == null) ||
                (_dialoguePresenter.IsDialoguePlaying) ||
                (_activeSettingsPresenter != null) ||
                (_playerFacade == null))
                return false;

            _dialogueLineChanged = onLineChanged;
            _dialogueFinished = onFinished;
            _dialoguePresenter.OnDialogueLineChanged += HandleDialogueLineChanged;
            _dialoguePresenter.OnDialogueEnded += HandleDialogueEnded;

            // 대화 중에는 상호작용 안내를 숨긴다.
            if (_playerHudPresenter != null)
                _playerHudPresenter.SetInteractPromptBlocked(true);

            SwitchToDialogueInput();

            if (!_dialoguePresenter.StartDialogue(sequence))
            {
                HandleDialogueEnded();
                return false;
            }

            return true;
        }

        // 대화를 끝까지 보지 않고 정리한다. 시작할 때 받은 onFinished도 호출된다.
        public void StopDialogue()
        {
            if (IsDialoguePlaying)
                _dialoguePresenter.StopDialogue();
        }

        private void HandleDialogueLineChanged(SDialogueLine line)
        {
            _dialogueLineChanged?.Invoke(line);
        }

        private void HandleDialogueEnded()
        {
            _dialoguePresenter.OnDialogueLineChanged -= HandleDialogueLineChanged;
            _dialoguePresenter.OnDialogueEnded -= HandleDialogueEnded;

            if (_playerHudPresenter != null)
                _playerHudPresenter.SetInteractPromptBlocked(false);

            if (_playerFacade != null)
                _playerFacade.SwitchToPlayerInput();

            Action onFinished = _dialogueFinished;
            _dialogueLineChanged = null;
            _dialogueFinished = null;
            onFinished?.Invoke();
        }

        private void HandleAdvanceDialogue(InputAction.CallbackContext context)
        {
            _dialoguePresenter?.ShowNextLine();
        }

        private void HandleOpenSettingsInDialogue(InputAction.CallbackContext context)
        {
            OpenSettings();
        }

        private void SwitchToDialogueInput()
        {
            if (_playerInput != null)
                _playerInput.SwitchCurrentActionMap(ACTION_MAP_DIALOGUE);
        }

        private void BindPlayerInput(PlayerFacade playerFacade)
        {
            UnbindPlayerInput();

            if (playerFacade == null) return;

            _playerFacade = playerFacade;
            _playerFacade.OnOpenSettingsRequested += OpenSettings;
            _playerFacade.OnCloseSettingsRequested += CloseSettings;

            // 대화 입력은 플레이어의 PlayerInput에서 직접 받는다.
            if (_playerFacade.TryGetComponent(out _playerInput) && (_playerInput.actions != null))
            {
                _advanceDialogueAction = _playerInput.actions.FindAction(ACTION_ADVANCE_DIALOGUE);
                _openSettingsInDialogueAction = _playerInput.actions.FindAction(ACTION_OPEN_SETTINGS_IN_DIALOGUE);

                if (_advanceDialogueAction != null) _advanceDialogueAction.performed += HandleAdvanceDialogue;
                if (_openSettingsInDialogueAction != null) _openSettingsInDialogueAction.performed += HandleOpenSettingsInDialogue;
            }

            // 설정창이 열렸거나 대화 중인 상태에서 플레이어가 교체된 경우에도 그 입력 맵을 유지.
            if (_activeSettingsPresenter != null)
            {
                _playerFacade.SwitchToUIInput();
            }
            else if (IsDialoguePlaying)
            {
                SwitchToDialogueInput();
            }
        }

        private void UnbindPlayerInput()
        {
            if (_playerFacade != null)
            {
                _playerFacade.OnOpenSettingsRequested -= OpenSettings;
                _playerFacade.OnCloseSettingsRequested -= CloseSettings;
            }

            if (_advanceDialogueAction != null) _advanceDialogueAction.performed -= HandleAdvanceDialogue;
            if (_openSettingsInDialogueAction != null) _openSettingsInDialogueAction.performed -= HandleOpenSettingsInDialogue;

            _playerFacade = null;
            _playerInput = null;
            _advanceDialogueAction = null;
            _openSettingsInDialogueAction = null;
        }

        public void OpenSettings()
        {
            if (_activeSettingsPresenter != null ||
                _settingsPresenter == null ||
                _playerFacade == null ||
                _gameManager == null)
                return;

            if (_gameManager.CurrentState != GameManager.GameState.Playing)
                return;

            // 현재 UI 지정 및 닫기 요청 연결.
            _activeSettingsPresenter = _settingsPresenter;
            _activeSettingsPresenter.CloseRequested += CloseSettings;

            // 게임 상태와 입력 전환.
            _gameManager.PauseGame();
            _playerFacade.SwitchToUIInput();

            _activeSettingsPresenter.Open();
        }

        public void CloseSettings()
        {
            if (_activeSettingsPresenter == null) return;

            SettingsPresenter presenter = _activeSettingsPresenter;
            _activeSettingsPresenter = null;

            // 닫기 요청 해제 후 설정창 닫기.
            presenter.CloseRequested -= CloseSettings;
            presenter.Close();

            // 대화 중에 연 설정창이면 대화 입력으로 돌아간다.
            if (IsDialoguePlaying)
                SwitchToDialogueInput();
            else if (_playerFacade != null)
                _playerFacade.SwitchToPlayerInput();


            if (_gameManager != null &&
                _gameManager.CurrentState == GameManager.GameState.Pause)
            {
                _gameManager.ResumeGame();
            }
        }

    }
}
