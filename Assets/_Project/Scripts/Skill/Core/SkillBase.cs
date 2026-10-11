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
        private Action<SkillBase> _notifyExecuted;
        #endregion
        
        #region Properties
        public string SkillName => _skillName;

        /// <summary>스킬의 쿨타임(초). ICooldownCost 코스트 중 가장 긴 값이며, 없으면 0.</summary>
        public float Cooldown
        {
            get
            {
                float cooldown = 0.0f;
                if (_costs == null) return cooldown;

                foreach (ISkillCost cost in _costs)
                {
                    if (cost is ICooldownCost cooldownCost) cooldown = Mathf.Max(cooldown, cooldownCost.Cooldown);
                }
                return cooldown;
            }
        }

        /// <summary>
        /// true 면 TryExecute 에서 코스트를 지불한다. false 면 TryExecute 는 판정만 하고, 스킬이 원하는 시점에 PayCosts 로 지불한다.
        /// </summary>
        protected virtual bool IsPayOnExecute => true;
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
        /// 스킬 발동을 알리기 위한 델리게이트 주입
        /// </summary>
        public void SetNotifyExecuted(Action<SkillBase> notifyExecuted)
        {
            if (notifyExecuted == null) return;
            _notifyExecuted = notifyExecuted;
        }

        /// <summary>
        /// 발동에 필요한 준비가 되어 있고, 모든 조건을 만족하며, 모든 코스트를 지불할 수 있는지 확인한다.
        /// </summary>
        /// /// <returns>사용 가능하다면 true</returns>
        public bool CanExecute()
        {
            // 컴포넌트가 비활성화된 스킬은 사용할 수 없다
            if (!isActiveAndEnabled) return false;

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
        /// 사용 가능할 때만 코스트를 지불하고 스킬을 발동한다. IsPayOnExecute 가 false 면 지불은 스킬에 맡긴다.
        /// </summary>
        /// <returns>실제로 발동했으면 true</returns>
        public bool TryExecute()
        {
            if (!CanExecute()) return false;

            // 지불 실패 시 발동하지 않는다
            if (IsPayOnExecute && !PayCosts()) return false;

            Execute();
            return true;
        }

        /// <summary>
        /// 진행 중인 스킬 동작을 중지한다. 지속형 스킬만 오버라이드하며 기본 동작은 없다.
        /// </summary>
        public virtual void Stop()
        {
        }

        /// <summary>
        /// 스킬 내부 상태를 초기화한다. 초기화할 상태가 있는 스킬만 오버라이드하며 기본 동작은 없다.
        /// </summary>
        public virtual void ResetState()
        {
        }
        #endregion
        
        #region Protected Methods
        /// <summary>
        /// 스킬의 실제 동작. CanUse 가 true 일 때만 호출된다.
        /// 기본 동작은 스킬 발동을 외부에 알리는 것이며, 자식 클래스는 필요하면 base.Execute() 로 호출한다.
        /// </summary>
        protected virtual void Execute()
        {
            _notifyExecuted?.Invoke(this);
        }

        /// <summary>
        /// 모든 코스트를 지불한다. 지불에 실패하면 그 자리에서 멈추며, 앞서 지불된 코스트는 되돌리지 않는다.
        /// </summary>
        /// <returns>모두 지불했으면 true</returns>
        protected bool PayCosts()
        {
            foreach (var cost in _costs)
            {
                if (!cost.Pay()) return false;
            }

            return true;
        }

        /// <summary>
        /// IScalableCost 코스트에 배수를 적용해 모든 코스트를 지불한다. 지불 후 배수를 1로 되돌린다.
        /// </summary>
        /// <param name="scale">이번 지불에 적용할 배수</param>
        /// <returns>모두 지불했으면 true</returns>
        protected bool PayCosts(float scale)
        {
            SetCostScale(scale);
            bool isPaid = PayCosts();
            SetCostScale(1.0f);

            return isPaid;
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// IScalableCost 코스트에 배수를 설정한다.
        /// </summary>
        private void SetCostScale(float scale)
        {
            foreach (var cost in _costs)
            {
                if (cost is IScalableCost scalableCost) scalableCost.SetScale(scale);
            }
        }
        #endregion
    }
}
