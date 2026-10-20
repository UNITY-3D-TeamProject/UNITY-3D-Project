using System;
using UnityEngine;
using UnityEngine.AI;
using Mediator;
using Mediator.SubMediators;

namespace AI
{
    /// <summary>
    /// AI가 캐릭터를 조종하는 조종부. PlayerInputComponent 의 AI 판(版)이며,
    /// 중재자 계층은 조종 주체를 모르므로 이 컴포넌트를 붙이는 것만으로 같은 캐릭터를 AI가 움직인다.
    /// NavMeshAgent 는 경로 계산에만 쓰고 실제 이동은 기존 이동 요청(CommandMove)을 통해 CharacterMotor 가 수행한다.
    /// BT의 액션·조건 노드는 Sensor 나 NavMeshAgent 가 아니라 이 컴포넌트의 공개 API 에만 묻는다.
    /// </summary>
    public class AIController : MonoBehaviour, IMoveController, IBodyRotateController, ISkillRequestController
    {
        #region Constants
        /// <summary>목적지를 NavMesh 위로 끌어당길 때 허용하는 최대 거리(m).</summary>
        private const float DESTINATION_SAMPLE_DISTANCE = 2.0f;

        /// <summary>
        /// 도착 판정 여유 거리(m).
        /// Agent 가 아니라 CharacterMotor 가 움직이므로 목적지에 정확히 멈추지 않는다.
        /// 여유가 없으면 목적지 주변에서 계속 진동한다.
        /// </summary>
        private const float ARRIVAL_THRESHOLD = 0.25f;

        /// <summary>정체 판정 검사 주기(초). 이 주기마다 이동 거리를 확인한다.</summary>
        private const float STUCK_CHECK_INTERVAL = 2.0f;

        /// <summary>정체 판정 검사 주기 동안 이 거리(m) 미만으로 이동했으면 정체로 본다.</summary>
        private const float STUCK_DISTANCE_THRESHOLD = 0.1f;

        /// <summary>주변 동료를 찾을 때 한 번에 감지하는 최대 수.</summary>
        private const int MAX_SEPARATION_NEIGHBORS = 8;

        /// <summary>NavMesh 밖으로 떨어진 Agent 를 되붙일 때 몸 주위에서 NavMesh 를 찾는 최대 거리(m).</summary>
        private const float RESYNC_SAMPLE_DISTANCE = 4.0f;

        /// <summary>Agent 되붙이기를 다시 시도하기까지의 간격(초).</summary>
        private const float RESYNC_RETRY_INTERVAL = 0.25f;
        #endregion

        #region Serialized Fields
        [Header("References")]
        [Tooltip("시야 감지를 담당하는 센서. 조종부 소속이다.")]
        [SerializeField] private Sensor _sensor;
        [Tooltip("경로 계산 전용 NavMeshAgent. 이동은 이 컴포넌트가 직접 하지 않는다.")]
        [SerializeField] private NavMeshAgent _agent;
        [Tooltip("실제로 이동하는 몸통 Transform (CharacterMotor 가 붙은 오브젝트). 비워두면 이 오브젝트의 Transform 을 쓴다.")]
        [SerializeField] private Transform _body;

        [Header("Patrol")]
        [Tooltip("스폰 지점 기준 순찰 반경(m).")]
        [SerializeField] private float _patrolRadius = 10.0f;
        [Tooltip("순찰 지점 목록. 비워두면 같은 오브젝트에서 찾고, 그래도 없으면 스폰 지점 반경 안 무작위로 순찰한다.")]
        [SerializeField] private WaypointPatrolRoute _patrolRoute;

        [Header("Stuck")]
        [Tooltip("한 목적지를 향해 이동을 시도할 수 있는 최대 시간(초). 초과하면 정체로 본다.")]
        [SerializeField, Min(0.1f)] private float _destinationTimeout = 10.0f;

        [Header("Separation")]
        [Tooltip("주변 동료로 볼 레이어. 비워두면 동료를 찾지 않는다.")]
        [SerializeField] private LayerMask _separationLayers;

        [Header("Speed")]
        [Tooltip("이동 속도 어트리뷰트를 바꿀 중재자. 비워두면 이 오브젝트에서 찾는다.")]
        [SerializeField] private CharacterMediator _mediator;
        [Tooltip("이동 속도 어트리뷰트 이름.")]
        [SerializeField] private string _speedAttributeKey = "MoveSpeed";

