using System;
using UnityEngine;
using UnityEngine.Serialization;
using CameraControl;

namespace Mediator.SubMediators
{
    public interface IRotateController
    {
        public void SetLookRequest(Action<Vector2> callback);
        public void ClearLookRequest();
    }
    
    /// <summary>
    /// 시점 회전 명령을 PlayerBaseCamera 에 전달하는 접착 컴포넌트.
    /// 회전 명령 시 카메라 피벗의 forward 를 이벤트로 알린다.
    /// </summary>
    public class CameraMediator : MediatorBase
    {
        #region Serialized Fields
        [Header("References")]
        [FormerlySerializedAs("targetCamera")]
        [SerializeField] private PlayerBaseCamera _targetCamera;
        #endregion

        #region Private Fields
        private IRotateController _rotateController;
        private bool _isAiming;
        #endregion

        #region Properties
        public IRotateController RotateController 
        { 
            get => _rotateController;
            set
            {
                if(value == null) return;
                UnBindRequest();
                _rotateController = value;
                BindRequest();
            }
        }
        #endregion

        #region Events
        /// <summary>회전 명령이 전달될 때 카메라 피벗의 forward 를 알린다.</summary>
        public event Action<Vector3> OnViewForwardChanged;
        #endregion

        #region Unity Lifecycle

        protected override void Awake()
        {
            base.Awake();
            _rotateController = GetComponentInParent<IRotateController>();
            BindRequest();
        }
        
        #endregion

        #region Public Methods
        /// <summary>
        /// 현재 카메라 피벗의 forward 를 OnViewForwardChanged 로 알린다.
        /// </summary>
        public void PublishViewForward()
        {
            if (_targetCamera == null || !_targetCamera.Pivot) return;

            OnViewForwardChanged?.Invoke(_targetCamera.Pivot.forward);
        }

        /// <summary>
        /// 조준 상태를 전환한다.
        /// </summary>
        public void ToggleAim()
        {
            _isAiming = !_isAiming;

            // TODO: 시네머신 카메라 거리 조절 연결
            Debug.Log($"[{name}] 조준 상태: {_isAiming}", this);
        }
        #endregion

        #region Protected Methods
        /// <inheritdoc />
        protected override void InitAttributeCallback()
        {
        }

        /// <inheritdoc />
        protected override void InitValue()
        {
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// 카메라 회전 명령을 전달한다.
        /// </summary>
        /// <param name="amount">시점 입력 (x: yaw, y: pitch)</param>
        private void CommandRotateCamera(Vector2 amount)
        {
            if (_targetCamera == null) return;

            _targetCamera.Look = amount;

            // 실제 회전은 PlayerBaseCamera.LateUpdate 에서 적용되므로 직전 프레임까지의 피벗 방향이 전달된다
            PublishViewForward();
        }
        /// <summary>
        /// Controller 에 대한 바인딩 실행
        /// </summary>
        private void BindRequest()
        {
            _rotateController?.SetLookRequest(CommandRotateCamera);
        }
        
        /// <summary>
        /// Controller 에 대한 언바인딩 실행
        /// </summary>
        private void UnBindRequest()
        {
            _rotateController?.ClearLookRequest();
        }
        
        #endregion
    }
}
