using System;
using Attribute.Core;
using Skill.Core;
using Skill.Skills;
using UnityEngine;

namespace Mediator.SubMediators
{
    public interface ISkillRequestController
    {
        public delegate bool RequestExecuteSkillDelegate(string skillName);
        public delegate void RequestStopSkillDelegate(string skillName);

        public void SetRequestExecuteSkill(RequestExecuteSkillDelegate requestExecuteSkill);
        public void ClearRequestExecuteSkill();
        public void SetRequestStopSkill(RequestStopSkillDelegate requestStopSkill);
        public void ClearRequestStopSkill();
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

        #region Events
        /// <summary>
        /// 코스트 지불 요청. (어트리뷰트 이름, 소모량)을 받아 지불 성공 여부를 반환한다.
        /// 반환값은 마지막 구독자의 값만 남으므로 단일 구독(CharacterMediator)을 전제로 한다.
        /// </summary>
        public event Func<string, float, bool> OnPayRequested;

        /// <summary>구르기 요청. (이동 거리, 이동 시간)</summary>
        public event Action<float, float> OnRollRequested;

        /// <summary>조준 전환 요청.</summary>
        public event Action OnAimToggled;
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
            _skillRequestController.SetRequestStopSkill(_skillController.StopSkill);
            _skillController.SetGetAttribute(key => AttributeGetter?.Invoke(key) ?? 0.0f);
            _skillController.SetRequestPay(OnRequestPay);
            _skillController.SetNotifyExecuted(OnSkillExecuted);
        }

        /// <summary>
        /// 컴포넌트 이벤트 에 대한 언바인딩 실행
        /// </summary>
        private void UnBindRequest()
        {
            if (_skillRequestController == null) return;
            
            _skillRequestController.ClearRequestExecuteSkill();
            _skillRequestController.ClearRequestStopSkill();
        }

        /// <summary>
        /// 코스트 지불 요청 처리
        /// </summary>
        /// <param name="key">소모할 어트리뷰트 이름</param>
        /// <param name="amount">소모량</param>
        /// <returns>지불에 성공했으면 true</returns>
        private bool OnRequestPay(string key, float amount)
        {
            // 구독자가 없으면 지불 실패로 처리
            return OnPayRequested?.Invoke(key, amount) ?? false;
        }

        /// <summary>
        /// 발동한 스킬 종류에 맞는 요청 이벤트 발행
        /// </summary>
        /// <param name="skill">발동한 스킬</param>
        private void OnSkillExecuted(SkillBase skill)
        {
            switch (skill)
            {
                case Roll roll:
                    OnRollRequested?.Invoke(roll.Distance, roll.Duration);
                    break;
                case Aim:
                    OnAimToggled?.Invoke();
                    break;
            }
        }
        #endregion
    }
}
