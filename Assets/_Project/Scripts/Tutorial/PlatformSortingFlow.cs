using System;
using System.Collections.Generic;
using UnityEngine;
using Map.Platforms;
using Movement;

namespace Tutorial
{
    /// <summary>
    /// 그라인더 뒤쪽의 발판 흐름: 파이프(Source) → 들어오는 줄 → 분기점 → 분류장(위·가운데·아래 3개의 원) → 합류점 → 나가는 줄 → 그라인더(Sink).
    /// 이 오브젝트의 위치가 가운데 분류장의 중심이고, forward 가 흐름 방향이다. (입구 칸 = -forward, 출구 칸 = +forward)
    /// 들어오는 줄 맨 앞 발판은 분기점에서 자리가 난 분류장으로 올라가거나 내려가 들어간다.
    /// 분류장은 한 번에 한 칸씩 돌고(스텝 회전), 가득 찬 분류장에서 가장 먼저 들어온 발판이 출구 칸에 오면 합류점으로 나간다.
    /// 들어오는 줄은 발판이 분류장에 들어갈 때, 나가는 줄은 분류장에서 발판이 나올 때 한 칸씩 전진한다.
    /// PauseFlow() / ResumeFlow() 로 흐름 전체를 멈추고 다시 움직일 수 있다.
    /// </summary>
    [DefaultExecutionOrder(-100)]   // 탑승자(PlatformRider)보다 먼저 움직여야 이번 프레임 이동량이 전달된다
    public class PlatformSortingFlow : MonoBehaviour
    {
        #region Constants
        private const int RING_COUNT      = 3;
        private const int RING_SLOT_COUNT = 8;
        private const int ENTRY_SLOT      = 0;
        private const int EXIT_SLOT       = RING_SLOT_COUNT / 2;

        // TransformMotor.MoveTo 가 이번 프레임 목표 위치에 항상 닿도록 하는 충분히 큰 속도
        private const float SNAP_SPEED = 1000.0f;

        private const float GIZMO_SLOT_SIZE = 0.6f;
        #endregion

        #region Serialized Fields
        [Header("References")]
        [Tooltip("흐름에 쓰는 발판 프리팹 (RideablePlatform 필요)")]
        [SerializeField] private RideablePlatform _platformPrefab;

        [Tooltip("발판이 생성되는 지점 (파이프 입구)")]
        [SerializeField] private Transform _sourcePoint;

        [Tooltip("발판이 사라지는 지점 (그라인더 입구)")]
        [SerializeField] private Transform _sinkPoint;

        [Header("Lines")]
        [Tooltip("파이프와 분기점 사이에서 대기하는 발판 수. 발판 사이 간격이 점프로 닿는 거리인지 Gizmo로 확인한다.")]
        [SerializeField, Min(1)] private int _inboundCount = 4;

        [Tooltip("합류점과 그라인더 사이에서 대기하는 발판 수")]
        [SerializeField, Min(1)] private int _outboundCount = 4;

        [Header("Sorting Rings")]
        [Tooltip("분류장 원의 반지름")]
        [SerializeField, Min(0.1f)] private float _ringRadius = 6.0f;

        [Tooltip("위·가운데·아래 분류장 사이의 높이 차")]
        [SerializeField, Min(0.0f)] private float _ringSpacing = 4.0f;

        [Tooltip("분기점(합류점)과 분류장 입구(출구) 칸 사이의 거리")]
        [SerializeField, Min(0.1f)] private float _branchLength = 5.0f;

        [Header("Timing")]
        [Tooltip("한 칸 이동한 뒤 멈춰 있는 시간(초)")]
        [SerializeField, Min(0.0f)] private float _waitDuration = 1.5f;

        [Tooltip("한 칸 이동하는 데 걸리는 시간(초)")]
        [SerializeField, Min(0.05f)] private float _moveDuration = 1.0f;

        [Header("Options")]
        [SerializeField] private bool _autoStart = true;

        [Tooltip("켜면 시작할 때 분류장과 나가는 줄이 이미 차 있는 상태로 만든다.")]
        [SerializeField] private bool _shouldPrewarm = true;
        #endregion

        #region Private Fields
        private readonly List<FlowPlatform> _platforms = new List<FlowPlatform>();

