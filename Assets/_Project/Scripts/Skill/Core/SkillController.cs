using System;
using System.Collections.Generic;
using UnityEngine;

namespace Skill.Core
{
    /// <summary>
    /// 보유한 스킬을 이름(대소문자 무시)으로 관리하고, 요청이 오면 사용 가능 여부를 확인한 뒤 발동하는 컴포넌트.
    /// Awake 에서 자식에 있는 스킬을 자동으로 등록한다. 비활성 스킬은 활성화되기 전까지 사용할 수 없다.
    /// </summary>
    public class SkillController : MonoBehaviour
    {
        #region Private Fields
        private readonly Dictionary<string, SkillBase> _skills = new(StringComparer.OrdinalIgnoreCase);
        private Func<string, float> _getAttribute;
        private Func<string, float, bool> _requestPay;
        private Action<SkillBase> _notifyExecuted;
        #endregion
        
        #region Unity Lifecycle
        private void Awake()
        {
            foreach (var skill in GetComponentsInChildren<SkillBase>())
            {
                RegisterSkill(skill);
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// 원하는 값을 얻기 위한 델리게이트 주입
        /// </summary>
        public void SetGetAttribute(Func<string, float> getAttribute)
        {
            if (getAttribute == null) return;
            _getAttribute = getAttribute;
            foreach(KeyValuePair<string, SkillBase> skillPair in _skills)
            {
                SkillBase skill = skillPair.Value;
                skill.SetGetAttribute(getAttribute);
            }
        }

        /// <summary>
        /// 코스트 지불을 요청하기 위한 델리게이트 주입
        /// </summary>
        public void SetRequestPay(Func<string, float, bool> requestPay)
        {
            if (requestPay == null) return;
            _requestPay = requestPay;
            foreach(KeyValuePair<string, SkillBase> skillPair in _skills)
            {
                SkillBase skill = skillPair.Value;
                skill.SetRequestPay(requestPay);
            }
        }

        /// <summary>
        /// 스킬 발동을 알리기 위한 델리게이트 주입
        /// </summary>
        public void SetNotifyExecuted(Action<SkillBase> notifyExecuted)
        {
            if (notifyExecuted == null) return;
            _notifyExecuted = notifyExecuted;
            foreach(KeyValuePair<string, SkillBase> skillPair in _skills)
            {
                SkillBase skill = skillPair.Value;
                skill.SetNotifyExecuted(notifyExecuted);
            }
        }

        /// <summary>
        /// 스킬을 등록한다. 이름이 비어 있거나 이미 같은 이름이 있으면 등록하지 않는다.
        /// </summary>
        /// <param name="skill">등록할 스킬</param>
        public void RegisterSkill(SkillBase skill)
        {
            if (!skill) return;

            if (string.IsNullOrWhiteSpace(skill.SkillName))
            {
                Debug.LogWarning($"[{name}] SkillName 이 비어 있는 스킬은 등록할 수 없습니다: {skill.name}", skill);
                return;
            }

            if (!_skills.TryAdd(skill.SkillName, skill))
            {
                Debug.LogWarning($"[{name}] '{skill.SkillName}' 이름의 스킬이 이미 등록되어 있습니다: {skill.name}", skill);
            }
            if (_getAttribute != null) skill.SetGetAttribute(_getAttribute);
            if (_requestPay != null) skill.SetRequestPay(_requestPay);
            if (_notifyExecuted != null) skill.SetNotifyExecuted(_notifyExecuted);
        }

        /// <summary>
        /// 스킬 등록을 해제한다.
        /// </summary>
        /// <param name="skillName">해제할 스킬 이름</param>
        public void UnregisterSkill(string skillName)
        {
            if (string.IsNullOrEmpty(skillName)) return;
            _skills.Remove(skillName);
        }

        /// <summary>
        /// 스킬 사용을 요청한다. 사용 가능할 때만 발동한다.
        /// </summary>
        /// <param name="skillName">사용할 스킬 이름</param>
        /// <returns>실제로 발동했으면 true</returns>
        public bool TryExecuteSkill(string skillName)
        {
            if (!TryGetSkill(skillName, out var skill)) return false;

            return skill.TryExecute();
        }

        /// <summary>
        /// 진행 중인 스킬 동작의 중지를 요청한다.
        /// </summary>
        /// <param name="skillName">중지할 스킬 이름</param>
        public void StopSkill(string skillName)
        {
            if (!TryGetSkill(skillName, out var skill)) return;

            skill.Stop();
        }

        /// <summary>
        /// 스킬 컴포넌트를 활성화/비활성화한다. 비활성화되면 진행 중인 동작은 스킬의 OnDisable 에서 중지된다.
        /// </summary>
        /// <param name="skillName">대상 스킬 이름</param>
        /// <param name="isEnabled">활성화 여부</param>
        public void SetSkillEnabled(string skillName, bool isEnabled)
        {
            if (!TryGetSkill(skillName, out var skill)) return;

            skill.enabled = isEnabled;
        }

        /// <summary>
        /// 스킬 컴포넌트의 활성화 여부를 반환한다.
        /// </summary>
        /// <param name="skillName">대상 스킬 이름</param>
        /// <returns>등록된 스킬이 활성화되어 있으면 true</returns>
        public bool IsSkillEnabled(string skillName)
        {
            if (!TryGetSkill(skillName, out var skill)) return false;

            return skill.enabled;
        }

        /// <summary>
        /// 등록된 모든 스킬의 진행 중인 동작을 중지한다.
        /// </summary>
        public void StopAllSkills()
        {
            foreach (KeyValuePair<string, SkillBase> skillPair in _skills)
            {
                SkillBase skill = skillPair.Value;
                if (skill) skill.Stop();
            }
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// 등록된 스킬을 이름으로 찾는다. 없으면 경고를 남긴다.
        /// </summary>
        /// <param name="skillName">찾을 스킬 이름</param>
        /// <param name="skill">찾은 스킬</param>
        /// <returns>찾았으면 true</returns>
        private bool TryGetSkill(string skillName, out SkillBase skill)
        {
            skill = null;
            if (string.IsNullOrEmpty(skillName)) return false;

            if (!_skills.TryGetValue(skillName, out skill) || !skill)
            {
                Debug.LogWarning($"[{name}] '{skillName}' 이름으로 등록된 스킬이 없습니다.", this);
                return false;
            }

            return true;
        }
        #endregion
    }
}
