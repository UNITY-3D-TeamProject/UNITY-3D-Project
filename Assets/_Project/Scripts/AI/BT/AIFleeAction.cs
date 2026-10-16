using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;

namespace AI.BT
{
    /// <summary>
    /// 보이는 대상에게서 주변 NavMesh 위 지점으로 달아난다. 대상을 놓치면 성공해서 그래프가 다음 가지(순찰)로 넘어간다.
    /// 도착했는데 아직 대상이 보이거나 정체되면 다시 고른다. 이동 중에는 남은 경로가 대상 근처를 지나게 됐을 때만 다시 고른다.
    /// 갈 곳이 없으면 멈추고 일정 시간 뒤 다시 찾는다. 이때 Failure 를 내지 않는 이유는,
    /// Failure 를 내면 그래프가 순찰 가지로 넘어가 대상 쪽으로 걸어올 수 있기 때문이다.
    /// 지점 계산은 FleePointFinder 가 한다.
    /// </summary>
    [Serializable, GeneratePropertyBag]
    [NodeDescription(
        name: "도망",
        description: "보이는 대상에게서 주변 NavMesh 위 지점으로 달아난다. 대상을 놓치면 성공한다.",
        story: "[MaxFleeDistance] 이내 지점으로 대상에게서 [AvoidRadius] 이상 떨어져 달아난다",
        category: "Action/적 AI",
        id: "4681f58e36804aaca8eb1967d87e355f")]
    public partial class AIFleeAction : AIActionBase
    {
        #region Constants
        /// <summary>갈 곳이 없을 때 다시 찾기까지 기다리는 시간(초).</summary>
        private const float RETRY_INTERVAL = 1.0f;

        /// <summary>이동 중 남은 경로가 대상 근처를 지나는지 확인하는 주기(초).</summary>
        private const float PATH_CHECK_INTERVAL = 0.25f;
        #endregion

        #region Serialized Fields
        [SerializeReference] public BlackboardVariable<float> MaxFleeDistance = new BlackboardVariable<float>(25.0f);
        [SerializeReference] public BlackboardVariable<float> AvoidRadius = new BlackboardVariable<float>(4.0f);
        #endregion

        #region Private Fields
        // StopMove 뒤에는 HasArrived 가 true 라서 "도착"과 "기다림"을 구분하려고 따로 기억한다
        [CreateProperty] private bool _isWaiting;
        [CreateProperty] private float _nextTryTime;
        [CreateProperty] private float _nextPathCheckTime;
        #endregion

        #region Node Lifecycle
        /// <inheritdoc />
        protected override Status OnStart()
        {
            _isWaiting = false;
            _nextTryTime = 0.0f;
            _nextPathCheckTime = 0.0f;

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
            else if (Time.time >= _nextPathCheckTime)
            {
                // 고른 뒤 대상이 경로 쪽으로 움직이면 대상에게 달려들게 되므로, 우회할 곳을 다시 찾는다.
                // 우회할 곳이 없으면 TryChooseFleePoint 가 멈추고 기다린다
                _nextPathCheckTime = Time.time + PATH_CHECK_INTERVAL;
                if (Controller.IsPathNearThreat(Controller.VisibleTarget.position, AvoidRadius.Value)) TryChooseFleePoint();
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
