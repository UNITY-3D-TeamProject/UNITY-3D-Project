using Skill.Core;
using UnityEngine;
using UnityEngine.Serialization;

namespace Skill.Costs
{
    /// <summary>
    /// 자원(Battery, Stamina 등)을 소모하는 코스트의 추상 부모. 소모 규칙은 이 클래스가 담당하고,
    /// 자원 값을 실제로 읽고 쓰는 방법은 서브클래스가 구현한다.
    /// </summary>
    public abstract class ResourceCost : MonoBehaviour, ISkillCost
    {
        #region Serialized Fields
        [FormerlySerializedAs("cost")]
        [Header("Settings")]
        [Tooltip("자원 사용량")]
        [SerializeField, Min(0.0f)] private float _cost = 1.0f;
        #endregion

        #region ISkillCost
        public bool CanPay()
        {
            return GetResource() >= _cost;
        }

        public void Pay()
        {
            SetResource(GetResource() - _cost);
        }
        #endregion

        #region Protected Methods
        /// <summary>
        /// 현재 자원 값을 읽는다.
        /// </summary>
        /// <returns>현재 자원 값</returns>
        protected abstract float GetResource();

        /// <summary>
        /// 자원 값을 수정한다.
        /// </summary>
        /// <param name="value">수정될 자원 값</param>
        protected abstract void SetResource(float value);
        #endregion
    }
}
