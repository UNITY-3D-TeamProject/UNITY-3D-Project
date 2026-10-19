using System;
using Unity.Behavior;
using Unity.Properties;
using AI.BT;
using AI.Boss.BossB;

namespace AI.Boss.BT
{
    /// <summary>
    /// B 발밑 칸이 예고 중(또는 구멍)인지 묻는다. 칸 상태는 아레나 질의 창구로 읽는다.
    /// </summary>
    [Serializable, GeneratePropertyBag]
    [Condition(
        name: "B 발밑 칸이 예고 중이다",
        description: "보스 B 가 서 있는 칸이 예고 중이거나 구멍이면 참이다.",
        story: "보스 B 발밑 칸이 예고 중이다",
        category: "Conditions/보스",
        id: "35754f9858fa4cb4af66678fb4903eb5")]
    public partial class BossBIsOnTelegraphedTileCondition : AIConditionBase
    {
        #region Private Fields
        private BossBController _boss;
        #endregion

        #region Public Methods
        /// <inheritdoc />
        public override bool IsTrue()
        {
            if (!TryResolveController()) return false;

            if (!_boss) _boss = this.GameObject.GetComponent<BossBController>();
            if (!_boss || (_boss.Arena == null)) return false;

            if (!_boss.Arena.TryGetTile(Controller.Position, out int tile)) return false;

            return _boss.Arena.IsHoleOrTelegraphed(tile);
        }
        #endregion
    }
}
