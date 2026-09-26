using System;
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
        #endregion

        #region Private Fields
        private IMoveController _moveController;
        private Vector2 _moveInput;
        private Transform _referenceFrame;
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

        private void OnEnable()
        {
            BindRequest();
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
        #endregion
        
        #region Private Methods
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
