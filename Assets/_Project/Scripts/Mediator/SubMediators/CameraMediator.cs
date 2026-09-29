using System;
using UnityEngine;
using UnityEngine.Serialization;
using CameraControl;

namespace Mediator.SubMediators
{
    public interface ICameraController
    {
        public void SetLookRequest(Action<Vector2> callback);
        public void ClearLookRequest();

        /// <summary>조준 요청 콜백 등록. true 면 조준 시작, false 면 조준 해제.</summary>
        public void SetAimRequest(Action<bool> callback);
        public void ClearAimRequest();
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
        private ICameraController _cameraController;
        #endregion

        #region Properties
        public ICameraController CameraController
        {
            get => _cameraController;
            set
            {
                if(value == null) return;
                UnBindRequest();
                _cameraController = value;
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
            _cameraController = GetComponentInParent<ICameraController>();
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
        /// 조준 명령을 전달한다.
        /// </summary>
        /// <param name="isAiming">true 면 조준 시작, false 면 조준 해제</param>
        private void CommandAim(bool isAiming)
        {
            if (_targetCamera == null) return;

            _targetCamera.SetAim(isAiming);
        }
        /// <summary>
        /// Controller 에 대한 바인딩 실행
        /// </summary>
        private void BindRequest()
        {
            _cameraController?.SetLookRequest(CommandRotateCamera);
            _cameraController?.SetAimRequest(CommandAim);
        }

        /// <summary>
        /// Controller 에 대한 언바인딩 실행
        /// </summary>
        private void UnBindRequest()
        {
            _cameraController?.ClearLookRequest();
            _cameraController?.ClearAimRequest();
        }
        
        #endregion
    }
}
