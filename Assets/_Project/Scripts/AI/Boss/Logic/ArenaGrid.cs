using System;
using System.Collections.Generic;

namespace AI.Boss.Logic
{
    /// <summary>
    /// 보스 아레나 5×5 칸의 상태·복구 타이머·좌표 변환·질의를 담당한다. Unity 를 참조하지 않는다.
    /// 인덱스 = row × 5 + col. 좌표는 칸 단위 실수이며 원점은 칸 (0,0) 의 바깥 모서리, x → col, z → row 이다.
    /// </summary>
    public class ArenaGrid
    {
        #region Constants
        public const int SIZE = 5;
        public const int COUNT = SIZE * SIZE;
        /// <summary>정가운데 칸 인덱스. SIZE 가 홀수일 때만 가운데 칸이 하나로 정해진다.</summary>
        public const int CENTER = (SIZE / 2) * SIZE + (SIZE / 2);

        private const float HOLE_SAMPLE_SPACING = 0.25f;
        #endregion

        #region Private Fields
        private readonly float _restoreSeconds;
        private readonly ETileState[] _states = new ETileState[COUNT];
        private readonly float[] _restoreRemaining = new float[COUNT];
        #endregion

        #region Properties
        /// <summary>지금 무너져 있는 칸 수.</summary>
        public int CollapsedCount { get; private set; }
        #endregion

        #region Events
        /// <summary>칸이 무너졌을 때 칸 인덱스를 알린다.</summary>
        public event Action<int> OnCollapsed;

        /// <summary>무너진 칸이 복구됐을 때 칸 인덱스를 알린다.</summary>
        public event Action<int> OnRestored;

        /// <summary>칸의 예고가 켜지거나 꺼졌을 때 (칸 인덱스, 켜짐 여부)를 알린다.</summary>
        public event Action<int, bool> OnTelegraphChanged;
        #endregion

        #region Constructors
        /// <param name="restoreSeconds">무너진 칸이 복구될 때까지의 시간(초)</param>
        public ArenaGrid(float restoreSeconds)
        {
            _restoreSeconds = restoreSeconds;
        }
        #endregion

        #region Public Static Methods
        public static int GetRow(int index)
        {
            return index / SIZE;
        }

        public static int GetCol(int index)
        {
            return index % SIZE;
        }

        /// <returns>칸 인덱스. 판 밖이면 -1</returns>
        public static int GetIndex(int row, int col)
        {
            if (!IsInside(row, col)) return -1;

            return row * SIZE + col;
        }

        public static bool IsInside(int row, int col)
        {
            return row >= 0 && row < SIZE && col >= 0 && col < SIZE;
        }
        #endregion

        #region Public Methods
        public ETileState GetState(int index)
        {
            return _states[index];
        }

        /// <summary>예고도 붕괴도 아닌 멀쩡한 칸인지.</summary>
        public bool IsSafe(int index)
        {
            return _states[index] == ETileState.Normal;
        }

        public bool IsHoleOrTelegraphed(int index)
        {
            return _states[index] != ETileState.Normal;
        }

        /// <summary>
        /// 칸의 예고를 켜거나 끈다. 무너진 칸에는 무시한다.
        /// </summary>
        public void SetTelegraphed(int index, bool isOn)
        {
            ETileState state = _states[index];
            if (state == ETileState.Collapsed) return;

            ETileState next = isOn ? ETileState.Telegraphed : ETileState.Normal;
            if (state == next) return;

            _states[index] = next;
            OnTelegraphChanged?.Invoke(index, isOn);
        }

        /// <summary>
        /// 칸을 무너뜨리고 복구 타이머를 처음부터 시작한다. 예고 중이었으면 예고를 끈다.
        /// 이미 무너진 칸이면 타이머만 다시 시작하고 이벤트를 보내지 않는다.
        /// </summary>
        public void Collapse(int index)
        {
            _restoreRemaining[index] = _restoreSeconds;

            ETileState state = _states[index];
            if (state == ETileState.Collapsed) return;

            _states[index] = ETileState.Collapsed;
            CollapsedCount++;

            if (state == ETileState.Telegraphed) OnTelegraphChanged?.Invoke(index, false);
            OnCollapsed?.Invoke(index);
        }

