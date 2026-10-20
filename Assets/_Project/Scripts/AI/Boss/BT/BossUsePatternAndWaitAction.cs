using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;

namespace AI.Boss.BT
{
    /// <summary>
    /// 지정한 이름의 A 패턴(그로기 손바닥 등)을 시작하고 끝날 때까지 기다린다.
    /// 노드가 중간에 중단되면 진행 중인 패턴을 취소한다. 유지형 패턴은 이 취소로 끝난다.
    /// </summary>
    [Serializable, GeneratePropertyBag]
    [NodeDescription(
        name: "보스 패턴 사용 후 대기",
        description: "지정한 이름의 보스 A 패턴을 시작하고 끝날 때까지 기다린다.",
        story: "보스 [SkillName] 패턴을 사용하고 끝날 때까지 기다린다",
        category: "Action/보스",
        id: "3d7a9e2b5f1c4086b4a2c8e0d9f65173")]
    public partial class BossUsePatternAndWaitAction : BossActionBase
    {
        #region Serialized Fields
        [SerializeReference] public BlackboardVariable<string> SkillName;
        #endregion

        #region Node Lifecycle
        /// <inheritdoc />
        protected override Status OnStart()
        {
            if (!TryResolveController()) return Status.Failure;
            if (!Controller.TryStartPattern(SkillName.Value)) return Status.Failure;

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
            if (Controller && Controller.IsPatternRunning) Controller.CancelPattern();
        }
        #endregion
    }
}