        [Header("Hit Reaction")]
        [Tooltip("피격 시 이동을 멈추고 맞은 쪽을 바라보는 시간(초). 0 이면 반응하지 않는다. CharacterRotator 회전 속도로 180° 돌 수 있는 시간 이상이어야 뒤에서 맞아도 완전히 돌아선다.")]
        [SerializeField, Min(0.0f)] private float _hitLookDuration = 1.0f;
        [Tooltip("대상이 보이는 전투 중에도 피격 시 대상을 바라본다. 360도 센서를 쓰는 보스 등에 사용한다. Hold 중인 스킬 방향은 유지한다.")]
        [SerializeField] private bool _shouldFaceVisibleTargetOnHit;
        [Tooltip("피격 시 이동과 스킬 요청을 멈추는 경직 시간(초). 0 이면 경직하지 않는다. 대상이 보이는 중에도 걸린다.")]
        [SerializeField, Min(0.0f)] private float _hitStunDuration = 0.4f;
        [Tooltip("경직이 끝난 뒤 다시 경직되지 않는 시간(초). 연사로 계속 묶이지 않게 한다. 피격 애니메이션은 이 동안에도 재생된다.")]
        [SerializeField, Min(0.0f)] private float _hitStunImmuneDuration = 1.5f;
        #endregion

        #region Private Fields
        private readonly Collider[] _separationHits = new Collider[MAX_SEPARATION_NEIGHBORS];
        private Vector3 _spawnPosition;
        private bool _hasDestination;
        private Vector3 _destination;
        private float _lastTargetSeenTime;

        private int _agentSyncSuspendCount;
        private float _resyncRetryTimer;
        private bool _isAgentOffNavMesh;

        private Vector3 _stuckCheckPosition;
        private float _stuckCheckTimer;
        private float _destinationTimer;
        private bool _isStuck;

        private float _hitLookTimer;
        private Vector2 _hitLookDirection;

        private float _hitStunTimer;
        private float _hitStunImmuneTimer;

        private float _holdTimer;
        private Vector2 _holdDirection;
        #endregion

        #region Events
        private event Action<Vector2> _onMoveRequested;
        private event Action _onJumpRequested;
        private event Action<Vector2> _onBodyRotateRequested;
        private event Action<string> _requestExecuteSkill;
        private event Action<string> _requestStopSkill;
        #endregion

        #region Properties
        /// <summary>몸통의 현재 월드 위치.</summary>
        public Vector3 Position => _body.position;

        /// <summary>현재 시야에 대상이 보이는지 여부.</summary>
        public bool HasVisibleTarget => _sensor && _sensor.HasTarget;

        /// <summary>현재 보이는 대상. 보이지 않으면 null.</summary>
        public Transform VisibleTarget => _sensor ? _sensor.CurrentTarget : null;

        /// <summary>대상을 마지막으로 본 위치가 기록되어 있는지 여부.</summary>
        public bool HasLastKnownPosition => _sensor && _sensor.HasLastKnownPosition;

        /// <summary>대상을 마지막으로 본 위치.</summary>
        public Vector3 LastKnownPosition => _sensor ? _sensor.LastKnownPosition : _body.position;

        /// <summary>대상을 마지막으로 본 뒤 지난 시간(초). 한 번도 보지 못했으면 생성 시점부터 잰다.</summary>
        public float TimeSinceTargetSeen => Time.time - _lastTargetSeenTime;

        /// <summary>현재 보이는 대상까지의 수평 거리(m). 보이는 대상이 없으면 양의 무한대.</summary>
        public float DistanceToTarget
        {
            get
            {
                if (!HasVisibleTarget) return float.PositiveInfinity;

                Vector3 toTarget = Vector3.ProjectOnPlane(VisibleTarget.position - _body.position, Vector3.up);
                return toTarget.magnitude;
            }
        }

        /// <summary>목적지에 도착했는지 여부. 목적지가 없으면 true.</summary>
        public bool HasArrived
        {
            get
            {
                if (!_hasDestination) return true;
                if (!_agent || !_agent.isOnNavMesh) return true;
                if (_agent.pathPending) return false;

                return _agent.remainingDistance <= (_agent.stoppingDistance + ARRIVAL_THRESHOLD);
            }
        }

        /// <summary>
        /// 목적지로 이동 중 정체되었는지 여부.
        /// 경로가 도달 불가능하거나(PathInvalid), 일정 시간 동안 거의 움직이지 못했거나,
        /// 제한 시간을 넘겨도 도착하지 못하면 true 가 된다. 목적지가 없으면 false.
        /// </summary>
        public bool IsStuck => _isStuck;

