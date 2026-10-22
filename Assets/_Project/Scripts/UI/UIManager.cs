using System.Collections.Generic;
using UnityEngine;
using Core;
using Core.InputMap;
using Facade.Player;
using UI.PlayerHud;
using UI.Settings;
using UIandCinematic;

namespace UI
{
    // 대화를 맡는 쪽(ICutsceneController)으로 등록되어 CinematicManager·StageManager가 UIManager를 직접 참조하지 않고 대화를 튼다.
    public class UIManager : MonoBehaviour, ICutsceneController
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
        [SerializeField] private CutscenePresenter _cutscenePresenter;
        // 대화 연출 (펫 세우기, 말하는 사람 카메라, UIandCinematic). 없으면 대화를 재생하지 않는다.
        [SerializeField] private CutsceneDirector _cutsceneDirector;
        [Tooltip("PlayDialogue(번호)로 재생할 대화 목록")]
        [SerializeField] private SODialogueTable _cutsceneTable;
        // 독백 진행 (게임을 멈추지 않고 대사를 펫 안내 패널에 띄운 뒤, 게이지가 다 줄면 숨긴다)
        [SerializeField] private MonologuePresenter _monologuePresenter;
        [Tooltip("PlayMonologue(번호)로 재생할 독백 목록. 독백마다 한 줄을 보여 주는 시간을 정할 수 있다")]
        [SerializeField] private SOMonologueTable _monologueTable;
        [Tooltip("대화 중 alpha를 0으로 내려 인게임 UI를 숨길 캔버스 루트의 CanvasGroup. 대화창·설정창은 Ignore Parent Groups로 계속 보인다.")]
        [SerializeField] private CanvasGroup _inGameCanvasGroup;
        #endregion

        #region Private Fields
        // 이벤트 구독용
        private GameManager _gameManager;

        // 현재 플레이어의 입력(설정창 열기·닫기, 대사 넘기기)을 받는다. 입력 맵은 InputMapController에 요청한다.
        private readonly UIInputHandler _input = new UIInputHandler();

        // 설정창이 열려 있으면 true. 열린 동안에는 대화를 시작하지 않는다.
        private bool _isSettingsOpen;

        // 펫 안내가 떠 있으면 true. E로 닫을 때까지 게임을 멈춘다.
        private bool _isPetGuideOpen;

        // 컷씬 대사로 펫을 세워 두었으면 true. 컷씬이 끝날 때 돌려놓는다.
        private bool _isCutsceneStaged;

        // 컷씬 대사가 패널에 떠 있으면 true.
        private bool _isCutsceneLineVisible;

        // 컷씬 대사가 E로 넘기기를 기다리는 중이면 true.
        private bool _isCutsceneLineWaiting;

        // 인게임 UI를 숨겨 달라고 한 쪽들(대화, 카메라 연출). 하나라도 남아 있으면 숨긴다.
        private readonly HashSet<Object> _inGameUIHiders = new();
        #endregion

        #region Properties
        // 지금 씬의 UIManager. 배치하지 않은 씬이거나 Awake 전이면 null이다.
        // DontDestroyOnLoad를 쓰지 않으므로 공용 Singleton<T>을 상속하지 않는다.
        public static UIManager Instance { get; private set; }

        // 대화가 재생 중이면 true. 대화 Presenter가 없으면 항상 false다.
        public bool IsDialoguePlaying => (_cutscenePresenter != null) && _cutscenePresenter.IsDialoguePlaying;

        // 펫 안내가 떠 있으면 true.
        public bool IsPetGuideOpen => _isPetGuideOpen;

        // 컷씬 대사 한 줄이 E로 넘기기를 기다리는 중이면 true.
        public bool IsCutsceneLineWaiting => _isCutsceneLineWaiting;

        // 카메라 연출이 재생 중이면 true. 연출 중에는 펫 안내·독백·밖에서 부른 대화를 시작하지 않는다.
        private bool IsCinematicPlaying => (CinematicManager.Instance != null) && CinematicManager.Instance.IsPlaying;
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
            CutsceneController.Register(this);

            // 입력 이벤트는 플레이어가 바뀌어도 _input이 그대로이므로 한 번만 구독한다.
            _input.OnOpenSettingsRequested += OpenSettings;
            _input.OnCloseSettingsRequested += CloseSettings;
            _input.OnNextLineRequested += HandleNextLine;

            // 카메라 연출 동안 인게임 UI를 숨긴다. 이미 연출 중에 켜졌으면 바로 숨긴다.
            CinematicManager.OnPlayStarted += HandleCinematicStarted;
            CinematicManager.OnPlayFinished += HandleCinematicFinished;
            if (IsCinematicPlaying)
                HandleCinematicStarted(CinematicManager.Instance);

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
            EndCutscene();
            HidePetGuide();
            StopMonologue();
            _input.UnbindPlayer();
            _input.OnOpenSettingsRequested -= OpenSettings;
            _input.OnCloseSettingsRequested -= CloseSettings;
            _input.OnNextLineRequested -= HandleNextLine;

