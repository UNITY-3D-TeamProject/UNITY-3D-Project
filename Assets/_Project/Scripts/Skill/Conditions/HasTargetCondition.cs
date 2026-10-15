using UnityEngine;
using Skill.Core;

namespace Skill.Conditions
{
    /// <summary>
    /// 대상이 있을 때만 스킬 발동을 허용하는 조건.
    /// 대상 유무는 이 조건을 쓰는 쪽이 SetHasTarget 으로 정한다.
    /// </summary>
    public class HasTargetCondition : MonoBehaviour, ISkillCondition
    {
        #region Private Fields
        private bool _hasTarget;
        #endregion

        #region Properties
        /// <summary>현재 대상이 있는지 여부.</summary>
        public bool HasTarget => _hasTarget;
        #endregion

        #region Public Methods
        /// <summary>
        /// 대상 유무를 설정한다.
        /// </summary>
        /// <param name="hasTarget">대상이 있으면 true</param>
        public void SetHasTarget(bool hasTarget)
        {
            _hasTarget = hasTarget;
        }
        #endregion

        #region ISkillCondition
        public bool IsCanExecute()
        {
            return _hasTarget;
        }
        #endregion
    }
}
