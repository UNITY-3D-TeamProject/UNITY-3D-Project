using Core.ObjectPool;
using Mediator;
using UnityEngine;

namespace AI
{
    /// <summary>
    /// 몬스터의 사망 후 처리가 끝나면 오브젝트 풀에 반납하고, 풀에서 다시 꺼내질 때 사망 상태를 되돌린다.
    /// CharacterMediator 는 플레이어도 쓰므로 풀 반납은 몬스터 프리팹에 붙이는 이 컴포넌트가 맡는다.
    /// </summary>
    public class EnemyPoolReturner : MonoBehaviour
    {
        #region Serialized Fields
        [Header("References")]
        [SerializeField] private CharacterMediator _characterMediator;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            if (!_characterMediator) _characterMediator = GetComponent<CharacterMediator>();
        }

        private void OnEnable()
        {
            _characterMediator.ResetForReuse();
            _characterMediator.OnDeathEnded += OnDeathEndedCallback;
        }

        private void OnDisable()
        {
            _characterMediator.OnDeathEnded -= OnDeathEndedCallback;
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// 사망 후 처리가 끝나면 풀에 반납한다. 매니저가 없는 씬이면 파괴한다.
        /// </summary>
        private void OnDeathEndedCallback()
        {
            if (ObjectPoolManager.Instance != null)
                ObjectPoolManager.Instance.Release(gameObject);
            else
                Destroy(gameObject);
        }
        #endregion
    }
}
