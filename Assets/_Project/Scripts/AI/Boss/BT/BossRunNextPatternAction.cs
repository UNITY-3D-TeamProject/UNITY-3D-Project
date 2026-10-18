using System;
using Unity.Behavior;
using Unity.Properties;
using AI.Boss.Logic;

namespace AI.Boss.BT
{
    /// <summary>
    /// 선택기가 고른 다음 A 패턴을 시작하고 끝날 때까지 기다린다. 쉼(Rest)이면 쉬는 시간 뒤에 성공한다.
    /// 노드가 중간에 중단되면 진행 중인 패턴을 취소한다.
    /// </summary>
    [Serializable, GeneratePropertyBag]
    [NodeDescription(
        name: "보스 다음 패턴",
        description: "보스 A 의 다음 패턴을 고르고 끝날 때까지 기다린다. 쉼이면 쉬는 시간만큼 기다린다.",
        story: "보스 다음 패턴을 실행하고 끝날 때까지 기다린다",
        category: "Action/보스",
        id: "8b2e6f4a1c9d4375a0d3e7f5b6c18294")]
    public partial class BossRunNextPatternAction : BossActionBase
    {
        #region Node Lifecycle
        /// <inheritdoc />
        protected override Status OnStart()
        {
            if (!TryResolveController()) return Status.Failure;

            EBossAPattern pattern = Controller.TryStartNextPattern();

            // 쉼이 아닌데 진행 중이 아니라면 스킬이 시작되지 못한 것이다
            if (pattern != EBossAPattern.Rest && !Controller.IsPatternRunning) return Status.Failure;

            return Status.Running;
        }

        /// <inheritdoc />
        protected override Status OnUpdate()
        {
            if (!Controller.IsPatternRunning) return Status.Success;

            return Status.Running;
        }

        /// <inheritdoc />
        protected override void OnEnd()
        {
            // 아직 진행 중이라면 끝까지 가지 못하고 중단된 것이다
            if (Controller && Controller.IsPatternRunning) Controller.CancelPattern();
        }
        #endregion
    }
}