        /// <summary>
        /// 이동 중 몸통을 이동 방향으로 돌릴지 여부. 기본 true.
        /// 이동하면서 대상을 바라봐야 할 때(후퇴 중 사격 등) false 로 두면 회전은 FaceDirection 요청만 따른다.
        /// </summary>
        public bool ShouldFaceMoveDirection { get; set; } = true;

        /// <summary>Hold 로 붙잡혀 제자리에 멈춰 있는지 여부.</summary>
        public bool IsHolding => _holdTimer > 0.0f;

        /// <summary>
        /// Agent 위치 동기화가 멈춰 있는지 여부. 스킬이 몸을 직접 옮기는 동안(도약·돌진) true 다.
        /// </summary>
        public bool IsAgentSyncSuspended => _agentSyncSuspendCount > 0;

        /// <summary>피격 경직 중인지 여부. 경직 중에는 이동하지 않고 스킬 요청(ExecuteSkill)도 무시한다.</summary>
        public bool IsStunned => _hitStunTimer > 0.0f;

        /// <summary>
        /// 피격되어도 경직하지 않는지 여부. 기본 false.
        /// Hold 로 붙잡히지 않는 스킬(원거리 예비 동작 등)을 쓰는 동안 BT 노드가 켜 둔다.
        /// </summary>
        public bool IsSuperArmor { get; set; }
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            if (!_sensor) _sensor = GetComponent<Sensor>();
            if (!_patrolRoute) _patrolRoute = GetComponent<WaypointPatrolRoute>();
            if (!_agent) _agent = GetComponent<NavMeshAgent>();
            if (!_body) _body = transform;
            if (!_mediator) _mediator = GetComponent<CharacterMediator>();

            if (!_agent)
            {
                Debug.LogError($"[{name}] NavMeshAgent 가 없어 경로를 계산할 수 없다.", this);
                return;
            }

            // Agent 는 경로만 계산하고 Transform 은 건드리지 않는다. 이동은 CharacterMotor 담당.
            _agent.updatePosition = false;
            _agent.updateRotation = false;
            _agent.updateUpAxis = false;
        }

        private void OnEnable()
        {
            if (_mediator) _mediator.OnHitFrom += OnHitFromCallback;
        }

        private void Start()
        {
            _spawnPosition = _body.position;
            _lastTargetSeenTime = Time.time;
            // 방어 코드
            if (_agent) _agent.nextPosition = _body.position;
        }

        private void Update()
        {
            if (HasVisibleTarget) _lastTargetSeenTime = Time.time;

            // NavMesh 밖에서도 안전 타이머가 흘러야 영구히 붙잡히지 않으므로 아래 early return 보다 앞에 둔다
            if (_holdTimer > 0.0f) _holdTimer -= Time.deltaTime;
            if (_hitStunTimer > 0.0f) _hitStunTimer -= Time.deltaTime;
            if (_hitStunImmuneTimer > 0.0f) _hitStunImmuneTimer -= Time.deltaTime;

            if (!_agent || !_agent.isOnNavMesh)
            {
                // nextPosition 대입으로는 한 번 떨어진 Agent 가 돌아오지 않으므로 주기적으로 되붙인다
                TickAgentResync();
                RequestMove(Vector2.zero);
                return;
            }

            _isAgentOffNavMesh = false;

            // CharacterMotor 가 옮긴 실제 위치를 Agent 에 되돌려야 다음 경로가 어긋나지 않는다.
            // 스킬이 몸을 직접 옮기는 동안에는 공중·구멍 위 좌표가 들어가 Agent 가 떨어지므로 멈춘다
            if (!IsAgentSyncSuspended) _agent.nextPosition = _body.position;

            // 붙잡힌 동안에는 목적지를 유지한 채 멈춰서 고정 방향을 바라본다. 풀리면 하던 이동을 이어간다
            if (IsHolding)
            {
                RequestMove(Vector2.zero);
                RequestRotate(_holdDirection);
                return;
            }

            // 피격 직후에는 목적지를 유지한 채 멈춰서 맞은 쪽을 바라본다. 끝나면 하던 이동을 이어간다
            if (_hitLookTimer > 0.0f)
            {
                // 돌아보는 중 대상을 발견하면 바로 전투로 넘어가도록 멈춤을 끝낸다
                _hitLookTimer = HasVisibleTarget ? 0.0f : _hitLookTimer - Time.deltaTime;
                RequestMove(Vector2.zero);
                RequestRotate(_hitLookDirection);
                return;
            }

            // 경직 중에는 목적지를 유지한 채 멈춘다. 끝나면 하던 이동을 이어간다
            if (IsStunned)
            {
                RequestMove(Vector2.zero);
                if (_shouldFaceVisibleTargetOnHit && HasVisibleTarget)
                {
                    FaceDirection(VisibleTarget.position - _body.position);
                }
                return;
            }

            UpdateStuckState();

            if (!_hasDestination || HasArrived)
            {
                RequestMove(Vector2.zero);
                return;
            }

            Vector3 desired = Vector3.ProjectOnPlane(_agent.desiredVelocity, Vector3.up);
            if (desired.sqrMagnitude <= Mathf.Epsilon)
            {
                RequestMove(Vector2.zero);
                return;
            }

            // 기준 프레임이 없는 MoveMediator 는 (x, y) 를 월드 (x, 0, y) 로 해석한다
            desired.Normalize();
            Vector2 desired2D = new Vector2(desired.x, desired.z);
            RequestMove(desired2D);
            // 회전도 이동과 같은 방향으로 요청한다. 정지 시(위의 zero 경로)에는 요청을 보내지 않아
            // CharacterRotator 가 직전 방향을 유지하게 둔다.
            if (ShouldFaceMoveDirection) RequestRotate(desired2D);
        }

