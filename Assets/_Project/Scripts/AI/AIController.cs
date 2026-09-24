using System;
using UnityEngine;
using UnityEngine.AI;
using Controller;

namespace AI
{
    /// <summary>
    /// AI가 캐릭터를 조종하는 조종부. PlayerInputComponent 의 AI 판(版)이며,
    /// 중재자 계층은 조종 주체를 모르므로 이 컴포넌트를 붙이는 것만으로 같은 캐릭터를 AI가 움직인다.
    /// NavMeshAgent 는 경로 계산에만 쓰고 실제 이동은 기존 이동 요청(CommandMove)을 통해 CharacterMotor 가 수행한다.
    /// BT의 액션·조건 노드는 Sensor 나 NavMeshAgent 가 아니라 이 컴포넌트의 공개 API 에만 묻는다.
    /// </summary>
    public class AIController : MonoBehaviour, ICharacterController
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
        #endregion

        #region Serialized Fields
        [Header("References")]
        [Tooltip("시야 감지를 담당하는 센서. 조종부 소속이다.")]
        [SerializeField] private Sensor _sensor;
        [Tooltip("경로 계산 전용 NavMeshAgent. 이동은 이 컴포넌트가 직접 하지 않는다.")]
        [SerializeField] private NavMeshAgent _agent;

        [Header("Patrol")]
        [Tooltip("스폰 지점 기준 순찰 반경(m).")]
        [SerializeField] private float _patrolRadius = 10.0f;
        #endregion

        #region Private Fields
        private Vector3 _spawnPosition;
        private bool _hasDestination;
        #endregion

        #region Events
        private event Action<Vector2> _onMoveRequested;
        private event Action<Vector2> _onLookRequested;
        private event Action _onJumpRequested;
        #endregion

        #region Properties
        /// <summary>현재 시야에 대상이 보이는지 여부.</summary>
        public bool HasVisibleTarget => _sensor && _sensor.HasTarget;

        /// <summary>현재 보이는 대상. 보이지 않으면 null.</summary>
        public Transform VisibleTarget => _sensor ? _sensor.CurrentTarget : null;

        /// <summary>대상을 마지막으로 본 위치가 기록되어 있는지 여부.</summary>
        public bool HasLastKnownPosition => _sensor && _sensor.HasLastKnownPosition;

        /// <summary>대상을 마지막으로 본 위치.</summary>
        public Vector3 LastKnownPosition => _sensor ? _sensor.LastKnownPosition : transform.position;

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
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            if (!_sensor) _sensor = GetComponent<Sensor>();
            if (!_agent) _agent = GetComponent<NavMeshAgent>();

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
            _spawnPosition = transform.position;
            // 방어 코드
            if (_agent) _agent.nextPosition = transform.position;
        }

        private void Update()
        {
            if (!_agent || !_agent.isOnNavMesh)
            {
                RequestMove(Vector2.zero);
                return;
            }

            // CharacterMotor 가 옮긴 실제 위치를 Agent 에 되돌려야 다음 경로가 어긋나지 않는다
            _agent.nextPosition = transform.position;

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
            RequestMove(new Vector2(desired.x, desired.z));
        }

        private void OnDisable()
        {
            // 비활성화 시 마지막 이동 명령이 남아 계속 미끄러지지 않도록 정지시킨다
            RequestMove(Vector2.zero);
        }
        #endregion

        #region ICharacterController
        /// <inheritdoc />
        public void SetMoveRequest(Action<Vector2> callback)
        {
            if (callback == null) return;
            _onMoveRequested = callback;
        }

        /// <inheritdoc />
        public void SetLookRequest(Action<Vector2> callback)
        {
            if (callback == null) return;
            _onLookRequested = callback;
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
        public void ClearLookRequest()
        {
            _onLookRequested = null;
        }

        /// <inheritdoc />
        public void ClearJumpRequest()
        {
            _onJumpRequested = null;
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
            return true;
        }

        /// <summary>
        /// 스폰 지점 기준 순찰 반경 안의 무작위 지점을 목적지로 삼는다.
        /// </summary>
        /// <returns>유효한 목적지를 잡았으면 true</returns>
        public bool MoveToRandomPatrolPoint()
        {
            Vector2 offset = UnityEngine.Random.insideUnitCircle * _patrolRadius;
            Vector3 candidate = _spawnPosition + new Vector3(offset.x, 0.0f, offset.y);

            return MoveTo(candidate);
        }

        /// <summary>
        /// 이동을 멈추고 목적지를 비운다.
        /// </summary>
        public void StopMove()
        {
            _hasDestination = false;

            if (_agent && _agent.isOnNavMesh)
            {
                _agent.ResetPath();
            }

            RequestMove(Vector2.zero);
        }

        /// <summary>
        /// 마지막 목격 위치 기록을 지운다. 수색을 마쳤을 때 호출한다.
        /// </summary>
        public void ClearLastKnownPosition()
        {
            if (_sensor) _sensor.ClearLastKnownPosition();
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
        #endregion
    }
}
