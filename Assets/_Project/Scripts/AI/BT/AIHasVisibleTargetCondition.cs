using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;

namespace AI.BT
{
    /// <summary>
    /// 현재 시야에 대상이 보이는지 묻는다. Conditional Guard 에 물려 추격 가지의 진입 조건으로 쓴다.
    /// </summary>
    [Serializable, GeneratePropertyBag]
    [Condition(
        name: "대상이 보인다",
        description: "현재 시야에 대상이 보이면 참이다.",
        story: "시야에 대상이 보인다",
        category: "Conditions/적 AI",
        id: "eff17b0e493e46d8a9a5ff6b3e7faf81")]
    public partial class AIHasVisibleTargetCondition : AIConditionBase
    {
        #region Public Methods
        /// <inheritdoc />
        public override bool IsTrue()
        {
            if (!TryResolveController()) return false;

            return Controller.HasVisibleTarget;
        }
        #endregion
    }
}
