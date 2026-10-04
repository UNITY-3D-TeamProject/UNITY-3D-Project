using System;
using UnityEngine;
using Rotation;

namespace Mediator.SubMediators
{
    public interface IBodyRotateController
    {
        public void SetBodyRotateRequest(Action<Vector2> callback);
        public void ClearBodyRotateRequest();
    }

    /// <summary>
    /// 몸통 회전 요청을 CharacterRotator 에 전달하는 접착 컴포넌트.
    /// 카메라 시점 방향이 전달되어 있으면 입력과 무관하게 그 정면을 바라보고,
    /// 없으면 회전 입력을 월드 축 기준 방향으로 해석한다.
    /// </summary>
    public class RotateMediator : MediatorBase
    {
        #region Serialized Fields
        [Header("References")]
        [SerializeField] private CharacterRotator _rotator;
        #endregion

        #region Private Fields
        private IBodyRotateController _bodyRotateController;
        private Vector2 _rotateInput;
        private Vector3 _viewForward;
        #endregion

        #region Properties
        public IBodyRotateController BodyRotateController
        {
            get => _bodyRotateController;
            set
            {
                if (value == null) return;
                UnBindRequest();
                _bodyRotateController = value;
                BindRequest();
            }
        }
        #endregion

        #region Unity Lifecycle
        protected override void Awake()
        {
            base.Awake();
            ResolveComponent(ref _rotator);
            _bodyRotateController = GetComponentInParent<IBodyRotateController>();
            BindRequest();
        }

        private void Update()
        {
            if (!_rotator) return;

            if (_viewForward == Vector3.zero)
            {
                _rotator.SetLookDirection(new Vector3(_rotateInput.x, 0.0f, _rotateInput.y));
                return;
            }

            // 카메라 시점이 있으면 입력과 무관하게 그 정면을 바라본다 (수평 투영은 SetLookDirection 내부에서 처리)
            _rotator.SetLookDirection(_viewForward);
        }

        protected override void OnEnable()
        {
            BindRequest();
            base.OnEnable();
        }

        private void OnDisable()
        {
            // 비활성화 시 입력 잔여값으로 계속 돌지 않도록 정지
            _rotateInput = Vector2.zero;
            UnBindRequest();
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// 카메라가 보는 방향을 설정한다. 설정되면 몸통이 그 정면을 바라본다.
        /// </summary>
        /// <param name="viewForward">카메라가 보는 방향 (월드 기준)</param>
        public void SetViewForward(Vector3 viewForward)
        {
            _viewForward = viewForward;
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
        /// 회전 명령을 전달한다. 값은 다음 Update 까지 유지된다.
        /// </summary>
        /// <param name="dir">회전 방향 입력 (x: 좌우, y: 전후)</param>
        private void CommandRotate(Vector2 dir)
        {
            _rotateInput = dir;
        }

        /// <summary>
        /// Controller 에 대한 바인딩 실행
        /// </summary>
        private void BindRequest()
        {
            _bodyRotateController?.SetBodyRotateRequest(CommandRotate);
        }

        /// <summary>
        /// Controller 에 대한 언바인딩 실행
        /// </summary>
        private void UnBindRequest()
        {
            _bodyRotateController?.ClearBodyRotateRequest();
        }
        #endregion
    }
}
