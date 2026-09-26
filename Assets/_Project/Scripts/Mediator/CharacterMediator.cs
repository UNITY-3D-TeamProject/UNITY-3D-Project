using System.Collections.Generic;
using Attribute.Core;
using Mediator.SubMediators;
using UnityEngine;
using UnityEngine.Serialization;

namespace Mediator
{
    /// // Awake 가 늦게 실행되어 다른 Mediator 에 대한 조정 실행
    [DefaultExecutionOrder(100)]
    public class CharacterMediator : MonoBehaviour
    {
        #region Serialized Fields
        [Header("References")]
        [SerializeField] private AttributeSet _attributeSet;
        [FormerlySerializedAs("moveMediator")]
        [SerializeField] private MoveMediator _moveMediator;
        [FormerlySerializedAs("cameraMediator")]
        [SerializeField] private CameraMediator _cameraMediator;
        [SerializeField] private CombatMediator _combatMediator;
        #endregion

        #region Private Fields
        private IMoveController _moveController;
        private IRotateController _rotateController;
        private MediatorBase[] _mediators;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            _mediators = GetComponentsInChildren<MediatorBase>();
        }

        private void OnEnable()
        {
            BindCallbacks();
            
            if (_moveMediator && _cameraMediator)
            {
                _moveMediator.SetReferenceFrame(_cameraMediator.ReferenceFrame);
            }
        }

        private void OnDisable()
        {
            UnbindCallbacks();
            
            if (_moveMediator)
            {
                _moveMediator.SetReferenceFrame(null);
            }
        }
        #endregion
        
        #region Private Methods
        /// <summary>
        /// 필요한 바인딩 실행
        /// </summary>
        private void BindCallbacks()
        {
            if(_attributeSet)
            {
                _attributeSet.AddOnAttributeChangedCallback(OnAttributeChangeCallback);
                foreach (var mediator in _mediators)
                {
                    mediator.SetGetAttribute(_attributeSet.GetValue);
                }
            }
        }
        /// <summary>
        /// 등록된 바인딩 해제
        /// </summary>
        private void UnbindCallbacks()
        {
            if(_attributeSet) _attributeSet.RemoveOnAttributeChangedCallback(OnAttributeChangeCallback);
            foreach (var mediator in _mediators)
            {
                mediator.ClearGetAttribute();
            }
        }
        /// <summary>
        /// 중재자들이 원하는 값 변경시 알림 발송
        /// </summary>
        private void OnAttributeChangeCallback(string attributeName, float newValue, float oldValue)
        {
            foreach (var mediator in _mediators)
            {
                mediator.NotifyAttributeChanged(attributeName, newValue, oldValue);
            }
        }
        #endregion
    }
}
