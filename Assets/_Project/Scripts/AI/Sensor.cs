using UnityEngine;

namespace AI
{
    /// <summary>
    /// 시야(거리 + 각도 + 차폐)로 대상을 감지하는 조종부 소속 자료원.
    /// 중재자가 관리하지 않으며, 조종부(AIController)만 이 컴포넌트를 참조한다.
    /// 출력은 "보이는가"(bool)가 아니라 대상 자체와 마지막 목격 위치다.
    /// 시야를 잃어도 마지막 목격 위치는 남아 수색의 목적지로 쓰인다.
    /// </summary>
    public class Sensor : MonoBehaviour
    {
        #region Constants
        private const int MAX_DETECT_TARGETS = 8;
        #endregion

        #region Serialized Fields
        [Header("References")]
        [Tooltip("시야의 원점. 비워두면 이 오브젝트의 Transform 을 쓴다.")]
        [SerializeField] private Transform _eye;

        [Header("Sight")]
        [Tooltip("시야 거리(m).")]
        [SerializeField] private float _sightRange = 15.0f;
        [Tooltip("시야 부채꼴의 전체 각도(도). 정면 기준 좌우로 절반씩 벌어진다.")]
        [Range(0.0f, 360.0f)]
        [SerializeField] private float _sightAngle = 90.0f;

        [Header("Layers")]
        [Tooltip("감지 대상 레이어 (Player 등).")]
        [SerializeField] private LayerMask _targetLayers;
        [Tooltip("시야를 가로막는 레이어 (Obstacle 등).")]
        [SerializeField] private LayerMask _obstacleLayers;

        [Header("Performance")]
        [Tooltip("감지 주기(초). 시작 시점은 적마다 무작위로 어긋나 프레임 몰림을 막는다.")]
        [SerializeField] private float _detectInterval = 0.2f;
        #endregion

        #region Private Fields
        private readonly Collider[] _hitBuffer = new Collider[MAX_DETECT_TARGETS];
        private Transform _currentTarget;
        private Vector3 _lastKnownPosition;
        private bool _hasLastKnownPosition;
        private float _detectTimer;
        #endregion

        #region Properties
        /// <summary>현재 보이는 대상. 보이지 않으면 null.</summary>
        public Transform CurrentTarget => _currentTarget;

        /// <summary>현재 보이는 대상이 있는지 여부.</summary>
        public bool HasTarget => _currentTarget != null;

        /// <summary>대상을 마지막으로 본 위치. 시야를 잃어도 유지된다.</summary>
        public Vector3 LastKnownPosition => _lastKnownPosition;

        /// <summary>마지막 목격 위치가 기록되어 있는지 여부.</summary>
        public bool HasLastKnownPosition => _hasLastKnownPosition;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            if (!_eye) _eye = transform;

            // 적마다 시작 오프셋을 줘 같은 프레임에 감지가 몰리지 않게 한다
            _detectTimer = Random.Range(0.0f, _detectInterval);

            if (_targetLayers == 0)
            {
                Debug.LogWarning($"[{name}] Sensor 의 Target Layers 가 비어 있어 아무것도 감지하지 않는다.", this);
            }
        }

        private void Update()
        {
            _detectTimer -= Time.deltaTime;
            if (_detectTimer > 0.0f) return;

            _detectTimer += _detectInterval;
            Detect();
        }

        private void OnDrawGizmosSelected()
        {
            Transform eye = _eye ? _eye : transform;

            Gizmos.color = HasTarget ? Color.red : Color.yellow;
            Gizmos.DrawWireSphere(eye.position, _sightRange);

            Vector3 leftEdge = Quaternion.Euler(0.0f, -_sightAngle * 0.5f, 0.0f) * eye.forward;
            Vector3 rightEdge = Quaternion.Euler(0.0f, _sightAngle * 0.5f, 0.0f) * eye.forward;
            Gizmos.DrawRay(eye.position, leftEdge * _sightRange);
            Gizmos.DrawRay(eye.position, rightEdge * _sightRange);
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// 마지막 목격 위치 기록을 지운다. 수색을 마쳤을 때 호출한다.
        /// </summary>
        public void ClearLastKnownPosition()
        {
            _hasLastKnownPosition = false;
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// 시야 범위 안의 대상 중 각도와 차폐 검사를 모두 통과한 가장 가까운 것을 찾는다.
        /// </summary>
        private void Detect()
        {
            _currentTarget = null;

            Vector3 eyePosition = _eye.position;
            int hitCount = Physics.OverlapSphereNonAlloc(
                eyePosition,
                _sightRange,
                _hitBuffer,
                _targetLayers);

            float nearestSqrDistance = float.MaxValue;

            for (int i = 0; i < hitCount; i++)
            {
                Collider hit = _hitBuffer[i];
                if (!hit) continue;

                // 발밑이 아니라 몸통 중심을 기준으로 봐야 바닥에 시야가 막히지 않는다
                Vector3 targetPoint = hit.bounds.center;
                Vector3 toTarget = targetPoint - eyePosition;

                if (!IsInSightAngle(toTarget)) continue;
                if (IsOccluded(eyePosition, toTarget)) continue;

                float sqrDistance = toTarget.sqrMagnitude;
                if (sqrDistance >= nearestSqrDistance) continue;

                nearestSqrDistance = sqrDistance;
                _currentTarget = hit.transform;
            }

            if (_currentTarget)
            {
                _lastKnownPosition = _currentTarget.position;
                _hasLastKnownPosition = true;
            }
        }

        /// <summary>
        /// 대상 방향이 시야 부채꼴 안에 있는지 검사한다. 수평 성분만 본다.
        /// </summary>
        /// <param name="toTarget">시야 원점에서 대상으로 향하는 벡터</param>
        /// <returns>시야각 안이면 true</returns>
        private bool IsInSightAngle(Vector3 toTarget)
        {
            Vector3 flatToTarget = Vector3.ProjectOnPlane(toTarget, Vector3.up);
            if (flatToTarget.sqrMagnitude <= Mathf.Epsilon) return true;

            Vector3 flatForward = Vector3.ProjectOnPlane(_eye.forward, Vector3.up);
            float angle = Vector3.Angle(flatForward, flatToTarget);
            return angle <= (_sightAngle * 0.5f);
        }

        /// <summary>
        /// 시야 원점과 대상 사이를 장애물이 가로막는지 검사한다.
        /// </summary>
        /// <param name="eyePosition">시야 원점</param>
        /// <param name="toTarget">시야 원점에서 대상으로 향하는 벡터</param>
        /// <returns>가로막혀 있으면 true</returns>
        private bool IsOccluded(Vector3 eyePosition, Vector3 toTarget)
        {
            float distance = toTarget.magnitude;
            if (distance <= Mathf.Epsilon) return false;

            return Physics.Raycast(
                eyePosition,
                toTarget / distance,
                distance,
                _obstacleLayers);
        }
        #endregion
    }
}
