using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;

namespace AI.BT
{
    /// <summary>
    /// 대상을 마지막으로 본 뒤 지정한 시간이 지났는지 묻는다. 한 번도 보지 못했으면 생성 시점부터 잰다.
    /// 오래 마주치지 못했을 때 대상 쪽으로 다가가는 압박 가지 등의 진입 조건으로 쓴다.
    /// </summary>
    [Serializable, GeneratePropertyBag]
    [Condition(
        name: "대상을 오래 못 봤다",
        description: "대상을 마지막으로 본 뒤 지정한 시간(초) 이상 지났으면 참이다.",
        story: "대상을 [Seconds] 초 동안 못 봤다",
        category: "Conditions/적 AI",
        id: "fda2dbf4edd74aabb575d9e5f7d5b2de")]
    public partial class AITimeSinceTargetSeenCondition : AIConditionBase
    {
        #region Serialized Fields
        [SerializeReference] public BlackboardVariable<float> Seconds;
        #endregion

        #region Public Methods
        /// <inheritdoc />
        public override bool IsTrue()
        {
            if (!TryResolveController()) return false;

            return Controller.TimeSinceTargetSeen >= Seconds.Value;
        }
        #endregion
    }
}
