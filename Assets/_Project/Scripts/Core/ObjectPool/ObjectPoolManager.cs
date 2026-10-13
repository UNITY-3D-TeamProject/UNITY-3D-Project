using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.Serialization;

namespace Core.ObjectPool
{
    /// <summary>
    /// 씬 단위 오브젝트 풀 매니저. 로비·각 스테이지 씬에 하나씩 배치한다. (싱글톤·DontDestroyOnLoad 아님)
    /// 인스펙터에 "키 → 프리팹"을 등록해 두고, 사용하는 쪽은 Get(키)로 꺼낸다.
    /// 씬이 내려가면 풀과 풀에서 나온 오브젝트도 함께 정리된다.
    /// </summary>
    // 사용 흐름
    // 1. 씬에 이 컴포넌트를 배치하고, 인스펙터의 카테고리 목록에 "키 → 프리팹"을 등록한다.
    // 2. Awake에서 키마다 빈 풀을 하나씩 만들어 "키 → 풀" 표(_pools)에 넣는다.
    // 3. 사용하는 쪽은 이 매니저 참조를 들고 Get("Monster1", 위치, 회전)으로 꺼낸다.
    //    (씬 배치 오브젝트는 인스펙터로 연결, 런타임 생성 오브젝트는 생성한 쪽이 넘겨준다.)
    // 4. 다 쓴 오브젝트는 매니저를 몰라도 PooledObject.ReleaseOrDestroy(gameObject)로 자기 풀에 돌아간다.
    public class ObjectPoolManager : MonoBehaviour
    {
        #region Constants
        // 처음 준비할 칸 수, 넘으면 자동으로 늘어남
        private const int DEFAULT_CAPACITY = 10;
        // 넘치면 Destroy함 
        private const int MAX_SIZE         = 50;
        #endregion

        #region Serialized Fields
        // 카테고리별 목록은 인스펙터에서 보기 좋게 나눠 둔 것일 뿐, 모두 같은 표(_pools)에 들어간다.
        // 그래서 꺼낼 때는 키만 넘기면 되고, 키는 모든 목록을 통틀어 겹치면 안 된다.
        // 씬에서 쓰지 않는 카테고리는 비워 둬도 된다.

        // 인스펙터에 나오는 주문서 목록 3개 => 실제 풀이 아님
        [Header("Pools (키 → 프리팹, 키는 모든 목록에서 유일해야 한다)")]
        [FormerlySerializedAs("_monsters")]
        [SerializeField] private List<SPoolEntry> _enemies       = new();   // 적 (근접·원거리·특수)
        [SerializeField] private List<SPoolEntry> _playerBullets = new();   // 플레이어가 쏘는 총알
        [SerializeField] private List<SPoolEntry> _enemyBullets  = new();   // 적이 쏘는 총알

        // 카테고리별 최대 개수. 그 카테고리에 등록된 키 하나하나에 적용된다.
        // 예: Enemies에 Melee, Ranged가 있고 값이 20이면 Melee 풀 20개, Ranged 풀 20개까지 보관한다.
        // 넘치는 반납분은 보관하지 않고 Destroy된다.
        [Header("Max Size (카테고리별, 키 하나당 보관 최대 개수)")]
        [FormerlySerializedAs("_monstersMaxSize")]
        [SerializeField, Min(1)] private int _enemiesMaxSize       = MAX_SIZE;
        [SerializeField, Min(1)] private int _playerBulletsMaxSize = MAX_SIZE;
        [SerializeField, Min(1)] private int _enemyBulletsMaxSize  = MAX_SIZE;

        [Header("Capacity (모든 키에 공통)")]
        [Tooltip("풀을 만들 때 미리 잡아 둘 목록 크기. 오브젝트를 미리 만들지는 않는다.")]
        [SerializeField, Min(0)] private int _defaultCapacity = DEFAULT_CAPACITY;
        #endregion

        #region Private Fields
        /// <summary>키 → 그 키의 풀</summary>
        // Awake 전에는 null이다. (Get에서 초기화 여부 확인용으로도 쓴다.)
        // 여러 종류의 오브젝트풀을 게임매니저가 갖고있음 
        private Dictionary<string, ObjectPool<GameObject>> _pools;
        #endregion

