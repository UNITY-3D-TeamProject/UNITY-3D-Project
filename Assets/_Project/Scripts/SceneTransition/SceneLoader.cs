using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SceneTransition
{
    /// <summary>
    /// 정해진 순서대로만 씬을 전환하고, 전환 직전에 이벤트를 발행한다.
    /// 순서: 튜토리얼 → 로비 → 피드 → 로비 → 파일 → 로비 → 보안 → 로비 → 라이브.
    /// 현재 단계는 처음 사용할 때 활성 씬의 이름으로 정한다.
    /// 씬은 Build Settings 번호가 아니라 이름으로 불러오므로, 메인 메뉴를 0번에 두는 등 순서를 바꿔도 된다.
    /// (로비는 순서에 여러 번 나오므로, 로비에서 바로 플레이를 시작하면 첫 번째 로비로 본다)
    /// 메인 메뉴처럼 순서에 없는 씬에서는 TryStartNewGame으로 순서를 처음부터 시작한다.
    /// 전환할 때는 로딩씬을 지금 씬 위에 겹쳐 띄우고, 로딩씬의 LoadingScreen이 지금 씬을 정리한 뒤 목적지 씬을 비동기로 불러온다.
    /// </summary>
    public static class SceneLoader
    {
        #region Constants
        // Build Settings 맨 끝에 등록한 로딩씬 이름.
        // ESceneType에 넣지 않아서 포탈 목적지로 고를 수 없게 한다.
        private const string LOADING_SCENE_NAME = "Loading";
        private const string MAIN_MENU_SCENE_NAME = "MainScene";

        private const int CURRENT_SAVE_VERSION = 1;
        private const string SAVE_VERSION_KEY = "GameProgress.SaveVersion";
        private const string SEQUENCE_STEP_KEY = "GameProgress.SequenceStep";
        private const string PLAYER_HP_KEY = "GameProgress.CurrentHp";
        private const string PLAYER_BATTERY_KEY = "GameProgress.CurrentBattery";
        #endregion

        #region Private Fields
        // ESceneType 값 순서대로 쓴 씬 이름. ESceneType에 씬을 추가하면 여기도 추가한다.
        private static readonly string[] SceneNames =
        {
            "Map_0_Tutorial",
            "Map_1_Lobby",
            "Map_2_Feed",
            "Map_3_File",
            "Map_4_Security",
            "Map_5_Live",
        };

        private static readonly ESceneType[] Sequence =
        {
            ESceneType.Tutorial,
            ESceneType.Lobby,
            ESceneType.Feed,
            ESceneType.Lobby,
            ESceneType.File,
            ESceneType.Lobby,
            ESceneType.Security,
            ESceneType.Lobby,
            ESceneType.Live,
        };

        private static int  _step;
        private static bool _hasStep;
        private static bool _isLoading;
        private static bool _isCampaignSession;
        private static bool _hasContinueRequest;
        #endregion

        #region Properties
        /// <summary>씬을 불러오는 중인지 여부. 로딩씬에 들어간 뒤 목적지 씬 로드가 끝날 때까지 true.</summary>
        public static bool IsLoading => _isLoading;

        /// <summary>이어 시작할 수 있는 정상적인 진행 저장이 있는지 여부.</summary>
        public static bool HasSavedProgress => TryGetSavedStep(out _);

        /// <summary>
        /// 지금 씬으로 넘어오기 직전에 있던 씬. 플레이를 시작한 뒤 한 번도 전환하지 않았으면 null.
        /// </summary>
        public static ESceneType? PreviousScene { get; private set; }

        /// <summary>
        /// 로딩씬이 불러갈 목적지 씬. 전환 중이 아니면 null.
        /// </summary>
        public static ESceneType? PendingScene { get; private set; }
        #endregion

        #region Events
        /// <summary>
        /// 씬 전환 직전에 (이전 씬, 목적지 씬)과 함께 호출된다.
        /// 플레이어 데이터 저장, 이전 씬 번호 기록 등에 사용한다.
        /// </summary>
        public static event Action<ESceneType, ESceneType> OnBeforeSceneChange;
        #endregion

        #region Public Methods
        /// <summary>
        /// 지금 단계에서 destination으로 넘어갈 수 있는지 확인한다.
        /// </summary>
        /// <param name="destination">가려는 씬</param>
        /// <returns>순서상 다음 씬이면 true</returns>
        public static bool CanEnter(ESceneType destination)
        {
            EnsureStep();

            bool hasNextStep = (_step >= 0) && (_step < Sequence.Length - 1);
            return hasNextStep && (Sequence[_step + 1] == destination);
        }

        /// <summary>
        /// 순서상 다음 씬일 때만 전환 직전 이벤트를 발행하고 로딩씬으로 넘어간다.
        /// 목적지 씬은 로딩씬에서 불러온다.
        /// </summary>
        /// <param name="destination">가려는 씬</param>
        /// <returns>전환을 시작했으면 true</returns>
        public static bool TryLoad(ESceneType destination)
        {
            if (_isLoading) return false;

            if (!CanEnter(destination))
            {
                Debug.LogWarning($"[SceneLoader] 지금 단계에서는 {destination} 씬으로 갈 수 없습니다.");
                return false;
            }

            if (!IsLoadable(destination)) return false;

            ESceneType previous = Sequence[_step];
            _step++;
            _isLoading = true;
            PreviousScene = previous;

            OnBeforeSceneChange?.Invoke(previous, destination);

            // 로딩씬은 지금 씬 위에 겹쳐 띄운다. 지금 씬 정리와 목적지 불러오기는 로딩씬의 LoadingScreen이 한다.
            // _isLoading은 목적지 로드가 끝날 때 푼다.
            PendingScene = destination;
            SceneManager.LoadSceneAsync(LOADING_SCENE_NAME, LoadSceneMode.Additive);
            return true;
        }

        /// <summary>
        /// 새 게임을 시작한다. 순서를 처음(튜토리얼)으로 되돌리고 로딩씬을 거쳐 튜토리얼을 연다.
        /// 메인 메뉴처럼 순서에 없는 씬에서 호출한다. 이전 씬이 없으므로 전환 직전 이벤트는 발행하지 않는다.
        /// </summary>
        /// <returns>전환을 시작했으면 true</returns>
        public static bool TryStartNewGame()
        {
            if (_isLoading) return false;

            _hasContinueRequest = false;
            if (!TryStartAtStep(0)) return false;

            DeleteSavedProgress();
            return true;
        }

        /// <summary>
        /// 저장된 진행 순번의 씬을 처음 상태로 불러온다.
        /// 플레이어의 체력·배터리는 복원하고, 위치·컷씬·라운드 상태는 복원하지 않는다.
        /// </summary>
        /// <returns>이어하기를 시작했으면 true</returns>
        public static bool TryContinueGame()
        {
            if (_isLoading) return false;

            if (!TryGetSavedStep(out int savedStep))
            {
                Debug.LogWarning("[SceneLoader] 이어서 시작할 진행 저장이 없습니다.");
                return false;
            }

            bool hasStarted = TryStartAtStep(savedStep);
            _hasContinueRequest = hasStarted;
            return hasStarted;
        }

        /// <summary>
        /// 캠페인에서 마지막으로 정상 진입한 씬 순번을 저장한다.
        /// 게임 씬을 에디터에서 직접 실행한 경우에는 기존 저장을 덮어쓰지 않는다.
        /// </summary>
        public static void SaveCurrentProgress()
        {
            bool canSave = _isCampaignSession &&
                _hasStep &&
                (_step >= 0) &&
                (_step < Sequence.Length);

            if (!canSave) return;

            PlayerPrefs.SetInt(SAVE_VERSION_KEY, CURRENT_SAVE_VERSION);
            PlayerPrefs.SetInt(SEQUENCE_STEP_KEY, _step);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// 캠페인의 현재 플레이어 체력과 배터리를 저장한다.
        /// 게임 씬을 에디터에서 직접 실행한 경우에는 기존 저장을 덮어쓰지 않는다.
        /// </summary>
        public static void SavePlayerAttributes(float currentHp, float currentBattery)
        {
            if (!_isCampaignSession) return;

            PlayerPrefs.SetFloat(PLAYER_HP_KEY, currentHp);
            PlayerPrefs.SetFloat(PLAYER_BATTERY_KEY, currentBattery);
            PlayerPrefs.Save();
        }

        /// <summary>이어하기에 사용할 체력과 배터리를 읽는다.</summary>
        /// <returns>정상적인 생존 상태의 저장값이 있으면 true</returns>
        public static bool TryLoadPlayerAttributes(out float currentHp, out float currentBattery)
        {
            currentHp = 0f;
            currentBattery = 0f;

            if (!TryGetSavedStep(out _) ||
                !PlayerPrefs.HasKey(PLAYER_HP_KEY) ||
                !PlayerPrefs.HasKey(PLAYER_BATTERY_KEY))
            {
                return false;
            }

            currentHp = PlayerPrefs.GetFloat(PLAYER_HP_KEY);
            currentBattery = PlayerPrefs.GetFloat(PLAYER_BATTERY_KEY);
            return currentHp > 0f;
        }

        /// <summary>이번 씬 진입이 이어하기로 시작됐는지 한 번만 확인한다.</summary>
        public static bool ConsumeContinueRequest()
        {
            if (!_hasContinueRequest) return false;

            _hasContinueRequest = false;
            return true;
        }

        /// <summary>
        /// 현재 진행을 저장하고 SceneLoader 런타임 상태를 비운 뒤 메인 메뉴로 이동한다.
        /// </summary>
        /// <returns>메인 메뉴를 불러왔으면 true</returns>
        public static bool TryLoadMainMenu()
        {
            if (_isLoading) return false;

            if (!Application.CanStreamedLevelBeLoaded(MAIN_MENU_SCENE_NAME))
            {
                Debug.LogError($"[SceneLoader] Build Settings에 {MAIN_MENU_SCENE_NAME} 씬이 등록되지 않았습니다.");
                return false;
            }

            SaveCurrentProgress();
            ResetSessionState();
            SceneManager.LoadScene(MAIN_MENU_SCENE_NAME);
            return true;
        }

        /// <summary>
        /// 로딩씬에서 호출한다. 기다리던 목적지 씬을 비동기로 불러오기 시작한다.
        /// 활성화를 막아 둔 채로 반환하므로, 호출한 쪽에서 allowSceneActivation을 true로 바꿔야 씬이 넘어간다.
        /// </summary>
        /// <returns>불러오기 작업. 기다리는 목적지가 없으면 null</returns>
        public static AsyncOperation LoadPendingSceneAsync()
        {
            if (!PendingScene.HasValue) return null;

            AsyncOperation operation = SceneManager.LoadSceneAsync(SceneNames[(int)PendingScene.Value]);
            operation.allowSceneActivation = false;
            operation.completed += _ =>
            {
                _isLoading = false;
                PendingScene = null;
                SaveCurrentProgress();
            };
            return operation;
        }
        #endregion

        #region Private Methods
        // 도메인 리로드 없이 플레이 모드에 들어가도 이전 플레이의 값이 남지 않도록 초기화한다.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetState()
        {
            ResetSessionState();
            OnBeforeSceneChange = null;
        }

        // 메인 메뉴로 돌아갈 때 디스크의 진행 저장은 남기고 현재 플레이 세션 값만 비운다.
        private static void ResetSessionState()
        {
            _step = 0;
            _hasStep = false;
            _isLoading = false;
            _isCampaignSession = false;
            _hasContinueRequest = false;
            PreviousScene = null;
            PendingScene = null;
        }

        private static bool TryGetSavedStep(out int savedStep)
        {
            savedStep = -1;

            if (!PlayerPrefs.HasKey(SAVE_VERSION_KEY) ||
                !PlayerPrefs.HasKey(SEQUENCE_STEP_KEY))
            {
                return false;
            }

            int saveVersion = PlayerPrefs.GetInt(SAVE_VERSION_KEY);
            int step = PlayerPrefs.GetInt(SEQUENCE_STEP_KEY);
            bool isValid = (saveVersion == CURRENT_SAVE_VERSION) &&
                (step >= 0) &&
                (step < Sequence.Length);

            if (!isValid) return false;

            savedStep = step;
            return true;
        }

        private static void DeleteSavedProgress()
        {
            PlayerPrefs.DeleteKey(SAVE_VERSION_KEY);
            PlayerPrefs.DeleteKey(SEQUENCE_STEP_KEY);
            PlayerPrefs.DeleteKey(PLAYER_HP_KEY);
            PlayerPrefs.DeleteKey(PLAYER_BATTERY_KEY);
            PlayerPrefs.Save();
        }

        private static bool TryStartAtStep(int step)
        {
            if ((step < 0) || (step >= Sequence.Length)) return false;

            ESceneType destination = Sequence[step];
            if (!IsLoadable(destination)) return false;

            _step = step;
            _hasStep = true;
            _isLoading = true;
            _isCampaignSession = true;
            // 출발 씬이 없으므로 컷씬도 없다.
            PreviousScene = null;

            PendingScene = destination;
            SceneManager.LoadSceneAsync(LOADING_SCENE_NAME, LoadSceneMode.Additive);
            return true;
        }

        // 목적지와 로딩씬이 Build Settings에 등록돼 있는지 확인한다.
        // 로딩씬이 없으면 _isLoading이 풀리지 않고 멈추므로 상태를 바꾸기 전에 확인한다.
        private static bool IsLoadable(ESceneType destination)
        {
            string sceneName = SceneNames[(int)destination];
            if (!Application.CanStreamedLevelBeLoaded(sceneName))
            {
                Debug.LogError($"[SceneLoader] Build Settings에 {destination} 씬({sceneName})이 등록되지 않았습니다.");
                return false;
            }

            if (!Application.CanStreamedLevelBeLoaded(LOADING_SCENE_NAME))
            {
                Debug.LogError($"[SceneLoader] Build Settings에 {LOADING_SCENE_NAME} 씬이 등록되지 않았습니다.");
                return false;
            }

            return true;
        }

        private static void EnsureStep()
        {
            if (_hasStep) return;

            // 순서에 없는 씬(메인 메뉴, 테스트 씬 등)이면 -1이 되어 어디로도 전환할 수 없다.
            int sceneIndex = Array.IndexOf(SceneNames, SceneManager.GetActiveScene().name);
            _step = (sceneIndex < 0) ? -1 : Array.IndexOf(Sequence, (ESceneType)sceneIndex);
            _hasStep = true;
        }
        #endregion
    }
}
