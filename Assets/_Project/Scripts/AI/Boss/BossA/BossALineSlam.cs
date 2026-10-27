using UnityEngine;
using AI.Boss.Logic;

namespace AI.Boss.BossA
{
    /// <summary>
    /// A2. 한 줄 전체에 예고를 띄우고, 한 손이 플레이어에게 가까운 끝 칸을 내려친 뒤 바닥을 쓸며 반대쪽 끝까지 간다.
    /// 손이 지나간 칸부터 차례로 무너지고, 무너지는 순간 그 칸 위에 있으면 피해를 받는다(한 패턴에 한 번).
    /// 표적에서 빠진 칸(B 가 쓰는 칸)은 손이 지나가도 무너뜨리지 않는다.
    /// </summary>
    public class BossALineSlam : BossAPatternBase
    {
        #region Serialized Fields
        [Tooltip("내려치기 전 줄 예고 시간(초). 보스 단계별 예고 배율(TelegraphScale)이 곱해진다")]
        [SerializeField, Min(0.0f)] private float _telegraphSeconds = 1.5f;
        [Tooltip("임시값. 손이 한 칸을 지나가는 시간(초). 이 간격으로 칸이 차례로 무너진다")]
        [SerializeField, Min(0.0f)] private float _sweepSecondsPerTile = 0.15f;
        #endregion

        #region Private Fields
        // 무너질 순서대로 담은 칸과, 첫 내려침부터 그 칸이 무너질 때까지의 시간
        private readonly int[] _sweepTiles = new int[ArenaGrid.SIZE];
        private readonly float[] _sweepTimes = new float[ArenaGrid.SIZE];
        private int _sweepCount;
        private int _sweepNext;
        private float _sweepElapsed;
        private bool _isSweeping;
        private bool _isHitApplied;
        private BossHand _sweepHand;
        #endregion

        #region Properties
        protected override float TelegraphSeconds => _telegraphSeconds;
        #endregion

        #region Public Methods
        public override void Stop()
        {
            _isSweeping = false;
            base.Stop();
        }
        #endregion

        #region Protected Methods
        protected override void BuildTargets()
        {
            SBossAPatternChoice choice = Controller.CurrentChoice;
            int[] tiles = choice.Tiles;
            _sweepCount = 0;
            if ((tiles == null) || (tiles.Length == 0)) return;

            int first = tiles[0];
            int last = tiles[tiles.Length - 1];
            int start = IsCloserToPlayer(last, first) ? last : first;

            // 시작 칸에서 먼 순서로 담는다. 표적 칸은 줄을 따라 정렬돼 있다
            int startLine = GetLinePosition(choice, start);
            for (int i = 0; i < tiles.Length; i++)
            {
                int tile = (start == first) ? tiles[i] : tiles[tiles.Length - 1 - i];
                _sweepTiles[_sweepCount] = tile;
                _sweepTimes[_sweepCount] = Mathf.Abs(GetLinePosition(choice, tile) - startLine) * _sweepSecondsPerTile;
                _sweepCount++;
            }

            bool isLeft = IsLeftHandCloser(Arena.GetTileCenter(start));
            _sweepHand = Controller.GetHand(isLeft);
            AddHand(isLeft, start);
        }

        protected override void OnImpact()
        {
            // 첫 내려침은 시작 칸을 무너뜨린다. 나머지는 쓸기 동안 OnTick 이 무너뜨린다
            _sweepNext = 0;
            _sweepElapsed = 0.0f;
            _isHitApplied = false;
            CollapseDueTiles(0.0f);
        }

        protected override bool StartFollowUp()
        {
            if (_sweepNext >= _sweepCount) return false;

            _isSweeping = true;
            if (_sweepHand)
            {
                int end = _sweepCount - 1;
                _sweepHand.Sweep(Arena.GetTileCenter(_sweepTiles[end]), _sweepTimes[end], OnSweepDone);
            }
            return true;
        }

        protected override void OnTick(float deltaTime)
        {
            if (!_isSweeping) return;

            _sweepElapsed += deltaTime;
            CollapseDueTiles(_sweepElapsed);

            // 손이 없으면 시간만으로 끝낸다. 손이 있으면 손이 끝 칸에 닿아야 머묾으로 넘어갈 수 있다
            if (!_sweepHand && (_sweepNext >= _sweepCount)) EndSweep();
        }
        #endregion

        #region Private Methods
        private void OnSweepDone()
        {
            CollapseDueTiles(float.MaxValue);
            EndSweep();
        }

        private void EndSweep()
        {
            if (!_isSweeping) return;

            _isSweeping = false;
            FinishFollowUp();
        }

        private void CollapseDueTiles(float elapsed)
        {
            while ((_sweepNext < _sweepCount) && (_sweepTimes[_sweepNext] <= elapsed))
            {
                int tile = _sweepTiles[_sweepNext];
                _sweepNext++;

                if (!_isHitApplied && IsPlayerOnTile(tile))
                {
                    _isHitApplied = true;
                    ApplyHitToPlayer();
                }

                // 예고 중인 칸은 무너질 때 예고도 함께 꺼진다
                Arena.Collapse(tile);
            }
        }

        /// <returns>플레이어가 a 칸에 b 칸보다 가까우면 true. 플레이어가 없으면 false</returns>
        private bool IsCloserToPlayer(int a, int b)
        {
            Transform body = Situation.PlayerBody;
            if (!body) return false;

            Vector3 position = body.position;
            return GetSqrDistanceXZ(position, Arena.GetTileCenter(a)) < GetSqrDistanceXZ(position, Arena.GetTileCenter(b));
        }

        /// <returns>줄 안에서 칸의 자리. 가로줄이면 col, 세로줄이면 row</returns>
        private static int GetLinePosition(SBossAPatternChoice choice, int tile)
        {
            return choice.IsRowLine ? ArenaGrid.GetCol(tile) : ArenaGrid.GetRow(tile);
        }
        #endregion
    }
}
