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
        [SerializeField] private SOAttributeData _playerAttributeData;
        public GameState CurrentState { get; private set; }

        public PlayerState playerState { get; private set; }

        public override void Awake()
        {
            base.Awake();
            playerState = new PlayerState(_playerAttributeData);
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
            playerState.SaveCurrentAttributes();
        }
    }
}
