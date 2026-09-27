using System;
using Attribute.Core;
using Skill.Core;
using UnityEngine;

namespace Mediator.SubMediators
{
    public interface ISkillRequestController
    {
        public delegate bool RequestExecuteSkillDelegate(string skillName);

        public void SetRequestExecuteSkill(RequestExecuteSkillDelegate requestExecuteSkill);
        public void ClearRequestExecuteSkill();
    }
    
    public class SkillMediator : MediatorBase
    {
        #region Serialized Fields
        [Header("References")]
        [SerializeField] private SkillController _skillController;
        #endregion
    
        #region Private Fields
        private ISkillRequestController _skillRequestController;
        private IEffectTarget _effectTarget;
        private IEffectTargetReceiver[] _effectTargetReceivers;
        #endregion

        #region Unity Lifecycle

        protected override void Awake()
        {
            base.Awake();
            _skillRequestController = GetComponentInParent<ISkillRequestController>();
            _effectTarget = GetComponentInParent<IEffectTarget>();
            _effectTargetReceivers = GetComponentsInChildren<IEffectTargetReceiver>(true);
            BindRequest();
        }

        protected override void OnEnable()
        {
            BindRequest();
            BindEffectTarget();
            base.OnEnable();
        }

        private void OnDisable()
        {
            UnBindRequest();
            UnBindEffectTarget();
        }
        #endregion
        
        #region Protected Methods
        protected override void InitAttributeCallback()
        {
        }

        protected override void InitValue()
        {
        }
        #endregion
        
        #region Private Methods
        /// <summary>
        /// 컴포넌트 이벤트 에 대한 바인딩 실행
        /// </summary>
        private void BindRequest()
        {
            if (_skillController == null || _skillRequestController == null) return;

            _skillRequestController.SetRequestExecuteSkill(_skillController.TryExecuteSkill);
        }

        /// <summary>
        /// 컴포넌트 이벤트 에 대한 언바인딩 실행
        /// </summary>
        private void UnBindRequest()
        {
            if (_skillRequestController == null) return;
            
            _skillRequestController.ClearRequestExecuteSkill();
        }

        /// <summary>
        /// 자식의 IEffectTargetReceiver 에 IEffectTarget 주입
        /// </summary>
        private void BindEffectTarget()
        {
            if (_effectTarget == null) return;

            foreach (var receiver in _effectTargetReceivers)
            {
                receiver.SetEffectTarget(_effectTarget);
            }
        }

        /// <summary>
        /// 주입한 IEffectTarget 해제
        /// </summary>
        private void UnBindEffectTarget()
        {
            foreach (var receiver in _effectTargetReceivers)
            {
                receiver.ClearEffectTarget();
            }
        }
        #endregion
    }
}
