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
        public bool TryUseSkill(string skillName)
        {
            if (!TryGetSkill(skillName, out var skill)) return false;

            return skill.TryExecute();
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