            // 꺼진 동안에는 연출 알림을 받지 못하므로 숨긴 UI를 돌려 두고, 다시 켜질 때 연출 중이면 다시 숨긴다.
            CinematicManager.OnPlayStarted -= HandleCinematicStarted;
            CinematicManager.OnPlayFinished -= HandleCinematicFinished;
            _inGameUIHiders.Clear();
            ApplyInGameUIVisibility();

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

            CutsceneController.Unregister(this);
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
        // E로 닫을 때까지 BGM만 남기고 게임을 멈추며, 입력은 Cutscene 맵(E 닫기·ESC 설정창)으로 바꾼다.
        // 대화·연출 중이거나 설정창이 열려 있거나 이미 안내 중이거나 플레이어가 없으면 무시한다.
        public void ShowPetGuide(int id)
        {
            if ((_petGuidePresenter == null) ||
                IsDialoguePlaying ||
                _isSettingsOpen ||
                _isPetGuideOpen ||
                IsCinematicPlaying ||
                (!_input.HasPlayer))
                return;

            // 같은 패널을 쓰므로 독백은 끄고 안내를 띄운다.
            StopMonologue();

            if (!_petGuidePresenter.Show(id)) return;

            _isPetGuideOpen = true;
            _gameManager?.PauseGameTime(this);
            InputMapController.Instance?.RequestMap(_petGuidePresenter, EInputMapType.Cutscene);
        }

        // 펫 안내를 닫고 게임과 플레이어 조작을 돌려준다.
        public void HidePetGuide()
        {
            if (!_isPetGuideOpen) return;

            _isPetGuideOpen = false;
            _petGuidePresenter.Hide();
            _gameManager?.ResumeGameTime(this);
            InputMapController.Instance?.ReleaseMap(_petGuidePresenter);
        }

        // 번호에 맞는 컷씬을 지금 플레이어 자리에서 재생한다. 번호와 대화는 SODialogueTable에서 정한다.
        // 연출(펫 세우기·카메라)은 CutsceneDirector가 맡는다. 본 기록은 남기지 않으므로 부를 때마다 재생된다.
        // 이미 대화 중이면 무시한다. 발판(TriggerPlate) 같은 UnityEvent에서도 연결할 수 있도록 반환값이 없다.
        // 카메라 연출 중에는 재생하지 않는다. 연출의 대화 단계는 PlayCinematicDialogue로 연다.
        public void PlayDialogue(int id)
        {
            if (IsCinematicPlaying)
            {
                Debug.LogWarning($"[UIManager] 카메라 연출 중이라 {id}번 컷씬을 재생하지 않았습니다.", this);
                return;
            }

            PlayDialogueById(id);
        }

        // 카메라 연출의 대화 단계에서 CinematicManager가 부른다. 연출 중에도 재생한다.
        public void PlayCinematicDialogue(int id) => PlayDialogueById(id);

        // 대화를 끝까지 보지 않고 정리한다. 연출과 입력도 대화가 끝났을 때처럼 돌려놓는다.
        public void StopDialogue()
        {
            if (IsDialoguePlaying)
                _cutscenePresenter.StopDialogue();
        }

        // 컷씬(SOCutscene)의 대사 한 줄을 띄운다. CinematicManager가 대사 단계마다 부르고, E를 누르면 IsCutsceneLineWaiting이 false가 된다.
        // 처음 부를 때 펫을 플레이어 앞에 세운다. 게임 시간·입력 맵·UI 숨김은 연출(CinematicManager)이 맡는다.
        // shouldUseSpeakerCamera가 false면 카메라 키로 고른 카메라가 보이도록 말하는 사람 카메라를 끈다.
        public bool ShowCutsceneLine(
            string speakerName,
            Sprite portrait,
            string message,
            bool isLastLine,
            bool shouldUseSpeakerCamera)
        {
            if ((_cutscenePresenter == null) || (_cutsceneDirector == null) || IsDialoguePlaying)
                return false;

            // 같은 패널을 쓰므로 독백은 끄고 대사를 띄운다.
            StopMonologue();

            // 첫 대사의 말하는 사람 카메라가 바로 정해지므로 펫과 카메라를 먼저 배치한다.
            if (!_isCutsceneStaged)
            {
                _cutsceneDirector.BeginStaging();
                _isCutsceneStaged = true;
            }

            if (!_cutscenePresenter.ShowSingleLine(speakerName, portrait, message, isLastLine))
                return false;

            _isCutsceneLineVisible = true;
            _isCutsceneLineWaiting = true;

            if (shouldUseSpeakerCamera)
                _cutsceneDirector.ShowLine(speakerName, -1);
            else
                _cutsceneDirector.HideCameras();

            return true;
        }

