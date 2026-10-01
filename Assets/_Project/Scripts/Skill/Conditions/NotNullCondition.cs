using UnityEngine;
using Skill.Core;

namespace Skill.Conditions
{
    /// <summary>
    /// 지정한 참조가 모두 연결되어 있을 때만 스킬 발동을 허용하는 조건.
    /// 스킬과 같은 GameObject 에 붙이고, 스킬이 필요로 하는 참조를 References 에 연결해 사용한다.
    /// </summary>
    public class NotNullCondition : MonoBehaviour, ISkillCondition
    {
        #region Serialized Fields
        [Header("Settings")]
        [Tooltip("null 이 아니어야 하는 참조 목록. 하나라도 비어 있거나 파괴되면 발동하지 않는다.")]
        [SerializeField] private Object[] _references;
        #endregion

        #region ISkillCondition
        public bool IsCanExecute()
        {
            if (_references == null) return true;

            foreach (Object reference in _references)
            {
                // UnityEngine.Object 의 == 오버로드로 파괴된 객체도 null 로 판정
                if (reference == null) return false;
            }

            return true;
        }
        #endregion
    }
}
