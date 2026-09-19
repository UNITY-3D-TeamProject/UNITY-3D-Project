using UnityEngine;

namespace Movement
{
    /// <summary>
    /// CharacterController 기반 이동을 담당하는 공용 컴포넌트.
    /// Player/Enemy가 각자 계산한 방향과 속도를 Direction/Speed 프로퍼티에 설정만 하면
    /// FixedUpdate에서 이동, 중력 누적, 지면 부착까지 이 컴포넌트가 대신 처리한다.
    /// 설정한 값은 유지되므로 멈추려면 Direction을 Vector3.zero(또는 Speed를 0)로 설정한다.
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
        [Tooltip("중력 가속도 크기(양수). 아래 방향으로 적용된다.")]
        [SerializeField] private float _gravity = 20.0f;
        #endregion

        #region Private Fields
        private CharacterController _controller;
        private float _verticalVelocity;
        private Vector3 _externalDisplacement;
        private Vector3 _direction;
        private float _speed;
        #endregion

        #region Properties
        /// <summary>이동 방향. 정규화되지 않아도 된다.</summary>
        public Vector3 Direction
        {
            get => _direction;
            set => _direction = value;
        }

        /// <summary>수평 이동 속도.</summary>
        public float Speed
        {
            get => _speed;
            set => _speed = value;
        }
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
        }

        private void FixedUpdate()
        {
            Move(Time.fixedDeltaTime);
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// 외부 요인에 의한 변위를 다음 FixedUpdate 스텝 이동에 합산한다.
        /// 여러 번 호출하면 누적된다.
        /// 이동 Platform 탑승, 컨베이어, 넉백, 바람 등에서 사용
        /// 이 컴포넌트는 변위의 원인을 알지 않는다.
        /// 누적된 값은 다음 FixedUpdate의 이동 때 적용되고 비워진다.
        /// </summary>
        /// <param name="displacement">다음 스텝에 추가로 적용할 위치 변화량.</param>
        public void AddExternalDisplacement(Vector3 displacement)
        {
            _externalDisplacement += displacement;
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Direction/Speed와 중력으로 이번 스텝의 이동을 적용한다.
        /// </summary>
        /// <param name="deltaTime">이번 스텝의 경과 시간.</param>
        private void Move(float deltaTime)
        {
            // 컨트롤러 없을때 방지
            if (!_controller.enabled) return;

            ApplyGravity(deltaTime);

            // 방향에 y값 섞여서 수평 속도 줄어들지 않게 방지
            Vector3 horizontalDirection = new Vector3(_direction.x, 0.0f, _direction.z).normalized;
            Vector3 horizontalVelocity = horizontalDirection * _speed;
            Vector3 velocity = horizontalVelocity + (Vector3.up * _verticalVelocity);

            Vector3 displacementToApply = _externalDisplacement;
            _externalDisplacement = Vector3.zero;
            
            // 순간이동 시 도루마무 되지 않기 위함
            Physics.SyncTransforms();
            
            _controller.Move((velocity * deltaTime) + displacementToApply);
        }

        private void ApplyGravity(float deltaTime)
        {
            bool isGroundedAndFalling = _controller.isGrounded && (_verticalVelocity < 0.0f);
            if (isGroundedAndFalling)
            {
                _verticalVelocity = GROUNDED_STICK_VELOCITY;
            }
            else
            {
                _verticalVelocity -= _gravity * deltaTime;
            }
        }
        #endregion
    }
}
