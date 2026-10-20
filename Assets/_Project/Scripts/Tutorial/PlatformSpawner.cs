using System.Collections.Generic;
using UnityEngine;

namespace Tutorial
{
    /// <summary>
    /// 파이프(이 오브젝트)의 단면 안에서 발판을 생성해 forward 방향으로 흘려보낸다.
    /// 발판 사이 간격과 높낮이는 난이도 단계(SPlatformGapTier)를 차례로 돌며 정한다. (예: 점프 → 2단 점프 → 2단 점프 + 대시 → 다시 점프)
    /// 간격은 발판 가장자리 기준이라 발판끼리 겹치거나 붙어 나오지 않는다.
    /// 높이는 위로 올라가다가 위쪽 한계에 닿으면 내려가고, 아래쪽 한계에 닿으면 다시 올라가는 물결을 그린다.
    /// 이 오브젝트의 forward가 발판이 나가는 방향, right/up이 단면의 축이다.
    /// 컴포넌트 메뉴의 Bake Platforms 로 경로 전체 발판을 에디터에 미리 깔아 둘 수 있고,
    /// 멈춘 채 시작하면 ResumeSpawning() 을 호출할 때 움직인다.
    /// </summary>
    public class PlatformSpawner : MonoBehaviour
    {
        #region Constants
        // 높이 한 칸을 바꿀 때 단계 최대값의 최소 비율. 너무 작은 변화는 높낮이로 느껴지지 않는다.
        private const float MIN_HEIGHT_STEP_RATIO = 0.4f;
        #endregion

        #region Serialized Fields
        [Header("References")]
        [Tooltip("생성할 발판 프리팹 (PlatformMover 필요)")]
        [SerializeField] private PlatformMover _platformPrefab;

        [Header("Movement")]
        [Tooltip("발판 이동 속도. 모든 발판이 같은 속도여야 발판 사이 간격이 유지된다. 플레이 중 변경하면 이후 생성되는 발판부터 적용된다.")]
        [SerializeField, Min(0.01f)] private float _platformSpeed = 3.0f;

        [Tooltip("파이프에서 이 거리만큼 이동하면 발판을 삭제(반환)한다.")]
        [SerializeField, Min(0.1f)] private float _maxTravelDistance = 40.0f;

        [Header("Spawn Area")]
        [Tooltip("발판 중심이 좌우로 벗어날 수 있는 최대 거리")]
        [SerializeField, Min(0.0f)] private float _spawnRadius = 1.5f;

        [Tooltip("발판 중심이 위아래로 벗어날 수 있는 최대 거리. 높이 물결은 이 범위 안에서 오르내린다.")]
        [SerializeField, Min(0.0f)] private float _heightRange = 4.0f;

        [Tooltip("발판의 이동 방향 길이. 가장자리 간격을 중심 간격으로 바꿀 때 쓴다.")]
        [SerializeField, Min(0.1f)] private float _platformLength = 2.8f;

        [Header("Difficulty")]
        [Tooltip("위에서부터 차례로 Count 개씩 생성하고, 끝나면 처음으로 돌아간다.\n" +
                 "기본값 기준(점프 8, 중력 20, 이동 속도 6, 2단 점프 비율 0.8, 구르기 5m): 점프 약 1.6m 높이·4.8m 거리, 2단 점프 약 2.9m 높이·7.8m 거리")]
        [SerializeField] private SPlatformGapTier[] _tiers =
        {
            new SPlatformGapTier("점프",              5, 1.0f, 2.5f, 1.0f, 1.5f, 1.5f),
            new SPlatformGapTier("2단 점프",          5, 2.5f, 4.5f, 2.0f, 2.5f, 2.5f),
            new SPlatformGapTier("2단 점프 + 대시",   3, 5.0f, 7.0f, 0.5f, 3.0f, 2.0f),
        };

        [Header("Options")]
        [SerializeField] private bool _autoStart = true;

        [Tooltip("켜면 시작할 때 이동 경로 전체에 발판을 미리 깔아 둔다. (이미 흘러가고 있던 것처럼 보인다)\n" +
                 "에디터에서 구운 발판(Baked Platforms)이 있으면 그 발판을 대신 쓴다.")]
        [SerializeField] private bool _shouldPrewarm = true;

        [Tooltip("켜면 멈춘 상태로 시작한다. ResumeSpawning() 을 호출해야 움직인다. (예: 튜토리얼 이벤트로 작동)")]
        [SerializeField] private bool _isPausedAtStart;

