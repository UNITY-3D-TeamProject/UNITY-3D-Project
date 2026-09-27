using System;
using Skill.Core;
using UnityEngine;
using UnityEngine.Serialization;

namespace Skill.Costs
{
    /// <summary>
    /// 자원(Battery, Stamina 등)을 소모하는 코스트의 추상 부모. 소모 규칙은 이 클래스가 담당하고,
    /// 자원 값을 실제로 읽고 쓰는 방법은 서브클래스가 구현한다.
    /// </summary>
    public class ResourceCost : MonoBehaviour, ISkillCost
    {
        #region Serialized Fields
        [FormerlySerializedAs("cost")]
        [Header("Settings")]
        [Tooltip("자원 사용량")]
        [SerializeField, Min(0.0f)] private float _cost = 1.0f;
        [Tooltip("자원으로 사용할 어트리뷰트 이름")]
        [SerializeField] private string _resourceKey;
        #endregion

        #region Private Fields
        private Func<string, float> _getAttribute;
        #endregion
        
        #region ISkillCost

        public void SetGetAttribute(Func<string, float> getAttribute)
        {
            if (getAttribute == null) return;
            _getAttribute = getAttribute;
        }

        public bool CanPay()
        {
            if (_getAttribute != null)
            {
                return _getAttribute.Invoke(_resourceKey) >= _cost;
            }
            return false;
        }

        public void Pay()
        {
        }
        
        #endregion
    }
}
