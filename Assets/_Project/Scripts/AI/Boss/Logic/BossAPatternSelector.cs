using System;
using System.Collections.Generic;

namespace AI.Boss.Logic
{
    /// <summary>
    /// 상황 스냅샷을 보고 A 의 다음 패턴과 표적 칸을 고른다. Unity 를 참조하지 않는다.
    /// 순서: 단계별 열린 패턴 → 붕괴 제한 → 상황 규칙 1~5 → 기본 순서. A4 는 직전에 끝난 패턴이 A4 면 고르지 않는다.
    /// </summary>
    public class BossAPatternSelector
    {
        #region Constants
        private const int CENTER = 4;
        #endregion

        #region Private Fields
        // 기본 순서의 자리. A1 자리는 최종에서 A1Double, A5Plus 자리는 +자·X자 무작위가 된다.
        private static readonly EBossAPattern[] DefaultOrder =
        {
            EBossAPattern.A1,
            EBossAPattern.A2,
            EBossAPattern.A4,
            EBossAPattern.A5Plus,
            EBossAPattern.A6,
        };

        private static readonly int[] PlusTiles = { 1, 3, 4, 5, 7 };
        private static readonly int[] CrossTiles = { 0, 2, 4, 6, 8 };

        // 상하좌우 방향. ArenaGrid.GetNeighbors4 와 같은 순서(row − 1, row + 1, col − 1, col + 1).
        private static readonly int[] RowSteps = { -1, 1, 0, 0 };
        private static readonly int[] ColSteps = { 0, 0, -1, 1 };

        private readonly Random _random;
        private readonly int _collapseLimit;
        private readonly float _lineStaySeconds;
        private readonly List<int> _candidates = new List<int>();

        private int _defaultIndex;
        private bool _isLastFinishedGrab;
        private bool _hasLastSingleHand;
        private bool _isLastSingleHandLeft;
        #endregion

        #region Constructors
        /// <param name="random">무작위 선택에 쓸 난수</param>
        /// <param name="collapseLimit">무너진 칸이 이 수 이상이면 칸을 무너뜨리는 패턴을 고르지 않는다</param>
        /// <param name="lineStaySeconds">플레이어가 한 줄에 이 시간(초) 이상 머물면 그 줄에 A2</param>
        public BossAPatternSelector(Random random, int collapseLimit = 4, float lineStaySeconds = 3)
        {
            _random = random;
            _collapseLimit = collapseLimit;
            _lineStaySeconds = lineStaySeconds;
        }
        #endregion

        #region Public Methods
        public SBossAPatternChoice Choose(in SBossSituation situation)
        {
            EBossStage stage = situation.Stage;
            if (stage == EBossStage.Defeated) return CreateRest();

            int targetTile = situation.PlayerTile >= 0 ? situation.PlayerTile : CENTER;

            if (situation.Grid.CollapsedCount >= _collapseLimit)
            {
                if (stage == EBossStage.Final) return CreateClap(ArenaGrid.GetRow(targetTile));
                if (CanUse(EBossAPattern.A4, stage)) return CreateGrab(targetTile);

                return CreateRest();
            }

            if (TryChooseBySituation(in situation, targetTile, out SBossAPatternChoice choice)) return choice;

            return ChooseByDefaultOrder(in situation, targetTile);
        }

        /// <summary>
        /// 단계가 바뀔 때 기본 순서를 처음부터 다시 세고, A4 연속 금지·직전 손 기록을 지운다.
        /// </summary>
        public void ResetForStage()
        {
            _defaultIndex = 0;
            _isLastFinishedGrab = false;
            _hasLastSingleHand = false;
        }

        /// <summary>
        /// 패턴이 끝까지 끝났을 때 부른다(A4 연속 금지 기록). 취소된 패턴은 부르지 않는다.
        /// </summary>
        public void NotifyPatternFinished(EBossAPattern pattern)
        {
            _isLastFinishedGrab = pattern == EBossAPattern.A4;
        }
        #endregion

