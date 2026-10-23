using System.Collections.Generic;
using UnityEngine;

namespace Core.ObjectPool
{
    /// <summary>
    /// 씬 단위 오브젝트 풀 매니저. 풀이 필요한 씬에만 하나씩 배치한다.
    /// 어디서든 ObjectPoolManager.Instance로 접근하지만, DontDestroyOnLoad가 아니라 씬이 내려가면 함께 사라진다.
    /// 카테고리별 PrefabPool을 들고 있고, 사용하는 쪽은 Get(키)로 꺼낸다.
    /// 씬이 내려가면 풀과 풀에서 나온 오브젝트도 함께 정리된다.
    /// 사용하는 쪽은 이 클래스만 알면 된다. (꺼내기 Get / 반납 Release)
    /// </summary>
    // 사용 흐름
    // 1. 풀이 필요한 씬에 이 컴포넌트를 배치하고, 인스펙터의 카테고리(PrefabPool)마다 "키 → 프리팹"을 등록한다.
    //    풀이 필요 없는 씬(로비·피드 등)에는 배치하지 않는다. 그 씬에서는 Instance가 null이다.
    // 2. Awake에서 각 PrefabPool이 키마다 풀을 만들어 그 항목의 Initial Count만큼 미리 채우고, 매니저는 "키 → 카테고리" 표(_poolByKey)를 만든다.
    // 3. 사용하는 쪽은 ObjectPoolManager.Instance.Get("MeleeEnemy", 위치, 회전)으로 꺼낸다.
    //    Instance는 Awake에서 채워지므로 다른 컴포넌트의 Start 이후에 사용한다.
    // 4. 다 쓴 오브젝트는 ObjectPoolManager.Instance.Release(gameObject)로 자기 풀에 돌아간다.
    //    어느 풀에서 왔는지는 풀이 생성할 때 자동으로 붙인 PooledObject가 기억하므로 키를 넘기지 않는다.
    // 5. 재사용 시 초기화는 프리팹 컴포넌트가 OnEnable에서 직접 한다. (Awake는 생성 때 한 번뿐)
    public class ObjectPoolManager : MonoBehaviour
    {
        #region Serialized Fields
        // 카테고리는 인스펙터 정리와 개수 설정용이다. 꺼낼 때는 키만 넘기면 된다.
        // 그래서 키는 모든 카테고리를 통틀어 겹치면 안 된다. 씬에서 쓰지 않는 카테고리는 비워 둬도 된다.
        [Header("Enemies (근접·원거리·특수)")]
        [SerializeField] private PrefabPool _enemies       = new();

        [Header("Player Bullets")]
        [SerializeField] private PrefabPool _playerBullets = new();

        // 차지 총알은 일반 총알보다 적게 쓰이므로 개수를 따로 정하기 위해 분리한다.
        [Header("Player Charged Bullets")]
        [SerializeField] private PrefabPool _playerChargedBullets = new();

        [Header("Enemy Bullets")]
        [SerializeField] private PrefabPool _enemyBullets  = new();

        [Header("Crowd (차량·보행자)")]
        [SerializeField] private PrefabPool _crowd         = new();
        #endregion

        #region Private Fields
        /// <summary>키 → 그 키가 등록된 카테고리</summary>
        // Awake 전에는 null이다. (Get에서 초기화 여부 확인용으로도 쓴다.)
        // 매니저가 받은 키를 어느 PrefabPool에 넘길지 찾는 표
        private Dictionary<string, PrefabPool> _poolByKey;

        /// <summary>프리팹 → 그 프리팹이 등록된 키</summary>
        // 프리팹 칸을 가진 쪽(스포너 등)이 키 문자열 없이 Get(프리팹)으로 꺼낼 수 있게 한다.
        private Dictionary<GameObject, string> _keyByPrefab;
        #endregion

        #region Properties
        /// <summary>지금 씬의 풀 매니저. 매니저를 배치하지 않은 씬이거나 Awake 전이면 null이다.</summary>
        // DontDestroyOnLoad를 쓰지 않으므로 공용 Singleton<T>을 상속하지 않는다.
        public static ObjectPoolManager Instance { get; private set; }
        #endregion

        #region Unity Lifecycle
        // 씬이 로드되면 호출된다. 키마다 풀을 만들고 그 항목의 Initial Count만큼 꺼진 오브젝트를 미리 만들어 둔다.
        // (미리 만든 것을 다 쓰면 그 뒤로는 Get할 때 필요한 만큼 추가로 생성된다.)
        private void Awake()
        {
            // 같은 씬에 매니저가 둘이면 배치 실수다. 키가 어느 매니저에 있는지 헷갈리므로 알린다.
            // (이전 씬의 매니저가 아직 파괴되지 않은 경우는 실수가 아니므로 씬으로 구분한다.)
            if ((Instance != null) && (Instance.gameObject.scene == gameObject.scene))
                Debug.LogError("[ObjectPoolManager] 한 씬에 매니저가 두 개 있습니다. 나중에 Awake된 쪽이 Instance가 됩니다.", this);

            Instance = this;

            _poolByKey = new Dictionary<string, PrefabPool>();
            _keyByPrefab = new Dictionary<GameObject, string>();

            // 키가 여러 카테고리에 중복되면 먼저 등록한 쪽이 이긴다. (이 순서대로)
            RegisterPool(_enemies);
            RegisterPool(_playerBullets);
            RegisterPool(_playerChargedBullets);
            RegisterPool(_enemyBullets);
            RegisterPool(_crowd);
        }

