using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace Core.ObjectPool
{
    /// <summary>
    /// 총알(Bullet) 전용 풀.
    /// 여러 종류의 총알 프리팹을 Dictionary로 관리하여 종류별 독립 풀을 운용한다.
    /// </summary>
    public class BulletPool : MonoBehaviour
    {
        #region Constants
        private const int DEFAULT_CAPACITY = 20;
        private const int MAX_SIZE         = 100;
        #endregion

        #region Serialized Fields
        [Header("Bullet Prefabs (종류별로 등록)")]
        [SerializeField] private List<GameObject> _bulletPrefabs;

        [Header("Pool Settings")]
        [SerializeField] private int _defaultCapacity = DEFAULT_CAPACITY;
        [SerializeField] private int _maxSize         = MAX_SIZE;
        #endregion

        #region Private Fields
        /// <summary>프리팹 → 해당 프리팹의 풀</summary>
        private Dictionary<GameObject, ObjectPool<GameObject>> _pools;

        /// <summary>인스턴스 → 원본 프리팹 역참조 (반납 시 사용)</summary>
        private Dictionary<GameObject, GameObject> _instanceToPrefab;

        private Transform _container;
        #endregion

        #region Properties
        // 현재 등록된 Pool 종류 개수를 알려준다.
        public int RegisteredPoolCount => _pools?.Count ?? 0;
        #endregion

        #region Public Methods
        /// <summary>풀을 초기화한다. ObjectPoolManager에서 호출한다.</summary>
        public void Initialize(Transform parent)
        {
            _container        = new GameObject("BulletPool_Container").transform;
            _container.SetParent(parent);

            _pools            = new Dictionary<GameObject, ObjectPool<GameObject>>();
            _instanceToPrefab = new Dictionary<GameObject, GameObject>();

            foreach (GameObject prefab in _bulletPrefabs)
                RegisterPrefab(prefab);
        }

        /// <summary>지정 프리팹의 풀에서 총알을 꺼낸다.</summary>
        public GameObject Get(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            if (!_pools.TryGetValue(prefab, out var pool))
            {
                Debug.LogWarning($"[BulletPool] 등록되지 않은 프리팹: {prefab.name}. 런타임 등록을 시도합니다.");
                RegisterPrefab(prefab);
                pool = _pools[prefab];
            }

            GameObject bullet = pool.Get();
            bullet.transform.SetPositionAndRotation(position, rotation);
            return bullet;
        }

        /// <summary>총알을 풀에 반납한다.</summary>
        public void Release(GameObject bullet)
        {
            if (!_instanceToPrefab.TryGetValue(bullet, out var prefab))
            {
                Debug.LogWarning($"[BulletPool] 추적되지 않는 인스턴스 반납 시도: {bullet.name}. Destroy로 처리합니다.");
                Destroy(bullet);
                return;
            }

            _pools[prefab].Release(bullet);
        }
        #endregion

        #region Private Methods
        private void RegisterPrefab(GameObject prefab)
        {
            if (_pools.ContainsKey(prefab)) return;

            var pool = new ObjectPool<GameObject>(
                createFunc:      () => CreateBullet(prefab),
                actionOnGet:     OnGet,
                actionOnRelease: OnRelease,
                actionOnDestroy: OnDestroyObject,
                collectionCheck: true,
                defaultCapacity: _defaultCapacity,
                maxSize:         _maxSize);

            _pools[prefab] = pool;
        }

        private GameObject CreateBullet(GameObject prefab)
        {
            GameObject obj = Instantiate(prefab, _container);
            _instanceToPrefab[obj] = prefab;
            obj.SetActive(false);
            return obj;
        }

        private void OnGet(GameObject obj)
        {
            obj.SetActive(true);

            if (obj.TryGetComponent<IPoolable>(out var poolable))
                poolable.OnGet();
        }

        private void OnRelease(GameObject obj)
        {
            if (obj.TryGetComponent<IPoolable>(out var poolable))
                poolable.OnRelease();

            obj.SetActive(false);
            obj.transform.SetParent(_container);
        }

        private void OnDestroyObject(GameObject obj)
        {
            _instanceToPrefab.Remove(obj);
            Destroy(obj);
        }
        #endregion
    }
}
