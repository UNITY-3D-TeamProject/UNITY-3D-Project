using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;

namespace AI.BT
{
    /// <summary>
    /// 대상을 마지막으로 본 지점까지 이동한다. 도착하면 기록을 지워 다음 틱에 순찰로 돌아가게 한다.
    /// 목격 지점이 NavMesh 밖이라 도달할 수 없거나, 이동 중 벽 등에 막혀 정체되면(Controller.IsStuck)
    /// 기록을 지우고 실패해, 같은 지점에 영구히 묶이지 않는다.
    /// </summary>
    [Serializable, GeneratePropertyBag]
    [NodeDescription(
        name: "수색",
        description: "대상을 마지막으로 본 지점까지 이동한다. 도착하면 기록을 지우고 성공한다.",
        story: "마지막 목격 지점을 수색한다",
        category: "Action/적 AI",
        id: "31748301db25446498e7d876890095cd")]
    public partial class AISearchLastKnownPositionAction : AIActionBase
    {
        #region Node Lifecycle
        /// <inheritdoc />
        protected override Status OnStart()
        {
            if (!TryResolveController()) return Status.Failure;
            if (!Controller.HasLastKnownPosition) return Status.Failure;

            if (!Controller.MoveTo(Controller.LastKnownPosition))
            {
                // 도달할 수 없는 지점이면 기록을 지워 다음 틱에 순찰로 복귀시킨다
                Controller.ClearLastKnownPosition();
                // LogFailure($"[{this.GameObject.name}] 마지막 목격 지점에 도달할 수 없어 수색을 포기한다.");
                return Status.Failure;
            }

            return Status.Running;
        }

        /// <inheritdoc />
        protected override Status OnUpdate()
        {
            if (Controller.HasArrived)
            {
                Controller.ClearLastKnownPosition();
                return Status.Success;
            }

            if (Controller.IsStuck)
            {
                Controller.ClearLastKnownPosition();
                // LogFailure($"[{this.GameObject.name}] 수색 중 정체되어 실패 처리한다.");
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
