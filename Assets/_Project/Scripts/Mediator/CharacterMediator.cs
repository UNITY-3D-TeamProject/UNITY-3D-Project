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
        [SerializeField] private RotateMediator _rotateMediator;
        #endregion

        #region Private Fields
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
            Subscribe();

            // 시점 입력 전에도 이동이 카메라 기준이 되도록 초기 방향을 한 번 전달
            if (_cameraMediator) _cameraMediator.PublishViewForward();
        }

        private void OnDisable()
        {
            UnbindCallbacks();
            Unsubscribe();
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
        /// <summary>
        /// 카메라 중재자가 알린 시점 방향을 이동·회전 중재자로 전달
        /// </summary>
        private void SendViewForward(Vector3 viewForward)
        {
            if (_moveMediator) _moveMediator.SetViewForward(viewForward);
            if (_rotateMediator) _rotateMediator.SetViewForward(viewForward);
        }
        /// <summary>
        /// 중재자 이벤트 구독
        /// </summary>
        private void Subscribe()
        {
            if (_cameraMediator) _cameraMediator.OnViewForwardChanged += SendViewForward;
        }
        /// <summary>
        /// 중재자 이벤트 구독 해지
        /// </summary>
        private void Unsubscribe()
        {
            if (_cameraMediator) _cameraMediator.OnViewForwardChanged -= SendViewForward;
        }
        #endregion
    }
}
