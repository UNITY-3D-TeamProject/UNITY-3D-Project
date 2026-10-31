using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Core.Stage;
using SceneTransition;

namespace Core.Editor
{
    /// <summary>
    /// 상단 메뉴 Tools > 진행 단계 에서 SceneLoader 순서의 한 단계를 골라 바로 들어간다.
    /// 고른 단계로 이어하기 저장을 쓰고, MainScene부터 Play를 시작해 이어하기(SceneLoader.TryContinueGame)를 부른다.
    /// 로비 단계는 직전 스테이지에서 돌아온 자리(PlayerSpawner의 복귀 위치)에서 시작하도록 GameManager.SpawnStage를 맞춘다.
    /// 에디터에 열린 씬은 바꾸지 않는다. (Play 시작 씬만 잠깐 MainScene으로 지정)
    /// </summary>
    [InitializeOnLoad]
    public static class ProgressStepMenu
    {
        #region Constants
        private const string MENU_ROOT = "Tools/진행 단계/";
        private const string MAIN_SCENE_PATH = "Assets/_Project/Scenes/MainScene.unity";
        private const string LOBBY_SCENE_NAME = "Map_1_Lobby";

        // SceneLoader의 이어하기 저장 키. SceneLoader에서 바뀌면 여기도 바꾼다.
        private const int SAVE_VERSION = 1;
        private const string SAVE_VERSION_KEY = "GameProgress.SaveVersion";
        private const string SEQUENCE_STEP_KEY = "GameProgress.SequenceStep";
        private const string PLAYER_HP_KEY = "GameProgress.CurrentHp";
        private const string PLAYER_BATTERY_KEY = "GameProgress.CurrentBattery";

        // Play 진입 중 도메인을 다시 불러와도 남도록 SessionState에 둔다.
        private const string PENDING_STEP_KEY = "ProgressStepMenu.PendingStep";
        private const int NO_STEP = -1;
        #endregion

        #region Private Fields
        // SceneLoader 순서의 단계마다, 로비라면 직전 스테이지(복귀 위치), 아니면 null.
        private static readonly EStageType?[] ReturnStages =
        {
            null,
            EStageType.Tutorial,
            null,
            EStageType.FeedApp,
            null,
            EStageType.FileApp,
            null,
            EStageType.SecurityApp,
            null,
        };

        private static EStageType? _lobbyReturnStage;
        #endregion

        #region Constructors
        static ProgressStepMenu()
        {
            EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;
            EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
        }
        #endregion

        #region Menu Items
        [MenuItem(MENU_ROOT + "1. 튜토리얼", false, 0)]
        private static void EnterTutorial() => Enter(0);

        [MenuItem(MENU_ROOT + "2. 로비 (튜토리얼 후)", false, 1)]
        private static void EnterLobbyAfterTutorial() => Enter(1);

        [MenuItem(MENU_ROOT + "3. 피드", false, 2)]
        private static void EnterFeed() => Enter(2);

        [MenuItem(MENU_ROOT + "4. 로비 (피드 후)", false, 3)]
        private static void EnterLobbyAfterFeed() => Enter(3);

        [MenuItem(MENU_ROOT + "5. 파일", false, 4)]
        private static void EnterFile() => Enter(4);

        [MenuItem(MENU_ROOT + "6. 로비 (파일 후)", false, 5)]
        private static void EnterLobbyAfterFile() => Enter(5);

        [MenuItem(MENU_ROOT + "7. 보안", false, 6)]
        private static void EnterSecurity() => Enter(6);

        [MenuItem(MENU_ROOT + "8. 로비 (보안 후)", false, 7)]
        private static void EnterLobbyAfterSecurity() => Enter(7);

        [MenuItem(MENU_ROOT + "9. 라이브", false, 8)]
        private static void EnterLive() => Enter(8);

        // 우선순위를 11 이상 띄워 위 단계들과 구분선을 둔다.
        [MenuItem(MENU_ROOT + "저장 지우기", false, 20)]
        private static void ClearSave()
        {
            PlayerPrefs.DeleteKey(SAVE_VERSION_KEY);
            PlayerPrefs.DeleteKey(SEQUENCE_STEP_KEY);
            PlayerPrefs.DeleteKey(PLAYER_HP_KEY);
            PlayerPrefs.DeleteKey(PLAYER_BATTERY_KEY);
            PlayerPrefs.Save();
            Debug.Log("[ProgressStepMenu] 이어하기 저장을 지웠습니다.");
        }
        #endregion

        #region Private Methods
        // 이어하기 저장을 step으로 쓰고 MainScene부터 Play를 시작한다. 이어하기는 Play에 들어간 뒤 부른다.
        private static void Enter(int step)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("[ProgressStepMenu] Play 중에는 쓸 수 없습니다. Play를 멈추고 다시 고르세요.");
                return;
            }

            SceneAsset mainScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(MAIN_SCENE_PATH);
            if (mainScene == null)
            {
                Debug.LogError($"[ProgressStepMenu] {MAIN_SCENE_PATH} 를 찾을 수 없습니다.");
                return;
            }

            PlayerPrefs.SetInt(SAVE_VERSION_KEY, SAVE_VERSION);
            PlayerPrefs.SetInt(SEQUENCE_STEP_KEY, step);
            PlayerPrefs.Save();

            SessionState.SetInt(PENDING_STEP_KEY, step);
            EditorSceneManager.playModeStartScene = mainScene;
            EditorApplication.isPlaying = true;
        }

        private static void HandlePlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingPlayMode)
            {
                SceneManager.sceneLoaded -= HandleSceneLoaded;
                return;
            }

            if (state != PlayModeStateChange.EnteredPlayMode) return;

            int step = SessionState.GetInt(PENDING_STEP_KEY, NO_STEP);
            if (step == NO_STEP) return;

            SessionState.EraseInt(PENDING_STEP_KEY);
            EditorSceneManager.playModeStartScene = null;

            _lobbyReturnStage = ((step >= 0) && (step < ReturnStages.Length)) ? ReturnStages[step] : null;
            if (_lobbyReturnStage.HasValue)
            {
                SceneManager.sceneLoaded -= HandleSceneLoaded;
                SceneManager.sceneLoaded += HandleSceneLoaded;
            }

            // MainScene의 Awake·Start가 끝난 다음 프레임에 이어하기를 부른다.
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlaying) SceneLoader.TryContinueGame();
            };
        }

        // 로비가 불러와지면 PlayerSpawner.Start보다 먼저 복귀 위치를 정해 둔다.
        // SpawnStage는 씬 이동 때만 정해지는 private set 이라 이 테스트 도구에서만 리플렉션으로 맞춘다.
        private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != LOBBY_SCENE_NAME) return;

            SceneManager.sceneLoaded -= HandleSceneLoaded;

            GameManager gameManager = GameManager.Instance;
            PropertyInfo spawnStage = typeof(GameManager).GetProperty(nameof(GameManager.SpawnStage));
            if ((gameManager == null) || (spawnStage == null)) return;

            spawnStage.SetValue(gameManager, _lobbyReturnStage);
        }
        #endregion
    }
}
