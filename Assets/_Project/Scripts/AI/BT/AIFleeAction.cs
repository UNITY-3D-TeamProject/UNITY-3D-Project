using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;

namespace AI.BT
{
    /// <summary>
    /// 보이는 대상에게서 순찰 지점(웨이포인트)을 따라 달아난다. 대상을 놓치면 성공해서 그래프가 다음 가지(순찰)로 넘어간다.
    /// 도착했는데 아직 대상이 보이거나 정체되면 다시 고른다. 이동 중 주기적으로 다시 고르지는 않는다.
    /// 갈 곳이 없으면 멈추고 일정 시간 뒤 다시 찾는다. 이때 Failure 를 내지 않는 이유는,
    /// Failure 를 내면 그래프가 순찰 가지로 넘어가 대상 쪽으로 걸어올 수 있기 때문이다.
    /// 지점 계산은 FleePointFinder 가 한다.
    /// </summary>
    [Serializable, GeneratePropertyBag]
    [NodeDescription(
        name: "도망",
        description: "보이는 대상에게서 웨이포인트를 따라 달아난다. 대상을 놓치면 성공한다.",
        story: "[MaxFleeDistance] 이내 웨이포인트로 대상에게서 [AvoidRadius] 이상 떨어져 달아난다",
        category: "Action/적 AI",
        id: "4681f58e36804aaca8eb1967d87e355f")]
    public partial class AIFleeAction : AIActionBase
    {
        #region Constants
        /// <summary>갈 곳이 없을 때 다시 찾기까지 기다리는 시간(초).</summary>
        private const float RETRY_INTERVAL = 1.0f;
        #endregion

        #region Serialized Fields
        [SerializeReference] public BlackboardVariable<float> MaxFleeDistance = new BlackboardVariable<float>(25.0f);
        [SerializeReference] public BlackboardVariable<float> AvoidRadius = new BlackboardVariable<float>(4.0f);
        #endregion

        #region Private Fields
        // StopMove 뒤에는 HasArrived 가 true 라서 "도착"과 "기다림"을 구분하려고 따로 기억한다
        [CreateProperty] private bool _isWaiting;
        [CreateProperty] private float _nextTryTime;
        #endregion

        #region Node Lifecycle
        /// <inheritdoc />
        protected override Status OnStart()
        {
            _isWaiting = false;
            _nextTryTime = 0.0f;

            if (!TryResolveController()) return Status.Failure;
            if (!Controller.HasVisibleTarget) return Status.Failure;

            TryChooseFleePoint();
            return Status.Running;
        }

        /// <inheritdoc />
        protected override Status OnUpdate()
        {
            if (!Controller.HasVisibleTarget) return Status.Success;

            if (_isWaiting)
            {
                if (Time.time >= _nextTryTime) TryChooseFleePoint();
            }
            else if (Controller.HasArrived || Controller.IsStuck)
            {
                TryChooseFleePoint();
            }

            return Status.Running;
        }

        /// <inheritdoc />
        protected override void OnEnd()
        {
            if (Controller) Controller.StopMove();
        }
        #endregion

        #region Private Methods
        private void TryChooseFleePoint()
        {
            bool hasPoint = Controller.MoveAwayFrom(
                Controller.VisibleTarget.position,
                MaxFleeDistance.Value,
                AvoidRadius.Value);

            if (hasPoint)
            {
                _isWaiting = false;
                return;
            }

            Controller.StopMove();
            _isWaiting = true;
            _nextTryTime = Time.time + RETRY_INTERVAL;
        }
        #endregion
    }
}