        #region Private Methods
        private bool TryChooseBySituation(in SBossSituation situation, int targetTile, out SBossAPatternChoice choice)
        {
            EBossStage stage = situation.Stage;
            ArenaGrid grid = situation.Grid;

            // 규칙 1: B 가 돌진을 준비 중이면 돌진 경로 바로 옆 칸 (1페이즈)
            if (situation.IsBChargePreparing && stage != EBossStage.Final)
            {
                int sideTile = PickChargeSideTile(grid, targetTile, situation.ChargeDirectionIsRow);
                if (sideTile >= 0)
                {
                    choice = CreateSlam(stage, grid, sideTile);
                    return true;
                }
            }

            // 규칙 2: 플레이어가 점프 중이면 착지할 칸
            if (situation.IsPlayerJumping)
            {
                int landingTile = situation.PlayerTile >= 0 ? situation.LandingTile : CENTER;
                choice = CreateSlam(stage, grid, landingTile);
                return true;
            }

            // 규칙 3: 모서리 칸이면 A4
            if (IsCorner(situation.PlayerTile) && CanUse(EBossAPattern.A4, stage))
            {
                choice = CreateGrab(situation.PlayerTile);
                return true;
            }

            // 규칙 4: 가운데 칸이면 A5 +자 (최종)
            if (situation.PlayerTile == CENTER && stage == EBossStage.Final)
            {
                choice = CreateChess(true);
                return true;
            }

            // 규칙 5: 한 줄에 오래 머물면 그 줄에 A2 (몰기보다 이 줄이 먼저)
            if (CanUse(EBossAPattern.A2, stage) && TryGetStayLine(in situation, out bool isRowLine, out int line))
            {
                choice = CreateLineSlam(isRowLine, line);
                return true;
            }

            choice = default;
            return false;
        }

        private SBossAPatternChoice ChooseByDefaultOrder(in SBossSituation situation, int targetTile)
        {
            EBossStage stage = situation.Stage;

            for (int i = 0; i < DefaultOrder.Length; i++)
            {
                int slot = (_defaultIndex + i) % DefaultOrder.Length;
                EBossAPattern pattern = DefaultOrder[slot];
                if ((pattern == EBossAPattern.A1) && (stage == EBossStage.Final)) pattern = EBossAPattern.A1Double;
                if (!CanUse(pattern, stage)) continue;

                _defaultIndex = (slot + 1) % DefaultOrder.Length;

                switch (pattern)
                {
                    case EBossAPattern.A1:
                    case EBossAPattern.A1Double:
                        return CreateSlam(stage, situation.Grid, targetTile);
                    case EBossAPattern.A2:
                        return CreateDefaultLineSlam(situation.Grid, targetTile);
                    case EBossAPattern.A4:
                        return CreateGrab(targetTile);
                    case EBossAPattern.A5Plus:
                        return CreateChess(_random.Next(2) == 0);
                    default:
                        return CreateClap(ArenaGrid.GetRow(targetTile));
                }
            }

            return CreateRest();
        }

        private bool CanUse(EBossAPattern pattern, EBossStage stage)
        {
            switch (pattern)
            {
                case EBossAPattern.A1:
                    return stage == EBossStage.Stage1_1 || stage == EBossStage.Stage1_2;
                case EBossAPattern.A2:
                    return stage == EBossStage.Stage1_2 || stage == EBossStage.Final;
                case EBossAPattern.A4:
                    return (stage == EBossStage.Stage1_2 || stage == EBossStage.Final) && !_isLastFinishedGrab;
                default:
                    return stage == EBossStage.Final;
            }
        }

        /// <summary>
        /// 표적 칸에서 돌진 방향에 수직인 두 이웃 중 멀쩡한 칸. 둘 다면 무작위, 없으면 -1.
        /// </summary>
        private int PickChargeSideTile(ArenaGrid grid, int tile, bool isChargeAlongRow)
        {
            int row = ArenaGrid.GetRow(tile);
            int col = ArenaGrid.GetCol(tile);

            _candidates.Clear();
            if (isChargeAlongRow)
            {
                AddIfSafe(grid, ArenaGrid.GetIndex(row, col - 1));
                AddIfSafe(grid, ArenaGrid.GetIndex(row, col + 1));
            }
            else
            {
                AddIfSafe(grid, ArenaGrid.GetIndex(row - 1, col));
                AddIfSafe(grid, ArenaGrid.GetIndex(row + 1, col));
            }

            return PickCandidate();
        }

