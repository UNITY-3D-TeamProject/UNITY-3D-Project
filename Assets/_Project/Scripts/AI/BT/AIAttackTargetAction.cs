using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;

namespace AI.BT
{
    /// <summary>
    /// 사거리 안의 대상을 제자리에서 공격한다. 사거리는 블랙보드 변수로 받는다.
    /// 매 틱 공격을 요청하지만 실제 발동 간격은 스킬의 코스트(쿨타임)가 정한다.
    /// 대상이 사거리를 벗어나거나 시야에서 사라지면 성공으로 끝나 그래프가 다음 가지를 다시 고른다.
    /// ExitRange 를 Range 보다 크게 두면 시작은 Range, 끝은 ExitRange 로 판정해
    /// 경계 근처에서 공격과 추격이 번갈아 바뀌며 멈칫거리지 않는다. Range 이하면 Range 로 끝낸다.
    /// </summary>
    [Serializable, GeneratePropertyBag]
    [NodeDescription(
        name: "공격",
        description: "사거리 안의 대상을 바라보며 공격한다. 대상이 사거리를 벗어나면 끝난다.",
        story: "[Range] 안의 대상을 공격한다",
        category: "Action/적 AI",
        id: "2b0eada8ad404084960e1b7082a3c916")]
    public partial class AIAttackTargetAction : AIActionBase
    {
        #region Serialized Fields
        [SerializeReference] public BlackboardVariable<float> Range;
        [SerializeReference] public BlackboardVariable<float> ExitRange = new BlackboardVariable<float>(0.0f);
        #endregion

        #region Node Lifecycle
        /// <inheritdoc />
        protected override Status OnStart()
        {
            if (!TryResolveController()) return Status.Failure;
            if (Controller.DistanceToTarget > Range.Value) return Status.Failure;

            Controller.StopMove();
            Controller.Attack();

            return Status.Running;
        }

        /// <inheritdoc />
        protected override Status OnUpdate()
        {
            // 대상을 잃으면 DistanceToTarget 이 무한대라 여기서 함께 끝난다
            float exitRange = (ExitRange != null) ? Mathf.Max(Range.Value, ExitRange.Value) : Range.Value;
            if (Controller.DistanceToTarget > exitRange) return Status.Success;

            Controller.Attack();

            return Status.Running;
        }
        #endregion
    }
}
