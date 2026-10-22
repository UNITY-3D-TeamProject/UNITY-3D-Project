using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;

namespace AI.BT
{
    /// <summary>
    /// 지정한 시간 동안 시야 거리를 줄인다. 교전을 끝내고 순찰로 돌아갈 때, 근처에 남은 대상에게 바로 다시 붙지 않게 쓴다.
    /// 제한은 Sensor 가 시간으로 관리하므로 이 노드는 걸어 두기만 하고 바로 성공한다.
    /// </summary>
    [Serializable, GeneratePropertyBag]
    [NodeDescription(
        name: "시야 거리 제한",
        description: "지정한 시간 동안 시야 거리를 줄인다. 바로 성공한다.",
        story: "[Seconds] 초 동안 시야 거리를 [Range] m 이하로 줄인다",
        category: "Action/적 AI",
        id: "a5887bcba8624f3496a5b9f01cb08111")]
    public partial class AILimitSightRangeAction : AIActionBase
    {
        #region Serialized Fields
        [SerializeReference] public BlackboardVariable<float> Range = new BlackboardVariable<float>(3.0f);
        [SerializeReference] public BlackboardVariable<float> Seconds = new BlackboardVariable<float>(3.0f);
        #endregion

        #region Node Lifecycle
        /// <inheritdoc />
        protected override Status OnStart()
        {
            if (!TryResolveController()) return Status.Failure;

            Controller.LimitSightRange(Range.Value, Seconds.Value);
            return Status.Success;
        }
        #endregion
    }
}