        /// <summary>
        /// 무너진 칸의 복구 타이머를 진행하고, 시간이 다 된 칸을 복구한다.
        /// </summary>
        public void Tick(float deltaTime)
        {
            for (int i = 0; i < COUNT; i++)
            {
                if (_states[i] != ETileState.Collapsed) continue;

                _restoreRemaining[i] -= deltaTime;
                if (_restoreRemaining[i] > 0.0f) continue;

                Restore(i);
            }
        }

        /// <summary>
        /// 모든 칸을 멀쩡한 상태로 되돌린다. 바뀐 칸마다 예고 꺼짐·복구 이벤트를 보낸다.
        /// </summary>
        public void ResetAll()
        {
            for (int i = 0; i < COUNT; i++)
            {
                if (_states[i] == ETileState.Telegraphed) SetTelegraphed(i, false);
                else if (_states[i] == ETileState.Collapsed) Restore(i);
            }
        }

        /// <summary>
        /// 칸 단위 좌표가 속한 칸을 찾는다. 칸의 시작 모서리는 그 칸에 속하고 끝 모서리는 다음 칸에 속한다.
        /// </summary>
        /// <returns>판 안이면 true. 판 밖이면 index = -1</returns>
        public bool TryGetTile(float x, float z, out int index)
        {
            int col = (int)Math.Floor(x);
            int row = (int)Math.Floor(z);

            index = GetIndex(row, col);
            return index >= 0;
        }

        public void GetTileCenter(int index, out float x, out float z)
        {
            x = GetCol(index) + 0.5f;
            z = GetRow(index) + 0.5f;
        }

        /// <summary>
        /// 멀쩡한 칸 중 칸 중심이 가장 가까운 칸을 찾는다. 거리가 같으면 인덱스가 작은 칸.
        /// </summary>
        /// <returns>칸 인덱스. 멀쩡한 칸이 없으면 -1</returns>
        public int FindNearestSafe(float x, float z)
        {
            int nearest = -1;
            float nearestSqrDistance = float.MaxValue;

            for (int i = 0; i < COUNT; i++)
            {
                if (!IsSafe(i)) continue;

                GetTileCenter(i, out float centerX, out float centerZ);
                float dx = centerX - x;
                float dz = centerZ - z;
                float sqrDistance = dx * dx + dz * dz;
                if (sqrDistance >= nearestSqrDistance) continue;

                nearest = i;
                nearestSqrDistance = sqrDistance;
            }

            return nearest;
        }

        /// <summary>
        /// 상하좌우 이웃 칸 중 판 안인 칸을 row − 1, row + 1, col − 1, col + 1 순으로 채운다.
        /// </summary>
        /// <param name="results">결과를 담을 목록. 먼저 비운다</param>
        public void GetNeighbors4(int index, List<int> results)
        {
            results.Clear();

            int row = GetRow(index);
            int col = GetCol(index);
            AddIfInside(row - 1, col, results);
            AddIfInside(row + 1, col, results);
            AddIfInside(row, col - 1, results);
            AddIfInside(row, col + 1, results);
        }

        /// <summary>
        /// 두 점을 잇는 선분 위에 구멍(붕괴·예고 칸)이나 판 밖이 있는지 0.25칸 간격 표본으로 판정한다.
        /// 두 끝점이 속한 칸은 검사하지 않는다.
        /// </summary>
        /// <returns>지나는 칸 중 구멍이 있거나 판 밖 표본이 있으면 true</returns>
        public bool HasHoleBetween(float ax, float az, float bx, float bz)
        {
            TryGetTile(ax, az, out int startTile);
            TryGetTile(bx, bz, out int endTile);

            float dx = bx - ax;
            float dz = bz - az;
            float length = (float)Math.Sqrt(dx * dx + dz * dz);
            int sampleCount = Math.Max(1, (int)Math.Ceiling(length / HOLE_SAMPLE_SPACING));

            for (int i = 0; i <= sampleCount; i++)
            {
                float t = (float)i / sampleCount;
                if (!TryGetTile(ax + dx * t, az + dz * t, out int tile)) return true;
                if (tile == startTile || tile == endTile) continue;
                if (IsHoleOrTelegraphed(tile)) return true;
            }

            return false;
        }
        #endregion

        #region Private Methods
        private void Restore(int index)
        {
            _states[index] = ETileState.Normal;
            _restoreRemaining[index] = 0.0f;
            CollapsedCount--;

            OnRestored?.Invoke(index);
        }

        private static void AddIfInside(int row, int col, List<int> results)
        {
            int index = GetIndex(row, col);
            if (index >= 0) results.Add(index);
        }
        #endregion
    }
}