        // 컷씬 대사를 숨기고 말하는 사람 카메라를 끈다. 카메라 단계의 샷 카메라를 말하는 사람 카메라가 덮지 않게 한다.
        public void HideCutsceneLine()
        {
            _isCutsceneLineWaiting = false;

            if (_isCutsceneLineVisible)
            {
                _isCutsceneLineVisible = false;
                _cutscenePresenter.HideSingleLine();
            }

            if (_isCutsceneStaged)
                _cutsceneDirector.HideCameras();
        }

        // 컷씬 대사를 숨기고 세워 둔 펫을 돌려놓는다. 컷씬 대사를 띄운 적이 없으면 아무것도 하지 않는다.
        public void EndCutscene()
        {
            HideCutsceneLine();

            if (!_isCutsceneStaged) return;

            _isCutsceneStaged = false;
            _cutsceneDirector.EndStaging();
        }

        // 번호에 맞는 독백을 띄운다. 번호는 SOMonologueTable, 대사·한 줄 시간은 SOMonologue에서 정한다.
        // 게임 시간·입력·카메라는 그대로 두고, 줄마다 정해진 시간이 지나면 다음 줄로 넘어가 마지막 줄 뒤에 저절로 사라진다.
        // 독백 중에 부르면 새 독백으로 바꾼다. 컷씬·펫 안내가 같은 패널을 쓰는 중이면 무시한다.
        // 발판(TriggerPlate) 같은 UnityEvent에서도 연결할 수 있도록 반환값이 없다.
        public void PlayMonologue(int id)
        {
            if ((_monologueTable == null) ||
                !_monologueTable.TryGetMonologue(id, out SOMonologue monologue))
            {
                Debug.LogWarning($"[UIManager] {id}번 독백이 테이블에 없습니다.", this);
                return;
            }

            if ((_monologuePresenter == null) ||
                IsDialoguePlaying ||
                _isPetGuideOpen ||
                IsCinematicPlaying)
                return;

            _monologuePresenter.Play(monologue);
        }

        // 독백을 바로 끝내고 패널을 숨긴다.
        public void StopMonologue() => _monologuePresenter?.Stop();

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
            InputMapController.Instance?.RequestMap(_settingsPresenter, EInputMapType.UI);

