using UnityEngine;
using Core;
using Facade.Player;

namespace Map.Gimmicks
{
    /// <summary>
    /// 플레이어 생성 연출이 끝날 때까지 이 오브젝트의 콜라이더를 꺼 둔다.
    /// 스폰 위치와 겹친 대화 박스(TriggerPlate)에 붙여, 생성 연출이 끝난 뒤에 대화가 나오게 한다.
    /// 켜질 때 이미 연출이 끝났으면(PlayerFacade.IsSpawnEnded) 바로 켜고, 아니면 OnSpawnEffectEnded를 기다린다.
    /// 한 번 켜면 다시 끄지 않는다.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class EnableColliderOnSpawnEnd : MonoBehaviour
    {
        #region Private Fields
        private Collider _collider;
        private GameManager _gameManager;
        private PlayerFacade _playerFacade;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            _collider = GetComponent<Collider>();
            _collider.enabled = false;
        }

        private void OnEnable()
        {
            if (_collider.enabled) return;

            // GameManager가 없는 테스트 씬에서는 기다릴 연출이 없으므로 바로 켠다.
            _gameManager = GameManager.Instance;
            if (_gameManager == null)
            {
                EnableCollider();
                return;
            }

            // 플레이어가 이미 있으면 그 플레이어의 연출 상태를 본다.
            PlayerFacade playerFacade = _gameManager.CurrentPlayerFacade;
            if ((playerFacade != null) && playerFacade.IsSpawnEnded)
            {
                EnableCollider();
                return;
            }

            _gameManager.OnPlayerSpawned += HandlePlayerSpawned;

            if (playerFacade != null)
                BindPlayer(playerFacade);
        }

        private void OnDisable()
        {
            Unbind();
        }
        #endregion

        #region Private Methods
        // [이벤트] GameManager.OnPlayerSpawned가 발생하면 호출된다. (OnEnable에서 구독)
        private void HandlePlayerSpawned(PlayerFacade playerFacade)
        {
            if (playerFacade == null) return;

            BindPlayer(playerFacade);
        }

        // [이벤트] PlayerFacade.OnSpawnEffectEnded가 발생하면 호출된다. (BindPlayer에서 구독)
        private void HandleSpawnEffectEnded()
        {
            EnableCollider();
        }

        // 연출을 기다릴 플레이어를 바꾼다. 재스폰으로 플레이어가 바뀌면 이전 플레이어 구독을 푼다.
        private void BindPlayer(PlayerFacade playerFacade)
        {
            if (_playerFacade != null)
                _playerFacade.OnSpawnEffectEnded -= HandleSpawnEffectEnded;

            _playerFacade = playerFacade;
            _playerFacade.OnSpawnEffectEnded += HandleSpawnEffectEnded;
        }

        private void EnableCollider()
        {
            _collider.enabled = true;
            Unbind();
        }

        private void Unbind()
        {
            if (_gameManager != null)
                _gameManager.OnPlayerSpawned -= HandlePlayerSpawned;

            if (_playerFacade != null)
                _playerFacade.OnSpawnEffectEnded -= HandleSpawnEffectEnded;

            _gameManager = null;
            _playerFacade = null;
        }
        #endregion
    }
}
