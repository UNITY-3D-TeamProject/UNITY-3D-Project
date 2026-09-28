using UnityEngine;

namespace Platform
{
    /// <summary>
    /// 파이프(이 오브젝트)의 원형 단면 안에서 발판을 랜덤 위치에 생성한다.
    /// 새 발판은 직전 발판에서 2단 점프로 닿을 수 있는 범위 안에서만 생성된다.
    /// 이 오브젝트의 forward가 발판이 나가는 방향, right/up이 원형 단면의 축이다.
    /// </summary>
    public class PlatformSpawner : MonoBehaviour
    {
        #region Constants
        private const int GIZMO_CIRCLE_SEGMENTS = 32;
        #endregion

        #region Serialized Fields
        [Header("References")]
        [Tooltip("생성할 발판 프리팹 (PlatformMover 필요)")]
        [SerializeField] private PlatformMover _platformPrefab;

        [Header("Movement")]
        [Tooltip("발판 이동 속도. 모든 발판이 같은 속도여야 발판 사이 간격이 유지되어 도달 가능 범위가 보장된다. 플레이 중 변경하면 이후 생성되는 발판부터 적용된다.")]
        [SerializeField, Min(0.01f)] private float _platformSpeed = 3.0f;

        [Tooltip("파이프에서 이 거리만큼 이동하면 발판을 삭제(반환)한다.")]
        [SerializeField, Min(0.1f)] private float _maxTravelDistance = 40.0f;

        [Header("Spawn Area")]
        [Tooltip("발판이 생성될 수 있는 원형 단면의 반지름 (파이프 안쪽 반지름 - 발판 크기 여유)")]
        [SerializeField, Min(0.0f)] private float _spawnRadius = 1.5f;

        [Header("Spawn Gap")]
        [Tooltip("직전 발판과의 최소 이동 방향 간격")]
        [SerializeField, Min(0.1f)] private float _minGap = 3.0f;

        [Tooltip("직전 발판과의 최대 이동 방향 간격")]
        [SerializeField, Min(0.1f)] private float _maxGap = 5.0f;

        [Header("Reach Limits (2단 점프 기준, 발판 중심 간 거리)")]
        [Tooltip("직전 발판과의 최대 수평 거리")]
        [SerializeField, Min(0.0f)] private float _maxHorizontalDistance = 5.0f;

        [Tooltip("직전 발판보다 높은 쪽으로 허용하는 최대 높이 차")]
        [SerializeField, Min(0.0f)] private float _maxJumpUpHeight = 2.0f;

        [Tooltip("직전 발판보다 낮은 쪽으로 허용하는 최대 높이 차")]
        [SerializeField, Min(0.0f)] private float _maxDropHeight = 3.0f;

        [Tooltip("도달 가능한 위치를 찾기 위한 랜덤 재시도 횟수. 모두 실패하면 직전 발판과 같은 단면 위치에 생성한다.")]
        [SerializeField, Min(1)] private int _maxPlacementTries = 10;

        [Header("Options")]
        [SerializeField] private bool _autoStart = true;
        #endregion

        #region Private Fields
        private bool    _isSpawning;
        private bool    _hasLastPlatform;
        private Vector3 _lastPlatformPosition;
        private Vector2 _lastSpawnOffset;
        private float   _distanceSinceLastSpawn;
        private float   _nextGap;
        #endregion

        #region Events
        /// <summary>
        /// 발판이 생성될 때 발판의 월드 위치와 함께 호출된다.
        /// </summary>
        public event System.Action<Vector3> OnPlatformSpawned;

        /// <summary>
        /// 발판이 삭제 지점에 도달해 반환될 때 발판의 월드 위치와 함께 호출된다.
        /// </summary>
        public event System.Action<Vector3> OnPlatformReleased;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            Debug.Assert(_platformPrefab != null, $"[{name}] 발판 프리팹이 연결되지 않았습니다.");
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

            // 직전 발판도 같은 속도로 이동하므로, 참조 없이 위치를 직접 추적한다.
            float step = _platformSpeed * Time.deltaTime;
            _lastPlatformPosition += transform.forward * step;
            _distanceSinceLastSpawn += step;

            if (_distanceSinceLastSpawn >= _nextGap)
            {
                SpawnPlatform();
            }
        }

        private void OnValidate()
        {
            _maxGap = Mathf.Max(_minGap, _maxGap);
            _maxGap = Mathf.Min(_maxGap, Mathf.Max(_minGap, _maxHorizontalDistance));
            _maxTravelDistance = Mathf.Max(_maxGap, _maxTravelDistance);
        }

