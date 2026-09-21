using UnityEngine;

namespace Platform
{
    /// <summary>
    /// 최초 생성 파이프에서 삭제 대상 앱 아이콘을 일정 간격으로 만들어 forward 방향으로 직선 이동시킨다.
    /// 설정한 이동 거리에 도달하면 "분쇄기에 도착"한 것으로 보고 삭제(반환)하며, 도착 이벤트를 알린다.
    /// 장식 연출용이라 충돌/판정은 없다. 앱 프리팹의 이동 처리는 PlatformMover를 그대로 재사용한다.
    /// </summary>
    public class AppSpawner : MonoBehaviour
    {
        #region Serialized Fields
        [Header("References")]
        [Tooltip("생성할 앱 아이콘 프리팹들 (PlatformMover 필요). 생성할 때마다 이 중에서 랜덤으로 고른다.")]
        [SerializeField] private PlatformMover[] _appPrefabs;

        [Tooltip("앱이 생성될 때 터지는 빛 이펙트 (선택)")]
        [SerializeField] private ParticleSystem _spawnFlash;

        [Header("Movement")]
        [Tooltip("앱 이동 속도")]
        [SerializeField, Min(0.01f)] private float _appSpeed = 5.0f;

        [Tooltip("생성 위치에서 이 거리만큼 이동하면 분쇄기에 도착한 것으로 보고 삭제한다. 씬 뷰의 Gizmo(노란 구)로 도착 지점을 확인한다.")]
        [SerializeField, Min(0.1f)] private float _travelDistance = 30.0f;

        [Header("Spawn")]
        [SerializeField, Min(0.1f)] private float _minInterval = 2.0f;
        [SerializeField, Min(0.1f)] private float _maxInterval = 4.0f;
        [SerializeField] private bool _autoStart = true;
        #endregion

        #region Private Fields
        private bool  _isSpawning;
        private float _elapsedSinceLastSpawn;
        private float _nextInterval;
        #endregion

        #region Events
        /// <summary>
        /// 앱이 생성될 때 앱의 월드 위치와 함께 호출된다.
        /// </summary>
        public event System.Action<Vector3> OnAppSpawned;

        /// <summary>
        /// 앱이 도착 지점에 닿아 반환될 때 앱의 월드 위치와 함께 호출된다.
        /// </summary>
        public event System.Action<Vector3> OnAppArrived;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            bool hasPrefab = (_appPrefabs != null) && (_appPrefabs.Length > 0);
            Debug.Assert(hasPrefab, $"[{name}] 앱 프리팹이 연결되지 않았습니다.");
        }

        private void Start()
        {
            if (_autoStart)
            {
                StartSpawning();
            }
        }

        private void Update()
        {
            if (!_isSpawning) return;

            _elapsedSinceLastSpawn += Time.deltaTime;

            if (_elapsedSinceLastSpawn >= _nextInterval)
            {
                SpawnApp();
            }
        }

        private void OnValidate()
        {
            _maxInterval = Mathf.Max(_minInterval, _maxInterval);
        }

        private void OnDrawGizmosSelected()
        {
            Vector3 origin = transform.position;
            Vector3 endPoint = origin + (transform.forward * _travelDistance);

            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(origin, endPoint);
            Gizmos.DrawWireSphere(endPoint, 1.0f);
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// 앱 생성을 시작한다. 첫 앱은 즉시 생성된다.
        /// </summary>
        public void StartSpawning()
        {
            _isSpawning = true;
            _elapsedSinceLastSpawn = 0.0f;
            _nextInterval = 0.0f;
        }

        /// <summary>
        /// 앱 생성을 멈춘다. 이미 생성된 앱은 계속 이동한다.
        /// </summary>
        public void StopSpawning()
        {
            _isSpawning = false;
        }
        #endregion

        #region Private Methods
        private void SpawnApp()
        {
            _elapsedSinceLastSpawn = 0.0f;
            _nextInterval = Random.Range(_minInterval, _maxInterval);

            bool hasPrefab = (_appPrefabs != null) && (_appPrefabs.Length > 0);
            if (!hasPrefab) return;

            PlatformMover prefab = _appPrefabs[Random.Range(0, _appPrefabs.Length)];

            // TODO: 오브젝트 풀 도입 시 풀에서 꺼내는 코드로 교체
            PlatformMover app = Instantiate(prefab, transform.position, prefab.transform.rotation);
            app.Initialize(transform.forward, _appSpeed, _travelDistance, ReleaseApp);

            if (_spawnFlash != null)
            {
                _spawnFlash.transform.position = transform.position;
                _spawnFlash.Emit(1);
            }

            OnAppSpawned?.Invoke(transform.position);
        }

        private void ReleaseApp(PlatformMover app)
        {
            OnAppArrived?.Invoke(app.transform.position);

            // TODO: 오브젝트 풀 도입 시 풀에 반환하는 코드로 교체
            Destroy(app.gameObject);
        }
        #endregion
    }
}
