using System;
using Attribute.Core;
using UnityEngine;

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


        // 플레이어를 스폰할 때 이벤트 
        public event Action<AttributeSet> OnPlayerSpawned;



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

        // 준범이가 만드는 씬 매니저?가 싱글톤일지 뭘지 모르겠는데
        // 이벤트 구독하기
        // 씬 전환 직전 이벤트를 받으면 호출하기
        private void HandleBeforeSceneChange()
        {
            // 씬이 바뀌면 현재 값을 저장한다.
            playerState.SaveCurrentAttributes();
        }
    }
}
