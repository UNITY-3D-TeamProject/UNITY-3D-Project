using Attribute.Core;
using Core.Stage;
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

        public GameState CurrentState {  get; private set; }

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

        public override void Awake()
        {
            base.Awake();
            if (Instance != this)
            {
                return;
            }
            // 인스펙터에서 받은 SO를 인자로 넣어준다.
            playerState = new PlayerState(_playerSOAttributeData);
        }

        // PlayerSpawner의 Start()에서 플레이어를 Instantiate 하고 나서
        // 그 플레이어의 어트리뷰트 셋을 넣어서 부르는 함수
        public void RegisterPlayer(AttributeSet _spawnedPlayerattributeSet)
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

            playerState.RegisterPlayer(_spawnedPlayerattributeSet);
            OnPlayerSpawned?.Invoke(_spawnedPlayerattributeSet);
        }

        public void ClearSpawnStage()
        {
            SpawnStage = null;
        }

        public void StartGame()
        {
            CurrentState = GameState.Playing;
        }

        public void PauseGame()
        {
            CurrentState = GameState.Pause;
            Time.timeScale = 0f;
        }

        public void ResumeGame()
        {
            CurrentState = GameState.Playing;
            Time.timeScale = 1f;
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += HandleSceneLoaded;
            // 씬전환매니저가 따로 있는데 거기서도 이벤트 구독해야함
            // 전환 직전 이벤트 += HandleBeforeSceneChange;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            // 씬전환매니저가 따로 있는데 거기서도 이벤트 구독해제해야함
            //HandleBeforeSceneChange
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

        // 준범이가 만드는 씬 매니저?가 싱글톤일지 뭘지 모르겠는데
        // 이벤트 구독하기
        // ~~씬전환 액션 구독받아놔야함 
        // 씬 전환 직전 이벤트를 받으면 호출하기
        private void HandleBeforeSceneChange() // 매개변수 받기
        {
            // 씬이 바뀌면 현재 값을 저장한다.
            playerState.SaveCurrentAttributes();

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
