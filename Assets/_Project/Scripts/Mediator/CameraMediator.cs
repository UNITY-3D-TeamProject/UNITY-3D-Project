using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using CameraControl;

namespace Mediator
{
    public interface IRotateController
    {
        public void SetLookRequest(Action<Vector2> callback);
        public void ClearLookRequest();
    }
    
    /// <summary>
    /// 시점 회전 명령을 PlayerBaseCamera 에 전달하는 접착 컴포넌트.
    /// 카메라 피벗을 이동 기준 프레임(ReferenceFrame)으로 외부에 제공한다.
    /// </summary>
    public class CameraMediator : MonoBehaviour
    {
        #region Serialized Fields
        [Header("References")]
        [FormerlySerializedAs("targetCamera")]
        [SerializeField] private PlayerBaseCamera _targetCamera;
        #endregion

        #region Private Fields
        private IRotateController _rotateController;
        private readonly Dictionary<string, Action<float, float>> _attributeCallback = new(StringComparer.OrdinalIgnoreCase);
        #endregion
        
        #region Properties
        public Dictionary<string, Action<float, float>> AttributeCallback { get=>_attributeCallback; }
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
        /// <summary>이동 방향 계산의 기준이 되는 카메라 피벗. 카메라가 없으면 null.</summary>
        public Transform ReferenceFrame => _targetCamera ? _targetCamera.Pivot : null;
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
