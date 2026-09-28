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
        #endregion

        #region Unity Lifecycle

        protected override void Awake()
        {
            base.Awake();
            _skillRequestController = GetComponentInParent<ISkillRequestController>();
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
            _skillController.SetGetAttribute(key => AttributeGetter?.Invoke(key) ?? 0.0f);
            _skillController.SetRequestPay(OnRequestPay);
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
        /// 코스트 지불 요청 처리
        /// </summary>
        /// <param name="key">소모할 어트리뷰트 이름</param>
        /// <param name="amount">소모량</param>
        /// <returns>지불에 성공했으면 true</returns>
        private bool OnRequestPay(string key, float amount)
        {
            // TODO: CharacterMediator 로 지불 요청 전달 후 결과 반환
            Debug.Log($"[{name}] 코스트 지불 요청: {key} -{amount}", this);
            return true;
        }
        #endregion
    }
}
