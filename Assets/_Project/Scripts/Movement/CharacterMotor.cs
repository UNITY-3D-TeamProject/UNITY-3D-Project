using UnityEngine;

namespace Movement
{
    /// <summary>
    /// CharacterController 기반 이동을 담당하는 공용 컴포넌트.
    /// Player/Enemy가 각자 계산한 방향과 속도를 Direction/Speed 프로퍼티에 설정만 하면
    /// FixedUpdate에서 이동, 중력 누적, 지면 부착까지 이 컴포넌트가 대신 처리한다.
    /// 설정한 값은 유지되므로 멈추려면 Direction을 Vector3.zero(또는 Speed를 0)로 설정한다.
    /// 이동하는 방향으로 오브젝트 회전 : 오브젝트 하는 사람이 알아서 처리
    /// 실제로 움직이는 것은 이 스크립트가 붙은 오브젝트가 아니라 CharacterController가 붙은 오브젝트다.
    /// 따라서 CharacterController는 캐릭터 루트에 두고, 이 스크립트는 자식에 있어도 된다.
    /// </summary>
    public class CharacterMotor : MonoBehaviour
    {
        #region Constants
        private const float GROUNDED_STICK_VELOCITY = -2.0f;
        #endregion

        #region Serialized Fields
        [Header("Gravity")]
        [Tooltip("중력 가속도 크기(양수). 아래 방향으로 적용된다.")]
        [SerializeField] private float _gravity = 9.8f;

        [Header("Jump")]
        [Tooltip("공중에서 목표 수평 속도로 수렴하는 가속도. 클수록 공중 조작이 민첩해진다.")]
        [SerializeField] private float _airAcceleration = 10.0f;
        #endregion

        #region Private Fields
        private CharacterController _controller;
        private float _verticalVelocity;
        private Vector3 _horizontalVelocity;
        private Vector3 _externalDisplacement;
        private Vector3 _direction;
        private float _speed;
        private float _jumpPower;       // Adapter를 통해 JumPower 값 가져옴.
        private bool _jumpRequested;
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

        /// <summary>점프 시 부여되는 초기 상승 속도.</summary>
        public float JumpSpeed
        {
            get => _jumpPower;
            set => _jumpPower = value;
        }

        /// <summary>지면에 닿아 있는지 여부.</summary>
        public bool IsGrounded => _controller.isGrounded;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            // GetComponentInParent는 자기 자신을 먼저 검사하므로 루트/자식 배치를 모두 커버한다
            _controller = GetComponentInParent<CharacterController>();

            if (_controller == null)
            {
                Debug.LogError($"[{name}] CharacterController를 찾지 못했습니다. 캐릭터 루트에 추가하세요.", this);
                enabled = false;
                return;
            }

            // 컨트롤러가 자식에 붙어 있으면 루트가 아니라 그 자식만 움직이게 된다
            bool isControllerOnChild = (_controller.gameObject == gameObject) && (transform.parent != null);
            if (isControllerOnChild)
            {
                Debug.LogError(
                    $"[{name}] CharacterController가 자식 오브젝트에 붙어 있습니다. 캐릭터 루트로 옮기세요.",
                    this);
            }
        }

        private void FixedUpdate()
        {
            ConsumeJumpRequest();
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

        /// <summary>
        /// 점프를 요청한다. 실제 접지 판정과 발동은 다음 FixedUpdate에서 이루어진다.
        /// Update 타이밍(입력 이벤트)에 곧바로 isGrounded를 확인하면, 물리 스텝 사이의
        /// 타이밍 불일치로 인해 실제로는 접지 상태인데도 입력이 씹히는 문제가 있어 분리했다.
        /// </summary>
        public void Jump()
        {
            // Debug.Log($"[JumpDebug] Jump() called. _jumpSpeed={_jumpSpeed}, _controller.enabled={_controller.enabled}");
            if (!_controller.enabled) return;

            _jumpRequested = true;
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// 요청된 점프를 FixedUpdate 타이밍의 접지 판정으로 소비한다.
        /// 접지가 아니어도 요청은 이번 호출로 소멸한다(다음 FixedUpdate까지만 유효).
        /// </summary>
        private void ConsumeJumpRequest()
        {
            // if (_jumpRequested)
            // {
            //     Debug.Log($"[JumpDebug] ConsumeJumpRequest(). isGrounded={_controller.isGrounded}, _jumpSpeed={_jumpPower}");
            // }

            if (_jumpRequested && _controller.isGrounded)
            {
                _verticalVelocity = _jumpPower;
            }

            _jumpRequested = false;
        }

        /// <summary>
        /// Direction/Speed와 중력으로 이번 스텝의 이동을 적용한다.
        /// </summary>
        /// <param name="deltaTime">이번 스텝의 경과 시간.</param>
        private void Move(float deltaTime)
        {
            // 컨트롤러 없을때 방지
            if (!_controller.enabled) return;

            ApplyGravity(deltaTime);
            UpdateHorizontalVelocity(deltaTime);

            Vector3 velocity = _horizontalVelocity + (Vector3.up * _verticalVelocity);

            Vector3 displacementToApply = _externalDisplacement;
            _externalDisplacement = Vector3.zero;
            
            // 순간이동 시 도루마무 되지 않기 위함
            Physics.SyncTransforms();
            
            _controller.Move((velocity * deltaTime) + displacementToApply);
        }

        /// <summary>
        /// 지상에서는 목표 수평 속도를 즉시 반영하고,
        /// 공중에서는 입력 유무와 관계없이 _airAcceleration 만큼씩 목표 속도(입력 없으면 0)로 수렴시킨다.
        /// 즉 공중에서도 관성 없이 점진적으로 감속/가속한다.
        /// </summary>
        /// <param name="deltaTime">이번 스텝의 경과 시간.</param>
        private void UpdateHorizontalVelocity(float deltaTime)
        {
            // 방향에 y값 섞여서 수평 속도 줄어들지 않게 방지
            Vector3 horizontalDirection = new Vector3(_direction.x, 0.0f, _direction.z).normalized;
            Vector3 targetVelocity = horizontalDirection * _speed;
 
            if (_controller.isGrounded)
            {
                _horizontalVelocity = targetVelocity;
                return;
            }

            _horizontalVelocity = Vector3.MoveTowards(
                _horizontalVelocity,
                targetVelocity,
                _airAcceleration * deltaTime);
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
