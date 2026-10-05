using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;

namespace AI.BT
{
    /// <summary>
    /// 너무 가까이 온 대상에게서 벌어지도록 뒤나 옆으로 물러난다. 원거리 적의 거리 유지용.
    /// 이동 지점은 RetreatPointFinder 가 NavMesh 후보점에서 고른다. 이동 중에는 공격하지 않는다.
    /// 갈 곳이 없으면 실패하고, 매 틱 후보 계산을 반복하지 않도록 일정 시간 재시도를 막는다.
    /// 그동안 그래프의 Try In Order 는 다음 가지(공격)로 넘어가 제자리에서 쏜다.
    /// </summary>
    [Serializable, GeneratePropertyBag]
    [NodeDescription(
        name: "후퇴",
        description: "대상과 지정한 거리 이상 벌어지는 지점(뒤 또는 옆)으로 이동한다. 갈 곳이 없으면 실패한다.",
        story: "대상과 [Range] 이상 벌어지도록 [MoveDistance] 만큼 물러난다",
        category: "Action/적 AI",
        id: "8ac60e949d4947eeb627f779937bc289")]
    public partial class AIRetreatAction : AIActionBase
    {
        #region Constants
        /// <summary>후퇴에 실패한 뒤 다시 시도하기까지 기다리는 시간(초).</summary>
        private const float RETRY_INTERVAL = 1.0f;
        #endregion

        #region Serialized Fields
        [SerializeReference] public BlackboardVariable<float> Range;
        [SerializeReference] public BlackboardVariable<float> MoveDistance = new BlackboardVariable<float>(6.0f);
        #endregion

        #region Private Fields
        [CreateProperty] private float _nextTryTime;
        #endregion

        #region Node Lifecycle
        /// <inheritdoc />
        protected override Status OnStart()
        {
            if (!TryResolveController()) return Status.Failure;
            if (Time.time < _nextTryTime) return Status.Failure;
            if (!Controller.HasVisibleTarget) return Status.Failure;

            bool hasPoint = RetreatPointFinder.TryFind(
                Controller.Position,
                Controller.VisibleTarget.position,
                MoveDistance.Value,
                Range.Value,
                out Vector3 point);

            if (!hasPoint || !Controller.MoveTo(point))
            {
                _nextTryTime = Time.time + RETRY_INTERVAL;
                return Status.Failure;
            }

            return Status.Running;
        }

        /// <inheritdoc />
        protected override Status OnUpdate()
        {
            if (Controller.IsStuck)
            {
                _nextTryTime = Time.time + RETRY_INTERVAL;
                return Status.Failure;
            }

            if (Controller.HasArrived) return Status.Success;

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
