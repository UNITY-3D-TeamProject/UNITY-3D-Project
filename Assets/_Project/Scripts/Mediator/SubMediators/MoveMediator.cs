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
        #endregion
        
        #region Private Fields
        private IMoveController _moveController;
        private Vector3 _lastDirection;
        private float _jumpPower;
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
        #endregion

        #region Unity Lifecycle

        protected override void Awake()
        {
            base.Awake();
            ResolveComponent(ref _motor);
            ResolveComponent(ref _moveDirectionCalculator);
            ResolveComponent(ref _rollMover);
            MoveController = GetComponentInParent<IMoveController>();
        }

        protected override void OnEnable()
        {
            BindRequest();
            Subscribe();
            base.OnEnable();
        }

        private void OnDisable()
        {
            // 비활성화 시 입력 잔여값으로 계속 움직이지 않도록 정지
            if (_moveDirectionCalculator) _moveDirectionCalculator.SetMoveInput(Vector2.zero);
            if (_motor) _motor.Direction = Vector3.zero;
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

            _rollMover.Roll(direction, distance, duration);
        }
        #endregion

        #region Protected Methods
        /// <summary>
        /// 원하는 속성값이 변한 경우에 대한 콜백
        /// </summary>
        protected override void InitAttributeCallback()
        {
            AttributeCallback.TryAdd(_speedValueKey, (float newValue, float oldValue) =>
            {
                if (_motor != null) _motor.Speed = newValue;
            });

            AttributeCallback.TryAdd(_jumpSpeedValueKey, (float newValue, float oldValue) =>
            {
                _jumpPower = newValue;
            });
        }

        /// <summary>
        /// 속성값에 대한 초기화 진행
        /// </summary>
        protected override void InitValue()
        {
            if (_motor == null || AttributeGetter == null) return;

            _motor.Speed = AttributeGetter.Invoke(_speedValueKey);
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
        /// 점프 명령을 전달한다. 구르는 중이거나 공중이면 무시한다.
        /// </summary>
        private void CommandJump()
        {
            if (!_motor) return;
            if (_rollMover && _rollMover.IsRolling) return;
            if (!_motor.IsGrounded) return;

            _motor.Jump(_jumpPower);
        }
        
        /// <summary>
        /// Controller 에 대한 바인딩 실행
        /// </summary>
        private void BindRequest()
        {
            MoveController?.SetMoveRequest(SendMoveInput);
            MoveController?.SetJumpRequest(CommandJump);
        }

        /// <summary>
        /// Controller 에 대한 언바인딩 실행
        /// </summary>
        private void UnBindRequest()
        {
            MoveController?.ClearMoveRequest();
            MoveController?.ClearJumpRequest();
        }

        /// <summary>
        /// 구르기 시작 시 입력에 의한 이동 정지
        /// </summary>
        private void OnRollStarted()
        {
            if (_motor) _motor.Direction = Vector3.zero;
        }

        /// <summary>
        /// 구르기 종료 시 구르는 동안 들어온 최신 입력 방향으로 이동 재개
        /// </summary>
        private void OnRollEnded()
        {
            if (_motor) _motor.Direction = _lastDirection;
        }

        /// <summary>
        /// CharacterMotor 의 점프 시작을 외부로 알린다.
        /// </summary>
        private void NotifyJumpStarted()
        {
            OnJumpStarted?.Invoke();
        }

        /// <summary>
        /// CharacterMotor 의 착지를 외부로 알린다.
        /// </summary>
        private void NotifyJumpEnded()
        {
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
                _rollMover.OnRollStarted += OnRollStarted;
                _rollMover.OnRollEnded += OnRollEnded;
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
                _rollMover.OnRollStarted -= OnRollStarted;
                _rollMover.OnRollEnded -= OnRollEnded;
                _rollMover.OnDisplacementCalculated -= ApplyRollDisplacement;
            }
        }

        #endregion
    }
}