        [Tooltip("에디터에서 미리 깔아 둔 발판. 컴포넌트 메뉴(⋮)의 Bake Platforms 로 만들고 Clear Baked Platforms 로 지운다.\n" +
                 "시작하면 이 발판들을 이어받아 흘려보낸다.")]
        [SerializeField] private List<PlatformMover> _bakedPlatforms = new List<PlatformMover>();
        #endregion

        #region Private Fields
        private readonly List<PlatformMover> _activePlatforms = new List<PlatformMover>();
        private bool    _isSpawning;
        private Vector3 _lastPlatformPosition;
        private float   _distanceSinceLastSpawn;

        // 다음 발판의 단면 위치(x = 좌우, y = 높이)와 직전 발판에서의 중심 간격
        private Vector2 _nextOffset;
        private float   _nextGap;

        private int   _tierIndex;
        private int   _tierSpawnCount;
        private float _heightDirection = 1.0f;
        #endregion

        #region Properties
        /// <summary>발판 흐름(생성과 이동)이 멈춰 있는지 여부.</summary>
        public bool IsPaused { get; private set; }
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
            Debug.Assert((_tiers != null) && (_tiers.Length > 0), $"[{name}] 난이도 단계가 하나도 없습니다.");

            IsPaused = _isPausedAtStart;
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
            if ((!_isSpawning) || IsPaused) return;

            // 직전 발판도 같은 속도로 이동하므로, 참조 없이 위치를 직접 추적한다.
            float step = _platformSpeed * Time.deltaTime;
            _lastPlatformPosition += transform.forward * step;
            _distanceSinceLastSpawn += step;

            if (_distanceSinceLastSpawn >= _nextGap)
            {
                SpawnPlatform(0.0f);
            }
        }

        private void OnValidate()
        {
            if (_tiers == null) return;

            for (int i = 0; i < _tiers.Length; i++)
            {
                _tiers[i].MaxGap = Mathf.Max(_tiers[i].MinGap, _tiers[i].MaxGap);
            }
        }

