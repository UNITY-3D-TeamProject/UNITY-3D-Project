using UnityEngine;
using Skill.Core;

namespace Skill.Conditions
{
    /// <summary>
    /// 사용 횟수가 최대치에 도달하기 전까지만 스킬 발동을 허용하는 조건.
    /// 언제 횟수를 소모하고 초기화할지는 이 조건을 쓰는 쪽이 Use / ResetCount 로 정한다.
    /// </summary>
    public class UseCountCondition : MonoBehaviour, ISkillCondition
    {
        #region Serialized Fields
        [Header("Settings")]
        [Tooltip("최대 사용 횟수")]
        [SerializeField, Min(1)] private int _maxCount = 2;
        #endregion

        #region Private Fields
        private int _usedCount;
        #endregion

        #region Properties
        /// <summary>최대 사용 횟수.</summary>
        public int MaxCount => _maxCount;

        /// <summary>현재까지 사용한 횟수.</summary>
        public int UsedCount => _usedCount;
        #endregion

        #region Public Methods
        /// <summary>
        /// 사용 횟수를 1 늘린다.
        /// </summary>
        public void Use()
        {
            _usedCount++;
        }

        /// <summary>
        /// 사용 횟수를 0 으로 되돌린다.
        /// </summary>
        public void ResetCount()
        {
            _usedCount = 0;
        }
        #endregion

        #region ISkillCondition
        public bool IsCanExecute()
        {
            return _usedCount < _maxCount;
        }
        #endregion
    }
}
