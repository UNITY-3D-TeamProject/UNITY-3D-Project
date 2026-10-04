using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using UnityEngine.AI;

namespace AI.BT
{
    /// <summary>
    /// 보이는 대상까지 사선(쏠 수 있는 직선)이 트여 있는지 묻는다. 원거리 공격 가지에 쓴다.
    /// Sensor 는 대상을 놓친 뒤 유예 시간 동안에도 대상을 유지하므로, 이 조건이 없으면 엄폐물 뒤의 대상에게 벽에 대고 쏜다.
    /// Behavior 패키지에는 조건 반전이 없으므로 기대값(IsClear)을 받는다.
    /// Guard 진입 조건에는 true 를, 사격 중 중단용 Fail 노드에는 false 를 준다.
    /// 판정은 NavMesh.Raycast 근사다. 벽이 NavMesh 에서 뚫려 있어야 한다.
    /// </summary>
    [Serializable, GeneratePropertyBag]
    [Condition(
        name: "사선이 트여 있다",
        description: "보이는 대상까지 NavMesh 상 막힘이 없는지가 기대값과 같으면 참이다. 보이는 대상이 없으면 거짓이다.",
        story: "대상까지 사선이 트여 있음이 [IsClear] 이다",
        category: "Conditions/적 AI",
        id: "589ae0d1e0084329a83f9248b9ab522e")]
    public partial class AIHasLineOfFireCondition : AIConditionBase
    {
        #region Serialized Fields
        [SerializeReference] public BlackboardVariable<bool> IsClear = new BlackboardVariable<bool>(true);
        #endregion

        #region Public Methods
        /// <inheritdoc />
        public override bool IsTrue()
        {
            if (!TryResolveController()) return false;
            if (!Controller.HasVisibleTarget) return false;

            bool isClear = !NavMesh.Raycast(Controller.Position, Controller.VisibleTarget.position, out _, NavMesh.AllAreas);
            return isClear == IsClear.Value;
        }
        #endregion
    }
}
