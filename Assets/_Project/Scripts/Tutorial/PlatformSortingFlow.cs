using System;
using System.Collections.Generic;
using UnityEngine;
using Map.Platforms;
using Movement;

namespace Tutorial
{
    /// <summary>
    /// 그라인더 뒤쪽의 발판 흐름: 파이프(Source) → 들어오는 줄 → 분기점 → 분류장(위·가운데·아래 3개의 원) → 합류점 → 나가는 줄 → 그라인더(Sink).
    /// 이 오브젝트의 위치가 가운데 분류장의 중심이고, forward 가 흐름 방향이다. (분류장 입구 = -forward, 출구 = +forward)
    /// 모든 발판은 멈추지 않고 일정한 속도로 같은 경로를 따라 움직인다. 파이프에서 일정 간격으로 나온 발판은
    /// 위·가운데·아래 분류장에 차례대로 나눠 들어가 정해진 바퀴 수만큼 돈 뒤 합류점으로 나가 그라인더로 들어간다.
    /// 분류장마다 들어가는 갈래·나오는 갈래의 시간이 같아서 합류점에서도 발판 간격이 일정하다.
    /// PauseFlow() / ResumeFlow() 로 흐름 전체를 멈추고 다시 움직일 수 있다.
    /// </summary>
    [DefaultExecutionOrder(-100)]   // 탑승자(PlatformRider)보다 먼저 움직여야 이번 프레임 이동량이 전달된다
    public class PlatformSortingFlow : MonoBehaviour
    {
        #region Constants
        private const int RING_COUNT = 3;

        // TransformMotor.MoveTo 가 이번 프레임 목표 위치에 항상 닿도록 하는 충분히 큰 속도
        private const float SNAP_SPEED = 1000.0f;

        private const float GIZMO_SLOT_SIZE = 0.6f;
        private const int   GIZMO_RING_SEGMENTS = 48;
        #endregion

        #region Serialized Fields
        [Header("References")]
        [Tooltip("흐름에 쓰는 발판 프리팹 (RideablePlatform 필요)")]
        [SerializeField] private RideablePlatform _platformPrefab;

        [Tooltip("발판이 생성되는 지점 (파이프 안쪽 깊은 곳)")]
        [SerializeField] private Transform _sourcePoint;

        [Tooltip("발판이 사라지는 지점 (그라인더 입구)")]
        [SerializeField] private Transform _sinkPoint;

        [Tooltip("나가는 줄이 그라인더로 빨려 들어가기 직전에 지나는 지점 (선택). 비우면 합류점에서 Sink 로 곧장 간다.")]
        [SerializeField] private Transform _sinkApproachPoint;

        [Header("Flow")]
        [Tooltip("줄 위에서 발판이 움직이는 속도(m/s). 분류장 둘레를 도는 속도도 같다.")]
        [SerializeField, Min(0.1f)] private float _speed = 2.0f;

        [Tooltip("줄 위 발판 사이의 거리(중심 기준, m). 같은 분류장 안에서는 이 값의 3배 간격이 된다.")]
        [SerializeField, Min(0.5f)] private float _spacing = 6.7f;

        [Header("Sorting Rings")]
        [Tooltip("분류장 원의 반지름")]
        [SerializeField, Min(0.1f)] private float _ringRadius = 8.0f;

        [Tooltip("위·가운데·아래 분류장 사이의 높이 차")]
        [SerializeField, Min(0.0f)] private float _ringSpacing = 6.0f;

        [Tooltip("분기점(합류점)과 분류장 입구(출구) 사이의 거리")]
        [SerializeField, Min(0.1f)] private float _branchLength = 5.0f;

