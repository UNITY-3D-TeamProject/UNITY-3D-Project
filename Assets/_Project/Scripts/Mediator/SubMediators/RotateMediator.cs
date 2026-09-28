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
    /// 몸통 회전 요청(월드 기준)을 CharacterRotator 에 전달하는 접착 컴포넌트.
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
            _bodyRotateController = GetComponentInParent<IBodyRotateController>();
            BindRequest();
        }

        private void Update()
        {
            if (!_rotator) return;

            _rotator.SetLookDirection(new Vector3(_rotateInput.x, 0.0f, _rotateInput.y));
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