        /// <summary>
        /// 1페이즈는 한 손 A1. 최종은 A1Double 로 표적 칸 + 구멍 반대쪽 이웃(몰기) 또는 멀쩡한 이웃 중 무작위 하나.
        /// 멀쩡한 이웃이 없으면 한 손으로 친다.
        /// </summary>
        private SBossAPatternChoice CreateSlam(EBossStage stage, ArenaGrid grid, int tile)
        {
            if (stage != EBossStage.Final) return CreateSingleHand(EBossAPattern.A1, tile);

            if (!TryGetHerdTiles(grid, tile, out _, out int secondTile))
            {
                int row = ArenaGrid.GetRow(tile);
                int col = ArenaGrid.GetCol(tile);

                _candidates.Clear();
                for (int d = 0; d < RowSteps.Length; d++)
                {
                    AddIfSafe(grid, ArenaGrid.GetIndex(row + RowSteps[d], col + ColSteps[d]));
                }

                secondTile = PickCandidate();
            }

            if (secondTile < 0) return CreateSingleHand(EBossAPattern.A1Double, tile);

            AssignHands(tile, secondTile, out int left, out int right);
            return new SBossAPatternChoice(EBossAPattern.A1Double, new[] { tile, secondTile }, left, right);
        }

        /// <summary>
        /// 몰기가 걸리면 구멍과 나란한 줄, 아니면 표적 칸을 지나는 가로/세로줄 중 무작위.
        /// </summary>
        private SBossAPatternChoice CreateDefaultLineSlam(ArenaGrid grid, int tile)
        {
            int row = ArenaGrid.GetRow(tile);
            int col = ArenaGrid.GetCol(tile);

            bool isRowLine;
            if (TryGetHerdTiles(grid, tile, out int holeTile, out _)) isRowLine = ArenaGrid.GetRow(holeTile) == row;
            else isRowLine = _random.Next(2) == 0;

            return CreateLineSlam(isRowLine, isRowLine ? row : col);
        }

        private SBossAPatternChoice CreateLineSlam(bool isRowLine, int line)
        {
            int[] tiles = GetLineTiles(isRowLine, line);
            return new SBossAPatternChoice(EBossAPattern.A2, tiles, tiles[0], tiles[2], isRowLine, line);
        }

        private SBossAPatternChoice CreateGrab(int tile)
        {
            return CreateSingleHand(EBossAPattern.A4, tile);
        }

        private static SBossAPatternChoice CreateChess(bool isPlus)
        {
            if (isPlus) return new SBossAPatternChoice(EBossAPattern.A5Plus, (int[])PlusTiles.Clone(), 3, 5);

            return new SBossAPatternChoice(EBossAPattern.A5Cross, (int[])CrossTiles.Clone(), 0, 2);
        }

        private static SBossAPatternChoice CreateClap(int row)
        {
            int[] tiles = GetLineTiles(true, row);
            return new SBossAPatternChoice(EBossAPattern.A6, tiles, tiles[0], tiles[2], true, row);
        }

        private static SBossAPatternChoice CreateRest()
        {
            return new SBossAPatternChoice(EBossAPattern.Rest, Array.Empty<int>(), -1, -1);
        }

        /// <summary>
        /// 한 손 패턴. col 0 → 왼손, col 2 → 오른손, col 1 → 직전에 한 손으로 쓰지 않은 손(기록이 없으면 왼손).
        /// </summary>
        private SBossAPatternChoice CreateSingleHand(EBossAPattern pattern, int tile)
        {
            int col = ArenaGrid.GetCol(tile);

            bool isLeft;
            if (col == 0) isLeft = true;
            else if (col == ArenaGrid.SIZE - 1) isLeft = false;
            else isLeft = !_hasLastSingleHand || !_isLastSingleHandLeft;

            _hasLastSingleHand = true;
            _isLastSingleHandLeft = isLeft;

            return new SBossAPatternChoice(pattern, new[] { tile }, isLeft ? tile : -1, isLeft ? -1 : tile);
        }

