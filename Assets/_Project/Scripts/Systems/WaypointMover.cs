using UnityEngine;

namespace Project.Systems
{
    /// <summary>WaypointMover가 목표 지점을 다 돌았을 때 어떻게 진행할지 결정한다.</summary>
    public enum EWaypointLoopMode
    {
        Once,
        Loop,
        PingPong,
    }

    /// <summary>
    /// 여러 웨이포인트를 순서대로 따라가는 좌표 기반 이동 공용 컴포넌트.
    /// 이동 Platform, Crowd(군중)처럼 "미리 정해진 좌표를 순서대로 따라가는" 오브젝트가 공유해서 쓴다.
    /// 사용법: Waypoints 배열을 인스펙터에서 연결한다. Crowd처럼 정지-대기가 필요하면
    /// IsPaused를 true/false로 토글하면 된다. Platform처럼 탑승자를 태워야 하면 DeltaThisFrame을
    /// 읽어서 넘겨주면 된다(CharacterMotor가 같은 오브젝트의 이 컴포넌트를 이미 찾아서 사용한다).
    /// </summary>
    public class WaypointMover : MonoBehaviour
    {
        #region Constants
        private const float ARRIVAL_THRESHOLD = 0.05f;
        #endregion

        #region Serialized Fields
        [Header("Waypoints")]
        [SerializeField] private Transform[] _waypoints;
        [SerializeField] private float _speed = 3.0f;
        [SerializeField] private EWaypointLoopMode _loopMode = EWaypointLoopMode.Loop;

        [Header("Rotation")]
        [Tooltip("이동 방향을 바라보도록 회전할지 여부. 일부 Platform은 회전이 필요 없다.")]
        [SerializeField] private bool _shouldRotate;
        [SerializeField] private float _rotationSpeed = 5.0f;
        #endregion

        #region Private Fields
        private int _currentIndex;
        private int _direction = 1;
        private bool _isPaused;
        #endregion

        #region Properties
        /// <summary>이번 프레임에 실제로 이동한 위치 변화량. 탑승자에게 그대로 전달하면 된다.</summary>
        public Vector3 DeltaThisFrame { get; private set; }

        /// <summary>true로 설정하면 웨이포인트 진행을 멈춘다 (Crowd의 정지-대기 등에 사용).</summary>
        public bool IsPaused
        {
            get => _isPaused;
            set => _isPaused = value;
        }
        #endregion

        #region Unity Lifecycle
        private void Update()
        {
            bool hasNoWaypoints = (_waypoints == null) || (_waypoints.Length == 0);
            if (_isPaused || hasNoWaypoints)
            {
                DeltaThisFrame = Vector3.zero;
                return;
            }

            Vector3 previousPosition = transform.position;
            Vector3 targetPosition = _waypoints[_currentIndex].position;

            transform.position = Vector3.MoveTowards(transform.position, targetPosition, _speed * Time.deltaTime);
            DeltaThisFrame = transform.position - previousPosition;

            if (_shouldRotate && (DeltaThisFrame != Vector3.zero))
            {
                RotateTowardsMovement();
            }

            bool hasArrived = Vector3.Distance(transform.position, targetPosition) <= ARRIVAL_THRESHOLD;
            if (hasArrived)
            {
                AdvanceWaypoint();
            }
        }
        #endregion

        #region Private Methods
        private void RotateTowardsMovement()
        {
            Quaternion targetRotation = Quaternion.LookRotation(DeltaThisFrame.normalized);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, _rotationSpeed * Time.deltaTime);
        }

        private void AdvanceWaypoint()
        {
            switch (_loopMode)
            {
                case EWaypointLoopMode.Once:
                    _currentIndex = Mathf.Min(_currentIndex + 1, _waypoints.Length - 1);
                    break;

                case EWaypointLoopMode.Loop:
                    _currentIndex = (_currentIndex + 1) % _waypoints.Length;
                    break;

                case EWaypointLoopMode.PingPong:
                    bool isAtEnd = (_currentIndex + _direction >= _waypoints.Length) || (_currentIndex + _direction < 0);
                    if (isAtEnd)
                    {
                        _direction *= -1;
                    }
                    _currentIndex += _direction;
                    break;
            }
        }
        #endregion
    }
}