        // 씬이 내려가 매니저가 파괴될 때 Instance를 비운다.
        // 새 씬의 매니저가 먼저 Instance가 됐다면 그 참조는 지우지 않는다.
        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// 키에 등록된 프리팹의 풀에서 오브젝트를 꺼내 지정 위치/회전으로 배치한다.
        /// </summary>
        /// <param name="key">인스펙터에 등록한 키 (예: "MeleeEnemy")</param>
        /// <returns>꺼낸 오브젝트. 키가 없으면 null</returns>
        // [호출] 몬스터 스포너, 총알 생성기 등 오브젝트가 필요한 쪽에서 호출한다.
        //        Awake에서 초기화하므로 다른 컴포넌트의 Start 이후에 호출해야 한다.
        public GameObject Get(string key, Vector3 position, Quaternion rotation)
        {
            // Awake가 아직 안 돌았으면 표가 없다. (다른 컴포넌트의 Awake에서 호출한 경우)
            if (_poolByKey == null)
            {
                Debug.LogError("[ObjectPoolManager] 초기화 전에 Get을 요청했습니다. Awake 이후(Start 등)에 사용하세요.", this);
                return null;
            }

            // 빈 키이거나 표에 없는 키(오타·미등록)면 꺼낼 수 없다.
            if (string.IsNullOrEmpty(key) || !_poolByKey.TryGetValue(key, out PrefabPool pool))
            {
                Debug.LogError($"[ObjectPoolManager] 등록되지 않은 키입니다: '{key}'", this);
                return null;
            }

            return pool.Get(key, position, rotation);
        }

        /// <summary>
        /// 프리팹이 등록된 키의 풀에서 오브젝트를 꺼내 지정 위치/회전으로 배치한다.
        /// </summary>
        /// <param name="prefab">인스펙터에 등록한 프리팹 (예: 스포너의 몬스터 프리팹 칸)</param>
        /// <returns>꺼낸 오브젝트. 등록되지 않은 프리팹이면 null</returns>
        // [호출] 키 문자열 대신 프리팹 칸을 가진 쪽에서 호출한다. 예: MonsterSpawner
        //        프리팹 이름이 아니라 참조로 찾으므로 프리팹 이름을 바꿔도 동작한다.
        public GameObject Get(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            if (_keyByPrefab == null)
            {
                Debug.LogError("[ObjectPoolManager] 초기화 전에 Get을 요청했습니다. Awake 이후(Start 등)에 사용하세요.", this);
                return null;
            }

            if ((prefab == null) || !_keyByPrefab.TryGetValue(prefab, out string key))
            {
                Debug.LogError($"[ObjectPoolManager] 등록되지 않은 프리팹입니다: '{(prefab != null ? prefab.name : "null")}'", this);
                return null;
            }

            return Get(key, position, rotation);
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

        /// <summary>
        /// 다 쓴 오브젝트를 자기가 나온 풀에 반납한다. 풀에서 나온 게 아니면(씬 배치·Instantiate) Destroy한다.
        /// </summary>
        /// <param name="obj">반납할 오브젝트 (Get으로 받은 루트)</param>
        // [호출] 기존 Destroy(gameObject) 자리에 쓴다. 예: 총알 수명 종료·충돌, 적 사망
        //        돌아갈 풀은 이 매니저가 아니라 오브젝트에 자동으로 붙은 PooledObject가 기억한다.
        //        그래서 사용하는 쪽은 키나 PooledObject를 몰라도 된다.
        //        매니저가 없는 씬에서도 실행될 수 있는 코드라면 Instance가 null인지 먼저 확인한다.
        public void Release(GameObject obj)
        {
            if (obj == null) return;

            // PooledObject가 있고 소속 풀이 기록돼 있으면 풀 출신 → 반납
            // 그 외(PooledObject가 없거나 풀이 기록되지 않음) → 일반 오브젝트이므로 파괴
            if (obj.TryGetComponent(out PooledObject pooled) && pooled.IsBound)
                pooled.Release();
            else
                Destroy(obj);
        }
        #endregion

        #region Private Methods
        // 카테고리의 풀을 만들고, 그 카테고리의 키들을 표에 넣는다. (키 → 카테고리, 프리팹 → 키)
        // [호출] Awake에서 카테고리마다 한 번씩 호출된다.
        private void RegisterPool(PrefabPool pool)
        {
            pool.Initialize(transform, this);

            foreach (string key in pool.Keys)
            {
                if (!_poolByKey.TryAdd(key, pool))
                {
                    Debug.LogError($"[ObjectPoolManager] 키 '{key}'가 다른 카테고리에 이미 등록되었습니다. 뒤의 항목은 무시됩니다.", this);
                    continue;
                }

                // 같은 프리팹을 여러 키로 등록했으면 Get(프리팹)은 먼저 등록한 키를 쓴다.
                GameObject prefab = pool.GetPrefab(key);
                if (!_keyByPrefab.TryAdd(prefab, key))
                    Debug.LogWarning($"[ObjectPoolManager] 프리팹 '{prefab.name}'이 키 '{_keyByPrefab[prefab]}'와 '{key}'에 중복 등록되었습니다. Get(프리팹)은 '{_keyByPrefab[prefab]}'를 씁니다.", this);
            }
        }
        #endregion
    }
}
