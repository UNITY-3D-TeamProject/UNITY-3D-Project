using Core.ObjectPool;
using Mediator;
using Unity.Behavior;
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
        [Tooltip("풀에 반납될 때 끝내고 다시 꺼낼 때 처음부터 돌릴 BT. 비워두면 이 오브젝트에서 찾는다.")]
        [SerializeField] private BehaviorGraphAgent _graphAgent;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            if (!_characterMediator) _characterMediator = GetComponent<CharacterMediator>();
            if (!_graphAgent) _graphAgent = GetComponent<BehaviorGraphAgent>();
        }

        private void OnEnable()
        {
            _characterMediator.ResetForReuse();
            _characterMediator.OnDeathEnded += OnDeathEndedCallback;
        }

        private void OnDisable()
        {
            _characterMediator.OnDeathEnded -= OnDeathEndedCallback;

            // BehaviorGraphAgent 는 꺼져도 그래프를 멈추기만 해서, 다시 꺼내면 하던 노드를 이어서 돈다.
            // 꺼진 상태의 Restart 는 실행 중인 노드의 OnEnd(후퇴 감속 복구 등)를 부르고, 다음 Update 에서 처음부터 시작하게 한다
            if (_graphAgent) _graphAgent.Restart();
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