        private void OnDisable()
        {
            if (_mediator) _mediator.OnHitFrom -= OnHitFromCallback;
            // 오브젝트 풀에서 다시 꺼냈을 때 멈춤이 남아 있지 않게 한다
            _hitLookTimer = 0.0f;
            _holdTimer = 0.0f;
            _hitStunTimer = 0.0f;
            _hitStunImmuneTimer = 0.0f;
            IsSuperArmor = false;
            // 스킬이 끝내지 못한 채 꺼졌을 때 동기화가 영구히 멈춘 상태로 남지 않게 한다
            _agentSyncSuspendCount = 0;
            _isAgentOffNavMesh = false;

            // 비활성화 시 마지막 이동 명령이 남아 계속 미끄러지지 않도록 정지시킨다
            RequestMove(Vector2.zero);
        }

#if UNITY_EDITOR
        // [임시 진단] 버그 1(벽에 머리 박힘) 확인 후 제거.
        // 경로 코너를 그려 NavMesh 경로가 벽을 우회하는지(코너 3개 이상), 관통하는지(코너 2개) 눈으로 확인한다.
        private void OnDrawGizmosSelected()
        {
            if (!_agent || !_agent.hasPath) return;

            Vector3[] corners = _agent.path.corners;
            Gizmos.color = Color.cyan;
            for (int i = 0; i < corners.Length; i++)
            {
                Gizmos.DrawSphere(corners[i], 0.15f);
                if (i > 0) Gizmos.DrawLine(corners[i - 1], corners[i]);
            }
        }
#endif
        #endregion

        #region IMoveController
        /// <inheritdoc />
        public void SetMoveRequest(Action<Vector2> callback)
        {
            if (callback == null) return;
            _onMoveRequested = callback;
        }

        /// <inheritdoc />
        public void SetJumpRequest(Action callback)
        {
            if (callback == null) return;
            _onJumpRequested = callback;
        }

        /// <inheritdoc />
        public void ClearMoveRequest()
        {
            _onMoveRequested = null;
        }

        /// <inheritdoc />
        public void ClearJumpRequest()
        {
            _onJumpRequested = null;
        }
        #endregion

        #region IBodyRotateController
        /// <inheritdoc />
        public void SetBodyRotateRequest(Action<Vector2> callback)
        {
            if (callback == null) return;
            _onBodyRotateRequested = callback;
        }

        /// <inheritdoc />
        public void ClearBodyRotateRequest()
        {
            _onBodyRotateRequested = null;
        }
        #endregion

        #region ISkillRequestController
        /// <inheritdoc />
        public void SetRequestExecuteSkill(Action<string> requestExecuteSkill)
        {
            if (requestExecuteSkill == null) return;
            _requestExecuteSkill = requestExecuteSkill;
        }

        /// <inheritdoc />
        public void ClearRequestExecuteSkill()
        {
            _requestExecuteSkill = null;
        }

        /// <inheritdoc />
        public void SetRequestStopSkill(Action<string> requestStopSkill)
        {
            if (requestStopSkill == null) return;
            _requestStopSkill = requestStopSkill;
        }

