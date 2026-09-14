using UnityEngine;

namespace Movement
{
    /// <summary>
    /// CharacterController 기반 이동을 담당하는 공용 컴포넌트.
    /// Player/Enemy가 각자 계산한 방향을 갖고 매 프레임 Move(direction, speed)만 호출하면
    /// 중력 누적, 지면 부착까지 이 컴포넌트가 대신 처리한다.
    /// 이동하는 방향으로 오브젝트 회전 : 오브젝트 하는 사람이 알아서 처리
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class CharacterMotor : MonoBehaviour
    {
        #region Constants
        private const float GROUNDED_STICK_VELOCITY = -2.0f;
        #endregion

        #region Serialized Fields
        [Header("Gravity")]
        [SerializeField] private float _gravity = -20.0f;
        #endregion

        #region Private Fields
        private CharacterController _controller;
        private float _verticalVelocity;
        private Vector3 _externalDisplacement;
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
        /// <param name="direction">이동 방향. 정규화되지 않아도 된다.</param>
        /// <param name="speed">이번 프레임에 적용할 수평 이동 속도.</param>
        public void Move(Vector3 direction, float speed)
        {
            ApplyGravity();

            Vector3 horizontalVelocity = direction.normalized * speed;
            Vector3 velocity = horizontalVelocity + (Vector3.up * _verticalVelocity);

            Vector3 displacementToApply = _externalDisplacement;
            _externalDisplacement = Vector3.zero;

            _controller.Move((velocity * Time.deltaTime) + displacementToApply);
        }

        /// <summary>
        /// 외부 요인에 의한 변위를 이번 프레임 이동에 합산한다.
        /// 한 프레임에 2번 부르지 않게끔 해주는 함수
        /// 이동 Platform 탑승, 컨베이어, 넉백, 바람 등에서 사용
        /// 이 컴포넌트는 변위의 원인을 알지 않는다.
        /// 누적된 값은 다음 Move() 호출 때 적용되고 비워지므로, Move()는 매 프레임 호출해야 한다.
        /// </summary>
        /// <param name="displacement">이번 프레임에 추가로 적용할 위치 변화량.</param>
        public void AddExternalDisplacement(Vector3 displacement)
        {
            _externalDisplacement += displacement;
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
                _verticalVelocity += _gravity * Time.deltaTime;
            }
        }
        #endregion
    }
}
