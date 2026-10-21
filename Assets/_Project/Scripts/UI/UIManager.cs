using UnityEngine;
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
        #region Serialized Fields
        // 플레이어 데이터를 UI에 보여줘야하는데 그 데이터를 조작하는 Presenter를 갖고있는다.
        [Header("References")]
        // 플레이어 HUD (HP, 스킬 쿨타임 표시)
        [SerializeField] private PlayerHudPresenter _playerHudPresenter;
        // 설정창 (볼륨 조절, 닫기)
        [SerializeField] private SettingsPresenter _settingsPresenter;
        // 펫 안내 문구 (번호로 테이블에서 찾아 패널에 표시)
        [SerializeField] private PetGuidePresenter _petGuidePresenter;
        // 대화 진행 (대화 SO를 한 줄씩 펫 안내 패널에 표시)
        [SerializeField] private DialoguePresenter _dialoguePresenter;
        // 대화 연출 (펫 세우기, 말하는 사람 카메라). 없으면 대화를 재생하지 않는다.
        [SerializeField] private DialogueDirector _dialogueDirector;
        [Tooltip("PlayDialogue(번호)로 재생할 대화 목록")]
        [SerializeField] private SODialogueTable _cutsceneTable;
        [Tooltip("대화 중 alpha를 0으로 내려 인게임 UI를 숨길 캔버스 루트의 CanvasGroup. 대화창·설정창은 Ignore Parent Groups로 계속 보인다.")]
        [SerializeField] private CanvasGroup _inGameCanvasGroup;
        #endregion

        #region Private Fields
        // 이벤트 구독용
        private GameManager _gameManager;

        // 현재 플레이어의 입력(설정창 열기·닫기, 대사 넘기기)과 입력 맵 전환을 맡는다.
        private readonly UIInputHandler _input = new UIInputHandler();

        // 설정창이 열려 있으면 true. 열린 동안에는 대화를 시작하지 않는다.
        private bool _isSettingsOpen;

        // 펫 안내가 떠 있으면 true. E로 닫을 때까지 게임을 멈춘다.
        private bool _isPetGuideOpen;
        #endregion

        #region Properties
        // 지금 씬의 UIManager. 배치하지 않은 씬이거나 Awake 전이면 null이다.
        // DontDestroyOnLoad를 쓰지 않으므로 공용 Singleton<T>을 상속하지 않는다.
        public static UIManager Instance { get; private set; }

        // 대화가 재생 중이면 true. 대화 Presenter가 없으면 항상 false다.
        public bool IsDialoguePlaying => (_dialoguePresenter != null) && _dialoguePresenter.IsDialoguePlaying;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            // 같은 씬에 둘이면 배치 실수다. (이전 씬의 UIManager가 아직 남아 있는 경우는 씬으로 구분한다)
            if ((Instance != null) && (Instance.gameObject.scene == gameObject.scene))
                Debug.LogError("[UIManager] 한 씬에 UIManager가 두 개 있습니다. 나중에 Awake된 쪽이 Instance가 됩니다.", this);

            Instance = this;
        }

        private void OnEnable()
        {
            // 입력 이벤트는 플레이어가 바뀌어도 _input이 그대로이므로 한 번만 구독한다.
            _input.OnOpenSettingsRequested += OpenSettings;
            _input.OnCloseSettingsRequested += CloseSettings;
            _input.OnNextLineRequested += HandleNextLine;

            // 1. 플레이어를 연결할 HUD가 있는지 확인한다. 없어도 설정창·대화는 동작한다.
            if (_playerHudPresenter == null)
            {
                Debug.LogWarning(
                    "PlayerHudPresenter가 연결되지 않았습니다. HUD 없이 동작합니다.", this);
            }

            // 2. 플레이어 등록을 담당하는 GameManager를 가져온다.
            _gameManager = GameManager.Instance;

            if (_gameManager == null)
            {
                Debug.LogError("씬에 GameManager가 없습니다.", this);
                return;
            }

            // 3. 앞으로 플레이어가 스폰되면
            // HandlePlayerSpawned를 실행해 달라고 등록한다. HandlePlayerSpawned 함수는 => UI를 스폰된 플레이어와 동기화 시키는 함수
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
            HidePetGuide();
            _input.UnbindPlayer();
            _input.OnOpenSettingsRequested -= OpenSettings;
            _input.OnCloseSettingsRequested -= CloseSettings;
            _input.OnNextLineRequested -= HandleNextLine;
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

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }
        #endregion

        #region Public Methods
        // 플레이어 HUD(HP, 스킬 쿨타임)를 보이거나 숨긴다.
        public void ShowPlayerHud() => _playerHudPresenter?.Show();
        public void HidePlayerHud() => _playerHudPresenter?.Hide();

        // 번호에 맞는 펫 안내 문구를 띄운다. 번호와 문구는 SOPetGuideTable에서 정한다.
        // E로 닫을 때까지 BGM만 남기고 게임을 멈추며, 입력은 Dialogue 맵(E 닫기·ESC 설정창)으로 바꾼다.
        // 대화 중이거나 설정창이 열려 있거나 이미 안내 중이거나 플레이어가 없으면 무시한다.
        public void ShowPetGuide(int id)
        {
            if ((_petGuidePresenter == null) ||
                IsDialoguePlaying ||
                _isSettingsOpen ||
                _isPetGuideOpen ||
                (!_input.HasPlayer))
                return;

            if (!_petGuidePresenter.Show(id)) return;

            _isPetGuideOpen = true;
            _gameManager?.PauseGameTime();
            _input.SwitchToDialogueInput();
        }

        // 펫 안내를 닫고 게임과 플레이어 조작을 돌려준다.
        public void HidePetGuide()
        {
            if (!_isPetGuideOpen) return;

            _isPetGuideOpen = false;
            _petGuidePresenter.Hide();
            _gameManager?.ResumeGameTime();
            _input.SwitchToPlayerInput();
        }

        // 번호에 맞는 컷씬을 지금 플레이어 자리에서 재생한다. 번호와 대화는 SODialogueTable에서 정한다.
        // 연출(펫 세우기·카메라)은 DialogueDirector가 맡는다. 본 기록은 남기지 않으므로 부를 때마다 재생된다.
        // 이미 대화 중이면 무시한다. 발판(TriggerPlate) 같은 UnityEvent에서도 연결할 수 있도록 반환값이 없다.
        public void PlayDialogue(int id)
        {
            if ((_cutsceneTable == null) || !_cutsceneTable.TryGetSequence(id, out SODialogue sequence))
            {
                Debug.LogWarning($"[UIManager] {id}번 컷씬이 테이블에 없습니다.", this);
                return;
            }

            if (_dialogueDirector == null)
            {
                Debug.LogWarning("[UIManager] DialogueDirector가 연결되지 않아 컷씬을 재생하지 못했습니다.", this);
                return;
            }

            StartDialogue(sequence);
        }

        // 대화를 끝까지 보지 않고 정리한다. 연출과 입력도 대화가 끝났을 때처럼 돌려놓는다.
        public void StopDialogue()
        {
            if (IsDialoguePlaying)
                _dialoguePresenter.StopDialogue();
        }

        // 설정창을 열고 게임을 일시정지한다. 입력은 UI 맵으로 바꾼다.
        // 이미 열려 있거나, 플레이어가 없거나, 게임이 Playing 상태가 아니면 무시한다.
        public void OpenSettings()
        {
            if (_isSettingsOpen ||
                _settingsPresenter == null ||
                !_input.HasPlayer ||
                _gameManager == null)
                return;

            if (_gameManager.CurrentState != GameManager.GameState.Playing)
                return;

            // 현재 UI 지정 및 닫기 요청 연결.
            _isSettingsOpen = true;
            _settingsPresenter.CloseRequested += CloseSettings;

            // 게임 상태와 입력 전환.
            _gameManager.PauseGame();
            _input.SwitchToUIInput();

            _settingsPresenter.Open();
        }

        // 설정창을 닫고 게임을 다시 진행한다. 입력은 대화 중이면 대화 맵, 아니면 플레이어 맵으로 돌린다.
        public void CloseSettings()
        {
            if (!_isSettingsOpen) return;

            _isSettingsOpen = false;

            // 닫기 요청 해제 후 설정창 닫기.
            _settingsPresenter.CloseRequested -= CloseSettings;
            _settingsPresenter.Close();

            // 대화·펫 안내 중에 연 설정창이면 대화 입력으로 돌아간다.
            if (IsDialoguePlaying || _isPetGuideOpen)
                _input.SwitchToDialogueInput();
            else
                _input.SwitchToPlayerInput();

            if (_gameManager != null &&
                _gameManager.CurrentState == GameManager.GameState.Pause)
            {
                _gameManager.ResumeGame();

                // 대화·펫 안내 중에 연 설정창이면 그것이 끝날 때까지 다시 멈춘다.
                if (IsDialoguePlaying || _isPetGuideOpen)
                    _gameManager.PauseGameTime();
            }
        }
        #endregion

        #region Private Methods
        // 플레이어가 스폰되거나 바뀌면 HUD와 입력을 새 플레이어에 연결한다. null이면 연결만 해제한다.
        private void HandlePlayerSpawned(PlayerFacade playerFacade)
        {
            // 전달받은 플레이어를 HUD에 연결한다.
            _playerHudPresenter?.SetPlayer(playerFacade);
            _input.BindPlayer(playerFacade);

            // 설정창이 열렸거나 대화·펫 안내 중인 상태에서 플레이어가 교체된 경우에도 그 입력 맵을 유지.
            if (_isSettingsOpen)
                _input.SwitchToUIInput();
            else if (IsDialoguePlaying || _isPetGuideOpen)
                _input.SwitchToDialogueInput();
        }

        // 대화를 시작한다. 끝날 때까지 Dialogue 입력 맵으로 바꿔 플레이어 조작을 막고, 대화창 외 UI를 숨긴다.
        // Dialogue 맵에서는 E로 대사를 넘기고, ESC로 설정창을 연다.
        // 이미 대화 중이거나 설정창·펫 안내가 열려 있거나 플레이어가 없으면 시작하지 않는다.
        private void StartDialogue(SODialogue sequence)
        {
            if ((_dialoguePresenter == null) ||
                (_dialoguePresenter.IsDialoguePlaying) ||
                _isSettingsOpen ||
                _isPetGuideOpen ||
                (!_input.HasPlayer))
                return;

            _dialoguePresenter.OnDialogueLineChanged += HandleDialogueLineChanged;
            _dialoguePresenter.OnDialogueEnded += HandleDialogueEnded;

            // 첫 줄의 카메라가 대화 시작과 동시에 정해지므로 펫과 카메라를 먼저 배치한다.
            _dialogueDirector.BeginStaging();

            // 대화 동안 플레이어·펫 대기 애니메이션과 BGM만 남기고 게임을 멈춘다.
            _gameManager?.PauseGameTime();

            // 대화 중에는 상호작용 안내를 숨긴다.
            if (_playerHudPresenter != null)
                _playerHudPresenter.SetInteractLabelBlocked(true);

            // 대화 중에는 대화창만 보이도록 나머지 인게임 UI를 숨긴다.
            if (_inGameCanvasGroup != null)
                _inGameCanvasGroup.alpha = 0.0f;

            _input.SwitchToDialogueInput();

            if (!_dialoguePresenter.StartDialogue(sequence))
                HandleDialogueEnded();
        }

        // 대사 줄이 바뀌면 그 줄의 말하는 사람·카메라 번호로 연출 카메라를 바꾼다.
        private void HandleDialogueLineChanged(SDialogueLine line)
        {
            if (_dialogueDirector != null)
                _dialogueDirector.ShowLine(line);
        }

        // 대화가 끝나면 구독을 풀고, 연출을 정리한 뒤 상호작용 안내·숨긴 UI와 플레이어 조작을 돌려준다.
        private void HandleDialogueEnded()
        {
            _dialoguePresenter.OnDialogueLineChanged -= HandleDialogueLineChanged;
            _dialoguePresenter.OnDialogueEnded -= HandleDialogueEnded;

            _gameManager?.ResumeGameTime();

            if (_dialogueDirector != null)
                _dialogueDirector.EndStaging();

            if (_playerHudPresenter != null)
                _playerHudPresenter.SetInteractLabelBlocked(false);

            if (_inGameCanvasGroup != null)
                _inGameCanvasGroup.alpha = 1.0f;

            _input.SwitchToPlayerInput();
        }

        // 대화 맵 E: 펫 안내 중이면 안내를 닫고, 아니면 다음 대사로 넘긴다.
        private void HandleNextLine()
        {
            if (_isPetGuideOpen)
            {
                HidePetGuide();
                return;
            }

            _dialoguePresenter?.ShowNextLine();
        }
        #endregion
    }
}
