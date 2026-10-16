using System;
using UnityEngine;
using UnityEngine.Serialization;
using Movement;

namespace Mediator.SubMediators
{
    public interface IMoveController
    {
        public void SetMoveRequest(Action<Vector2> callback);
        public void SetJumpRequest(Action callback);
        public void ClearMoveRequest();
        public void ClearJumpRequest();
    }

    /// <summary>
    /// 점프 입력을 뗐을 때를 알려줄 수 있는 컨트롤러. 누름만 알리는 IMoveController 와 따로 둔다.
    /// </summary>
    public interface IJumpReleaseController
    {
        public void SetJumpReleaseRequest(Action callback);
        public void ClearJumpReleaseRequest();
    }
    
    /// <summary>
    /// 이동 입력을 MoveDirectionCalculator 로 넘기고, 계산된 이동 방향과 점프 입력·속도 어트리뷰트를 CharacterMotor 로 전달하는 접착 컴포넌트.
    /// </summary>
    public class MoveMediator : MediatorBase
    {
        #region Serialized Fields
        [Header("References")]
        [FormerlySerializedAs("motor")]
        [SerializeField] private CharacterMotor _motor;
        [SerializeField] private MoveDirectionCalculator _moveDirectionCalculator;
        [SerializeField] private RollMover _rollMover;
        [Header("Settings")]
        [Tooltip("CharacterMotor.Speed 로 연결할 어트리뷰트 이름")]
        [SerializeField] private string _speedValueKey;
        [Tooltip("점프력으로 사용할 어트리뷰트 이름")]
        [SerializeField] private string _jumpSpeedValueKey;
        [Tooltip("발판에서 떨어진 뒤 지상 점프를 허용하는 유예 시간(초)")]
        [SerializeField, Min(0.0f)] private float _coyoteTime = 0.1f;
        [Tooltip("공중에서 쓰이지 않은 점프 입력을 기억했다가 착지 시 지상 점프로 실행하는 시간(초)")]
        [SerializeField, Min(0.0f)] private float _jumpBufferTime = 0.1f;
        #endregion

        #region Private Fields
        private IMoveController _moveController;
        private IJumpReleaseController _jumpReleaseController;
        private Vector3 _lastDirection;
        private Vector3 _rollDirection;
        private float _jumpPower;
        private float _baseGravity;
        private float _moveSpeedRatio = 1.0f;
        private float _jumpPowerRatio = 1.0f;
        private float _lastGroundedTime = float.NegativeInfinity;
        private float _lastJumpPressedTime = float.NegativeInfinity;
        private bool _hasJumpedSinceGrounded;
        #endregion

        #region Properties
        public IMoveController MoveController 
        { 
            get => _moveController;
            set
            {
                if(value == null) return;
                UnBindRequest();
                _moveController = value;
                BindRequest();
            }
        }
        #endregion

        #region Events
        /// <summary>이동 입력으로 계산된 이동 방향이 갱신되었을 때 발생한다. 구르는 중에도 발생한다.</summary>
        public event Action<Vector3> OnMoveDirectionChanged;
        /// <summary>점프를 시작했을 때 발생한다. 공중에서 다시 점프해도 매번 발생한다.</summary>
        public event Action OnJumpStarted;
        /// <summary>점프 후 착지했을 때 발생한다.</summary>
        public event Action OnJumpEnded;
        /// <summary>공중에서 점프 입력이 들어왔을 때 발생한다. 점프 여부는 구독하는 쪽이 판단해 CommandAirJump 로 요청한다.</summary>
        public event Action OnAirJumpRequested;
        /// <summary>점프 입력을 뗐을 때 발생한다.</summary>
        public event Action OnJumpReleased;
        /// <summary>구르기를 시작했을 때 구르기 방향(월드, 정규화되지 않음)과 함께 발생한다.</summary>
        public event Action<Vector3> OnRollStarted;
        /// <summary>구르기가 끝났거나 중단되었을 때 발생한다.</summary>
        public event Action OnRollEnded;
        #endregion

        #region Unity Lifecycle

        protected override void Awake()
        {
            base.Awake();
            ResolveComponent(ref _motor);
            ResolveComponent(ref _moveDirectionCalculator);
            ResolveComponent(ref _rollMover);
            if (_motor) _baseGravity = _motor.Gravity;
            _jumpReleaseController = GetComponentInParent<IJumpReleaseController>();
            MoveController = GetComponentInParent<IMoveController>();
        }

        protected override void OnEnable()
        {
            BindRequest();
            Subscribe();
            base.OnEnable();
        }

