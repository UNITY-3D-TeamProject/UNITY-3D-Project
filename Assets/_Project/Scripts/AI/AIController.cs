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
        #endregion

        #region Private Fields
        private readonly Collider[] _separationHits = new Collider[MAX_SEPARATION_NEIGHBORS];
        private Vector3 _spawnPosition;
        private bool _hasDestination;
        private float _lastTargetSeenTime;

        private Vector3 _stuckCheckPosition;
        private float _stuckCheckTimer;
        private float _destinationTimer;
        private bool _isStuck;
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

            if (!_agent || !_agent.isOnNavMesh)
            {
                RequestMove(Vector2.zero);
                return;
            }

            // CharacterMotor 가 옮긴 실제 위치를 Agent 에 되돌려야 다음 경로가 어긋나지 않는다
            _agent.nextPosition = _body.position;

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
        /// 이동을 멈추고 목적지를 비운다.
        /// </summary>
        public void StopMove()
        {
            _hasDestination = false;
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
        /// 유지형 스킬(조준선 등)은 끝낼 때 StopSkill 을 호출해야 한다.
        /// </summary>
        /// <param name="skillName">발동할 스킬 이름</param>
        public void ExecuteSkill(string skillName)
        {
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
        #endregion
    }
}
