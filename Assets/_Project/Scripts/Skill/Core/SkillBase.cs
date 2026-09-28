using System;
using UnityEngine;

namespace Skill.Core
{
    /// <summary>
    /// 모든 스킬의 추상 부모. 스킬 동작은 상속받아 Execute 에 구현한다.
    /// 사용 조건(ISkillCondition)과 코스트(ISkillCost)는 같은 GameObject 에 붙은 컴포넌트를 참조해 판단한다.
    /// </summary>
    public abstract class SkillBase : MonoBehaviour
    {
        #region Serialized Fields
        [Header("Settings")]
        [Tooltip("SkillRunner 에서 이 스킬을 찾을 때 쓰는 이름")]
        [SerializeField] private string _skillName;
        #endregion

        #region Private Fields
        private ISkillCondition[] _conditions;
        private ISkillCost[] _costs;
        private Func<string, float> _getAttribute;
        private Func<string, float, bool> _requestPay;
        #endregion
        
        #region Properties
        public string SkillName => _skillName;
        #endregion

        #region Unity Lifecycle
        protected virtual void Awake()
        {
            _conditions = GetComponents<ISkillCondition>();
            _costs = GetComponents<ISkillCost>();
            
            if (_costs == null) return;
            foreach (ISkillCost cost in _costs)
            {
                cost.SetGetAttribute(_getAttribute);
                cost.SetRequestPay(_requestPay);
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
            // Awake 이전이면 저장만 하고, Awake 에서 코스트에 전달
            if (_costs == null) return;
            foreach (ISkillCost cost in _costs)
            {
                cost.SetGetAttribute(_getAttribute);
            }
        }

        /// <summary>
        /// 코스트 지불을 요청하기 위한 델리게이트 주입
        /// </summary>
        public void SetRequestPay(Func<string, float, bool> requestPay)
        {
            if (requestPay == null) return;
            _requestPay = requestPay;
            // Awake 이전이면 저장만 하고, Awake 에서 코스트에 전달
            if (_costs == null) return;
            foreach (ISkillCost cost in _costs)
            {
                cost.SetRequestPay(_requestPay);
            }
        }
        
        /// <summary>
        /// 발동에 필요한 준비가 되어 있고, 모든 조건을 만족하며, 모든 코스트를 지불할 수 있는지 확인한다.
        /// </summary>
        /// /// <returns>사용 가능하다면 true</returns>
        public bool CanExecute()
        {
            foreach (var condition in _conditions)
            {
                if (!condition.IsCanExecute()) return false;
            }

            foreach (var cost in _costs)
            {
                if (!cost.CanPay()) return false;
            }

            return true;
        }

        /// <summary>
        /// 사용 가능할 때만 코스트를 지불하고 스킬을 발동한다.
        /// </summary>
        /// <returns>실제로 발동했으면 true</returns>
        public bool TryExecute()
        {
            if (!CanExecute()) return false;

            foreach (var cost in _costs)
            {
                // 지불 실패 시 발동하지 않는다. 앞서 지불된 코스트는 되돌리지 않는다.
                if (!cost.Pay()) return false;
            }

            Execute();
            return true;
        }
        #endregion
        
        #region Protected Methods
        /// <summary>
        /// 스킬의 실제 동작. CanUse 가 true 일 때만 호출된다.
        /// </summary>
        protected abstract void Execute();
        #endregion
    }
}
