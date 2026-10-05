using UnityEngine;
using UnityEngine.Pool;

namespace Core.ObjectPool
{
    /// <summary>
    /// NPC 전용 풀.
    /// 단일 프리팹 기준 풀을 운용하며, IPoolable 인터페이스로 초기화/정리를 위임한다.
    /// </summary>
    public class NpcPool : MonoBehaviour
    {
        #region Constants
        private const int DEFAULT_CAPACITY = 5;
        private const int MAX_SIZE         = 20;
        #endregion

        #region Serialized Fields
        [Header("Pool Settings")]
        [SerializeField] private GameObject _npcPrefab;
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
            _container = new GameObject("NpcPool_Container").transform;
            _container.SetParent(parent);

            _pool = new ObjectPool<GameObject>(
                createFunc:      CreateNpc,
                actionOnGet:     OnGet,
                actionOnRelease: OnRelease,
                actionOnDestroy: OnDestroyObject,
                collectionCheck: true,
                defaultCapacity: _defaultCapacity,
                maxSize:         _maxSize);
        }

        /// <summary>풀에서 NPC를 꺼내 지정 위치/회전으로 배치한다.</summary>
        public GameObject Get(Vector3 position, Quaternion rotation)
        {
            GameObject npc = _pool.Get();
            npc.transform.SetPositionAndRotation(position, rotation);
            return npc;
        }

        /// <summary>NPC를 풀에 반납한다.</summary>
        public void Release(GameObject npc)
        {
            _pool.Release(npc);
        }
        #endregion

        #region Private Methods
        private GameObject CreateNpc()
        {
            GameObject obj = Instantiate(_npcPrefab, _container);
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
            Destroy(obj);
        }
        #endregion
    }
}