        // 분류장별 [칸 위치] = 그 자리에 있는 발판
        private readonly FlowPlatform[][] _rings = new FlowPlatform[RING_COUNT][];
        private readonly FlowPlatform[][] _ringBuffers = new FlowPlatform[RING_COUNT][];

        // 분류장별 들어온 순서. 맨 앞이 가장 먼저 들어온 발판이다.
        private readonly Queue<FlowPlatform>[] _ringOrders = new Queue<FlowPlatform>[RING_COUNT];

        private FlowPlatform[] _inbound;
        private FlowPlatform[] _outbound;
        private FlowPlatform   _leaving;

        // 여러 분류장이 동시에 조건을 만족할 때 차례대로 고르기 위한 시작 번호
        private int _nextEntryRing;
        private int _nextExitRing;

        private float _timer;
        private bool  _isRunning;
        private bool  _isStepping;
        private bool  _isPrewarming;
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
            Debug.Assert(_sourcePoint != null, $"[{name}] Source Point(파이프 입구)가 연결되지 않았습니다.");
            Debug.Assert(_sinkPoint != null, $"[{name}] Sink Point(그라인더 입구)가 연결되지 않았습니다.");

            for (int i = 0; i < RING_COUNT; i++)
            {
                _rings[i]       = new FlowPlatform[RING_SLOT_COUNT];
                _ringBuffers[i] = new FlowPlatform[RING_SLOT_COUNT];
                _ringOrders[i]  = new Queue<FlowPlatform>();
            }

            _inbound  = new FlowPlatform[_inboundCount];
            _outbound = new FlowPlatform[_outboundCount];
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

            _timer += Time.deltaTime;

            if (_isStepping)
            {
                float progress = Mathf.Clamp01(_timer / _moveDuration);
                MovePlatforms(Mathf.SmoothStep(0.0f, 1.0f, progress));

                if (progress >= 1.0f)
                {
                    FinishStep();
                    _isStepping = false;
                    _timer = 0.0f;
                }

                return;
            }

            HoldPlatforms();

            if (_timer >= _waitDuration)
            {
                AdvanceStep();
                _isStepping = true;
                _timer = 0.0f;
            }
        }

