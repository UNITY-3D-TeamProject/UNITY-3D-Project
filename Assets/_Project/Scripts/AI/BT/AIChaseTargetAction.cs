using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;

namespace AI.BT
{
    /// <summary>
    /// 시야에 보이는 대상을 추격한다. 대상은 움직이므로 목적지를 주기적으로 갱신한다.
    /// 1차 범위에는 공격이 없어 스스로 성공하지 않는다. 시야를 잃으면 실패로 끝나고
    /// 그래프의 Try In Order 가 다음 가지(수색)로 넘어간다.
    /// </summary>
    [Serializable, GeneratePropertyBag]
    [NodeDescription(
        name: "추격",
        description: "시야에 보이는 대상을 쫓아간다. 시야를 잃으면 실패한다.",
        story: "보이는 대상을 추격한다",
        category: "Action/적 AI",
        id: "24265e128b264cd391a5dfc8ca2d76f7")]
    public partial class AIChaseTargetAction : AIActionBase
    {
        #region Constants
        /// <summary>목적지 갱신 주기(초). 매 프레임 경로를 다시 계산하는 낭비를 막는다.</summary>
        private const float REPATH_INTERVAL = 0.2f;
        #endregion

        #region Private Fields
        [CreateProperty] private float _repathTimer;
        #endregion

        #region Node Lifecycle
        /// <inheritdoc />
        protected override Status OnStart()
        {
            if (!TryResolveController()) return Status.Failure;
            if (!Controller.HasVisibleTarget) return Status.Failure;

            Controller.MoveTo(Controller.VisibleTarget.position);
            _repathTimer = REPATH_INTERVAL;

            return Status.Running;
        }

        /// <inheritdoc />
        protected override Status OnUpdate()
        {
            if (!Controller.HasVisibleTarget) return Status.Failure;

            _repathTimer -= Time.deltaTime;
            if (_repathTimer <= 0.0f)
            {
                _repathTimer = REPATH_INTERVAL;

                // 대상이 일시적으로 NavMesh 밖에 있으면 실패하지만, 기존 경로를 유지하며 계속 쫓는다
                Controller.MoveTo(Controller.VisibleTarget.position);
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
