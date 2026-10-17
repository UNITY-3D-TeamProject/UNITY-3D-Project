using UnityEngine;
using Core;
using Core.Stage;
using Facade.Player;

namespace AI.RoundSpawning
{
    /// <summary>
    /// 지정한 라운드에만 적을 만들고 라운드가 끝나면 제거하는 스포너의 공통 부모.
    /// 라운드 확인, 플레이어 재스폰 시 리셋, 플레이어 재스폰 알림 전달, 실패 로그 1회를 맡는다.
    /// 무엇을 어디에 만드는지는 자식이 Spawn / Despawn 으로 정한다.
    /// </summary>
    public abstract class RoundSpawnerBase : MonoBehaviour
    {
        #region Serialized Fields
        [Header("Round")]
        [SerializeField] private StageManager _stageManager;
        [Tooltip("StageManager.CurrentRoundIndex 가 이 값일 때만 존재한다. 0 부터 시작하므로 라운드 2 는 1, 라운드 3 은 2 이다.")]
        [SerializeField] private int _activeRoundIndex = 1;
        #endregion

        #region Private Fields
        private GameManager _gameManager;
        private bool _hasLoggedSpawnFailure;
        #endregion

        #region Properties
        protected StageManager StageManager => _stageManager;

        /// <summary>현재 플레이어. GameManager 가 없거나 플레이어가 아직 없으면 null.</summary>
        protected PlayerFacade CurrentPlayerFacade => _gameManager ? _gameManager.CurrentPlayerFacade : null;

        /// <summary>
        /// 이번 라운드의 생성을 마쳤는가. 인스턴스가 살아 있는가가 아니다.
        /// 전부 잡혀 사라져도 true 여야 다시 생성되지 않는다.
        /// </summary>
        protected abstract bool IsSpawned { get; }
        #endregion

        #region Unity Lifecycle
        /// <summary>자식이 덮어쓸 때는 base.OnEnable() 을 반드시 부른다.</summary>
        protected virtual void OnEnable()
        {
            if (_stageManager) _stageManager.OnPlayerRespawnRequested += HandlePlayerRespawnRequested;

            _gameManager = GameManager.Instance;
            if (_gameManager) _gameManager.OnPlayerSpawned += HandlePlayerSpawned;
        }

        /// <summary>자식이 덮어쓸 때는 base.OnDisable() 을 반드시 부른다.</summary>
        protected virtual void OnDisable()
        {
            if (_stageManager) _stageManager.OnPlayerRespawnRequested -= HandlePlayerRespawnRequested;

            if (_gameManager) _gameManager.OnPlayerSpawned -= HandlePlayerSpawned;
            _gameManager = null;
        }

        /// <summary>자식이 덮어쓸 때는 base.Update() 를 반드시 부른다.</summary>
        protected virtual void Update()
        {
            if (!_stageManager) return;

            bool shouldExist = _stageManager.IsRunning && (_stageManager.CurrentRoundIndex == _activeRoundIndex);

            if (shouldExist && !IsSpawned)
            {
                Spawn();
            }
            else if (!shouldExist && IsSpawned)
            {
                Despawn();
            }
        }
        #endregion

        #region Protected Methods
        protected abstract void Spawn();

        protected abstract void Despawn();

        /// <summary>
        /// 라운드 중 플레이어 재스폰 요청이 오면 호출된다. 기본은 생성돼 있을 때 제거 후 다시 만든다.
        /// </summary>
        protected virtual void HandlePlayerRespawnRequested()
        {
            if (!IsSpawned) return;

            Despawn();
            Spawn();
        }

        /// <summary>
        /// 플레이어가 새로 만들어졌을 때 호출된다. 기본은 아무것도 하지 않는다.
        /// </summary>
        /// <param name="playerFacade">새로 만들어진 플레이어</param>
        protected virtual void OnPlayerSpawned(PlayerFacade playerFacade)
        {
        }

        /// <summary>
        /// 생성 실패를 로그로 한 번만 남긴다. 매 프레임 재시도하며 로그가 쌓이지 않게 한다.
        /// </summary>
        /// <param name="message">실패 사유</param>
        protected void LogSpawnFailureOnce(string message)
        {
            if (_hasLoggedSpawnFailure) return;

            _hasLoggedSpawnFailure = true;
            Debug.LogError($"[{name}] {message}", this);
        }
        #endregion

        #region Private Methods
        private void HandlePlayerSpawned(PlayerFacade playerFacade)
        {
            OnPlayerSpawned(playerFacade);
        }
        #endregion
    }
}
