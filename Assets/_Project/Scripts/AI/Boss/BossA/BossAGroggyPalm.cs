using AI.Boss.Logic;

namespace AI.Boss.BossA
{
    /// <summary>
    /// 그로기 손바닥. 양손을 가운데 col 의 양 끝 칸(row 0·마지막) 위에 내려 놓고 Stop 될 때까지 유지한다. 예고·붕괴·피해는 없다.
    /// 손이 머무는 동안 BossHand 의 NavMesh 장애물이 켜져 길을 막는다.
    /// </summary>
    public class BossAGroggyPalm : BossAPatternBase
    {
        #region Properties
        protected override float TelegraphSeconds => 0.0f;
        protected override float StaySeconds => float.PositiveInfinity;
        #endregion

        #region Protected Methods
        protected override void BuildTargets()
        {
            int col = ArenaGrid.SIZE / 2;

            AddHand(true, ArenaGrid.GetIndex(0, col));
            AddHand(false, ArenaGrid.GetIndex(ArenaGrid.SIZE - 1, col));
        }

        protected override int[] GetTargetTiles()
        {
            return null;
        }

        protected override void OnImpact()
        {
        }
        #endregion
    }
}
