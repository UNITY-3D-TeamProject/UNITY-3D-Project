using UnityEngine;
using AI.Boss.Logic;

namespace AI.Boss.BossA
{
    /// <summary>
    /// A6 박수치기. 한 줄의 양 끝 칸을 양손으로 내려친 뒤(1타) 간격을 두고 두 손을 가운데 칸에 모아 친다(2타).
    /// 칸은 무너뜨리지 않고 손도 머물지 않으며 어깨 포격도 없다. 최종 단계 전용이다.
    /// 예고는 맞는 칸에만 띄운다. 1타는 양 끝 칸, 1타 직후 가운데 칸으로 바꿔 2타 간격 동안 채운다.
    /// </summary>
    public class BossAClap : BossAPatternBase
    {
        #region Serialized Fields
        [Tooltip("임시값. 스펙에 정해진 시간이 없어 A2·A4 와 같게 둔다")]
        [SerializeField, Min(0.0f)] private float _telegraphSeconds = 1.5f;
        [Tooltip("1타에서 2타까지의 간격(초)")]
        [SerializeField, Min(0.0f)] private float _strikeInterval = 1.0f;
        #endregion

        #region Properties
        protected override float TelegraphSeconds => _telegraphSeconds;
        protected override float StaySeconds => 0.0f;
        protected override int StrikeCount => 2;
        protected override float NextStrikeDelay => _strikeInterval;
        #endregion

        #region Protected Methods
        protected override int[] GetTargetTiles()
        {
            // 1타는 줄의 양 끝 칸만 맞는다
            int[] tiles = Controller.CurrentChoice.Tiles;
            return new[] { tiles[0], tiles[ArenaGrid.SIZE - 1] };
        }

        protected override void RetargetHands()
        {
            // 줄의 가운데 칸에 두 손을 모은다
            int middle = Controller.CurrentChoice.Tiles[ArenaGrid.SIZE / 2];
            AddHand(true, middle);
            AddHand(false, middle);

            // 2타는 1타 충돌에서 간격만큼 뒤에 닿으므로 그동안 가운데 칸 예고를 채운다
            ReplaceTelegraph(new[] { middle }, NextStrikeDelay);
        }

        protected override void OnImpact()
        {
            // 1타는 양 끝 칸, 2타는 가운데 칸. 칸은 무너뜨리지 않는다
            int[] tiles = Controller.CurrentChoice.Tiles;
            bool isHit = (StrikeIndex == 0)
                ? (IsPlayerOnTile(tiles[0]) || IsPlayerOnTile(tiles[ArenaGrid.SIZE - 1]))
                : IsPlayerOnTile(tiles[ArenaGrid.SIZE / 2]);

            if (isHit) ApplyHitToPlayer();
        }
        #endregion
    }
}
