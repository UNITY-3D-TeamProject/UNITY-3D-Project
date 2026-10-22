using System;
using System.Collections.Generic;
using UnityEngine;
using AI.Boss.Logic;

namespace AI.Boss.BossA
{
    /// <summary>
    /// A5 체스판. 모양은 BossAPatternSelector 가 정한다. 최종 단계 전용이다.
    /// +자: 양손이 첫 줄 양 끝을 내려치면 첫 줄이 한 번에 무너지고, 둘째 줄 양 끝으로 옮겨 다시 내려치면 둘째 줄이 무너진다.
    /// 예고는 지금 칠 줄에만 띄운다. 가운데 칸은 첫 줄과 함께 무너진다.
    /// X자: 양손이 보스 쪽 두 모서리를 내려친 뒤 각자 맞은편 모서리까지 대각선으로 쓸며 한 칸씩 무너뜨린다(가운데에서 교차).
    /// 두 손이 가운데에서 겹치지 않게 한 손은 늦게 출발한다. 피해는 한 패턴에 한 번이다.
    /// 어느 쪽이든 표적에서 빠진 칸(B 가 쓰는 칸)은 무너뜨리지 않는다.
    /// </summary>
    public class BossAChessSlam : BossAPatternBase
    {
        #region Constants
        private const int HAND_COUNT = 2;
        #endregion

        #region Serialized Fields
        [Tooltip("임시값. 스펙에 정해진 시간이 없어 A2·A4 와 같게 둔다")]
        [SerializeField, Min(0.0f)] private float _telegraphSeconds = 1.5f;
        [Tooltip("임시값. +자 첫 줄 충돌에서 둘째 줄 충돌까지의 간격(초). 둘째 줄 예고도 이 시간 동안 채운다")]
        [SerializeField, Min(0.0f)] private float _secondLineDelay = 1.0f;
        [Tooltip("임시값. X자에서 손이 한 칸을 지나가는 시간(초). A2 와 같게 둔다")]
        [SerializeField, Min(0.0f)] private float _sweepSecondsPerTile = 0.15f;
        [Tooltip("임시값. X자에서 두 번째 손이 늦게 출발하는 시간(초). 가운데에서 두 손이 겹치지 않게 한다")]
        [SerializeField, Min(0.0f)] private float _crossDelaySeconds = 0.3f;
        #endregion

        #region Private Fields
        private bool _isPlus;
        private int[] _firstLineTiles;
        private int[] _secondLineTiles;

        // X자: 손마다 무너질 순서대로 담은 칸과, 첫 내려침부터 그 칸이 무너질 때까지의 시간
        private readonly BossHand[] _sweepHands = new BossHand[HAND_COUNT];
        private readonly Vector3[] _sweepEnds = new Vector3[HAND_COUNT];
        private readonly float[] _sweepDelays = new float[HAND_COUNT];
        private readonly int[,] _sweepTiles = new int[HAND_COUNT, ArenaGrid.SIZE];
        private readonly float[,] _sweepTimes = new float[HAND_COUNT, ArenaGrid.SIZE];
        private readonly int[] _sweepCounts = new int[HAND_COUNT];
        private readonly int[] _sweepNext = new int[HAND_COUNT];
        private readonly bool[] _isSweepStarted = new bool[HAND_COUNT];
        private readonly bool[] _isSweepDone = new bool[HAND_COUNT];
        private int _sweepHandCount;
        private float _sweepElapsed;
        private bool _isSweeping;
        private bool _isHitApplied;
        #endregion

        #region Properties
        protected override float TelegraphSeconds => _telegraphSeconds;
        protected override int StrikeCount => (_isPlus && (_secondLineTiles.Length > 0)) ? 2 : 1;
        protected override float NextStrikeDelay => _secondLineDelay;
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
            _isPlus = choice.Pattern == EBossAPattern.A5Plus;
            if (_isPlus) BuildPlusTargets(choice);
            else BuildCrossTargets(choice);
        }

        protected override int[] GetTargetTiles()
        {
            // +자는 첫 줄만 예고한다. 둘째 줄은 RetargetHands 에서 바꿔 띄운다
            return _isPlus ? _firstLineTiles : base.GetTargetTiles();
        }

