using Attribute.Core;
using Core.Stage;
using SceneTransition;
using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Core
{
    public class GameManager : Singleton<GameManager>
    {
        // 게임 상태 관리 
        public enum GameState
        {
            Title,
            Playing,
            Pause,
        }

        // 플레이어 스폰 이유
        public enum EPlayerSpawnReason
        {
            NewGame,
            Respawn,
            SceneTransition,
        }

        public EPlayerSpawnReason SpawnReason { get; private set; }
            = EPlayerSpawnReason.NewGame;



        public GameState CurrentState { get; private set; } = GameState.Playing;

        // 저장할 능력치 이름 목록을 확인하기 위해 (playerState를 초기화 할 때 넣어줄 매개변수 값)
        [SerializeField] private SOAttributeData _playerSOAttributeData;

        public PlayerState playerState { get; private set; }

        // playerState가 아직 초기화되지 않았다면 null 반환
        public AttributeSet CurrentPlayerState => playerState?.CurrentAttributeSet;

        // 스폰할 때 위치마다 다 달라서 저장해둘 값
        public EStageType? SpawnStage { get; private set; }

        // 플레이어를 스폰할 때 이벤트 
        public event Action<AttributeSet> OnPlayerSpawned;

        private StageManager _stageManager;

        // GameManager의 초기화가 완료됐는지 확인한다.
        // playerState가 new를 통해 생성되면 그 후 부터 true
        public bool HasPlayerState => playerState != null;

        public bool HasSavedPlayerAttributes => playerState?.HasSavedAttributes ?? false;

        public override void Awake()
        {
            base.Awake();
            if (Instance != this)
            {
                return;
            }
            // 인스펙터에서 받은 SO를 인자로 넣어준다.
            playerState = new PlayerState(_playerSOAttributeData);
            SetCursorVisible(false);
        }

        // 플레이어의 상태 처리를 완료하고 스폰 이벤트를 알린다.
        public void CompletePlayerSpawn(AttributeSet _spawnedPlayerattributeSet)
        { 
            if (_spawnedPlayerattributeSet == null)
            {
                Debug.LogError("등록할 플레이어의 AttributeSet이 없습니다.", this);
                return;
            }
            // playerState가 있는지
            if (!HasPlayerState)
            {
                Debug.LogError("PlayerState가 초기화되지 않았습니다.", this);
                return;
            }

            // playerState가 방금 생성한 플레이어 인스턴스의 어트리뷰트를 알게됨
            playerState.SetCurrentPlayer(_spawnedPlayerattributeSet);

            // 씬 이동은 저장값을 복원하고, 새 게임·부활은 Effect 초기값을 유지한다.
            bool shouldRestore = SpawnReason == EPlayerSpawnReason.SceneTransition;
            if (shouldRestore)
            {
                playerState.RestoreSavedAttributes();
            }
            else
            {
                playerState.ClearSavedAttributes();
            }

            // UIManager가 이벤트를 받아서 ui와 플레이어 어트리뷰트 셋을 연동함 
            OnPlayerSpawned?.Invoke(_spawnedPlayerattributeSet);
        }

        public void ClearSpawnStage()
        {
            SpawnStage = null;
        }

        // 새 게임 시작
        public void StartGame()
        {
            PrepareSpawn(EPlayerSpawnReason.NewGame);
            CurrentState = GameState.Playing;
        }

        // 씬 이동이나 재생성을 시작하기 전에 호출한다.
        public void PrepareSpawn(EPlayerSpawnReason reason)
        {
            // 씬 이동일 때만 현재 능력치를 저장한다.
            if (reason == EPlayerSpawnReason.SceneTransition)
            {
                playerState.SaveCurrentAttributes();
            }

            SpawnReason = reason;
        }


        public void PauseGame()
        {
            CurrentState = GameState.Pause;
            Time.timeScale = 0f;
            SetCursorVisible(true);
        }

        public void ResumeGame()
        {
            CurrentState = GameState.Playing;
            Time.timeScale = 1f;
            SetCursorVisible(false);
        }


        private void SetCursorVisible(bool isVisible)
        {
            Cursor.lockState = isVisible
                ? CursorLockMode.None
                : CursorLockMode.Locked;

            Cursor.visible = isVisible;
            Debug.Log(isVisible ? "[GameManager] 마우스 커서 켜짐" : "[GameManager] 마우스 커서 꺼짐", this);
        }

        private void OnEnable()
        {
            if (Instance != this)
            {
                return;
            }

            // 씬 로딩 완료 시점을 알리는 이벤트 구독 => 새 씬에 배치된 StageManager 탐색
            SceneManager.sceneLoaded += HandleSceneLoaded;
            
            // 씬 전환 직전 시점을 알리는 이벤트 구독 => 기존 플레이어가 파괴되기전 현재 
            // 능력치를 임시 저장하고 스폰 사유를 Scene Transition으로 설정
            SceneLoader.OnBeforeSceneChange += HandleBeforeSceneChange;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            SceneLoader.OnBeforeSceneChange -= HandleBeforeSceneChange;
        }

        // Scene => 방금 로드된 씬의 정보가 들어가 있다. scene.name으로 씬 이름 확인 가능
        // LoadSceneMode => 씬을 어떻게 로드 했는지에 대한 enum 타입 => Single(새 씬만), Additive(기존 씬 유지)
        private void HandleSceneLoaded(Scene scene,LoadSceneMode mode)
        {
            // scene.name이 스테이지 씬인지 확인
            _stageManager = FindAnyObjectByType<StageManager>();

            if (_stageManager == null)
                return;

            // stageManager.TakeCurrentStatge(stageType);
        }

        // SceneLoader의 일반 씬 이동 직전에 현재 능력치를 저장한다.
        private void HandleBeforeSceneChange(ESceneType previous, ESceneType destination)
        {
            if (CurrentPlayerState == null)
            {
                return;
            }

            PrepareSpawn(EPlayerSpawnReason.SceneTransition);

            // 여기서 무슨 씬으로 바꿀건지도 가져온다.
            // SpawnStage = destinationStage.Value;
        }
        //private void HandleBeforeSceneChange(EStageType? destinationStage)
        //{
        //    playerState.SaveCurrentAttributes();

        //    if (destinationStage.HasValue)
        //    {
        //        SpawnStage = destinationStage.Value;
        //    }
        //}
    }
}