        private void OnDrawGizmosSelected()
        {
            Vector3 origin = transform.position;
            Vector3 forward = transform.forward;

            // 스폰 단면 (좌우 × 높이 사각형)
            Gizmos.color = Color.cyan;
            DrawRect(origin, transform.right * _spawnRadius, transform.up * _heightRange);

            // 이동 경로와 삭제 지점
            Vector3 endPoint = origin + (forward * _maxTravelDistance);
            Gizmos.color = Color.red;
            Gizmos.DrawLine(origin, endPoint);
            Gizmos.DrawWireSphere(endPoint, 0.5f);
            DrawRect(endPoint, transform.right * _spawnRadius, transform.up * _heightRange);
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// 발판 생성을 시작한다. 첫 발판은 즉시 생성된다.
        /// </summary>
        public void StartSpawning()
        {
            _isSpawning = true;
            ResetPlan();

            if (_bakedPlatforms.Count > 0)
            {
                AdoptBakedPlatforms();
            }
            else if (_shouldPrewarm)
            {
                Prewarm();
            }
        }

        /// <summary>
        /// 발판 생성을 멈춘다. 이미 생성된 발판은 계속 이동한다.
        /// </summary>
        public void StopSpawning()
        {
            _isSpawning = false;
        }

        /// <summary>
        /// 발판 흐름 전체(새 발판 생성과 이미 있는 발판의 이동)를 그 자리에서 멈춘다.
        /// </summary>
        public void PauseSpawning()
        {
            SetPaused(true);
        }

        /// <summary>
        /// 멈춘 발판 흐름을 다시 움직인다. (튜토리얼 이벤트에서 호출)
        /// </summary>
        public void ResumeSpawning()
        {
            SetPaused(false);
        }

        /// <summary>
        /// 플레이어가 올라탈 발판의 현재 위치를 돌려준다.
        /// 출구에서 preferredDistance 만큼 나아간 지점에 가장 가까운 발판을 고르고, 발판이 하나도 없으면 새로 만든다.
        /// </summary>
        /// <param name="preferredDistance">출구에서 이만큼 떨어진 지점의 발판을 고른다.</param>
        /// <returns>올라탈 발판의 중심 위치(월드 좌표)</returns>
        public Vector3 GetBoardingPosition(float preferredDistance)
        {
            PlatformMover nearest = null;
            float nearestGap = float.MaxValue;

            foreach (PlatformMover platform in _activePlatforms)
            {
                float travelledDistance = Vector3.Dot(platform.transform.position - transform.position, transform.forward);
                float gap = Mathf.Abs(travelledDistance - preferredDistance);
                if (gap < nearestGap)
                {
                    nearest = platform;
                    nearestGap = gap;
                }
            }

            if (nearest != null) return nearest.transform.position;

            SpawnPlatform(0.0f);
            return _lastPlatformPosition;
        }
        #endregion

        #region Private Methods
        private void ResetPlan()
        {
            _distanceSinceLastSpawn = 0.0f;
            _nextGap = 0.0f;
            _nextOffset = Vector2.zero;
            _tierIndex = 0;
            _tierSpawnCount = 0;
            _heightDirection = 1.0f;
        }

        private void SetPaused(bool isPaused)
        {
            IsPaused = isPaused;

            foreach (PlatformMover platform in _activePlatforms)
            {
                platform.IsPaused = isPaused;
            }
        }

        // 에디터에서 구운 발판을 이어받아 흘려보낸다. 출구에 가장 가까운 발판을 직전 발판으로 삼아 다음 발판을 정한다.
        private void AdoptBakedPlatforms()
        {
            PlatformMover nearest = null;
            float nearestDistance = float.MaxValue;

            foreach (PlatformMover platform in _bakedPlatforms)
            {
                if (platform == null) continue;

                float travelledDistance = Vector3.Dot(platform.transform.position - transform.position, transform.forward);
                platform.Initialize(transform.forward, _platformSpeed, _maxTravelDistance - travelledDistance, ReleasePlatform);
                platform.IsPaused = IsPaused;
                _activePlatforms.Add(platform);

                if (travelledDistance < nearestDistance)
                {
                    nearest = platform;
                    nearestDistance = travelledDistance;
                }
            }

            _bakedPlatforms.Clear();
            if (nearest == null) return;

            Vector3 offset = nearest.transform.position - transform.position;
            _nextOffset = new Vector2(Vector3.Dot(offset, transform.right), Vector3.Dot(offset, transform.up));
            _lastPlatformPosition = nearest.transform.position;
            _distanceSinceLastSpawn = nearestDistance;
            PlanNextPlatform();
        }

        // 삭제 지점 쪽부터 출구 쪽으로 거슬러 오며, 평소와 같은 간격 규칙으로 발판을 미리 놓는다.
        private void Prewarm()
        {
            float travelledDistance = _maxTravelDistance;

            while (true)
            {
                travelledDistance -= _nextGap;
                if (travelledDistance <= 0.0f) break;

                SpawnPlatform(travelledDistance);
            }
        }

        // travelledDistance: 출구에서 이미 이만큼 나아간 자리에 생성한다. 평소에는 0.
        private void SpawnPlatform(float travelledDistance)
        {
            Vector3 spawnPosition = ToWorldPosition(_nextOffset, travelledDistance);

            // TODO: 오브젝트 풀 도입 시 풀에서 꺼내는 코드로 교체
            PlatformMover platform = Instantiate(_platformPrefab, spawnPosition, _platformPrefab.transform.rotation);
            platform.Initialize(transform.forward, _platformSpeed, _maxTravelDistance - travelledDistance, ReleasePlatform);
            platform.IsPaused = IsPaused;
            _activePlatforms.Add(platform);

            _lastPlatformPosition = spawnPosition;
            _distanceSinceLastSpawn = travelledDistance;
            PlanNextPlatform();

            OnPlatformSpawned?.Invoke(spawnPosition);
        }

        private void ReleasePlatform(PlatformMover platform)
        {
            OnPlatformReleased?.Invoke(platform.transform.position);
            _activePlatforms.Remove(platform);

            // TODO: 오브젝트 풀 도입 시 풀에 반환하는 코드로 교체
            Destroy(platform.gameObject);
        }

        // 방금 생성한 발판을 기준으로 다음 발판의 간격, 좌우 위치, 높이를 정한다.
        private void PlanNextPlatform()
        {
            SPlatformGapTier tier = GetCurrentTier();
            Vector2 lastOffset = _nextOffset;

            // 앞뒤 가장자리 간격
            float forwardGap = Random.Range(tier.MinGap, tier.MaxGap);

            // 좌우로 벌어진 만큼도 뛰어야 하는 거리에 더해지므로, 전체 가장자리 간격이 MaxGap 을 넘지 않게 좌우 이동을 줄인다.
            float lateralEdgeLimit = Mathf.Sqrt(Mathf.Max(0.0f, (tier.MaxGap * tier.MaxGap) - (forwardGap * forwardGap)));
            float lateralLimit = Mathf.Min(tier.MaxLateralShift, _platformLength + lateralEdgeLimit);
            float lateral = Mathf.Clamp(lastOffset.x + Random.Range(-lateralLimit, lateralLimit), -_spawnRadius, _spawnRadius);

            _nextOffset = new Vector2(lateral, GetNextHeight(lastOffset.y, tier));
            _nextGap = _platformLength + forwardGap;
        }

        // 지금 방향으로 높이를 바꾸고, 범위 끝에 닿으면 방향을 뒤집어 오르내리는 물결을 만든다.
        private float GetNextHeight(float lastHeight, SPlatformGapTier tier)
        {
            float maxStep = (_heightDirection > 0.0f) ? tier.MaxRise : tier.MaxDrop;
            float nextHeight = lastHeight + (_heightDirection * Random.Range(maxStep * MIN_HEIGHT_STEP_RATIO, maxStep));

            if (Mathf.Abs(nextHeight) >= _heightRange)
            {
                nextHeight = Mathf.Clamp(nextHeight, -_heightRange, _heightRange);
                _heightDirection = -_heightDirection;
            }

            return nextHeight;
        }

        // 현재 단계를 돌려주고, 이 단계로 Count 개를 다 만들었으면 다음 단계로 넘어간다.
        private SPlatformGapTier GetCurrentTier()
        {
            SPlatformGapTier tier = _tiers[_tierIndex];

            _tierSpawnCount++;
            if (_tierSpawnCount >= tier.Count)
            {
                _tierSpawnCount = 0;
                _tierIndex = (_tierIndex + 1) % _tiers.Length;
            }

            return tier;
        }

        private Vector3 ToWorldPosition(Vector2 offset, float travelledDistance)
        {
            return transform.position +
                (transform.right * offset.x) +
                (transform.up * offset.y) +
                (transform.forward * travelledDistance);
        }

        private static void DrawRect(Vector3 center, Vector3 halfWidth, Vector3 halfHeight)
        {
            Vector3 a = center - halfWidth - halfHeight;
            Vector3 b = center + halfWidth - halfHeight;
            Vector3 c = center + halfWidth + halfHeight;
            Vector3 d = center - halfWidth + halfHeight;

            Gizmos.DrawLine(a, b);
            Gizmos.DrawLine(b, c);
            Gizmos.DrawLine(c, d);
            Gizmos.DrawLine(d, a);
        }
        #endregion

#if UNITY_EDITOR
        #region Editor
        // Prewarm 과 같은 규칙으로 경로 전체에 발판을 씬 오브젝트로 만들어 둔다. (Play 중에는 쓰지 않는다)
        [ContextMenu("Bake Platforms")]
        private void BakePlatforms()
        {
            if (Application.isPlaying || (_platformPrefab == null)) return;

            ClearBakedPlatforms();
            ResetPlan();

            float travelledDistance = _maxTravelDistance;

            while (true)
            {
                travelledDistance -= _nextGap;
                if (travelledDistance <= 0.0f) break;

                // 스포너 부모의 스케일을 물려받지 않도록 런타임 발판처럼 씬 루트에 만든다.
                var instance = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(_platformPrefab.gameObject, gameObject.scene);
                instance.transform.SetPositionAndRotation(
                    ToWorldPosition(_nextOffset, travelledDistance),
                    _platformPrefab.transform.rotation);
                instance.name = $"{_platformPrefab.name}_Baked_{_bakedPlatforms.Count:00}";
                UnityEditor.Undo.RegisterCreatedObjectUndo(instance, "Bake Platforms");

                _bakedPlatforms.Add(instance.GetComponent<PlatformMover>());
                PlanNextPlatform();
            }
        }

        [ContextMenu("Clear Baked Platforms")]
        private void ClearBakedPlatforms()
        {
            if (Application.isPlaying) return;

            UnityEditor.Undo.RecordObject(this, "Clear Baked Platforms");

            foreach (PlatformMover platform in _bakedPlatforms)
            {
                if (platform != null)
                {
                    UnityEditor.Undo.DestroyObjectImmediate(platform.gameObject);
                }
            }

            _bakedPlatforms.Clear();
        }
        #endregion
#endif
    }
}
