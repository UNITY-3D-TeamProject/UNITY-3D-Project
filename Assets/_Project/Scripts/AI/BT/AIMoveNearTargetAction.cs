using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;

namespace AI.BT
{
    /// <summary>
    /// 지정한 대상에게 가장 가까운 순찰 지점까지 이동하고, 도착할 때까지 Running 을 유지한다.
    /// 대상의 정확한 위치가 아니라 근처 순찰 지점까지만 가므로, 그 뒤 발견 여부는 시야 감지에 맡긴다.
    /// 벽 등에 막혀 정체되면(Controller.IsStuck) 무한 Running 에 빠지지 않도록 실패로 종료한다.
    /// 반복 간격은 이 노드가 아니라 그래프의 Cooldown 노드가 담당한다.
    /// </summary>
    [Serializable, GeneratePropertyBag]
    [NodeDescription(
        name: "대상 근처 순찰 지점으로 이동",
        description: "대상에게 가장 가까운 순찰 지점까지 이동한다. 도착하면 성공한다.",
        story: "[Target] 근처 순찰 지점으로 이동한다",
        category: "Action/적 AI",
        id: "d2e56405529c489b98f055b4033d8a75")]
    public partial class AIMoveNearTargetAction : AIActionBase
    {
        #region Serialized Fields
        [SerializeReference] public BlackboardVariable<GameObject> Target;
        #endregion

        #region Node Lifecycle
        /// <inheritdoc />
        protected override Status OnStart()
        {
            if (!TryResolveController()) return Status.Failure;
            if (!Target.Value) return Status.Failure;

            if (!Controller.MoveToPatrolPointNear(Target.Value.transform.position)) return Status.Failure;

            return Status.Running;
        }

        /// <inheritdoc />
        protected override Status OnUpdate()
        {
            if (Controller.HasArrived) return Status.Success;
            if (Controller.IsStuck) return Status.Failure;

            return Status.Running;
        }

        /// <inheritdoc />
        protected override void OnEnd()
        {
            if (Controller) Controller.StopMove();
        }
        #endregion
    }
}