        [Tooltip("분류장 안에서 도는 바퀴 수. 입구 반대편이 출구라서 0.5, 1.5, 2.5 처럼 n + 0.5 로 둔다.\n" +
                 "같은 분류장 발판끼리 겹치지 않는지 Gizmo로 확인한다. (둘레 ÷ (간격 × 3) 이 n + 0.5 에 가까우면 고르게 퍼진다)")]
        [SerializeField, Min(0.5f)] private float _ringLaps = 1.5f;

        [Header("Options")]
        [SerializeField] private bool _autoStart = true;

        [Tooltip("켜면 시작할 때 경로 전체가 이미 발판으로 차 있는 상태로 만든다.")]
        [SerializeField] private bool _shouldPrewarm = true;

        [Tooltip("켜면 멈춘 상태로 시작한다. ResumeFlow() 를 호출해야 움직인다. (예: 원거리 스위치로 작동)")]
        [SerializeField] private bool _isPausedAtStart;

        [Tooltip("에디터에서 미리 깔아 둔 발판. 컴포넌트 메뉴(⋮)의 Bake Platforms 로 만들고 Clear Baked Platforms 로 지운다.\n" +
                 "시작하면 새로 만들지 않고 이 발판들을 차례로 쓴다. 경로·간격 설정을 바꾸면 다시 굽는다.")]
        [SerializeField] private List<RideablePlatform> _bakedPlatforms = new List<RideablePlatform>();
        #endregion

        #region Private Fields
        private readonly List<FlowPlatform> _platforms = new List<FlowPlatform>();
        private readonly List<FlowPlatform> _consumed  = new List<FlowPlatform>();

        // 구간별 걸리는 시간(초). 한 발판의 나이(생성 후 지난 시간)로 위치를 구한다.
        private float _inboundDuration;
        private float _branchDuration;
        private float _ringDuration;
        private float _outboundDuration;
        private float _approachDuration;
        private float _totalDuration;
        private float _spawnInterval;

        private float _clock;
        private int   _nextSpawnIndex;
        private bool  _isRunning;
        #endregion

        #region Properties
        /// <summary>흐름이 멈춰 있는지 여부.</summary>
        public bool IsPaused { get; private set; }
        #endregion

        #region Events
        /// <summary>
        /// 발판이 파이프에서 생성될 때 발판의 월드 위치와 함께 호출된다.
        /// </summary>
        public event Action<Vector3> OnPlatformSpawned;

        /// <summary>
        /// 발판이 그라인더에 도착해 사라질 때 발판의 월드 위치와 함께 호출된다.
        /// </summary>
        public event Action<Vector3> OnPlatformConsumed;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            Debug.Assert(_platformPrefab != null, $"[{name}] 발판 프리팹이 연결되지 않았습니다.");
            Debug.Assert(_sourcePoint != null, $"[{name}] Source Point(파이프 안쪽)가 연결되지 않았습니다.");
            Debug.Assert(_sinkPoint != null, $"[{name}] Sink Point(그라인더 입구)가 연결되지 않았습니다.");

            IsPaused = _isPausedAtStart;
        }

        private void Start()
        {
            if (_autoStart)
            {
                StartFlow();
            }
        }

        private void Update()
        {
            if (!_isRunning) return;

            if (IsPaused)
            {
                HoldPlatforms();
                return;
            }

            _clock += Time.deltaTime;
            SpawnDuePlatforms(true);
            MovePlatforms();
        }

