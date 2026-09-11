using UnityEngine;

namespace Project.Systems
{
    /// <summary>
    /// CharacterController 기반 이동을 담당하는 공용 컴포넌트.
    /// Player/Enemy가 각자 계산한 방향을 갖고 매 프레임 Move(direction, speed)만 호출하면
    /// 중력 누적, 지면 부착, 이동 방향으로 회전(FaceDirection), 이동 Platform 탑승까지
    /// 이 컴포넌트가 대신 처리한다.
    /// 사용법: 같은 오브젝트에 CharacterController와 함께 붙이고, GetComponent&lt;CharacterMotor&gt;()로
    /// 참조한 뒤 Move()만 호출하면 된다. Move()는 프레임당 정확히 한 번만 호출해야 한다 —
    /// CharacterController.Move()를 여러 번 호출하면 충돌 판정이 깨진다.
    /// 이동 Platform에 탑승하려면 Platform 프리팹을 인스펙터의 Platform Layer Mask에 포함된
    /// 레이어로 설정하고, 같은 오브젝트에 WaypointMover를 붙여야 한다.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class CharacterMotor : MonoBehaviour
    {
        #region Constants
        private const float GROUNDED_STICK_VELOCITY = -2.0f;
        private const float PLATFORM_NORMAL_THRESHOLD = 0.5f;
        #endregion

        #region Serialized Fields
        [Header("References")]
        [SerializeField] private SOMovementConfig _config;

        [Header("Platform")]
        [Tooltip("탑승 가능한 이동 Platform으로 인식할 레이어. Platform 프리팹을 이 레이어로 설정해야 한다.")]
        [SerializeField] private LayerMask _platformLayerMask;
        #endregion

        #region Private Fields
        private CharacterController _controller;
        private float _verticalVelocity;
        private Vector3 _platformDelta;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// 방향과 속도로 이번 프레임의 이동을 적용한다.
        /// </summary>
        /// <param name="direction">이동 방향. 정규화되지 않아도 되며, 크기가 0이면 회전하지 않는다.</param>
        /// <param name="speed">이번 프레임에 적용할 수평 이동 속도.</param>
        public void Move(Vector3 direction, float speed)
        {
            ApplyGravity();

            Vector3 horizontalVelocity = direction.normalized * speed;
            Vector3 velocity = horizontalVelocity + (Vector3.up * _verticalVelocity);

            Vector3 platformDeltaToApply = _platformDelta;
            _platformDelta = Vector3.zero;

            _controller.Move((velocity * Time.deltaTime) + platformDeltaToApply);

            if (direction != Vector3.zero)
            {
                FaceDirection(direction);
            }
        }
        #endregion

        #region Private Methods
        private void ApplyGravity()
        {
            bool isGroundedAndFalling = _controller.isGrounded && (_verticalVelocity < 0.0f);
            if (isGroundedAndFalling)
            {
                _verticalVelocity = GROUNDED_STICK_VELOCITY;
            }
            else
            {
                _verticalVelocity += _config.Gravity * Time.deltaTime;
            }
        }

        private void FaceDirection(Vector3 direction)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, _config.RotationSpeed * Time.deltaTime);
        }

        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            bool isFloorHit = hit.normal.y > PLATFORM_NORMAL_THRESHOLD;
            bool isPlatformLayer = (_platformLayerMask.value & (1 << hit.gameObject.layer)) != 0;

            if (!isFloorHit || !isPlatformLayer)
            {
                return;
            }

            if (hit.collider.TryGetComponent(out WaypointMover platform))
            {
                _platformDelta += platform.DeltaThisFrame;
            }
        }
        #endregion
    }
}