            _settingsPresenter.Open();
        }

        // 설정창을 닫고 게임을 다시 진행한다. 입력은 설정창 요청만 풀므로, 대화·펫 안내·연출 중이면 그쪽 요청대로 대화 맵이 된다.
        public void CloseSettings()
        {
            if (!_isSettingsOpen) return;

            _isSettingsOpen = false;

            // 닫기 요청 해제 후 설정창 닫기.
            _settingsPresenter.CloseRequested -= CloseSettings;
            _settingsPresenter.Close();

            InputMapController.Instance?.ReleaseMap(_settingsPresenter);

            // 대화·펫 안내·연출 중에 연 설정창이면 그쪽 시간 정지 요청이 남아 있어 시간은 멈춘 채로 있다.
            if (_gameManager != null &&
                _gameManager.CurrentState == GameManager.GameState.Pause)
                _gameManager.ResumeGame();
        }
        #endregion

        #region Private Methods
        // 플레이어가 스폰되거나 바뀌면 HUD와 입력을 새 플레이어에 연결한다. null이면 연결만 해제한다.
        // 새 플레이어의 입력 맵은 InputMapController가 지금 요청대로 다시 적용한다.
        private void HandlePlayerSpawned(PlayerFacade playerFacade)
        {
            // 전달받은 플레이어를 HUD에 연결한다.
            _playerHudPresenter?.SetPlayer(playerFacade);
            _input.BindPlayer(playerFacade);
        }

        // 번호에 맞는 대화를 테이블에서 찾아 시작한다.
        private void PlayDialogueById(int id)
        {
            if ((_cutsceneTable == null) || !_cutsceneTable.TryGetSequence(id, out SODialogue sequence))
            {
                Debug.LogWarning($"[UIManager] {id}번 컷씬이 테이블에 없습니다.", this);
                return;
            }

            if (_cutsceneDirector == null)
            {
                Debug.LogWarning("[UIManager] CutsceneDirector가 연결되지 않아 컷씬을 재생하지 못했습니다.", this);
                return;
            }

            StartDialogue(sequence);
        }

        // 대화를 시작한다. 끝날 때까지 Cutscene 입력 맵으로 바꿔 플레이어 조작을 막고, 대화창 외 UI를 숨긴다.
        // Cutscene 맵에서는 E로 대사를 넘기고, ESC로 설정창을 연다.
        // 이미 대화 중이거나 설정창·펫 안내가 열려 있거나 플레이어가 없으면 시작하지 않는다.
        private void StartDialogue(SODialogue sequence)
        {
            if ((_cutscenePresenter == null) ||
                (_cutscenePresenter.IsDialoguePlaying) ||
                _isSettingsOpen ||
                _isPetGuideOpen ||
                (!_input.HasPlayer))
                return;

            // 같은 패널을 쓰므로 독백은 끄고 대화를 띄운다.
            StopMonologue();

            _cutscenePresenter.OnDialogueLineChanged += HandleDialogueLineChanged;
            _cutscenePresenter.OnDialogueEnded += HandleDialogueEnded;

            // 첫 줄의 카메라가 대화 시작과 동시에 정해지므로 펫과 카메라를 먼저 배치한다.
            _cutsceneDirector.BeginStaging();

            // 대화 동안 플레이어·펫 대기 애니메이션과 BGM만 남기고 게임을 멈춘다.
            _gameManager?.PauseGameTime(this);

            // 대화 중에는 대화창만 보이도록 상호작용 안내와 나머지 인게임 UI를 숨긴다.
            HideInGameUI(_cutscenePresenter);

            InputMapController.Instance?.RequestMap(_cutscenePresenter, EInputMapType.Cutscene);

            if (!_cutscenePresenter.StartDialogue(sequence))
                HandleDialogueEnded();
        }

        // 대사 줄이 바뀌면 그 줄의 말하는 사람·카메라 번호로 연출 카메라를 바꾼다.
        private void HandleDialogueLineChanged(SDialogueLine line)
        {
            if (_cutsceneDirector != null)
                _cutsceneDirector.ShowLine(line.SpeakerName, line.CameraIndex);
        }

        // 대화가 끝나면 구독을 풀고, 연출을 정리한 뒤 상호작용 안내·숨긴 UI와 플레이어 조작을 돌려준다.
        // 게임 시간·입력 맵·UI 숨김은 대화의 요청만 푼다. 카메라 연출 안에서 연 대화면 연출의 요청이 남아 시간은 멈추고 대화 맵·UI 숨김이 유지된다.
        private void HandleDialogueEnded()
        {
            _cutscenePresenter.OnDialogueLineChanged -= HandleDialogueLineChanged;
            _cutscenePresenter.OnDialogueEnded -= HandleDialogueEnded;

            _gameManager?.ResumeGameTime(this);
            InputMapController.Instance?.ReleaseMap(_cutscenePresenter);

            if (_cutsceneDirector != null)
                _cutsceneDirector.EndStaging();

            ShowInGameUI(_cutscenePresenter);
        }

        // 카메라 연출이 시작되면 인게임 UI를 숨긴다. 같은 패널을 쓰는 독백이 연출 화면에 남지 않도록 끈다.
        private void HandleCinematicStarted(CinematicManager cinematicManager)
        {
            StopMonologue();
            HideInGameUI(cinematicManager);
        }

        // 카메라 연출이 끝나면 숨긴 인게임 UI를 돌려준다.
        private void HandleCinematicFinished(CinematicManager cinematicManager)
        {
            ShowInGameUI(cinematicManager);
        }

        // hider 이름으로 인게임 UI를 숨긴다.
        private void HideInGameUI(Object hider)
        {
            if (hider == null) return;

            _inGameUIHiders.Add(hider);
            ApplyInGameUIVisibility();
        }

        // hider의 숨김 요청을 푼다. 다른 쪽이 아직 숨기고 있으면 숨긴 채로 둔다.
        private void ShowInGameUI(Object hider)
        {
            if ((hider == null) || !_inGameUIHiders.Remove(hider)) return;

            ApplyInGameUIVisibility();
        }

        // 숨김 요청이 하나라도 있으면 상호작용 안내와 인게임 UI를 숨기고, 없으면 보인다.
        private void ApplyInGameUIVisibility()
        {
            bool shouldHide = _inGameUIHiders.Count > 0;

            if (_playerHudPresenter != null)
                _playerHudPresenter.SetInteractLabelBlocked(shouldHide);

            if (_inGameCanvasGroup != null)
                _inGameCanvasGroup.alpha = shouldHide ? 0.0f : 1.0f;
        }

        // 대화 맵 E: 펫 안내 중이면 안내를 닫고, 컷씬 대사면 다음 단계로 넘기고, 아니면 다음 대사로 넘긴다.
        private void HandleNextLine()
        {
            if (_isPetGuideOpen)
            {
                HidePetGuide();
                return;
            }

            // 패널은 다음 단계가 새 대사로 바꾸거나 숨길 때까지 그대로 둔다.
            if (_isCutsceneLineWaiting)
            {
                _isCutsceneLineWaiting = false;
                return;
            }

            _cutscenePresenter?.ShowNextLine();
        }
        #endregion
    }
}
