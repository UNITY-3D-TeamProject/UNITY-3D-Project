using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;

namespace AI.BT
{
    /// <summary>
    /// 보이는 대상이 지정한 거리 안에 있는지 묻는다. 거리는 블랙보드 변수로 받으므로
    /// 공격 사거리, 돌진 거리 등 거리 기준 가지의 진입 조건으로 두루 쓴다.
    /// </summary>
    [Serializable, GeneratePropertyBag]
    [Condition(
        name: "대상이 거리 안에 있다",
        description: "보이는 대상과의 수평 거리가 지정한 거리 이하이면 참이다.",
        story: "대상이 [Range] 안에 있다",
        category: "Conditions/적 AI",
        id: "880a837d9927465390ab590d33a51154")]
    public partial class AIIsTargetWithinRangeCondition : AIConditionBase
    {
        #region Serialized Fields
        [SerializeReference] public BlackboardVariable<float> Range;
        #endregion

        #region Public Methods
        /// <inheritdoc />
        public override bool IsTrue()
        {
            if (!TryResolveController()) return false;

            return Controller.DistanceToTarget <= Range.Value;
        }
        #endregion
    }
}
