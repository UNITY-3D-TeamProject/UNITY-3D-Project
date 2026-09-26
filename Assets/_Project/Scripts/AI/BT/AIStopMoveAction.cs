using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;

namespace AI.BT
{
    /// <summary>
    /// 이동을 멈추고 목적지를 비운다. 이동 요청은 래치이므로 대기에 들어가기 직전에 한 번 호출한다.
    /// </summary>
    [Serializable, GeneratePropertyBag]
    [NodeDescription(
        name: "정지",
        description: "이동을 멈추고 목적지를 비운다.",
        story: "이동을 멈춘다",
        category: "Action/적 AI",
        id: "ccd1f6461cc343e78e7fcb312b0fe7d2")]
    public partial class AIStopMoveAction : AIActionBase
    {
        #region Node Lifecycle
        /// <inheritdoc />
        protected override Status OnStart()
        {
            if (!TryResolveController()) return Status.Failure;

            Controller.StopMove();
            return Status.Success;
        }
        #endregion
    }
}
