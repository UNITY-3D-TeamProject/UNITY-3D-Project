using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;

namespace AI.BT
{
    /// <summary>
    /// 스폰 지점 반경 안의 무작위 지점을 목적지로 잡고, 도착할 때까지 Running 을 유지한다.
    /// 벽 등에 막혀 정체되면(Controller.IsStuck) 무한 Running 에 빠지지 않도록 실패로 종료한다.
    /// 반복과 도착 후 대기는 이 노드가 아니라 그래프의 Repeat / Wait 노드가 담당한다.
    /// </summary>
    [Serializable, GeneratePropertyBag]
    [NodeDescription(
        name: "순찰",
        description: "스폰 지점 반경 안의 무작위 지점까지 이동한다. 도착하면 성공한다.",
        story: "스폰 지점 주변을 순찰한다",
        category: "Action/적 AI",
        id: "56e07f4ca751485d9cbdb93ffaefebb4")]
    public partial class AIPatrolAction : AIActionBase
    {
        #region Node Lifecycle
        /// <inheritdoc />
        protected override Status OnStart()
        {
            if (!TryResolveController()) return Status.Failure;

            if (!Controller.MoveToRandomPatrolPoint())
            {
                // LogFailure($"[{this.GameObject.name}] 순찰 지점을 NavMesh 위에서 찾지 못했다.");
                return Status.Failure;
            }

            return Status.Running;
        }

        /// <inheritdoc />
        protected override Status OnUpdate()
        {
            if (Controller.HasArrived) return Status.Success;

            if (Controller.IsStuck)
            {
                // LogFailure($"[{this.GameObject.name}] 순찰 중 정체되어 실패 처리한다.");
                return Status.Failure;
            }

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
