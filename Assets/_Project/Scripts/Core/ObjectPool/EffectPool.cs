using System.Collections;
using UnityEngine;
using UnityEngine.Pool;

namespace Core.ObjectPool
{
    /// <summary>
    /// 이펙트(ParticleSystem) 전용 풀.
    /// ParticleSystem 재생이 끝나면 코루틴으로 자동 반환한다.
    /// </summary>
    public class EffectPool : MonoBehaviour
    {
        #region Constants
        private const int DEFAULT_CAPACITY = 10;
        private const int MAX_SIZE         = 50;
        #endregion

        #region Serialized Fields
        [Header("Pool Settings")]
        [SerializeField] private GameObject _effectPrefab;
        [SerializeField] private int        _defaultCapacity = DEFAULT_CAPACITY;
        [SerializeField] private int        _maxSize         = MAX_SIZE;
        #endregion

        #region Private Fields
        private ObjectPool<GameObject> _pool;
        private Transform              _container;
        #endregion

        #region Properties
        public int CountActive   => _pool?.CountActive ?? 0;
        public int CountInactive => _pool?.CountInactive ?? 0;
        #endregion

        #region Public Methods
        /// <summary>풀을 초기화한다. ObjectPoolManager에서 호출한다.</summary>
        public void Initialize(Transform parent)
        {
            _container = new GameObject("EffectPool_Container").transform;
            _container.SetParent(parent);

            _pool = new ObjectPool<GameObject>(
                createFunc:    CreateEffect,
                actionOnGet:   OnGet,
                actionOnRelease: OnRelease,
                actionOnDestroy: OnDestroyObject,
                collectionCheck: true,
                defaultCapacity: _defaultCapacity,
                maxSize:         _maxSize);
        }

        /// <summary>풀에서 이펙트를 꺼내 지정 위치/회전으로 재생한다.</summary>
        public GameObject Get(Vector3 position, Quaternion rotation)
        {
            GameObject effect = _pool.Get();
            effect.transform.SetPositionAndRotation(position, rotation);

            if (effect.TryGetComponent<ParticleSystem>(out var ps))
                StartCoroutine(CoAutoRelease(effect, ps));

            return effect;
        }

        /// <summary>이펙트를 풀에 반납한다.</summary>
        public void Release(GameObject effect)
        {
            _pool.Release(effect);
        }
        #endregion

        #region Private Methods
        private GameObject CreateEffect()
        {
            GameObject obj = Instantiate(_effectPrefab, _container);
            obj.SetActive(false);
            return obj;
        }

        private void OnGet(GameObject obj)
        {
            obj.SetActive(true);

            if (obj.TryGetComponent<ParticleSystem>(out var ps))
                ps.Play();
        }

        private void OnRelease(GameObject obj)
        {
            if (obj.TryGetComponent<ParticleSystem>(out var ps))
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            obj.SetActive(false);
            obj.transform.SetParent(_container);
        }

        private void OnDestroyObject(GameObject obj)
        {
            Destroy(obj);
        }
        #endregion

        #region Coroutines
        /// <summary>ParticleSystem 재생이 끝나면 자동으로 풀에 반환한다.</summary>
        private IEnumerator CoAutoRelease(GameObject effect, ParticleSystem ps)
        {
            yield return new WaitUntil(() => !ps.IsAlive(true));

            // 이미 비활성화됐으면(수동 반납됐으면) 중복 반납 방지
            if (effect.activeSelf)
                Release(effect);
        }
        #endregion
    }
}
