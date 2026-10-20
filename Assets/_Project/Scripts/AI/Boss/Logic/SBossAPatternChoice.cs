namespace AI.Boss.Logic
{
    /// <summary>
    /// A 패턴 선택 결과. 표적 칸은 고른 순간에 고정된다.
    /// </summary>
    public readonly struct SBossAPatternChoice
    {
        public readonly EBossAPattern Pattern;

        /// <summary>무너뜨리거나 칠 칸(최대 5). Rest 면 빈 배열.</summary>
        public readonly int[] Tiles;

        /// <summary>왼손이 칠 칸. -1 이면 왼손을 쓰지 않는다.</summary>
        public readonly int HandTileLeft;

        /// <summary>오른손이 칠 칸. -1 이면 오른손을 쓰지 않는다.</summary>
        public readonly int HandTileRight;

        /// <summary>A2·A6 의 줄이 가로줄(row)인지. false 면 세로줄(col).</summary>
        public readonly bool IsRowLine;

        /// <summary>A2·A6 의 줄 인덱스. 줄 패턴이 아니면 -1.</summary>
        public readonly int LineIndex;

        public int TileCount => Tiles == null ? 0 : Tiles.Length;

        public SBossAPatternChoice(
            EBossAPattern pattern,
            int[] tiles,
            int handTileLeft,
            int handTileRight,
            bool isRowLine = false,
            int lineIndex = -1)
        {
            Pattern = pattern;
            Tiles = tiles;
            HandTileLeft = handTileLeft;
            HandTileRight = handTileRight;
            IsRowLine = isRowLine;
            LineIndex = lineIndex;
        }
    }
}
