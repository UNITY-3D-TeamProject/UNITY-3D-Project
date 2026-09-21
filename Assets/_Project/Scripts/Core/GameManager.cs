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
        public GameState CurrentState { get; private set; }

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




        // 게임 진행 흐름 관리 (전체 진행도)
    }
}
