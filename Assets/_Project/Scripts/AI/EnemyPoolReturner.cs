using System.Collections;
using Core.ObjectPool;
using Mediator;
using Unity.Behavior;
using UnityEngine;

namespace AI
{
    /// <summary>
    /// 몬스터의 사망 후 처리가 끝나면 스케일을 0까지 줄인 뒤 오브젝트 풀에 반납한다. 반납될 때 스케일을, 풀에서 다시 꺼내질 때 사망 상태를 되돌린다.
    /// CharacterMediator 는 플레이어도 쓰므로 풀 반납은 몬스터 프리팹에 붙이는 이 컴포넌트가 맡는다.
    /// </summary>
    public class EnemyPoolReturner : MonoBehaviour
    {
        #region Serialized Fields
        [Header("References")]
        [SerializeField] private CharacterMediator _characterMediator;
        [Tooltip("풀에 반납될 때 끝내고 다시 꺼낼 때 처음부터 돌릴 BT. 비워두면 이 오브젝트에서 찾는다.")]
        [SerializeField] private BehaviorGraphAgent _graphAgent;

        [Header("Death Shrink")]
        [Tooltip("사망 애니메이션이 끝난 뒤 줄어들기 시작할 때까지 기다리는 시간(초)")]
        [SerializeField, Min(0.0f)] private float _shrinkDelay = 0.0f;
        [Tooltip("스케일이 0까지 줄어드는 시간(초). 0이면 바로 반납한다.")]
        [SerializeField, Min(0.0f)] private float _shrinkDuration = 0.4f;
        #endregion

        #region Private Fields
        private Vector3 _originalScale;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            if (!_characterMediator) _characterMediator = GetComponent<CharacterMediator>();
            if (!_graphAgent) _graphAgent = GetComponent<BehaviorGraphAgent>();
            _originalScale = transform.localScale;
        }

        private void OnEnable()
        {
            _characterMediator.ResetForReuse();
            _characterMediator.OnDeathEnded += OnDeathEndedCallback;
        }

        private void OnDisable()
        {
            // 풀은 꺼낼 때 위치·회전만 맞추므로 꺼지는 시점에 스케일을 되돌린다.
            // OnEnable 에서 되돌리면 먼저 켜지는 CharacterController 가 스케일 0으로 다시 만들어져 비활성으로 남는다
            transform.localScale = _originalScale;
            _characterMediator.OnDeathEnded -= OnDeathEndedCallback;

            // BehaviorGraphAgent 는 꺼져도 그래프를 멈추기만 해서, 다시 꺼내면 하던 노드를 이어서 돈다.
            // 꺼진 상태의 Restart 는 실행 중인 노드의 OnEnd(후퇴 감속 복구 등)를 부르고, 다음 Update 에서 처음부터 시작하게 한다
            if (_graphAgent) _graphAgent.Restart();
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// 사망 후 처리가 끝나면 줄어드는 연출을 시작한다. 연출이 끝나면 풀에 반납된다.
        /// </summary>
        private void OnDeathEndedCallback()
        {
            StartCoroutine(CoShrinkAndRelease());
        }

        /// <summary>
        /// 풀에 반납한다. 매니저가 없는 씬이면 파괴한다.
        /// </summary>
        private void ReleaseToPool()
        {
            if (ObjectPoolManager.Instance != null)
                ObjectPoolManager.Instance.Release(gameObject);
            else
                Destroy(gameObject);
        }
        #endregion

        #region Coroutines
        /// <summary>
        /// 원래 스케일에서 0까지 줄인 뒤 풀에 반납한다.
        /// 도중에 꺼지면(강제 반납 등) 코루틴이 멈추고, OnDisable 에서 스케일을 되돌린다.
        /// </summary>
        private IEnumerator CoShrinkAndRelease()
        {
            if (_shrinkDelay > 0.0f) yield return new WaitForSeconds(_shrinkDelay);

            // 스케일 0을 넣은 프레임에 바로 반납해, 0인 채로 남는 프레임이 없게 한다
            float elapsed = 0.0f;
            while (elapsed < _shrinkDuration)
            {
                yield return null;
                elapsed += Time.deltaTime;
                float progress = Mathf.SmoothStep(0.0f, 1.0f, elapsed / _shrinkDuration);
                transform.localScale = _originalScale * (1.0f - progress);
            }

            ReleaseToPool();
        }
        #endregion
    }
}
