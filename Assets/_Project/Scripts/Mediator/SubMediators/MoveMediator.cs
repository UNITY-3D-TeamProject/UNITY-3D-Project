using System;
using System.Collections;
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
        private static readonly WaitForFixedUpdate WaitFixedUpdate = new();
        private IMoveController _moveController;
        private Coroutine _rollCoroutine;
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
            // 컴포넌트만 비활성화되면 코루틴이 계속 돌기 때문에 직접 정지
            if (_rollCoroutine != null)
            {
                StopCoroutine(_rollCoroutine);
                _rollCoroutine = null;
            }
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
        /// 카메라가 보는 수평 방향으로 일정 시간 동안 일정 거리를 구른다.
        /// 이미 구르는 중이면 무시한다.
        /// </summary>
        /// <param name="distance">이동 거리</param>
        /// <param name="duration">이동에 걸리는 시간(초)</param>
        public void CommandRoll(float distance, float duration)
        {
            if (!_motor || !_moveDirectionCalculator || _rollCoroutine != null) return;

            Vector3 direction = _moveDirectionCalculator.ViewDirection;

            // 시간이 0 이하면 다음 물리 스텝에 한 번에 이동
            if (duration <= 0.0f)
            {
                _motor.AddExternalDisplacement(direction * distance);
                return;
            }

            _rollCoroutine = StartCoroutine(CoRoll(direction, distance, duration));
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

        #region Coroutines
        /// <summary>
        /// duration 동안 물리 스텝마다 나눠서 direction 으로 distance 만큼 이동시킨다.
        /// </summary>
        /// <param name="direction">정규화된 이동 방향</param>
        /// <param name="distance">이동 거리</param>
        /// <param name="duration">이동에 걸리는 시간(초). 0 보다 커야 한다</param>
        private IEnumerator CoRoll(Vector3 direction, float distance, float duration)
        {
            float speed = distance / duration;
            float elapsed = 0.0f;

            while (elapsed < duration)
            {
                yield return WaitFixedUpdate;

                // 마지막 스텝은 남은 시간만큼만 이동해 총 이동 거리를 맞춘다
                float deltaTime = Mathf.Min(Time.fixedDeltaTime, duration - elapsed);
                _motor.AddExternalDisplacement(direction * (speed * deltaTime));
                elapsed += deltaTime;
            }

            _rollCoroutine = null;
        }
        #endregion
    }
}
