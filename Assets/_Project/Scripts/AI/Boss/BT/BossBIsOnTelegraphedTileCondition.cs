using System;
using Unity.Behavior;
using Unity.Properties;
using AI.BT;
using AI.Boss.BossB;

namespace AI.Boss.BT
{
    /// <summary>
    /// B 발밑 칸이 예고 중(또는 구멍)인지 묻는다. 칸 상태는 아레나 질의 창구로 읽는다.
    /// 도약으로 날아가는 동안은 지나는 칸을 발밑으로 보지 않는다. 참이 되면 공격 가지가 끊겨 도약이 공중에서 멈추기 때문이다.
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
            if (_boss.IsBodyInFlight) return false;

            if (!_boss.Arena.TryGetTile(Controller.Position, out int tile)) return false;

            return _boss.Arena.IsHoleOrTelegraphed(tile);
        }
        #endregion
    }
}