        /// <summary>
        /// 구멍 쪽 몰기. 표적 칸 상하좌우의 붕괴 칸(두 방향 이상이면 무작위 하나)의 반대쪽 이웃이 멀쩡하면 true.
        /// </summary>
        private bool TryGetHerdTiles(ArenaGrid grid, int tile, out int holeTile, out int oppositeTile)
        {
            int row = ArenaGrid.GetRow(tile);
            int col = ArenaGrid.GetCol(tile);

            _candidates.Clear();
            for (int d = 0; d < RowSteps.Length; d++)
            {
                int neighbor = ArenaGrid.GetIndex(row + RowSteps[d], col + ColSteps[d]);
                if (neighbor >= 0 && grid.GetState(neighbor) == ETileState.Collapsed) _candidates.Add(d);
            }

            holeTile = -1;
            oppositeTile = -1;

            int direction = PickCandidate();
            if (direction < 0) return false;

            int opposite = ArenaGrid.GetIndex(row - RowSteps[direction], col - ColSteps[direction]);
            if (opposite < 0 || !grid.IsSafe(opposite)) return false;

            holeTile = ArenaGrid.GetIndex(row + RowSteps[direction], col + ColSteps[direction]);
            oppositeTile = opposite;
            return true;
        }

        /// <summary>
        /// 가로줄·세로줄 중 lineStaySeconds 이상 머문 줄. 둘 다면 더 오래 머문 줄, 같으면 무작위.
        /// </summary>
        private bool TryGetStayLine(in SBossSituation situation, out bool isRowLine, out int line)
        {
            bool isRowStay = situation.StayRow >= 0 && situation.RowStaySeconds >= _lineStaySeconds;
            bool isColStay = situation.StayCol >= 0 && situation.ColStaySeconds >= _lineStaySeconds;

            if (isRowStay && isColStay)
            {
                if (situation.RowStaySeconds > situation.ColStaySeconds) isRowLine = true;
                else if (situation.RowStaySeconds < situation.ColStaySeconds) isRowLine = false;
                else isRowLine = _random.Next(2) == 0;
            }
            else
            {
                isRowLine = isRowStay;
            }

            line = isRowLine ? situation.StayRow : situation.StayCol;
            return isRowStay || isColStay;
        }

        private void AddIfSafe(ArenaGrid grid, int tile)
        {
            if (tile >= 0 && grid.IsSafe(tile)) _candidates.Add(tile);
        }

        /// <returns>후보 중 하나. 둘 이상이면 무작위, 없으면 -1</returns>
        private int PickCandidate()
        {
            if (_candidates.Count == 0) return -1;
            if (_candidates.Count == 1) return _candidates[0];

            return _candidates[_random.Next(_candidates.Count)];
        }

        private static bool IsCorner(int tile)
        {
            return tile == 0 || tile == 2 || tile == 6 || tile == 8;
        }

        /// <summary>
        /// 줄의 세 칸. 가로줄은 col 0 → 2, 세로줄은 row 0 → 2 순이다(첫 칸 = 왼손, 끝 칸 = 오른손).
        /// </summary>
        private static int[] GetLineTiles(bool isRowLine, int line)
        {
            int[] tiles = new int[ArenaGrid.SIZE];
            for (int i = 0; i < ArenaGrid.SIZE; i++)
            {
                tiles[i] = isRowLine ? ArenaGrid.GetIndex(line, i) : ArenaGrid.GetIndex(i, line);
            }

            return tiles;
        }

        /// <summary>
        /// 두 칸의 손 배정. col 이 작은 칸 = 왼손, 같은 col 이면 row 가 작은 칸 = 왼손.
        /// </summary>
        private static void AssignHands(int a, int b, out int left, out int right)
        {
            int colA = ArenaGrid.GetCol(a);
            int colB = ArenaGrid.GetCol(b);
            bool isALeft = colA < colB || (colA == colB && ArenaGrid.GetRow(a) < ArenaGrid.GetRow(b));

            left = isALeft ? a : b;
            right = isALeft ? b : a;
        }
        #endregion
    }
}
