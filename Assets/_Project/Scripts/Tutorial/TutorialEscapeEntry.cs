using UnityEngine;
using Core;
using Facade.Player;
using Map.Gimmicks;

namespace Tutorial
{
    /// <summary>탈출 구간 진입 시 입구 파이프를 숨기고, 플레이어 재스폰 시 복원한다.</summary>
    [RequireComponent(typeof(Collider))]
    public class TutorialEscapeEntry : MonoBehaviour
    {
        #region Serialized Fields
        [SerializeField] private GameObject _entryPipe;
        #endregion

        #region Private Fields
        private GameManager _gameManager;
        private bool _wasPipeActive;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            Debug.Assert(_entryPipe != null, $"[{name}] 입구 파이프가 연결되지 않았습니다.");
            if (_entryPipe != null) _wasPipeActive = _entryPipe.activeSelf;
        }

        private void Start()
        {
            _gameManager = GameManager.Instance;
            if (_gameManager != null) _gameManager.OnPlayerSpawned += RestoreEntryPipe;
        }

        private void OnDestroy()
        {
            if (_gameManager != null) _gameManager.OnPlayerSpawned -= RestoreEntryPipe;
        }

        private void OnTriggerEnter(Collider other)
        {
            if ((_entryPipe == null) || !GimmickUtility.TryGetPlayer(other, out _)) return;

            _entryPipe.SetActive(false);
        }
        #endregion

        #region Private Methods
        private void RestoreEntryPipe(PlayerFacade player)
        {
            if ((player != null) && (_entryPipe != null)) _entryPipe.SetActive(_wasPipeActive);
        }
        #endregion
    }
}
