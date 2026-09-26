using UnityEngine;
using Skill.Core;

namespace Skill.Costs
{
    /// <summary>
    /// 스킬 사용 후 일정 시간 동안 다시 사용할 수 없게 하는 쿨타임 코스트.
    /// 스킬과 같은 GameObject 에 붙여 사용한다.
    /// </summary>
    public class CooldownCost : MonoBehaviour, ISkillCost
    {
        #region Serialized Fields
        [Header("Settings")]
        [Tooltip("재사용 대기 시간(초)")]
        [SerializeField, Min(0.0f)] private float _cooldown = 1.0f;
        #endregion

        #region Private Fields
        private float _readyTime = 0;
        #endregion

        #region ISkillCost
        public bool CanPay()
        {
            return Time.time >= _readyTime;
        }

        public void Pay()
        {
            _readyTime = Time.time + _cooldown;
        }
        #endregion
    }
}
