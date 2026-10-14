using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using Object = UnityEngine.Object;

namespace Core.ObjectPool
{
    /// <summary>
    /// 카테고리 하나(예: Enemies)의 풀 묶음. ObjectPoolManager 인스펙터 안에 한 칸씩 들어간다.
    /// 등록된 "키 → 프리팹"마다 풀을 하나씩 만들고, 인스턴스 생성·반납·파괴를 맡는다.
    /// </summary>
    // 이 클래스는 직접 꺼내 쓰지 않는다. 꺼내기는 ObjectPoolManager.Get(키)를 거친다.
    // 재사용 시 초기화는 풀이 하지 않는다. 프리팹 컴포넌트가 OnEnable에서 스스로 초기화한다.
    [Serializable]
    public class PrefabPool
    {
        #region Constants
        // 처음 준비할 칸 수, 넘으면 자동으로 늘어남
        private const int DEFAULT_CAPACITY = 10;
        // 넘치면 Destroy함
        private const int MAX_SIZE         = 50;
        // 씬 시작 시 키마다 미리 만들어 둘 개수
        private const int PREWARM_COUNT    = 10;
        #endregion

        #region Serialized Fields
        // 키는 모든 카테고리를 통틀어 겹치면 안 된다. (꺼낼 때 키만 넘기기 때문)
        [SerializeField] private List<SPoolEntry> _entries = new();

        [Tooltip("풀을 만들 때 미리 잡아 둘 목록 크기. 오브젝트를 미리 만들지는 않는다.")]
        [SerializeField, Min(0)] private int _defaultCapacity = DEFAULT_CAPACITY;

        // 등록된 키 하나하나에 적용된다. 첫 스폰 순간의 Instantiate 끊김을 막기 위해 씬 시작 시 미리 만든다.
        // 다 쓰면 그때부터는 Get할 때 하나씩 추가로 만든다. Max Size보다 크면 Max Size까지만 만든다.
        [Tooltip("키 하나당 씬 시작 시 미리 만들어 둘 개수. 부족하면 Get할 때 추가로 만든다.")]
        [SerializeField, Min(0)] private int _prewarmCount = PREWARM_COUNT;

        // 등록된 키 하나하나에 적용된다.
        // 예: Melee, Ranged가 있고 값이 20이면 Melee 풀 20개, Ranged 풀 20개까지 보관한다.
        // 넘치는 반납분은 보관하지 않고 Destroy된다.
        [Tooltip("키 하나당 보관 최대 개수. 넘치는 반납분은 Destroy된다.")]
        [SerializeField, Min(1)] private int _maxSize = MAX_SIZE;
        #endregion

        #region Private Fields
        /// <summary>키 → 그 키의 풀</summary>
        private Dictionary<string, ObjectPool<GameObject>> _pools;

        /// <summary>키 → 그 키에 등록된 프리팹</summary>
        private Dictionary<string, GameObject> _prefabByKey;

        // 생성·반납된 인스턴스의 부모. (매니저 Transform)
        private Transform _root;
        #endregion

        #region Properties
        // 매니저가 살아 있는지. 씬이 내려가 매니저가 파괴됐으면 false다.
        // 일반 클래스라 PrefabPool 자체는 null이 되지 않으므로 이걸로 판단한다.
        public bool IsAlive => _root != null;

        // 이 카테고리에 등록된 키 목록. 매니저가 "키 → 카테고리" 표를 만들 때 쓴다.
        public IEnumerable<string> Keys => _pools.Keys;
        #endregion

        #region Nested Types
        // 인스펙터에서 키와 프리팹을 한 쌍씩 입력받기 위한 칸.
        // 유니티 인스펙터는 Dictionary를 보여 주지 못하므로 List로 입력받고 Initialize에서 _pools로 옮긴다.
        [Serializable]
        private struct SPoolEntry
        {
            [SerializeField] private string     _key;
            [SerializeField] private GameObject _prefab;

            public string     Key    => _key;
            public GameObject Prefab => _prefab;
        }
        #endregion

        #region Public Methods
        /// <summary>등록된 항목마다 풀을 하나씩 만들고, Prewarm Count만큼 미리 만들어 둔다.</summary>
        // [호출] ObjectPoolManager.Awake에서 카테고리마다 한 번 호출된다.
        public void Initialize(Transform root, Object logContext)
        {
            _root  = root;
            _pools = new Dictionary<string, ObjectPool<GameObject>>();
            _prefabByKey = new Dictionary<string, GameObject>();

            foreach (SPoolEntry entry in _entries)
            {
                if (string.IsNullOrEmpty(entry.Key) || (entry.Prefab == null))
                {
                    Debug.LogWarning("[PrefabPool] 키 또는 프리팹이 비어 있는 항목은 건너뜁니다.", logContext);
                    continue;
                }

                if (_pools.ContainsKey(entry.Key))
                {
                    Debug.LogError($"[PrefabPool] 키 '{entry.Key}'가 중복 등록되었습니다. 뒤의 항목은 무시됩니다.", logContext);
                    continue;
                }

                // 람다가 entry 대신 지역 변수를 기억하도록 따로 담는다.
                string     key    = entry.Key;
                GameObject prefab = entry.Prefab;

                // 유니티 내장 풀. 꺼낼 게 없으면 createFunc로 만들고, 반납 시 actionOnRelease를 부른다.
                ObjectPool<GameObject> pool = new ObjectPool<GameObject>(
                    createFunc:      () => CreateObject(key, prefab),
                    actionOnGet:     null,
                    actionOnRelease: OnReleaseObject,
                    actionOnDestroy: OnDestroyObject,
                    collectionCheck: true,
                    defaultCapacity: _defaultCapacity,
                    maxSize:         _maxSize);

                _pools.Add(key, pool);
                _prefabByKey.Add(key, prefab);
                Prewarm(pool);
            }
        }