        private void Update()
        {
            // 코요테 타임 판정을 위해 마지막으로 접지한 시각을 기록
            if (_motor && _motor.IsGrounded) _lastGroundedTime = Time.time;
            TryBufferedJump();
        }

        private void OnDisable()
        {
            // 비활성화 시 입력 잔여값으로 계속 움직이지 않도록 정지
            if (_moveDirectionCalculator) _moveDirectionCalculator.SetMoveInput(Vector2.zero);
            if (_motor) _motor.Direction = Vector3.zero;
            // 모터도 비활성화되면 착지 알림 없이 점프 상태가 풀리므로 함께 초기화
            _hasJumpedSinceGrounded = false;
            _lastGroundedTime = float.NegativeInfinity;
            _lastJumpPressedTime = float.NegativeInfinity;
            Unsubscribe();
            UnBindRequest();
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// 카메라가 보는 방향을 MoveDirectionCalculator 로 전달한다.
        /// </summary>
        /// <param name="viewForward">카메라가 보는 방향 (월드 기준)</param>
        public void SetViewForward(Vector3 viewForward)
        {
            if (!_moveDirectionCalculator) return;

            _moveDirectionCalculator.SetViewForward(viewForward);
        }

        /// <summary>
        /// 카메라가 보는 방향을 수평면에 투영한 앞 방향을 반환한다.
        /// </summary>
        /// <returns>정규화된 수평 앞 방향. MoveDirectionCalculator 가 없으면 Vector3.zero</returns>
        public Vector3 GetViewDirection()
        {
            if (!_moveDirectionCalculator) return Vector3.zero;

            return _moveDirectionCalculator.ViewDirection;
        }

        /// <summary>
        /// CharacterMotor 가 지면에 닿아 있는지 반환한다.
        /// </summary>
        /// <returns>지면에 닿아 있으면 true. CharacterMotor 가 없으면 false</returns>
        public bool IsGrounded()
        {
            return _motor && _motor.IsGrounded;
        }

        /// <summary>
        /// 이동 입력 방향으로 구르기를 RollMover 에 요청한다. 입력이 없으면 카메라가 보는 수평 방향으로 구른다.
        /// 구르는 동안에는 이동/점프 입력이 모터에 전달되지 않는다.
        /// </summary>
        /// <param name="distance">이동 거리</param>
        /// <param name="duration">이동에 걸리는 시간(초)</param>
        public void CommandRoll(float distance, float duration)
        {
            if (!_rollMover || !_moveDirectionCalculator) return;
            if (_rollMover.IsRolling) return;
            
            // 이동 입력이 있으면 그 방향, 없으면 카메라 앞 방향
            Vector3 direction = (_lastDirection.sqrMagnitude > 0.0001f)
                ? _lastDirection
                : _moveDirectionCalculator.ViewDirection;

            _rollDirection = direction;
            _rollMover.Roll(direction, distance, duration);
        }

        /// <summary>
        /// 접지 여부와 관계없이 점프한다. 구르는 중이면 무시한다.
        /// </summary>
        /// <param name="powerRatio">지상 점프 대비 높이 비율. 높이는 속도의 제곱에 비례하므로 속도에는 제곱근을 곱한다.</param>
        public void CommandAirJump(float powerRatio)
        {
            if (!_motor) return;
            if (_rollMover && _rollMover.IsRolling) return;

            _motor.Jump(_jumpPower * _jumpPowerRatio * Mathf.Sqrt(powerRatio));
        }

        /// <summary>
        /// 이동속도 어트리뷰트에 곱할 배율을 설정한다.
        /// </summary>
        /// <param name="ratio">배율. 1 이면 변화 없음</param>
        public void SetMoveSpeedRatio(float ratio)
        {
            _moveSpeedRatio = ratio;
            if (_motor && AttributeGetter != null) _motor.Speed = AttributeGetter.Invoke(_speedValueKey) * _moveSpeedRatio;
        }

        /// <summary>
        /// 점프력 어트리뷰트에 곱할 배율을 설정한다. 지상·공중 점프 모두에 적용된다.
        /// </summary>
        /// <param name="ratio">배율. 1 이면 변화 없음</param>
        public void SetJumpPowerRatio(float ratio)
        {
            _jumpPowerRatio = ratio;
        }

        /// <summary>
        /// CharacterMotor 의 기본 중력에 곱할 배율을 설정한다.
        /// </summary>
        /// <param name="ratio">배율. 1 이면 변화 없음</param>
        public void SetGravityRatio(float ratio)
        {
            if (_motor) _motor.Gravity = _baseGravity * ratio;
        }

        /// <summary>
        /// 자동 전진을 켜거나 끈다. MoveDirectionCalculator 로 전달한다.
        /// </summary>
        /// <param name="isEnabled">켜면 true</param>
        /// <param name="turnSpeed">전진 방향이 시점 방향을 따라 도는 속도(도/초)</param>
        public void SetAutoForward(bool isEnabled, float turnSpeed)
        {
            if (!_moveDirectionCalculator) return;

            _moveDirectionCalculator.SetAutoForward(isEnabled, turnSpeed);
        }

        /// <summary>
        /// 일정 속도 상승(요시 점프)을 시작하거나 끝낸다. CharacterMotor 로 전달한다.
        /// 시작하면 그 점프 입력은 쓰인 것이므로 점프 버퍼를 비운다.
        /// </summary>
        /// <param name="isHovering">시작이면 true</param>
        /// <param name="riseSpeed">상승 속도. 끝낼 때는 쓰지 않는다</param>
        public void SetHover(bool isHovering, float riseSpeed)
        {
            if (isHovering) _lastJumpPressedTime = float.NegativeInfinity;
            if (!_motor) return;

            if (isHovering)
                _motor.StartHover(riseSpeed);
            else
                _motor.StopHover();
        }
        #endregion

        #region Protected Methods
        /// <summary>
        /// 원하는 속성값이 변한 경우에 대한 콜백
        /// </summary>
        protected override void InitAttributeCallback()
        {
            AttributeCallback.TryAdd(_speedValueKey, data =>
            {
                if (_motor != null) _motor.Speed = data.NewValue * _moveSpeedRatio;
            });

            AttributeCallback.TryAdd(_jumpSpeedValueKey, data =>
            {
                _jumpPower = data.NewValue;
            });
        }

        /// <summary>
        /// 속성값에 대한 초기화 진행
        /// </summary>
        protected override void InitValue()
        {
            if (_motor == null || AttributeGetter == null) return;

            _motor.Speed = AttributeGetter.Invoke(_speedValueKey) * _moveSpeedRatio;
            _jumpPower = AttributeGetter.Invoke(_jumpSpeedValueKey);
        }
        #endregion

        #region Private Methods

        /// <summary>
        /// 이동 입력을 MoveDirectionCalculator 로 전달한다.
        /// </summary>
        /// <param name="input">이동 입력 (x: 좌우, y: 전후)</param>
        private void SendMoveInput(Vector2 input)
        {
            if (!_moveDirectionCalculator) return;

            _moveDirectionCalculator.SetMoveInput(input);
        }

        /// <summary>
        /// MoveDirectionCalculator 가 계산한 이동 방향을 CharacterMotor 로 전달한다.
        /// 구르는 중에는 방향만 기억해 두고, 구르기가 끝날 때 적용한다.
        /// </summary>
        /// <param name="direction">계산된 월드 이동 방향</param>
        private void ApplyDirection(Vector3 direction)
        {
            _lastDirection = direction;
            OnMoveDirectionChanged?.Invoke(direction);
            //모터가 없거나 구르는 중일경우 return
            if (!_motor || (_rollMover && _rollMover.IsRolling)) return;

            _motor.Direction = direction;
        }

        /// <summary>
        /// 점프 명령을 전달한다. 구르는 중이면 무시하고, 공중이면 공중 점프를 요청한다.
        /// 걸어서 떨어진 직후 코요테 타임 안이면 지상 점프로 처리한다.
        /// 공중 입력은 버퍼에 기록하고, 공중 점프·요시 점프로 쓰이면 그 자리에서 버퍼가 비워진다.
        /// </summary>
        private void CommandJump()
        {
            if (!_motor) return;
            if (_rollMover && _rollMover.IsRolling) return;
            if (!_motor.IsGrounded && !IsInCoyoteTime())
            {
                _lastJumpPressedTime = Time.time;
                OnAirJumpRequested?.Invoke();
                return;
            }

            _motor.Jump(_jumpPower * _jumpPowerRatio);
        }

        /// <summary>
        /// 접지 상태에서 점프 버퍼 시간 안에 쓰이지 않은 공중 입력이 있으면 지상 점프한다. 구르는 중이면 무시한다.
        /// </summary>
        private void TryBufferedJump()
        {
            if (!_motor || !_motor.IsGrounded) return;
            if (_rollMover && _rollMover.IsRolling) return;
            if (Time.time - _lastJumpPressedTime > _jumpBufferTime) return;

            _motor.Jump(_jumpPower * _jumpPowerRatio);
        }

        /// <summary>
        /// 점프 없이 지면을 벗어난 뒤 코요테 타임 안인지 반환한다.
        /// </summary>
        /// <returns>마지막 착지 이후 점프한 적이 없고, 마지막 접지로부터 _coyoteTime 이 지나지 않았으면 true</returns>
        private bool IsInCoyoteTime()
        {
            return !_hasJumpedSinceGrounded && (Time.time - _lastGroundedTime <= _coyoteTime);
        }
        
        /// <summary>
        /// Controller 에 대한 바인딩 실행
        /// </summary>
        private void BindRequest()
        {
            MoveController?.SetMoveRequest(SendMoveInput);
            MoveController?.SetJumpRequest(CommandJump);
            _jumpReleaseController?.SetJumpReleaseRequest(NotifyJumpReleased);
        }

        /// <summary>
        /// Controller 에 대한 언바인딩 실행
        /// </summary>
        private void UnBindRequest()
        {
            MoveController?.ClearMoveRequest();
            MoveController?.ClearJumpRequest();
            _jumpReleaseController?.ClearJumpReleaseRequest();
        }

        /// <summary>
        /// 구르기 시작 시 입력에 의한 이동 정지
        /// </summary>
        private void HandleRollStarted()
        {
            if (_motor) _motor.Direction = Vector3.zero;
            OnRollStarted?.Invoke(_rollDirection);
        }

        /// <summary>
        /// 구르기 종료 시 구르는 동안 들어온 최신 입력 방향으로 이동 재개
        /// </summary>
        private void HandleRollEnded()
        {
            if (_motor) _motor.Direction = _lastDirection;
            OnRollEnded?.Invoke();
        }

        /// <summary>
        /// CharacterMotor 의 점프 시작을 외부로 알린다.
        /// </summary>
        private void NotifyJumpStarted()
        {
            // 점프 직후 유예 시간 안에 지상 점프가 다시 나가지 않도록 막는다
            _hasJumpedSinceGrounded = true;
            // 지상·공중·버퍼 점프 모두 입력이 쓰였으므로 버퍼를 비운다
            _lastJumpPressedTime = float.NegativeInfinity;
            OnJumpStarted?.Invoke();
        }

        /// <summary>
        /// 점프 입력을 뗀 것을 외부로 알린다.
        /// </summary>
        private void NotifyJumpReleased()
        {
            OnJumpReleased?.Invoke();
        }

        /// <summary>
        /// CharacterMotor 의 착지를 외부로 알린다.
        /// </summary>
        private void NotifyJumpEnded()
        {
            _hasJumpedSinceGrounded = false;
            OnJumpEnded?.Invoke();
        }

        /// <summary>
        /// RollMover 가 계산한 구르기 이동량을 CharacterMotor 로 전달한다.
        /// </summary>
        /// <param name="displacement">이번 물리 스텝에 적용할 이동량</param>
        private void ApplyRollDisplacement(Vector3 displacement)
        {
            if (_motor) _motor.AddExternalDisplacement(displacement);
        }

        /// <summary>
        /// MoveDirectionCalculator, RollMover, CharacterMotor 이벤트 구독
        /// </summary>
        private void Subscribe()
        {
            if (_moveDirectionCalculator) _moveDirectionCalculator.OnDirectionCalculated += ApplyDirection;
            if (_motor)
            {
                _motor.OnJumpStarted += NotifyJumpStarted;
                _motor.OnJumpEnded += NotifyJumpEnded;
            }
            if (_rollMover)
            {
                _rollMover.OnRollStarted += HandleRollStarted;
                _rollMover.OnRollEnded += HandleRollEnded;
                _rollMover.OnDisplacementCalculated += ApplyRollDisplacement;
            }
        }

        /// <summary>
        /// MoveDirectionCalculator, RollMover, CharacterMotor 이벤트 구독 해지
        /// </summary>
        private void Unsubscribe()
        {
            if (_moveDirectionCalculator) _moveDirectionCalculator.OnDirectionCalculated -= ApplyDirection;
            if (_motor)
            {
                _motor.OnJumpStarted -= NotifyJumpStarted;
                _motor.OnJumpEnded -= NotifyJumpEnded;
            }
            if (_rollMover)
            {
                _rollMover.OnRollStarted -= HandleRollStarted;
                _rollMover.OnRollEnded -= HandleRollEnded;
                _rollMover.OnDisplacementCalculated -= ApplyRollDisplacement;
            }
        }

        #endregion
    }
}