        private void OnDrawGizmosSelected()
        {
            Vector3 origin = transform.position;
            Vector3 forward = transform.forward;

            // 스폰 단면 (원)
            Gizmos.color = Color.cyan;
            DrawCircle(origin, transform.right, transform.up, _spawnRadius);

            // 이동 경로와 삭제 지점
            Vector3 endPoint = origin + (forward * _maxTravelDistance);
            Gizmos.color = Color.red;
            Gizmos.DrawLine(origin, endPoint);
            Gizmos.DrawWireSphere(endPoint, 0.5f);

            // 도달 범위: 직전 발판이 최대 간격만큼 나간 지점 기준 (위/아래 한계 높이의 원 2개)
            Vector3 reachCenter = origin + (forward * _maxGap);
            Vector3 upperCenter = reachCenter + (Vector3.up * _maxJumpUpHeight);
            Vector3 lowerCenter = reachCenter - (Vector3.up * _maxDropHeight);
            Gizmos.color = Color.green;
            DrawCircle(upperCenter, Vector3.right, Vector3.forward, _maxHorizontalDistance);
            DrawCircle(lowerCenter, Vector3.right, Vector3.forward, _maxHorizontalDistance);
            Gizmos.DrawLine(upperCenter, lowerCenter);
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// 발판 생성을 시작한다. 첫 발판은 즉시 생성된다.
        /// </summary>
        public void StartSpawning()
        {
            _isSpawning = true;
            _hasLastPlatform = false;
            _distanceSinceLastSpawn = 0.0f;
            _nextGap = 0.0f;
        }

        /// <summary>
        /// 발판 생성을 멈춘다. 이미 생성된 발판은 계속 이동한다.
        /// </summary>
        public void StopSpawning()
        {
            _isSpawning = false;
        }
        #endregion

        #region Private Methods
        private void SpawnPlatform()
        {
            Vector3 spawnPosition = FindSpawnPosition();

            // TODO: 오브젝트 풀 도입 시 풀에서 꺼내는 코드로 교체
            PlatformMover platform = Instantiate(_platformPrefab, spawnPosition, _platformPrefab.transform.rotation);
            platform.Initialize(transform.forward, _platformSpeed, _maxTravelDistance, ReleasePlatform);

            _hasLastPlatform = true;
            _lastPlatformPosition = spawnPosition;
            _distanceSinceLastSpawn = 0.0f;
            _nextGap = Random.Range(_minGap, _maxGap);

            OnPlatformSpawned?.Invoke(spawnPosition);
        }

        private void ReleasePlatform(PlatformMover platform)
        {
            OnPlatformReleased?.Invoke(platform.transform.position);

            // TODO: 오브젝트 풀 도입 시 풀에 반환하는 코드로 교체
            Destroy(platform.gameObject);
        }

        private Vector3 FindSpawnPosition()
        {
            // 재시도가 모두 실패하면 직전 발판과 같은 단면 위치를 사용한다.
            Vector2 chosenOffset = _lastSpawnOffset;

            for (int i = 0; i < _maxPlacementTries; i++)
            {
                Vector2 candidateOffset = Random.insideUnitCircle * _spawnRadius;

                if (!_hasLastPlatform || IsReachable(ToWorldPosition(candidateOffset)))
                {
                    chosenOffset = candidateOffset;
                    break;
                }
            }

            _lastSpawnOffset = chosenOffset;
            return ToWorldPosition(chosenOffset);
        }

        private bool IsReachable(Vector3 candidatePosition)
        {
            Vector3 delta = candidatePosition - _lastPlatformPosition;
            float heightDelta = delta.y;
            delta.y = 0.0f;

            bool isHorizontalReachable = delta.magnitude <= _maxHorizontalDistance;
            bool isHeightReachable = (heightDelta <= _maxJumpUpHeight) && (heightDelta >= -_maxDropHeight);

            return isHorizontalReachable && isHeightReachable;
        }

        private Vector3 ToWorldPosition(Vector2 offset)
        {
            return transform.position + (transform.right * offset.x) + (transform.up * offset.y);
        }

        private static void DrawCircle(Vector3 center, Vector3 axisA, Vector3 axisB, float radius)
        {
            Vector3 previous = center + (axisA * radius);

            for (int i = 1; i <= GIZMO_CIRCLE_SEGMENTS; i++)
            {
                float angle = (i / (float)GIZMO_CIRCLE_SEGMENTS) * Mathf.PI * 2.0f;
                Vector3 next = center + (((axisA * Mathf.Cos(angle)) + (axisB * Mathf.Sin(angle))) * radius);
                Gizmos.DrawLine(previous, next);
                previous = next;
            }
        }
        #endregion
    }
}