        protected override void RetargetHands()
        {
            AddLineEndHands(_secondLineTiles);
            ReplaceTelegraph(_secondLineTiles, _secondLineDelay);
        }

        protected override void OnImpact()
        {
            if (_isPlus)
            {
                // 지금 예고 중인 줄 위 플레이어 피해 + 그 줄 붕괴
                base.OnImpact();
                return;
            }

            // 첫 내려침은 두 모서리를 무너뜨린다. 나머지는 쓸기 동안 OnTick 이 무너뜨린다
            _sweepElapsed = 0.0f;
            _isHitApplied = false;
            for (int i = 0; i < _sweepHandCount; i++)
            {
                _sweepNext[i] = 0;
                _isSweepStarted[i] = false;
                _isSweepDone[i] = false;
            }
            CollapseDueTiles(0.0f);
        }

        protected override bool StartFollowUp()
        {
            if (_isPlus || (_sweepHandCount == 0)) return false;

            _isSweeping = true;
            StartDueSweeps();
            return true;
        }

        protected override void OnTick(float deltaTime)
        {
            if (!_isSweeping) return;

            _sweepElapsed += deltaTime;
            StartDueSweeps();
            CollapseDueTiles(_sweepElapsed);

            // 손이 없는 쪽은 시간만으로 끝낸다
            for (int i = 0; i < _sweepHandCount; i++)
            {
                bool isTimeOnlyDone = !_sweepHands[i] && (_sweepNext[i] >= _sweepCounts[i]);
                if (isTimeOnlyDone) _isSweepDone[i] = true;
            }
            TryEndSweep();
        }
        #endregion

        #region Private Methods
        private void BuildPlusTargets(SBossAPatternChoice choice)
        {
            // choice.IsRowLine 이 먼저 무너질 줄. 가운데 칸은 첫 줄에 넣는다
            int middle = ArenaGrid.SIZE / 2;
            List<int> first = new List<int>();
            List<int> second = new List<int>();
            foreach (int tile in choice.Tiles)
            {
                int line = choice.IsRowLine ? ArenaGrid.GetRow(tile) : ArenaGrid.GetCol(tile);
                if (line == middle) first.Add(tile);
                else second.Add(tile);
            }

            // 첫 줄 칸이 모두 빠졌으면 남은 줄을 한 번에 친다
            if (first.Count == 0)
            {
                first = second;
                second = new List<int>();
            }

            // 칸 인덱스 순서 = 줄을 따라 한쪽 끝 → 반대쪽 끝
            first.Sort();
            second.Sort();
            _firstLineTiles = first.ToArray();
            _secondLineTiles = second.ToArray();

            AddLineEndHands(_firstLineTiles);
        }

        /// <summary>줄의 양 끝 칸에 손을 하나씩 등록한다. 왼손은 왼손에 가까운 끝, 칸이 하나면 그 칸에 가까운 손 하나.</summary>
        private void AddLineEndHands(int[] lineTiles)
        {
            if (lineTiles.Length == 0) return;

            int first = lineTiles[0];
            int last = lineTiles[lineTiles.Length - 1];
            if (first == last)
            {
                AddHand(IsLeftHandCloser(Arena.GetTileCenter(first)), first);
                return;
            }

            bool isFirstLeft = IsLeftHandCloser(Arena.GetTileCenter(first));
            AddHand(true, isFirstLeft ? first : last);
            AddHand(false, isFirstLeft ? last : first);
        }

        private void BuildCrossTargets(SBossAPatternChoice choice)
        {
            _sweepHandCount = 0;

            // 보스에 가까운 두 모서리에서 출발한다
            int last = ArenaGrid.SIZE - 1;
            int[] corners =
            {
                ArenaGrid.GetIndex(0, 0),
                ArenaGrid.GetIndex(0, last),
                ArenaGrid.GetIndex(last, 0),
                ArenaGrid.GetIndex(last, last),
            };
            Vector3 bossPosition = Controller.transform.position;
            Array.Sort(corners, (a, b) => GetSqrDistanceXZ(bossPosition, Arena.GetTileCenter(a))
                .CompareTo(GetSqrDistanceXZ(bossPosition, Arena.GetTileCenter(b))));

            // 왼손은 두 모서리 중 왼손에 가까운 쪽에서 먼저 출발하고, 오른손이 늦게 출발한다
            bool isFirstLeft = IsLeftHandCloser(Arena.GetTileCenter(corners[0]));
            int leftCorner = isFirstLeft ? corners[0] : corners[1];
            int rightCorner = isFirstLeft ? corners[1] : corners[0];

            AddSweepHand(choice, true, leftCorner, 0.0f);
            AddSweepHand(choice, false, rightCorner, _crossDelaySeconds);
        }

