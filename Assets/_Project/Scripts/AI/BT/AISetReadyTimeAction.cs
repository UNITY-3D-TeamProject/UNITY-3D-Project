using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;

namespace AI.BT
{
    /// <summary>
    /// 준비 시각을 지금부터 지정한 시간 뒤로 설정하고 즉시 성공한다. 스킬이 끝난 뒤 쿨타임을 시작할 때 쓴다.
    /// AIIsTimeReachedCondition 과 같은 변수를 링크한다.
    /// </summary>
    [Serializable, GeneratePropertyBag]
    [NodeDescription(
        name: "준비 시각 설정",
        description: "준비 시각을 현재 시각 + 지정한 시간(초)으로 설정한다.",
        story: "[ReadyTime] 을 지금부터 [Delay] 초 뒤로 설정한다",
        category: "Action/적 AI",
        id: "0e7b25d9a4c84f1e93a6b8d02f5c1a47")]
    public partial class AISetReadyTimeAction : AIActionBase
    {
        #region Serialized Fields
        [SerializeReference] public BlackboardVariable<float> ReadyTime;
        [SerializeReference] public BlackboardVariable<float> Delay;
        #endregion

        #region Node Lifecycle
        /// <inheritdoc />
        protected override Status OnStart()
        {
            ReadyTime.Value = Time.time + Delay.Value;

            return Status.Success;
        }
        #endregion
    }
}
