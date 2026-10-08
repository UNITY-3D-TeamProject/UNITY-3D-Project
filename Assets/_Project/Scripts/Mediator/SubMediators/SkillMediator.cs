using System;
using Attribute.Core;
using Skill.Core;
using Skill.Skills;
using UnityEngine;

namespace Mediator.SubMediators
{
    public interface ISkillRequestController
    {
        public void SetRequestExecuteSkill(Action<string> requestExecuteSkill);
        public void ClearRequestExecuteSkill();
        public void SetRequestStopSkill(Action<string> requestStopSkill);
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

        /// <summary>사격 요청. (총알 생성 위치)</summary>
        public event Action<Vector3> OnFireRequested;

        /// <summary>
        /// 실행 요청으로 스킬이 발동했을 때 발생한다. (스킬 이름, 쿨타임)
        /// 요청 1회당 1번만 발생하며, 연사처럼 스킬 내부에서 반복 발동하는 경우는 알리지 않는다.
        /// </summary>
        public event Action<string, float> OnSkillUsed;
        #endregion

        #region Unity Lifecycle

        protected override void Awake()
        {
            base.Awake();
            ResolveComponent(ref _skillController);
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
            // Stop 요청 경로가 끊기기 전에 진행 중인 지속형 스킬(연사 등)을 멈춘다
            if (_skillController) _skillController.StopAllSkills();
            UnBindRequest();
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// 스킬을 활성화/비활성화한다.
        /// </summary>
        /// <param name="skillName">대상 스킬 이름</param>
        /// <param name="isEnabled">활성화 여부</param>
        public void SetSkillEnabled(string skillName, bool isEnabled)
        {
            if (!_skillController) return;

            _skillController.SetSkillEnabled(skillName, isEnabled);
        }

        /// <summary>
        /// 스킬의 활성화 여부를 반환한다.
        /// </summary>
        /// <param name="skillName">대상 스킬 이름</param>
        /// <returns>스킬이 활성화되어 있으면 true</returns>
        public bool IsSkillEnabled(string skillName)
        {
            return _skillController && _skillController.IsSkillEnabled(skillName);
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

            _skillRequestController.SetRequestExecuteSkill(OnRequestExecuteSkill);
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
        /// 스킬 실행 요청 처리. 발동에 성공하면 사용된 스킬과 쿨타임을 알린다.
        /// </summary>
        /// <param name="skillName">실행할 스킬 이름</param>
        private void OnRequestExecuteSkill(string skillName)
        {
            if (!_skillController.TryExecuteSkill(skillName)) return;

            OnSkillUsed?.Invoke(skillName, _skillController.GetSkillCooldown(skillName));
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
                case Fire fire:
                    OnFireRequested?.Invoke(fire.FirePosition);
                    break;
            }
        }
        #endregion
    }
}