        private void AddSweepHand(SBossAPatternChoice choice, bool isLeft, int corner, float delay)
        {
            int index = _sweepHandCount;
            _sweepHandCount++;

            int last = ArenaGrid.SIZE - 1;
            int row = ArenaGrid.GetRow(corner);
            int col = ArenaGrid.GetCol(corner);
            int rowStep = (row == 0) ? 1 : -1;
            int colStep = (col == 0) ? 1 : -1;

            // 표적에 있는 칸만, 먼저 출발한 손이 이미 맡은 칸(가운데)은 빼고 담는다
            int count = 0;
            for (int step = 0; step < ArenaGrid.SIZE; step++)
            {
                int tile = ArenaGrid.GetIndex(row + rowStep * step, col + colStep * step);
                if ((Array.IndexOf(choice.Tiles, tile) < 0) || IsTakenByOtherHand(index, tile)) continue;

                // 모서리는 내려치는 순간 무너지고, 나머지는 손이 출발한 뒤 지나가는 순서대로 무너진다
                _sweepTiles[index, count] = tile;
                _sweepTimes[index, count] = (step == 0) ? 0.0f : delay + step * _sweepSecondsPerTile;
                count++;
            }

            _sweepCounts[index] = count;
            _sweepDelays[index] = delay;
            _sweepEnds[index] = Arena.GetTileCenter(ArenaGrid.GetIndex(last - row, last - col));
            _sweepHands[index] = Controller.GetHand(isLeft);
            AddHand(isLeft, corner);
        }

        private bool IsTakenByOtherHand(int handIndex, int tile)
        {
            for (int i = 0; i < handIndex; i++)
            {
                for (int j = 0; j < _sweepCounts[i]; j++)
                {
                    if (_sweepTiles[i, j] == tile) return true;
                }
            }
            return false;
        }

        private void StartDueSweeps()
        {
            float seconds = (ArenaGrid.SIZE - 1) * _sweepSecondsPerTile;
            for (int i = 0; i < _sweepHandCount; i++)
            {
                if (_isSweepStarted[i] || (_sweepElapsed < _sweepDelays[i])) continue;

                _isSweepStarted[i] = true;
                if (!_sweepHands[i]) continue;

                int handIndex = i;
                _sweepHands[i].Sweep(_sweepEnds[i], seconds, () => OnSweepDone(handIndex));
            }
        }

        private void OnSweepDone(int handIndex)
        {
            _isSweepDone[handIndex] = true;
            CollapseDueTiles(handIndex, float.MaxValue);
            TryEndSweep();
        }

        private void TryEndSweep()
        {
            if (!_isSweeping) return;

            for (int i = 0; i < _sweepHandCount; i++)
            {
                if (!_isSweepDone[i]) return;
            }

            _isSweeping = false;
            FinishFollowUp();
        }

        private void CollapseDueTiles(float elapsed)
        {
            for (int i = 0; i < _sweepHandCount; i++)
            {
                CollapseDueTiles(i, elapsed);
            }
        }

        private void CollapseDueTiles(int handIndex, float elapsed)
        {
            while ((_sweepNext[handIndex] < _sweepCounts[handIndex]) &&
                   (_sweepTimes[handIndex, _sweepNext[handIndex]] <= elapsed))
            {
                int tile = _sweepTiles[handIndex, _sweepNext[handIndex]];
                _sweepNext[handIndex]++;

                if (!_isHitApplied && IsPlayerOnTile(tile))
                {
                    _isHitApplied = true;
                    ApplyHitToPlayer();
                }

                // 예고 중인 칸은 무너질 때 예고도 함께 꺼진다
                Arena.Collapse(tile);
            }
        }
        #endregion
    }
}
