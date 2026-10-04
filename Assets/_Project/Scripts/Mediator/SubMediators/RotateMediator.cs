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
    /// 몸통 회전 요청과 카메라 시점 방향을 CharacterRotator 에 전달하는 접착 컴포넌트.
    /// 회전 입력은 월드 축 기준 방향으로 해석해 넘기며, 둘 중 무엇을 따를지는 CharacterRotator 가 판단한다.
    /// </summary>
    public class RotateMediator : MediatorBase
    {
        #region Serialized Fields
        [Header("References")]
        [SerializeField] private CharacterRotator _rotator;
        #endregion

        #region Private Fields
        private IBodyRotateController _bodyRotateController;
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

        protected override void OnEnable()
        {
            BindRequest();
            base.OnEnable();
        }

        private void OnDisable()
        {
            UnBindRequest();
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// 카메라가 보는 방향을 CharacterRotator 로 전달한다.
        /// </summary>
        /// <param name="viewForward">카메라가 보는 방향 (월드 기준)</param>
        public void SetViewForward(Vector3 viewForward)
        {
            if (_rotator) _rotator.SetViewForward(viewForward);
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
        /// 회전 명령을 월드 축 기준 방향으로 CharacterRotator 에 전달한다.
        /// </summary>
        /// <param name="dir">회전 방향 입력 (x: 좌우, y: 전후)</param>
        private void CommandRotate(Vector2 dir)
        {
            if (_rotator) _rotator.SetLookDirection(new Vector3(dir.x, 0.0f, dir.y));
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