        private void OnDrawGizmosSelected()
        {
            if ((_sourcePoint == null) || (_sinkPoint == null)) return;

            CalculateDurations();

            // 분류장별 경로 (입구 = 초록, 출구 = 빨강)
            for (int ring = 0; ring < RING_COUNT; ring++)
            {
                Gizmos.color = Color.cyan;
                for (int i = 0; i < GIZMO_RING_SEGMENTS; i++)
                {
                    Gizmos.DrawLine(
                        GetRingPosition(ring, i / (float)GIZMO_RING_SEGMENTS),
                        GetRingPosition(ring, (i + 1) / (float)GIZMO_RING_SEGMENTS));
                }

                Gizmos.color = Color.green;
                Gizmos.DrawWireCube(GetRingPosition(ring, 0.0f), Vector3.one * GIZMO_SLOT_SIZE);
                Gizmos.color = Color.red;
                Gizmos.DrawWireCube(GetRingPosition(ring, 0.5f), Vector3.one * GIZMO_SLOT_SIZE);

                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(GetSplitPosition(), GetRingPosition(ring, 0.0f));
                Gizmos.DrawLine(GetRingPosition(ring, 0.5f), GetMergePosition());
            }

            // 줄 위 발판 자리 (멈춰 있을 때 보이는 간격)
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(_sourcePoint.position, GetSplitPosition());
            Gizmos.DrawLine(GetMergePosition(), GetApproachPosition());
            Gizmos.DrawLine(GetApproachPosition(), _sinkPoint.position);

            for (float age = 0.0f; age < _totalDuration; age += _spawnInterval)
            {
                Gizmos.DrawWireCube(GetPathPosition(0, age), Vector3.one * GIZMO_SLOT_SIZE);
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// 흐름을 시작한다. 설정에 따라 경로 전체를 미리 발판으로 채운다.
        /// </summary>
        public void StartFlow()
        {
            if (_isRunning) return;
            if ((_platformPrefab == null) || (_sourcePoint == null) || (_sinkPoint == null)) return;

            CalculateDurations();

            // 미리 채울 때는 시계를 경로 전체 시간만큼 앞당겨, 그동안 나왔을 발판을 제자리에 바로 만든다.
            _clock = _shouldPrewarm ? _totalDuration : 0.0f;
            _nextSpawnIndex = 0;

            SpawnDuePlatforms(false);
            MovePlatforms();
            DestroyUnusedBakedPlatforms();

            _isRunning = true;
        }

        /// <summary>
        /// 흐름 전체를 그 자리에서 멈춘다. (원거리 상호작용 등에서 호출)
        /// </summary>
        public void PauseFlow()
        {
            IsPaused = true;
        }

        /// <summary>
        /// 멈춘 흐름을 이어서 진행한다.
        /// </summary>
        public void ResumeFlow()
        {
            IsPaused = false;
        }

        /// <summary>
        /// 지금 흐름 위에 있는 발판 중 position 에 가장 가까운 발판을 돌려준다. (예: 음악 파일을 얹을 발판 고르기)
        /// </summary>
        /// <returns>가장 가까운 발판의 Transform. 발판이 없으면 null</returns>
        public Transform FindNearestPlatform(Vector3 position)
        {
            Transform nearest = null;
            float nearestDistance = float.MaxValue;

            foreach (FlowPlatform platform in _platforms)
            {
                float distance = Vector3.Distance(platform.Motor.transform.position, position);
                if (distance < nearestDistance)
                {
                    nearest = platform.Motor.transform;
                    nearestDistance = distance;
                }
            }

            return nearest;
        }
        #endregion

        #region Private Methods
        private void CalculateDurations()
        {
            float ringLength = _ringLaps * 2.0f * Mathf.PI * _ringRadius;

            _inboundDuration  = Vector3.Distance(_sourcePoint.position, GetSplitPosition()) / _speed;
            _branchDuration   = _branchLength / _speed;
            _ringDuration     = ringLength / _speed;
            _outboundDuration = Vector3.Distance(GetMergePosition(), GetApproachPosition()) / _speed;
            _approachDuration = Vector3.Distance(GetApproachPosition(), _sinkPoint.position) / _speed;

            _totalDuration = _inboundDuration + (_branchDuration * 2.0f) + _ringDuration + _outboundDuration + _approachDuration;
            _spawnInterval = _spacing / _speed;
        }

        // 생성 시각이 지금까지 된 발판을 모두 만든다. 경로 끝을 이미 지난 발판은 만들지 않는다.
        private void SpawnDuePlatforms(bool shouldNotify)
        {
            while ((_nextSpawnIndex * _spawnInterval) <= _clock)
            {
                float spawnTime = _nextSpawnIndex * _spawnInterval;
                if ((_clock - spawnTime) < _totalDuration)
                {
                    SpawnPlatform(_nextSpawnIndex, spawnTime, shouldNotify);
                }

                _nextSpawnIndex++;
            }
        }

        private void SpawnPlatform(int index, float spawnTime, bool shouldNotify)
        {
            int ring = index % RING_COUNT;
            Vector3 position = GetPathPosition(ring, _clock - spawnTime);

            RideablePlatform instance = TakeBakedPlatform(position);
            if (instance == null)
            {
                // TODO: 오브젝트 풀 도입 시 풀에서 꺼내는 코드로 교체
                instance = Instantiate(_platformPrefab, position, _platformPrefab.transform.rotation);
            }

            _platforms.Add(new FlowPlatform
            {
                Motor = instance.GetComponent<TransformMotor>(),
                Ring = ring,
                SpawnTime = spawnTime,
            });

            if (shouldNotify)
            {
                OnPlatformSpawned?.Invoke(position);
            }
        }

        // 에디터에서 구운 발판이 남아 있으면 앞에서부터 하나 꺼내 position 에 놓는다. 없으면 null.
        private RideablePlatform TakeBakedPlatform(Vector3 position)
        {
            while (_bakedPlatforms.Count > 0)
            {
                RideablePlatform platform = _bakedPlatforms[0];
                _bakedPlatforms.RemoveAt(0);
                if (platform == null) continue;

                platform.transform.position = position;
                return platform;
            }

            return null;
        }

        // 설정이 바뀌어 구운 발판이 남으면 정리한다.
        private void DestroyUnusedBakedPlatforms()
        {
            foreach (RideablePlatform platform in _bakedPlatforms)
            {
                if (platform != null)
                {
                    Destroy(platform.gameObject);
                }
            }

            _bakedPlatforms.Clear();
        }

        private void MovePlatforms()
        {
            foreach (FlowPlatform platform in _platforms)
            {
                float age = _clock - platform.SpawnTime;
                if (age >= _totalDuration)
                {
                    _consumed.Add(platform);
                    continue;
                }

                platform.Motor.MoveTo(GetPathPosition(platform.Ring, age), SNAP_SPEED);
            }

            // 그라인더에 도착한 발판을 없앤다.
            foreach (FlowPlatform platform in _consumed)
            {
                OnPlatformConsumed?.Invoke(platform.Motor.transform.position);

                // TODO: 오브젝트 풀 도입 시 풀에 반환하는 코드로 교체
                _platforms.Remove(platform);
                Destroy(platform.Motor.gameObject);
            }

            _consumed.Clear();
        }

        private void HoldPlatforms()
        {
            // TransformMotor.DeltaThisFrame은 MoveTo를 호출하지 않으면 직전 값이 남는다.
            // 정지 중에 탑승자가 밀리지 않도록 제자리 이동으로 0을 만든다.
            foreach (FlowPlatform platform in _platforms)
            {
                platform.Motor.MoveTo(platform.Motor.transform.position, 0.0f);
            }
        }

        // 발판이 생성된 뒤 age 초가 지났을 때의 위치
        private Vector3 GetPathPosition(int ring, float age)
        {
            if (age < _inboundDuration)
            {
                return Vector3.Lerp(_sourcePoint.position, GetSplitPosition(), age / _inboundDuration);
            }
            age -= _inboundDuration;

            if (age < _branchDuration)
            {
                return Vector3.Lerp(GetSplitPosition(), GetRingPosition(ring, 0.0f), age / _branchDuration);
            }
            age -= _branchDuration;

            if (age < _ringDuration)
            {
                return GetRingPosition(ring, _ringLaps * (age / _ringDuration));
            }
            age -= _ringDuration;

            if (age < _branchDuration)
            {
                return Vector3.Lerp(GetRingPosition(ring, _ringLaps), GetMergePosition(), age / _branchDuration);
            }
            age -= _branchDuration;

            if (age < _outboundDuration)
            {
                return Vector3.Lerp(GetMergePosition(), GetApproachPosition(), age / _outboundDuration);
            }
            age -= _outboundDuration;

            return Vector3.Lerp(GetApproachPosition(), _sinkPoint.position, Mathf.Clamp01(age / _approachDuration));
        }

        // ring: 0 = 위, 1 = 가운데, 2 = 아래. turns 는 입구에서부터 돈 바퀴 수 (0.5 = 출구)
        private Vector3 GetRingPosition(int ring, float turns)
        {
            float angle = turns * Mathf.PI * 2.0f;

            // 이웃한 분류장끼리는 서로 반대 방향으로 돈다.
            float side = ((ring % 2) == 0) ? 1.0f : -1.0f;
            float height = (1 - ring) * _ringSpacing;

            // 입구가 -forward, 출구(반 바퀴)가 +forward
            var localPosition = new Vector3(Mathf.Sin(angle) * _ringRadius * side, height, -Mathf.Cos(angle) * _ringRadius);
            return ToWorldPosition(localPosition);
        }

        // 들어오는 줄이 끝나고 세 분류장으로 갈라지는 지점 (가운데 분류장 높이)
        private Vector3 GetSplitPosition()
        {
            return ToWorldPosition(new Vector3(0.0f, 0.0f, -(_ringRadius + _branchLength)));
        }

        // 세 분류장에서 나온 발판이 모여 나가는 줄이 시작되는 지점 (가운데 분류장 높이)
        private Vector3 GetMergePosition()
        {
            return ToWorldPosition(new Vector3(0.0f, 0.0f, _ringRadius + _branchLength));
        }

        private Vector3 GetApproachPosition()
        {
            return (_sinkApproachPoint != null) ? _sinkApproachPoint.position : GetMergePosition();
        }

        private Vector3 ToWorldPosition(Vector3 localPosition)
        {
            return transform.position + (transform.rotation * localPosition);
        }
        #endregion

#if UNITY_EDITOR
        #region Editor
        // 미리 채운 시작 상태(Prewarm)와 같은 자리에 발판을 씬 오브젝트로 만들어 둔다. (Play 중에는 쓰지 않는다)
        [ContextMenu("Bake Platforms")]
        private void BakePlatforms()
        {
            if (Application.isPlaying || (_platformPrefab == null) || (_sourcePoint == null) || (_sinkPoint == null)) return;

            ClearBakedPlatforms();
            CalculateDurations();

            for (int index = 0; (index * _spawnInterval) <= _totalDuration; index++)
            {
                float age = _totalDuration - (index * _spawnInterval);
                if (age >= _totalDuration) continue;

                var instance = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(_platformPrefab.gameObject, transform);
                instance.transform.SetPositionAndRotation(
                    GetPathPosition(index % RING_COUNT, age),
                    _platformPrefab.transform.rotation);
                instance.name = $"{_platformPrefab.name}_Baked_{index:00}";
                UnityEditor.Undo.RegisterCreatedObjectUndo(instance, "Bake Platforms");

                _bakedPlatforms.Add(instance.GetComponent<RideablePlatform>());
            }
        }

        [ContextMenu("Clear Baked Platforms")]
        private void ClearBakedPlatforms()
        {
            if (Application.isPlaying) return;

            UnityEditor.Undo.RecordObject(this, "Clear Baked Platforms");

            foreach (RideablePlatform platform in _bakedPlatforms)
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

        #region Nested Types
        private sealed class FlowPlatform
        {
            public TransformMotor Motor;
            public int   Ring;
            public float SpawnTime;
        }
        #endregion
    }
}
