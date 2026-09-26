using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using Movement;

namespace Mediator
{
    public interface IMoveController
    {
        public void SetMoveRequest(Action<Vector2> callback);
        public void SetJumpRequest(Action callback);
        public void ClearMoveRequest();
        public void ClearJumpRequest();
    }
    
    /// <summary>
    /// 2D 이동 입력을 월드 방향으로 변환해 CharacterMotor.Direction 에 전달하는 접착 컴포넌트.
    /// 기준 프레임(카메라 피벗 등)이 설정되어 있으면 그 수평 forward/right 를 기준으로 방향을 계산하고,
    /// 없으면 월드 축 기준으로 계산한다.
    /// </summary>
    public class MoveMediator : MonoBehaviour
    {
        #region Serialized Fields
        [Header("References")]
        [FormerlySerializedAs("motor")]
        [SerializeField] private CharacterMotor _motor;
        [Header("Settings")]
        [Tooltip("CharacterMotor.Speed 로 연결할 어트리뷰트 이름")]
        [SerializeField] private string _speedValueKey;
        [Tooltip("CharacterMotor.JumpSpeed 로 연결할 어트리뷰트 이름")]
        [SerializeField] private string _jumpSpeedValueKey;
        #endregion
        
        #region Private Fields
        private IMoveController _moveController;
        private Vector2 _moveInput;
        private Transform _referenceFrame;
        private readonly Dictionary<string, Action<float, float>> _attributeCallback = new(StringComparer.OrdinalIgnoreCase);
        private GetAttributeDelegate _getAttribute;
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

        public Dictionary<string, Action<float, float>> AttributeCallback { get=>_attributeCallback; }
        #endregion
        
        #region Delegates
        public delegate float GetAttributeDelegate(string key);
        #endregion
        
        #region Unity Lifecycle
        private void Update()
        {
            if (!_motor) return;

            if (!_referenceFrame)
            {
                _motor.Direction = new Vector3(_moveInput.x, 0, _moveInput.y);
                return;
            }

            // 기준 프레임의 forward/right 를 수평면에 투영해 카메라 기울기가 이동에 섞이지 않게 한다
            Vector3 forward = Vector3.ProjectOnPlane(_referenceFrame.forward, Vector3.up).normalized;
            Vector3 right = Vector3.ProjectOnPlane(_referenceFrame.right, Vector3.up).normalized;
            _motor.Direction = (right * _moveInput.x) + (forward * _moveInput.y);
        }

        private void Awake()
        {
            InitAttributeCallback();
        }

        private void OnEnable()
        {
            BindRequest();
            InitValue();
        }

        private void OnDisable()
        {
            // 비활성화 시 입력 잔여값으로 계속 움직이지 않도록 정지
            _moveInput = Vector2.zero;
            if (_motor) _motor.Direction = Vector3.zero;
            UnBindRequest();
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// 이동 방향 계산의 기준 프레임을 설정한다. null 이면 월드 축 기준.
        /// </summary>
        /// <param name="frame">기준이 될 Transform (보통 카메라 피벗)</param>
        public void SetReferenceFrame(Transform frame)
        {
            _referenceFrame = frame;
        }

        /// <summary>
        /// 속성값을 읽기 위한 델리게이트 설정
        /// </summary>
        public void SetGetAttribute(GetAttributeDelegate callback)
        {
            if (callback == null) return;
            _getAttribute = callback;
            InitValue();
        }
        
        /// <summary>
        /// 속성값을 읽기 위한 델리게이트 제거
        /// </summary>
        public void ClearGetAttribute()
        {
            _getAttribute = null;
        }
        
        #endregion
        
        #region Private Methods

        /// <summary>
        /// 원하는 속성값이 변한 경우에 대한 콜백
        /// </summary>
        private void InitAttributeCallback()
        {
            _attributeCallback.Add(_speedValueKey, (float newValue, float oldValue) =>
            {
                if (_motor != null) _motor.Speed = newValue;
            });
            
            _attributeCallback.Add(_jumpSpeedValueKey, (float newValue, float oldValue) =>
            {
                if (_motor != null) _motor.JumpSpeed = newValue;
            });
        }

        /// <summary>
        /// 속성값에 대한 초기화 진행
        /// </summary>
        private void InitValue()
        {
            if (_getAttribute == null) return;

            _motor.Speed = _getAttribute.Invoke(_speedValueKey);
            _motor.JumpSpeed = _getAttribute.Invoke(_jumpSpeedValueKey);
        }
        
        /// <summary>
        /// 이동 명령을 전달한다. 값은 다음 Update 까지 유지된다.
        /// </summary>
        /// <param name="dir">이동 입력 (x: 좌우, y: 전후)</param>
        private void CommandMove(Vector2 dir)
        {
            _moveInput = dir;
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
            MoveController?.SetMoveRequest(CommandMove);
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
        
        #endregion
    }
}
