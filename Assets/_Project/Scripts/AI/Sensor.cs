using UnityEngine;

namespace AI
{
    /// <summary>
    /// 시야(거리 + 각도 + 차폐)로 대상을 감지하는 조종부 소속 자료원.
    /// 중재자가 관리하지 않으며, 조종부(AIController)만 이 컴포넌트를 참조한다.
    /// 출력은 "보이는가"(bool)가 아니라 대상 자체와 마지막 목격 위치다.
    /// 시야에 들어오면 바로 대상이 확정되고,
    /// 확정된 대상이 있는 동안(전투 중)에는 시야각을 보지 않고 시야 거리 안을 360도로 찾는다.
    /// 확정된 대상을 놓쳐도 유예 시간 동안은 실제 위치를 계속 추적한다.
    /// 유예가 끝나도 마지막 목격 위치는 남아 수색의 목적지로 쓰인다.
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

        [Header("Awareness")]
        [Tooltip("확정된 대상을 놓친 뒤에도 실제 위치를 계속 추적하는 유예 시간(초).")]
        [SerializeField] private float _loseGraceTime = 3.0f;

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
        private float _graceTimer;

        // 비활성화된 동안에도 시간이 흐르도록 남은 시간이 아니라 끝나는 시각을 기억한다
        private float _limitedSightRange;
        private float _sightLimitEndTime;
        #endregion

        #region Properties
        /// <summary>발견이 확정된 대상. 놓친 뒤 유예 시간 동안에도 유지된다. 없으면 null.</summary>
        public Transform CurrentTarget => _currentTarget;

        /// <summary>발견이 확정된 대상이 있는지 여부.</summary>
        public bool HasTarget => _currentTarget != null;

        /// <summary>대상을 마지막으로 본 위치. 시야를 잃어도 유지된다.</summary>
        public Vector3 LastKnownPosition => _lastKnownPosition;

        /// <summary>마지막 목격 위치가 기록되어 있는지 여부.</summary>
        public bool HasLastKnownPosition => _hasLastKnownPosition;

        /// <summary>지금 적용 중인 시야 거리. 제한 중이면 제한 거리, 아니면 시야 거리 설정값.</summary>
        public float CurrentSightRange =>
            (Time.time < _sightLimitEndTime) ? Mathf.Min(_limitedSightRange, _sightRange) : _sightRange;
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

        /// <summary>
        /// 확정된 대상, 놓친 뒤 유예, 마지막 목격 위치를 모두 지운다.
        /// 다음 감지부터는 대상이 없는 상태처럼 시야각 안에서만 다시 찾는다.
        /// </summary>
        public void ForgetTarget()
        {
            _currentTarget = null;
            _graceTimer = 0.0f;
            _hasLastKnownPosition = false;
        }

        /// <summary>
        /// 지정한 시간 동안 시야 거리를 range 이하로 줄인다. 시야 거리 설정값보다 넓어지지는 않는다.
        /// 제한 중에 다시 부르면 덮어쓴다. seconds 가 0 이하면 제한을 바로 푼다.
        /// </summary>
        /// <param name="range">제한할 시야 거리(m). 0 이면 그동안 아무것도 감지하지 않는다</param>
        /// <param name="seconds">제한 시간(초)</param>
        public void LimitSightRange(float range, float seconds)
        {
            _limitedSightRange = Mathf.Max(0.0f, range);
            _sightLimitEndTime = Time.time + seconds;
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// 시야 범위 안의 대상 중 각도와 차폐 검사를 모두 통과한 가장 가까운 것을 찾아
        /// 대상과 놓친 뒤 유예 시간을 갱신한다.
        /// 확정된 대상이 있는 동안에는 각도 검사를 건너뛴다.
        /// </summary>
        private void Detect()
        {
            Transform seenTarget = null;

            Vector3 eyePosition = _eye.position;
            int hitCount = Physics.OverlapSphereNonAlloc(
                eyePosition,
                CurrentSightRange,
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

                // 대상이 확정된 동안(전투 중)에는 시야각을 무시하고 시야 거리 안이면 360도로 찾는다
                if (!HasTarget && !IsInSightAngle(toTarget)) continue;
                if (IsOccluded(eyePosition, toTarget)) continue;

                float sqrDistance = toTarget.sqrMagnitude;
                if (sqrDistance >= nearestSqrDistance) continue;

                nearestSqrDistance = sqrDistance;
                seenTarget = hit.transform;
            }

            if (seenTarget)
            {
                _currentTarget = seenTarget;
                _graceTimer = _loseGraceTime;
                _lastKnownPosition = _currentTarget.position;
                _hasLastKnownPosition = true;
                return;
            }

            // 확정된 대상을 놓쳐도 유예 시간 동안은 실제 위치를 계속 추적한다
            if (_currentTarget && (_graceTimer > 0.0f))
            {
                _graceTimer -= _detectInterval;
                _lastKnownPosition = _currentTarget.position;
                return;
            }

            // 유예가 끝나면 놓친다. 마지막 목격 위치는 남아 수색이 이어받는다
            _currentTarget = null;
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
