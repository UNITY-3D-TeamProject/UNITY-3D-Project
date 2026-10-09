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

        [Header("Aim")]
        [Tooltip("조준점을 찾을 최대 거리. 아무것도 맞지 않으면 이 거리의 지점을 조준점으로 쓴다.")]
        [SerializeField, Min(0.0f)] private float _aimMaxDistance = 100.0f;
        [Tooltip("조준점 레이캐스트에 맞을 레이어. 자기 몸에 맞지 않도록 캐릭터 레이어는 뺀다.")]
        [SerializeField] private LayerMask _aimLayers = Physics.DefaultRaycastLayers;
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
            ResolveComponent(ref _targetCamera);
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

        /// <summary>
        /// 화면 중앙 조준점이 가리키는 월드 위치를 반환한다.
        /// 카메라와 캐릭터 사이의 물체는 무시하도록, 레이캐스트를 캐릭터(피벗) 깊이에서 시작한다.
        /// 아무것도 맞지 않으면 최대 거리 지점을 반환한다.
        /// </summary>
        /// <returns>조준점 (월드 기준)</returns>
        public Vector3 GetAimPoint()
        {
            if (_targetCamera == null) return transform.position + (transform.forward * _aimMaxDistance);

            Ray aimRay = _targetCamera.GetAimRay();

            // 피벗을 레이에 투영한 거리만큼 시작점을 앞당긴다 — 카메라 뒤쪽이면 카메라 위치에서 시작
            float startDistance = 0.0f;
            if (_targetCamera.Pivot)
            {
                startDistance = Mathf.Max(0.0f, Vector3.Dot(_targetCamera.Pivot.position - aimRay.origin, aimRay.direction));
            }

            float castDistance = Mathf.Max(0.0f, _aimMaxDistance - startDistance);
            bool hasHit = Physics.Raycast(
                aimRay.GetPoint(startDistance),
                aimRay.direction,
                out RaycastHit hit,
                castDistance,
                _aimLayers,
                QueryTriggerInteraction.Ignore);

            return hasHit ? hit.point : aimRay.GetPoint(_aimMaxDistance);
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

            _targetCamera.ApplyLook(amount);

            // 회전이 즉시 적용되므로 이번 입력이 반영된 피벗 방향이 전달된다
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
