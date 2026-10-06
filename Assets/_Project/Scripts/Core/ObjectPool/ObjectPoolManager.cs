using UnityEngine;

namespace Core.ObjectPool
{
    /// <summary>
    /// 오브젝트 풀링 시스템의 진입점 싱글톤 매니저.
    /// MonsterPool, BulletPool, EffectPool, NpcPool을 초기화하고 외부에 단일 인터페이스를 제공한다.
    /// </summary>
    public class ObjectPoolManager : MonoBehaviour
    {
        #region Singleton
        public static ObjectPoolManager Instance { get; private set; }
        #endregion

        #region Serialized Fields
        [Header("Sub Pools (각 풀 컴포넌트를 같은 GameObject에 추가하거나 Inspector에서 연결)")]
        [SerializeField] private MonsterPool _monsterPool;
        [SerializeField] private BulletPool  _bulletPool;
        [SerializeField] private EffectPool  _effectPool;
        [SerializeField] private NpcPool     _npcPool;
        #endregion

        #region Properties
        public MonsterPool Monster => _monsterPool;
        public BulletPool  Bullet  => _bulletPool;
        public EffectPool  Effect  => _effectPool;
        public NpcPool     Npc     => _npcPool;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            InitializePools();
        }
        #endregion

        #region Private Methods
        private void InitializePools()
        {
            Debug.Assert(_monsterPool != null, "[ObjectPoolManager] MonsterPool이 연결되지 않았습니다.");
            Debug.Assert(_bulletPool  != null, "[ObjectPoolManager] BulletPool이 연결되지 않았습니다.");
            Debug.Assert(_effectPool  != null, "[ObjectPoolManager] EffectPool이 연결되지 않았습니다.");
            Debug.Assert(_npcPool     != null, "[ObjectPoolManager] NpcPool이 연결되지 않았습니다.");

            _monsterPool.Initialize(transform);
            _bulletPool.Initialize(transform);
            _effectPool.Initialize(transform);
            _npcPool.Initialize(transform);
        }
        #endregion
    }
}
