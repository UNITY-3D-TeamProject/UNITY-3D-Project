using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;

namespace AI.BT
{
    /// <summary>
    /// 추적 중인 대상과 마지막 목격 위치를 잊는다. 교전을 끝내고 순찰로 돌아갈 때 쓴다.
    /// 다음 감지부터는 대상이 없는 상태처럼 시야각 안에서만 다시 찾는다.
    /// </summary>
    [Serializable, GeneratePropertyBag]
    [NodeDescription(
        name: "대상 잊기",
        description: "추적 중인 대상과 마지막 목격 위치를 지운다.",
        story: "대상을 잊는다",
        category: "Action/적 AI",
        id: "b3f9a27c5e8d4c1fa06d29e74c81b5d3")]
    public partial class AIForgetTargetAction : AIActionBase
    {
        #region Node Lifecycle
        /// <inheritdoc />
        protected override Status OnStart()
        {
            if (!TryResolveController()) return Status.Failure;

            Controller.ForgetTarget();
            return Status.Success;
        }
        #endregion
    }
}
