using UnityEngine;

namespace Pet
{
    /// <summary>
    /// 지정한 대상(Transform) 위치를 부드럽게 따라가는 펫.
    /// 실제 이동 속도를 PetAnimator 에 전달하고, StopFollow 이후에는 추종을 멈춘다.
    /// </summary>
    public class PetFollower : MonoBehaviour
    {
        #region Constants
        // 이 속도(m/s) 이하면 멈춘 것으로 보고 대상의 정면을 바라본다
        private const float MOVE_THRESHOLD = 0.1f;
        #endregion

        #region Serialized Fields
        [Header("Follow")]
        [Tooltip("대상 위치까지 따라가는 데 걸리는 대략적인 시간(초)")]
        [SerializeField, Min(0.01f)] private float _smoothTime = 0.3f;
        [Tooltip("대상과 이 거리(m) 이상 멀어지면 대상 위치로 즉시 이동한다")]
        [SerializeField, Min(0.0f)] private float _teleportDistance = 10.0f;
        [Tooltip("초당 회전 각도")]
        [SerializeField] private float _rotateSpeed = 720.0f;

        [Header("References")]
        [SerializeField] private PetAnimator _petAnimator;
        #endregion

        #region Private Fields
        private Transform _target;
        private Vector3 _velocity;
        private bool _isStopped;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            if (!_petAnimator) _petAnimator = GetComponent<PetAnimator>();
        }

        // 대상(플레이어)이 Update 에서 이동한 뒤에 따라간다
        private void LateUpdate()
        {
            if (_isStopped || !_target) return;

            float deltaTime = Time.deltaTime;
            if (deltaTime <= 0.0f) return;

            Vector3 targetPosition = _target.position;
            if (Vector3.Distance(transform.position, targetPosition) >= _teleportDistance)
            {
                transform.SetPositionAndRotation(targetPosition, _target.rotation);
                _velocity = Vector3.zero;
            }
            else
            {
                transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref _velocity, _smoothTime);
            }

            Vector3 horizontalVelocity = Vector3.ProjectOnPlane(_velocity, Vector3.up);
            float moveSpeed = horizontalVelocity.magnitude;

            Rotate(moveSpeed > MOVE_THRESHOLD ? horizontalVelocity : _target.forward, deltaTime);
            if (_petAnimator) _petAnimator.SetMoveSpeed(moveSpeed);
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// 따라갈 대상을 설정하고 그 위치로 즉시 이동한다.
        /// </summary>
        /// <param name="target">따라갈 대상</param>
        public void SetTarget(Transform target)
        {
            _target = target;
            _velocity = Vector3.zero;
            if (_target) transform.SetPositionAndRotation(_target.position, _target.rotation);
        }

        /// <summary>
        /// 추종을 멈추고 그 자리에 머문다.
        /// </summary>
        public void StopFollow()
        {
            _isStopped = true;
            _velocity = Vector3.zero;
        }
        #endregion

        #region Private Methods
        private void Rotate(Vector3 direction, float deltaTime)
        {
            direction = Vector3.ProjectOnPlane(direction, Vector3.up);
            if (direction.sqrMagnitude <= Mathf.Epsilon) return;

            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, _rotateSpeed * deltaTime);
        }
        #endregion
    }
}
