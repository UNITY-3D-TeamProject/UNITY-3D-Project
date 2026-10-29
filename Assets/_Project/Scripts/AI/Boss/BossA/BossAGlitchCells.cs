using System;

namespace AI.Boss.BossA
{
    /// <summary>
    /// 보스 A 글리치를 넣을 칸을 고른다. 칸 번호는 행(아래→위) × 3 + 열(왼→오른)이고, 결과는 칸 번호 비트 마스크다.
    /// Unity 를 참조하지 않는다.
    /// </summary>
    public static class BossAGlitchCells
    {
        #region Constants
        public const int GRID_SIZE = 3;
        public const int CELL_COUNT = GRID_SIZE * GRID_SIZE;
        #endregion

        #region Public Methods
        /// <summary>
        /// 후보 칸 중 minCount~maxCount 개(개수도 무작위)를 겹치지 않게 고른다. 후보가 모자라면 후보 전부를 돌려준다.
        /// </summary>
        /// <param name="random">난수 생성기</param>
        /// <param name="candidateMask">고를 수 있는 칸 비트 마스크</param>
        /// <param name="minCount">최소 개수</param>
        /// <param name="maxCount">최대 개수 (포함)</param>
        /// <returns>고른 칸 비트 마스크</returns>
        public static int PickMask(Random random, int candidateMask, int minCount, int maxCount)
        {
            int[] candidates = new int[CELL_COUNT];
            int candidateCount = 0;
            for (int cell = 0; cell < CELL_COUNT; cell++)
            {
                if ((candidateMask & (1 << cell)) != 0) candidates[candidateCount++] = cell;
            }

            int count = random.Next(minCount, Math.Max(minCount, maxCount) + 1);
            count = Math.Min(count, candidateCount);

            // 앞에서부터 남은 후보 중 하나와 바꿔 가며 count 개를 뽑는다
            int mask = 0;
            for (int i = 0; i < count; i++)
            {
                int pick = i + random.Next(candidateCount - i);
                (candidates[i], candidates[pick]) = (candidates[pick], candidates[i]);
                mask |= 1 << candidates[i];
            }

            return mask;
        }
        #endregion
    }
}
