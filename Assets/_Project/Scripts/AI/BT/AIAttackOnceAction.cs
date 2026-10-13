using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;

namespace AI.BT
{
    /// <summary>
    /// 사거리 안의 대상에게 제자리에서 공격을 한 번 요청하고 곧바로 성공으로 끝난다.
    /// 공격 뒤의 멈춤·반복은 노드가 아니라 그래프(Wait 등)가 맡는다.
    /// 실제 발동 여부는 스킬의 조건·코스트가 정하므로 헛손질이어도 성공으로 끝난다.
    /// </summary>
    [Serializable, GeneratePropertyBag]
    [NodeDescription(
        name: "공격 1회",
        description: "사거리 안의 대상을 바라보며 공격을 한 번 요청하고 끝난다. 사거리 밖이면 실패한다.",
        story: "[Range] 안의 대상을 한 번 공격한다",
        category: "Action/적 AI",
        id: "7c4e1b9a3d5f4a28b6e0f2d91a8c3b57")]
    public partial class AIAttackOnceAction : AIActionBase
    {
        #region Serialized Fields
        [SerializeReference] public BlackboardVariable<float> Range;
        #endregion

        #region Node Lifecycle
        /// <inheritdoc />
        protected override Status OnStart()
        {
            if (!TryResolveController()) return Status.Failure;
            if (Controller.DistanceToTarget > Range.Value) return Status.Failure;

            Controller.StopMove();
            Controller.Attack();

            return Status.Success;
        }
        #endregion
    }
}