        private void OnDrawGizmosSelected()
        {
            Vector3 splitPoint = GetSplitPosition();
            Vector3 mergePoint = GetMergePosition();

            // 분류장 칸 (입구 = 초록, 출구 = 빨강)과 분기점·합류점으로 이어지는 길
            for (int ring = 0; ring < RING_COUNT; ring++)
            {
                for (int slot = 0; slot < RING_SLOT_COUNT; slot++)
                {
                    Gizmos.color = Color.cyan;
                    Gizmos.DrawLine(GetRingPosition(ring, slot), GetRingPosition(ring, slot + 1));

                    if (slot == ENTRY_SLOT)
                    {
                        Gizmos.color = Color.green;
                    }
                    else if (slot == EXIT_SLOT)
                    {
                        Gizmos.color = Color.red;
                    }

                    Gizmos.DrawWireCube(GetRingPosition(ring, slot), Vector3.one * GIZMO_SLOT_SIZE);
                }

                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(splitPoint, GetRingPosition(ring, ENTRY_SLOT));
                Gizmos.DrawLine(GetRingPosition(ring, EXIT_SLOT), mergePoint);
            }

            // 들어오는 줄 / 나가는 줄의 대기 자리
            Gizmos.color = Color.yellow;
            if (_sourcePoint != null)
            {
                Gizmos.DrawLine(_sourcePoint.position, splitPoint);
                for (int i = 0; i < _inboundCount; i++)
                {
                    Gizmos.DrawWireCube(GetInboundPosition(i), Vector3.one * GIZMO_SLOT_SIZE);
                }
            }

            if (_sinkPoint != null)
            {
                Gizmos.DrawLine(mergePoint, _sinkPoint.position);
                for (int i = 0; i < _outboundCount; i++)
                {
                    Gizmos.DrawWireCube(GetOutboundPosition(i), Vector3.one * GIZMO_SLOT_SIZE);
                }
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// 흐름을 시작한다. 들어오는 줄을 채우고, 설정에 따라 분류장과 나가는 줄도 미리 채운다.
        /// </summary>
        public void StartFlow()
        {
            if (_isRunning) return;
            if ((_platformPrefab == null) || (_sourcePoint == null) || (_sinkPoint == null)) return;

            _isPrewarming = true;

            for (int i = 0; i < _inboundCount; i++)
            {
                _inbound[i] = SpawnPlatform(GetInboundPosition(i));
            }

            if (_shouldPrewarm)
            {
                // 분류장 3개가 가득 차고 나가는 줄이 다 찰 만큼 미리 진행시킨다.
                int fillSteps = RING_COUNT * RING_SLOT_COUNT * 2;
                int prewarmSteps = fillSteps + ((RING_SLOT_COUNT + 1) * _outboundCount);

                for (int i = 0; i < prewarmSteps; i++)
                {
                    AdvanceStep();
                    FinishStep();
                }

                foreach (FlowPlatform platform in _platforms)
                {
                    platform.Motor.transform.position = platform.To;
                }
            }

            _isPrewarming = false;
            _isRunning = true;
            _isStepping = false;
            _timer = 0.0f;
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
        #endregion

        #region Private Methods
        // 모든 발판의 다음 자리를 정한다. 실제 이동은 Update 에서 From → To 로 보간한다.
        private void AdvanceStep()
        {
            foreach (FlowPlatform platform in _platforms)
            {
                platform.From = platform.To;
                platform.ArcStartSlot = -1;
            }

            // 합류점은 하나라서 한 스텝에 한 분류장에서만 나가고, 분기점도 하나라서 한 분류장으로만 들어간다.
            FlowPlatform exiting = FindExitingPlatform();
            int entryRing = FindEntryRing();

            for (int ring = 0; ring < RING_COUNT; ring++)
            {
                RotateRing(ring, exiting);
            }

            if (exiting != null)
            {
                ShiftOutbound(exiting);
            }

            if (entryRing >= 0)
            {
                ShiftInbound(entryRing);
            }
        }

        // 가득 찬 분류장에서 가장 먼저 들어온 발판이 출구 칸에 있으면 그 발판이 나간다.
        private FlowPlatform FindExitingPlatform()
        {
            for (int i = 0; i < RING_COUNT; i++)
            {
                int ring = (_nextExitRing + i) % RING_COUNT;
                FlowPlatform atExit = _rings[ring][EXIT_SLOT];
                bool isRingFull = _ringOrders[ring].Count == RING_SLOT_COUNT;

                if (isRingFull && (atExit != null) && (atExit == _ringOrders[ring].Peek()))
                {
                    _ringOrders[ring].Dequeue();
                    _nextExitRing = (ring + 1) % RING_COUNT;
                    return atExit;
                }
            }

            return null;
        }

        // 이번 회전으로 입구 칸이 비게 되는 분류장을 고른다. 없으면 -1.
        private int FindEntryRing()
        {
            if (_inbound[_inboundCount - 1] == null) return -1;

            for (int i = 0; i < RING_COUNT; i++)
            {
                int ring = (_nextEntryRing + i) % RING_COUNT;
                if (_rings[ring][RING_SLOT_COUNT - 1] == null)
                {
                    _nextEntryRing = (ring + 1) % RING_COUNT;
                    return ring;
                }
            }

            return -1;
        }

        private void RotateRing(int ring, FlowPlatform exiting)
        {
            FlowPlatform[] current = _rings[ring];
            FlowPlatform[] rotated = _ringBuffers[ring];
            Array.Clear(rotated, 0, RING_SLOT_COUNT);

            for (int slot = 0; slot < RING_SLOT_COUNT; slot++)
            {
                FlowPlatform platform = current[slot];
                if ((platform == null) || (platform == exiting)) continue;

                int nextSlot = (slot + 1) % RING_SLOT_COUNT;
                rotated[nextSlot] = platform;
                platform.ArcRing = ring;
                platform.ArcStartSlot = slot;
                platform.To = GetRingPosition(ring, nextSlot);
            }

            _rings[ring] = rotated;
            _ringBuffers[ring] = current;
        }

        // 분류장에서 나온 발판을 합류점(나가는 줄 맨 앞)에 넣고, 맨 끝 발판을 그라인더로 보낸다.
        private void ShiftOutbound(FlowPlatform exiting)
        {
            int lastOutbound = _outboundCount - 1;

            _leaving = _outbound[lastOutbound];
            if (_leaving != null)
            {
                _leaving.To = _sinkPoint.position;
            }

            for (int i = lastOutbound; i > 0; i--)
            {
                _outbound[i] = _outbound[i - 1];
                if (_outbound[i] != null)
                {
                    _outbound[i].To = GetOutboundPosition(i);
                }
            }

            _outbound[0] = exiting;
            exiting.To = GetOutboundPosition(0);
        }

        // 분기점에 있던 맨 앞 발판을 고른 분류장의 입구 칸에 넣고, 파이프에서 새 발판을 만든다.
        private void ShiftInbound(int ring)
        {
            int lastInbound = _inboundCount - 1;

            FlowPlatform entering = _inbound[lastInbound];
            entering.To = GetRingPosition(ring, ENTRY_SLOT);
            _rings[ring][ENTRY_SLOT] = entering;
            _ringOrders[ring].Enqueue(entering);

            for (int i = lastInbound; i > 0; i--)
            {
                _inbound[i] = _inbound[i - 1];
                _inbound[i].To = GetInboundPosition(i);
            }

            _inbound[0] = SpawnPlatform(_sourcePoint.position);
            _inbound[0].To = GetInboundPosition(0);
        }

        // 그라인더에 도착한 발판을 없앤다.
        private void FinishStep()
        {
            if (_leaving == null) return;

            if (!_isPrewarming)
            {
                OnPlatformConsumed?.Invoke(_leaving.Motor.transform.position);
            }

            // TODO: 오브젝트 풀 도입 시 풀에 반환하는 코드로 교체
            _platforms.Remove(_leaving);
            Destroy(_leaving.Motor.gameObject);
            _leaving = null;
        }

        private FlowPlatform SpawnPlatform(Vector3 position)
        {
            // TODO: 오브젝트 풀 도입 시 풀에서 꺼내는 코드로 교체
            RideablePlatform instance = Instantiate(_platformPrefab, position, _platformPrefab.transform.rotation);

            var platform = new FlowPlatform
            {
                Motor = instance.GetComponent<TransformMotor>(),
                From = position,
                To = position,
                ArcStartSlot = -1,
            };
            _platforms.Add(platform);

            if (!_isPrewarming)
            {
                OnPlatformSpawned?.Invoke(position);
            }

            return platform;
        }

        private void MovePlatforms(float progress)
        {
            foreach (FlowPlatform platform in _platforms)
            {
                // 분류장 안에서는 직선이 아니라 원호를 따라 움직인다.
                Vector3 target = (platform.ArcStartSlot >= 0)
                    ? GetRingPosition(platform.ArcRing, platform.ArcStartSlot + progress)
                    : Vector3.Lerp(platform.From, platform.To, progress);

                platform.Motor.MoveTo(target, SNAP_SPEED);
            }
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

        // ring: 0 = 위, 1 = 가운데, 2 = 아래. slot 은 소수도 받는다. (칸과 칸 사이를 이동 중인 위치)
        private Vector3 GetRingPosition(int ring, float slot)
        {
            float angle = Mathf.Repeat(slot, RING_SLOT_COUNT) * ((Mathf.PI * 2.0f) / RING_SLOT_COUNT);

            // 이웃한 분류장끼리는 서로 반대 방향으로 돈다.
            float side = ((ring % 2) == 0) ? 1.0f : -1.0f;
            float height = (1 - ring) * _ringSpacing;

            // 0번 칸(입구)이 -forward, 4번 칸(출구)이 +forward
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

        // 0번이 파이프 쪽, 마지막 번호가 분기점
        private Vector3 GetInboundPosition(int index)
        {
            float ratio = (index + 1) / (float)_inboundCount;
            return Vector3.Lerp(_sourcePoint.position, GetSplitPosition(), ratio);
        }

        // 0번이 합류점, 마지막 번호가 그라인더 쪽
        private Vector3 GetOutboundPosition(int index)
        {
            float ratio = index / (float)(_outboundCount + 1);
            return Vector3.Lerp(GetMergePosition(), _sinkPoint.position, ratio);
        }

        private Vector3 ToWorldPosition(Vector3 localPosition)
        {
            return transform.position + (transform.rotation * localPosition);
        }
        #endregion

        #region Nested Types
        private sealed class FlowPlatform
        {
            public TransformMotor Motor;
            public Vector3 From;
            public Vector3 To;

            // 이번 스텝에 분류장 원호를 따라 움직이면 그 분류장 번호와 출발 칸 번호. 아니면 ArcStartSlot 이 -1
            public int ArcRing;
            public int ArcStartSlot;
        }
        #endregion
    }
}