        /// <summary>키의 풀에서 오브젝트를 꺼내 지정 위치/회전으로 배치하고 켠다.</summary>
        // [호출] ObjectPoolManager.Get에서 키 확인 후 호출된다.
        public GameObject Get(string key, Vector3 position, Quaternion rotation)
        {
            // 풀에 남는 게 있으면 그걸, 없으면 CreateObject로 새로 만들어 돌려준다.
            GameObject obj = _pools[key].Get();

            // 위치를 먼저 잡고 켠다. 그래야 OnEnable에서 새 위치를 볼 수 있다.
            // 사용 중 표시도 켜기 전에 한다. 그래야 OnEnable에서 IsReleased가 false로 보인다.
            obj.transform.SetPositionAndRotation(position, rotation);
            obj.GetComponent<PooledObject>().PrepareForGet();
            obj.SetActive(true);
            return obj;
        }

        /// <summary>키에 등록된 프리팹을 돌려준다.</summary>
        // [호출] ObjectPoolManager.RegisterPool에서 "프리팹 → 키" 표를 만들 때 호출된다.
        public GameObject GetPrefab(string key)
        {
            return _prefabByKey[key];
        }

        /// <summary>오브젝트를 키의 풀에 돌려놓는다.</summary>
        // [호출] PooledObject.Release에서 호출된다. 사용하는 쪽은 ObjectPoolManager.Release를 쓴다.
        public void Release(string key, PooledObject pooled)
        {
            // 풀이 OnReleaseObject를 불러 끈다. 풀이 꽉 찼으면 OnDestroyObject로 파괴한다.
            _pools[key].Release(pooled.gameObject);
        }
        #endregion

        #region Private Methods
        // 풀에서 _prewarmCount개를 꺼냈다가 바로 반납해, 꺼진 상태로 보관해 둔다.
        // 매니저 Get과 달리 켜지 않으므로 OnEnable이 추가로 불리지 않고, 풀의 개수 집계(CountAll 등)도 맞게 유지된다.
        // [호출] Initialize에서 키마다 풀을 만든 직후 한 번 호출된다.
        private void Prewarm(ObjectPool<GameObject> pool)
        {
            // maxSize를 넘게 만들면 반납 때 넘친 만큼 바로 파괴되므로 maxSize까지만 만든다.
            int count = Mathf.Min(_prewarmCount, _maxSize);

            List<GameObject> created = new List<GameObject>(count);
            for (int i = 0; i < count; i++)
                created.Add(pool.Get());

            foreach (GameObject obj in created)
                pool.Release(obj);
        }

        // 풀이 비어 있을 때 새 오브젝트를 만든다. 매니저 아래에 꺼진 상태로 만든다.
        // [호출] ObjectPool이 Get 시 남은 오브젝트가 없을 때 자동 호출한다. (Prewarm 포함)
        private GameObject CreateObject(string key, GameObject prefab)
        {
            GameObject obj = Object.Instantiate(prefab, _root);
            obj.SetActive(false);

            // 돌아갈 곳(이 카테고리 + 키)을 기억하게 한다. 프리팹에 PooledObject가 없으면 자동으로 붙인다.
            // 그래서 프리팹을 만드는 사람이 PooledObject를 직접 붙일 필요가 없다.
            if (!obj.TryGetComponent(out PooledObject pooled))
                pooled = obj.AddComponent<PooledObject>();

            pooled.Bind(this, key);
            return obj;
        }

        // 반납된 오브젝트를 끄고 매니저 아래로 돌려놓는다.
        // [호출] ObjectPool이 Release 시 자동 호출한다.
        private void OnReleaseObject(GameObject obj)
        {
            // 반납 표시를 끄기 전에 한다. 그래야 OnDisable에서 IsReleased가 true로 보인다.
            obj.GetComponent<PooledObject>().PrepareForRelease();

            obj.SetActive(false);
            obj.transform.SetParent(_root);
        }

        // 풀이 꽉 차서 보관할 수 없는 오브젝트를 파괴한다.
        // [호출] ObjectPool이 maxSize를 넘는 반납을 받았을 때 자동 호출한다.
        private void OnDestroyObject(GameObject obj)
        {
            Object.Destroy(obj);
        }
        #endregion
    }
}
