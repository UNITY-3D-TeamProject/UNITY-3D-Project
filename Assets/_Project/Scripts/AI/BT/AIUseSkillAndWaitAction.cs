using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;

namespace AI.BT
{
    /// <summary>
    /// 지정한 이름의 스킬을 한 번 발동하고, 스킬이 몸을 붙잡고(Hold) 있는 동안 기다린다.
    /// 스킬이 ReleaseHold 로 붙잡힘을 풀면 성공으로 끝난다. 발동 직후 붙잡히지 않았다면 실패한다.
    /// 노드가 중간에 중단되면(Abort 등) 스킬을 멈춘다. 바람처럼 Stop 이 곧 취소인 스킬용이다.
    /// </summary>
    [Serializable, GeneratePropertyBag]
    [NodeDescription(
        name: "스킬 사용 후 대기",
        description: "대상이 사거리 안이면 스킬을 한 번 발동하고, 스킬이 끝나 몸이 풀릴 때까지 기다린다.",
        story: "[SkillName] 스킬을 [Range] 안에서 사용하고 끝날 때까지 기다린다",
        category: "Action/적 AI",
        id: "5f3a9c1e7b2d4a68b0e4d1c79a36f852")]
    public partial class AIUseSkillAndWaitAction : AIActionBase
    {
        #region Serialized Fields
        [SerializeReference] public BlackboardVariable<string> SkillName;
        [SerializeReference] public BlackboardVariable<float> Range;
        #endregion

        #region Node Lifecycle
        /// <inheritdoc />
        protected override Status OnStart()
        {
            if (!TryResolveController()) return Status.Failure;
            if (Controller.DistanceToTarget > Range.Value) return Status.Failure;

            Controller.ExecuteSkill(SkillName.Value);

            // 스킬이 시작되지 못했다면(대상 없음 등) 붙잡히지 않으므로 기다릴 이유가 없다
            if (!Controller.IsHolding) return Status.Failure;

            return Status.Running;
        }

        /// <inheritdoc />
        protected override Status OnUpdate()
        {
            if (!Controller.IsHolding) return Status.Success;

            return Status.Running;
        }

        /// <inheritdoc />
        protected override void OnEnd()
        {
            // 아직 붙잡혀 있다면 끝까지 가지 못하고 중단된 것이다
            if (Controller && Controller.IsHolding) Controller.StopSkill(SkillName.Value);
        }
        #endregion
    }
}
