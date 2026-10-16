using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;

namespace AI.BT
{
    /// <summary>
    /// 블랙보드에 저장한 준비 시각(Time.time 기준)이 지났는지 묻는다.
    /// AISetReadyTimeAction 과 같은 변수를 링크해 스킬 쿨타임 가드로 쓴다.
    /// </summary>
    [Serializable, GeneratePropertyBag]
    [Condition(
        name: "준비 시각이 지났다",
        description: "현재 시각이 지정한 준비 시각 이상이면 참이다.",
        story: "[ReadyTime] 이 지났다",
        category: "Conditions/적 AI",
        id: "c81d4e6a2f9b4375a1d0e8b53c7f2a94")]
    public partial class AIIsTimeReachedCondition : AIConditionBase
    {
        #region Serialized Fields
        [SerializeReference] public BlackboardVariable<float> ReadyTime;
        #endregion

        #region Public Methods
        /// <inheritdoc />
        public override bool IsTrue()
        {
            return Time.time >= ReadyTime.Value;
        }
        #endregion
    }
}
