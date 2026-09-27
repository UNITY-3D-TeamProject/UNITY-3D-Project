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
        [Header("Settings")]
        [Tooltip("CharacterMotor.Speed 로 연결할 어트리뷰트 이름")]
        [SerializeField] private string _speedValueKey;
        [Tooltip("CharacterMotor.JumpSpeed 로 연결할 어트리뷰트 이름")]
        [SerializeField] private string _jumpSpeedValueKey;
        #endregion
        
        #region Private Fields
        private IMoveController _moveController;
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

        #region Unity Lifecycle

        protected override void Awake()
        {
            base.Awake();
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
                if (_motor != null) _motor.JumpSpeed = newValue;
            });
        }

        /// <summary>
        /// 속성값에 대한 초기화 진행
        /// </summary>
        protected override void InitValue()
        {
            if (_motor == null || AttributeGetter == null) return;

            _motor.Speed = AttributeGetter.Invoke(_speedValueKey);
            _motor.JumpSpeed = AttributeGetter.Invoke(_jumpSpeedValueKey);
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
        /// </summary>
        /// <param name="direction">계산된 월드 이동 방향</param>
        private void ApplyDirection(Vector3 direction)
        {
            if (!_motor) return;

            _motor.Direction = direction;
        }

        /// <summary>
        /// 점프 명령을 전달한다.
        /// </summary>
        private void CommandJump()
        {
            _motor?.Jump();
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
        /// MoveDirectionCalculator 이벤트 구독
        /// </summary>
        private void Subscribe()
        {
            if (_moveDirectionCalculator) _moveDirectionCalculator.OnDirectionCalculated += ApplyDirection;
        }

        /// <summary>
        /// MoveDirectionCalculator 이벤트 구독 해지
        /// </summary>
        private void Unsubscribe()
        {
            if (_moveDirectionCalculator) _moveDirectionCalculator.OnDirectionCalculated -= ApplyDirection;
        }

        #endregion
    }
}