        #region Nested Types
        // 인스펙터에서 키와 프리팹을 한 쌍씩 입력받기 위한 칸.
        // 유니티 인스펙터는 Dictionary를 보여 주지 못하므로 List로 입력받고 Awake에서 _pools로 옮긴다.
        [Serializable]
        private struct SPoolEntry
        {
            [SerializeField] private string     _key;
            [SerializeField] private GameObject _prefab;

            public string     Key    => _key;
            public GameObject Prefab => _prefab;
        }
        #endregion

        #region Unity Lifecycle
        // 씬이 로드되면 호출된다. 이 시점에는 빈 풀만 만들고 오브젝트는 만들지 않는다.
        // (오브젝트는 처음 Get할 때 필요한 만큼 생성된다.)
        private void Awake()
        {
            // 빈 딕셔너리 객체 하나 만든다.
            _pools = new Dictionary<string, ObjectPool<GameObject>>();

            // 키가 여러 목록에 중복되면 먼저 등록한 쪽이 이긴다. (이 순서대로)
            // 두 번째 값은 그 카테고리의 최대 개수다.
            RegisterEntries(_enemies, _enemiesMaxSize);
            RegisterEntries(_playerBullets, _playerBulletsMaxSize);
            RegisterEntries(_enemyBullets, _enemyBulletsMaxSize);
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// 키에 등록된 프리팹의 풀에서 오브젝트를 꺼내 지정 위치/회전으로 배치한다.
        /// </summary>
        /// <param name="key">인스펙터에 등록한 키 (예: "Monster1")</param>
        /// <returns>꺼낸 오브젝트. 키가 없으면 null</returns>
        // [호출] 몬스터 스포너, 총알 생성기 등 오브젝트가 필요한 쪽에서 호출한다.
        //        Awake에서 초기화하므로 다른 컴포넌트의 Start 이후에 호출해야 한다.
        public GameObject Get(string key, Vector3 position, Quaternion rotation)
        {
            // Awake가 아직 안 돌았으면 표가 없다. (다른 컴포넌트의 Awake에서 호출한 경우)
            if (_pools == null)
            {
                Debug.LogError("[ObjectPoolManager] 초기화 전에 Get을 요청했습니다. Awake 이후(Start 등)에 사용하세요.", this);
                return null;
            }

            // 빈 키이거나 표에 없는 키(오타·미등록)면 꺼낼 수 없다.
            if (string.IsNullOrEmpty(key) || !_pools.TryGetValue(key, out ObjectPool<GameObject> pool))
            {
                Debug.LogError($"[ObjectPoolManager] 등록되지 않은 키입니다: '{key}'", this);
                return null;
            }

            // 풀에 남는 게 있으면 그걸, 없으면 CreateObject로 새로 만들어 돌려준다.
            GameObject obj = pool.Get();

            // 위치를 먼저 잡고 켠다. 그래야 OnEnable·IPoolable.OnGet에서 새 위치를 볼 수 있다.
            obj.transform.SetPositionAndRotation(position, rotation);
            obj.SetActive(true);
            obj.GetComponent<PooledObject>().PrepareForGet();
            return obj;
        }

        /// <summary>
        /// 키에 등록된 프리팹의 풀에서 오브젝트를 꺼내 T 컴포넌트로 돌려준다.
        /// </summary>
        /// <returns>꺼낸 오브젝트의 T 컴포넌트. 키가 없거나 T가 없으면 null</returns>
        // [호출] 꺼낸 직후 컴포넌트로 초기화해야 할 때 사용한다.
        //        예: BulletController를 받아 Initialize(방향, 발사자)를 호출
        //        Get(key, ...) + GetComponent<T>()를 한 번에 하는 편의 함수다.
        public T Get<T>(string key, Vector3 position, Quaternion rotation) where T : Component
        {
            GameObject obj = Get(key, position, rotation);
            if (obj == null) return null;

            // 프리팹에 T가 없으면 등록 실수다. 오브젝트는 이미 꺼내졌으므로 null만 돌려준다.
            if (!obj.TryGetComponent(out T component))
                Debug.LogError($"[ObjectPoolManager] 키 '{key}'의 프리팹에 {typeof(T).Name}이 없습니다.", obj);

            return component;
        }
        #endregion

        #region Internal Methods
        /// <summary>오브젝트를 키의 풀에 돌려놓는다.</summary>
        // [호출] PooledObject.Release에서 호출된다. 사용하는 쪽은 PooledObject.ReleaseOrDestroy를 쓴다.
        internal void Release(string key, PooledObject pooled)
        {
            // 풀이 OnReleaseObject를 불러 정리·비활성화한다. 풀이 꽉 찼으면 OnDestroyObject로 파괴한다.
            _pools[key].Release(pooled.gameObject);
        }
        #endregion

        #region Private Methods
        // 목록의 항목마다 빈 풀을 하나씩 만들어 표에 넣는다. 만든 풀은 모두 maxSize개까지 보관한다.
        // [호출] Awake에서 카테고리 목록마다 한 번씩 호출된다.
        private void RegisterEntries(List<SPoolEntry> entries, int maxSize)
        {
            foreach (SPoolEntry entry in entries)
            {
                if (string.IsNullOrEmpty(entry.Key) || (entry.Prefab == null))
                {
                    Debug.LogWarning("[ObjectPoolManager] 키 또는 프리팹이 비어 있는 항목은 건너뜁니다.", this);
                    continue;
                }

                if (_pools.ContainsKey(entry.Key))
                {
                    Debug.LogError($"[ObjectPoolManager] 키 '{entry.Key}'가 중복 등록되었습니다. 뒤의 항목은 무시됩니다.", this);
                    continue;
                }

                // 람다가 entry 대신 지역 변수를 기억하도록 따로 담는다.
                string     key    = entry.Key;
                GameObject prefab = entry.Prefab;

                // 유니티 내장 풀. 꺼낼 게 없으면 createFunc로 만들고, 반납 시 actionOnRelease를 부른다.
                // 비어있는 _pools 에다가 Enemies(melee,range)를 하나씩 만든다.
                _pools.Add(key, new ObjectPool<GameObject>(
                    createFunc:      () => CreateObject(key, prefab),
                    actionOnGet:     null,
                    actionOnRelease: OnReleaseObject,
                    actionOnDestroy: OnDestroyObject,
                    collectionCheck: true,
                    defaultCapacity: _defaultCapacity,
                    maxSize:         maxSize));
            }
        }

        // 풀이 비어 있을 때 새 오브젝트를 만든다. 매니저 아래에 꺼진 상태로 만든다.
        // [호출] ObjectPool이 Get 시 남은 오브젝트가 없을 때 자동 호출한다.
        private GameObject CreateObject(string key, GameObject prefab)
        {
            GameObject obj = Instantiate(prefab, transform);
            obj.SetActive(false);

            // 돌아갈 곳(이 매니저 + 키)을 기억하게 한다. 프리팹에 PooledObject가 없으면 자동으로 붙인다.
            // 그래서 프리팹을 만드는 사람이 PooledObject를 직접 붙일 필요가 없다.
            if (!obj.TryGetComponent(out PooledObject pooled))
                pooled = obj.AddComponent<PooledObject>();

            // 돌아갈곳 기억하기
            pooled.Bind(this, key);
            return obj;
        }

        // 반납된 오브젝트를 정리하고 끈다.
        // [호출] ObjectPool이 Release 시 자동 호출한다.
        private void OnReleaseObject(GameObject obj)
        {
            obj.GetComponent<PooledObject>().PrepareForRelease();

            obj.SetActive(false);
            obj.transform.SetParent(transform);
        }

        // 풀이 꽉 차서 보관할 수 없는 오브젝트를 파괴한다.
        // [호출] ObjectPool이 maxSize를 넘는 반납을 받았을 때 자동 호출한다.
        private void OnDestroyObject(GameObject obj)
        {
            Destroy(obj);
        }
        #endregion
    }
}
