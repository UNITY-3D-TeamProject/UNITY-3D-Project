using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;

namespace AI.BT
{
    /// <summary>
    /// 대상을 마지막으로 본 위치가 기록되어 있는지 묻는다. 수색 가지의 진입 조건으로 쓴다.
    /// </summary>
    [Serializable, GeneratePropertyBag]
    [Condition(
        name: "목격 지점을 기억한다",
        description: "대상을 마지막으로 본 위치가 기록되어 있으면 참이다.",
        story: "마지막 목격 지점이 기록되어 있다",
        category: "Conditions/적 AI",
        id: "1932f979d02a42b3a45910553d927773")]
    public partial class AIHasLastKnownPositionCondition : AIConditionBase
    {
        #region Public Methods
        /// <inheritdoc />
        public override bool IsTrue()
        {
            if (!TryResolveController()) return false;

            return Controller.HasLastKnownPosition;
        }
        #endregion
    }
}