        /// <inheritdoc />
        public void ClearRequestStopSkill()
        {
            _requestStopSkill = null;
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// 지정한 월드 위치를 목적지로 삼는다. 위치가 NavMesh 밖이면 가까운 NavMesh 지점으로 보정한다.
        /// </summary>
        /// <param name="worldPosition">목표 월드 위치</param>
        /// <returns>유효한 목적지를 잡았으면 true</returns>
        public bool MoveTo(Vector3 worldPosition)
        {
            if (!_agent || !_agent.isOnNavMesh) return false;

            bool isOnNavMesh = NavMesh.SamplePosition(
                worldPosition,
                out NavMeshHit hit,
                DESTINATION_SAMPLE_DISTANCE,
                NavMesh.AllAreas);
            if (!isOnNavMesh) return false;

            if (!_agent.SetDestination(hit.position)) return false;

            _hasDestination = true;
            _destination = hit.position;
            ResetStuckState();
            return true;
        }

        /// <summary>
        /// 다음 순찰 지점을 목적지로 삼는다. WaypointPatrolRoute 에서 지점을 받을 수 있으면 그 지점을,
        /// 아니면 스폰 지점 기준 순찰 반경 안의 무작위 지점을 쓴다.
        /// </summary>
        /// <returns>유효한 목적지를 잡았으면 true</returns>
        public bool MoveToRandomPatrolPoint()
        {
            if (_patrolRoute && _patrolRoute.TryGetNextWaypoint(out Vector3 waypoint))
            {
                return MoveTo(waypoint);
            }

            Vector2 offset = UnityEngine.Random.insideUnitCircle * _patrolRadius;
            Vector3 candidate = _spawnPosition + new Vector3(offset.x, 0.0f, offset.y);

            return MoveTo(candidate);
        }

        /// <summary>
        /// 지정한 위치에서 가장 가까운 순찰 지점을 목적지로 삼는다. 정확한 위치가 아니라 근처 순찰 지점까지만 간다.
        /// </summary>
        /// <param name="worldPosition">기준 월드 위치</param>
        /// <returns>WaypointPatrolRoute 에서 지점을 받아 유효한 목적지를 잡았으면 true</returns>
        public bool MoveToPatrolPointNear(Vector3 worldPosition)
        {
            if (!_patrolRoute) return false;
            if (!_patrolRoute.TryGetNearestWaypoint(worldPosition, out Vector3 waypoint)) return false;

            return MoveTo(waypoint);
        }

        /// <summary>
        /// 주변 NavMesh 위에서 위협에게서 달아날 지점을 골라 목적지로 삼는다. 후보마다 경로 계산을 하므로 자주 부르지 않는다.
        /// </summary>
        /// <param name="threatPosition">위협 위치</param>
        /// <param name="maxPathLength">허용하는 최대 경로 길이(m)</param>
        /// <param name="avoidRadius">경로가 위협에 이 거리 안으로 들어가면 후보에서 뺀다(m)</param>
        /// <returns>달아날 지점을 찾아 유효한 목적지를 잡았으면 true</returns>
        public bool MoveAwayFrom(Vector3 threatPosition, float maxPathLength, float avoidRadius)
        {
            bool hasPoint = FleePointFinder.TryFind(
                Position,
                threatPosition,
                maxPathLength,
                avoidRadius,
                out Vector3 point);

            return hasPoint && MoveTo(point);
        }

        /// <summary>
        /// 지금 가는 경로의 남은 구간이 위협 근처를 지나는지 확인한다. 기준은 MoveAwayFrom 과 같다.
        /// 경로를 복사하므로 매 프레임이 아니라 주기적으로 부른다.
        /// </summary>
        /// <param name="threatPosition">위협 위치</param>
        /// <param name="avoidRadius">경로가 위협에 이 거리 안으로 들어가면 막힌 것으로 본다(m)</param>
        /// <returns>남은 경로가 위협 근처를 지나면 true. 목적지나 경로가 없으면 false</returns>
        public bool IsPathNearThreat(Vector3 threatPosition, float avoidRadius)
        {
            if (!_hasDestination || !_agent || !_agent.isOnNavMesh || !_agent.hasPath) return false;

            return FleePointFinder.IsPathBlocked(Position, _agent.path.corners, threatPosition, avoidRadius);
        }

        /// <summary>
        /// 이동을 멈추고 목적지를 비운다.
        /// </summary>
        public void StopMove()
        {
            _hasDestination = false;
            _destination = _body.position;
            ResetStuckState();

            if (_agent && _agent.isOnNavMesh)
            {
                _agent.ResetPath();
            }

            RequestMove(Vector2.zero);
        }

        /// <summary>
        /// 이동 속도 어트리뷰트에 배율을 곱한다. 되돌릴 때는 1/ratio 로 다시 호출한다.
        /// 덮어쓰지 않고 곱하므로 감속 지대 등 다른 곳의 변경과 겹쳐도 어긋나지 않는다.
        /// 중재자가 없거나 어트리뷰트가 없으면 무시한다.
        /// </summary>
        /// <param name="ratio">곱할 배율 (양수)</param>
        public void ScaleMoveSpeed(float ratio)
        {
            if (!_mediator || !_mediator.IsValidAttribute(_speedAttributeKey)) return;

            float speed = _mediator.GetAttribute(_speedAttributeKey);
            _mediator.SetAttribute(_speedAttributeKey, speed * ratio);
        }

        /// <summary>
        /// 지정한 월드 방향을 바라보며 지정한 시간 동안 제자리에 멈춘다. 이미 붙잡혀 있으면 방향과 시간을 덮어쓴다.
        /// 목적지는 건드리지 않으므로 풀리면 BT 가 다시 이동을 정한다. 시간은 안전 장치이며,
        /// 끝나는 시점을 아는 쪽은 ReleaseHold 로 직접 푼다.
        /// </summary>
        /// <param name="worldDirection">바라볼 월드 방향 (수직 성분은 무시)</param>
        /// <param name="seconds">붙잡아 둘 최대 시간(초)</param>
        public void Hold(Vector3 worldDirection, float seconds)
        {
            Vector3 flat = Vector3.ProjectOnPlane(worldDirection, Vector3.up);
            if (flat.sqrMagnitude <= Mathf.Epsilon) flat = _body.forward;

            _holdDirection = new Vector2(flat.x, flat.z);
            _holdTimer = Mathf.Max(0.0f, seconds);
        }

        /// <summary>
        /// 붙잡힘을 즉시 푼다.
        /// </summary>
        public void ReleaseHold()
        {
            _holdTimer = 0.0f;
        }

        /// <summary>
        /// 몸을 직접 옮기는 동안(도약·돌진 등) Agent 위치 동기화를 멈춘다.
        /// 공중이나 구멍 위를 지나는 몸 위치를 Agent 에 밀어 넣으면 Agent 가 NavMesh 밖으로 떨어지기 때문이다.
        /// 중첩 호출을 허용하므로 ResumeAgentSync 와 짝을 맞춰 부른다.
        /// </summary>
        public void SuspendAgentSync()
        {
            _agentSyncSuspendCount++;
        }

        /// <summary>
        /// SuspendAgentSync 를 푼다. 마지막 짝이 풀리는 순간 Agent 를 몸 위치로 되붙인다.
        /// 짝이 맞지 않게 더 불러도 아무것도 하지 않는다.
        /// </summary>
        public void ResumeAgentSync()
        {
            if (_agentSyncSuspendCount <= 0) return;

            _agentSyncSuspendCount--;
            if (_agentSyncSuspendCount > 0) return;

            TryResyncAgent();
        }

        /// <summary>
        /// 보이는 대상을 바라보며 공격 스킬을 한 번 요청한다. 보이는 대상이 없으면 무시한다.
        /// 실제 발동 여부와 간격은 스킬의 조건·코스트가 정하므로 매 틱 호출해도 된다.
        /// 이 캐릭터의 SkillController 에 SkillName 이 "Attack" 인 스킬이 등록되어 있어야 한다.
        /// </summary>
        public void Attack()
        {
            if (!HasVisibleTarget) return;

            FaceDirection(VisibleTarget.position - _body.position);

            ExecuteSkill("Attack");
            // 누르고 있는 동안 반복되는 스킬(연사 등)이어도 한 번만 발동하도록 바로 뗀다
            StopSkill("Attack");
        }

        /// <summary>
        /// 몸통을 지정한 월드 방향으로 돌리도록 요청한다. 수직 성분은 무시한다.
        /// </summary>
        /// <param name="worldDirection">바라볼 월드 방향</param>
        public void FaceDirection(Vector3 worldDirection)
        {
            RequestRotate(new Vector2(worldDirection.x, worldDirection.z));
        }

        /// <summary>
        /// 지정한 이름의 스킬 발동을 요청한다. 자동으로 떼지 않으므로
        /// 유지형 스킬(조준선 등)은 끝낼 때 StopSkill 을 호출해야 한다. 경직 중에는 무시한다.
        /// </summary>
        /// <param name="skillName">발동할 스킬 이름</param>
        public void ExecuteSkill(string skillName)
        {
            if (IsStunned) return;

            _requestExecuteSkill?.Invoke(skillName);
        }

        /// <summary>
        /// 지정한 이름의 스킬 중지를 요청한다.
        /// </summary>
        /// <param name="skillName">중지할 스킬 이름</param>
        public void StopSkill(string skillName)
        {
            _requestStopSkill?.Invoke(skillName);
        }

        /// <summary>
        /// 마지막 목격 위치 기록을 지운다. 수색을 마쳤을 때 호출한다.
        /// </summary>
        public void ClearLastKnownPosition()
        {
            if (_sensor) _sensor.ClearLastKnownPosition();
        }

        /// <summary>
        /// 반경 안 동료들에게서 멀어지는 수평 방향을 구한다. 가까운 동료일수록 크게 반영한다.
        /// 붙은 두 적이 동시에 서로를 피해 자리만 맞바꾸지 않도록, 나보다 InstanceID 가 작은 동료만 피한다.
        /// 그래서 뭉친 무리에서 ID 가 가장 작은 적은 제자리에 있고 나머지가 비킨다.
        /// </summary>
        /// <param name="radius">동료를 찾을 반경(m)</param>
        /// <param name="direction">동료들에게서 멀어지는 수평 방향 (정규화됨)</param>
        /// <returns>반경 안에 피해야 할 동료가 있으면 true</returns>
        public bool TryGetAwayFromAllies(float radius, out Vector3 direction)
        {
            direction = Vector3.zero;
            if (_separationLayers.value == 0) return false;

            int hitCount = Physics.OverlapSphereNonAlloc(
                _body.position,
                radius,
                _separationHits,
                _separationLayers,
                QueryTriggerInteraction.Ignore);

            bool hasAlly = false;
            for (int i = 0; i < hitCount; i++)
            {
                AIController ally = _separationHits[i].GetComponentInParent<AIController>();
                if (!ally || ally == this) continue;
                if (ally.GetInstanceID() > GetInstanceID()) continue;

                hasAlly = true;
                Vector3 away = Vector3.ProjectOnPlane(_body.position - ally.Position, Vector3.up);
                float distance = away.magnitude;
                if (distance <= Mathf.Epsilon) continue;

                // 중심이 반경 밖인데 콜라이더 표면만 걸린 동료는 음수 가중치로 방향을 뒤집지 않게 0으로 둔다
                direction += away / distance * Mathf.Clamp01(1.0f - distance / radius);
            }

            if (!hasAlly) return false;

            // 동료와 위치가 정확히 겹치거나 양쪽 동료가 상쇄되면 옆으로 벌어진다
            direction = (direction.sqrMagnitude > Mathf.Epsilon) ? direction.normalized : _body.right;
            return true;
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// 중재자에 이동을 요청한다.
        /// </summary>
        /// <param name="input">이동 입력 (x: 좌우, y: 전후)</param>
        private void RequestMove(Vector2 input)
        {
            _onMoveRequested?.Invoke(input);
        }

        /// <summary>
        /// 중재자에 몸통 회전을 요청한다.
        /// </summary>
        /// <param name="direction">회전 방향 입력 (x, y — 월드 기준)</param>
        private void RequestRotate(Vector2 direction)
        {
            _onBodyRotateRequested?.Invoke(direction);
        }

        /// <summary>
        /// 피격되면 짧게 경직시키고, 대상이 안 보이면 잠시 멈춰 맞은 쪽을 바라보게 한다.
        /// 경직은 스킬 사용 중(Hold·슈퍼아머)이거나 직전 경직의 면역 시간 안이면 걸리지 않는다.
        /// 전투 중 돌아보기 설정이 켜져 있으면 보이는 대상을 바라본다. Hold·슈퍼아머 중 스킬 방향은 유지한다.
        /// </summary>
        /// <param name="hitSource">피격 방향 기준 월드 위치(맞은 지점 또는 공격자 위치)</param>
        private void OnHitFromCallback(Vector3 hitSource)
        {
            bool canStun = (_hitStunDuration > 0.0f) &&
                (!IsHolding && !IsSuperArmor) &&
                (_hitStunImmuneTimer <= 0.0f);
            if (canStun)
            {
                _hitStunTimer = _hitStunDuration;
                _hitStunImmuneTimer = _hitStunDuration + _hitStunImmuneDuration;
            }

            if (HasVisibleTarget)
            {
                if (_shouldFaceVisibleTargetOnHit && !IsHolding && !IsSuperArmor)
                {
                    FaceDirection(VisibleTarget.position - _body.position);
                }
                return;
            }

            Vector3 toSource = Vector3.ProjectOnPlane(hitSource - _body.position, Vector3.up);
            if (toSource.sqrMagnitude <= Mathf.Epsilon) return;

            _hitLookDirection = new Vector2(toSource.x, toSource.z);
            _hitLookTimer = _hitLookDuration;
        }

        /// <summary>
        /// 목적지로 이동 중인 동안 매 프레임 호출해 정체 여부를 갱신한다.
        /// 경로를 계산할 수 없거나(PathInvalid), 검사 주기 동안 거의 못 움직였거나,
        /// 제한 시간을 넘기면 정체로 판정한다.
        /// </summary>
        private void UpdateStuckState()
        {
            if (!_hasDestination || HasArrived)
            {
                ResetStuckState();
                return;
            }

            if (!_agent.pathPending && _agent.pathStatus == NavMeshPathStatus.PathInvalid)
            {
                _isStuck = true;
                return;
            }

            _destinationTimer += Time.deltaTime;
            if (_destinationTimer >= _destinationTimeout)
            {
                _isStuck = true;
                return;
            }

            _stuckCheckTimer += Time.deltaTime;
            if (_stuckCheckTimer < STUCK_CHECK_INTERVAL) return;

            float movedDistance = Vector3.Distance(_body.position, _stuckCheckPosition);
            if (movedDistance < STUCK_DISTANCE_THRESHOLD)
            {
                _isStuck = true;
            }

            _stuckCheckPosition = _body.position;
            _stuckCheckTimer = 0.0f;
        }

        /// <summary>
        /// 정체 판정 상태와 그 측정에 쓰이는 타이머·기준 위치를 초기화한다.
        /// 새 목적지를 잡거나(MoveTo), 이동을 멈출 때(StopMove) 호출한다.
        /// </summary>
        private void ResetStuckState()
        {
            _isStuck = false;
            _destinationTimer = 0.0f;
            _stuckCheckTimer = 0.0f;
            _stuckCheckPosition = _body.position;
        }

        /// <summary>
        /// Agent 가 NavMesh 밖으로 떨어졌을 때 일정 간격으로 되붙이기를 시도한다.
        /// 붕괴한 칸이 복구되어 NavMesh 가 돌아와도 Agent 는 스스로 다시 붙지 않으므로 이 검사가 필요하다.
        /// </summary>
        private void TickAgentResync()
        {
            if (!_agent || !_agent.enabled || IsAgentSyncSuspended) return;

            if (!_isAgentOffNavMesh)
            {
                _isAgentOffNavMesh = true;
                _resyncRetryTimer = 0.0f;
                Debug.LogWarning($"[{name}] Agent 가 NavMesh 밖으로 떨어져 이동을 멈췄다. 되붙이기를 시도한다. 위치={_body.position}", this);
            }

            _resyncRetryTimer -= Time.deltaTime;
            if (_resyncRetryTimer > 0.0f) return;

            _resyncRetryTimer = RESYNC_RETRY_INTERVAL;
            TryResyncAgent();
        }

        /// <summary>
        /// Agent 를 몸 위치의 NavMesh 위로 되붙인다. 몸 주위에서 NavMesh 를 찾지 못하면 실패한다.
        /// nextPosition 대입으로는 한 번 떨어진 Agent 가 돌아오지 않으므로 Warp 를 쓴다.
        /// updatePosition 이 꺼져 있어 Warp 는 몸(Transform)을 움직이지 않는다.
        /// Warp 는 경로를 비우므로 가지고 있던 목적지를 다시 건다.
        /// </summary>
        /// <returns>되붙였으면 true</returns>
        private bool TryResyncAgent()
        {
            if (!_agent || !_agent.enabled) return false;

            bool isFound = NavMesh.SamplePosition(
                _body.position,
                out NavMeshHit hit,
                RESYNC_SAMPLE_DISTANCE,
                _agent.areaMask);
            if (!isFound) return false;

            if (!_agent.Warp(hit.position)) return false;

            _isAgentOffNavMesh = false;

            // Warp 가 비운 경로를 되살린다. 다시 걸지 못하면 BT 가 새 목적지를 정하도록 목적지를 비운다
            if (_hasDestination && !_agent.SetDestination(_destination)) _hasDestination = false;

            ResetStuckState();
            return true;
        }
        #endregion
    }
}
