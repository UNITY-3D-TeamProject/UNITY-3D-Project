using System;

namespace AI.Boss.Logic
{
    /// <summary>
    /// 플레이어 표본으로 서 있는 칸·점프 여부·착지 예상 칸·같은 줄에 머문 시간을 계산한다. Unity 를 참조하지 않는다.
    /// 좌표는 아레나 로컬 m 단위로 받고 안에서 칸 단위로 바꾼다(<see cref="ArenaGrid"/> 좌표와 같은 기준).
    /// </summary>
    public class PlayerArenaTracker
    {
        #region Private Fields
        private readonly float _gravity;
        private readonly float _tileSize;

        private bool _hasSample;
        private float _lastTime;
        private float _x;
        private float _z;
        private float _y;
        private float _groundY;
        #endregion

        #region Properties
        /// <summary>서 있는 칸 인덱스. 판 밖이면 -1.</summary>
        public int TileIndex { get; private set; } = -1;

        public bool IsOnArena => TileIndex >= 0;

        public bool IsJumping { get; private set; }

        /// <summary>수평 속도 x 성분(칸/초).</summary>
        public float VelocityX { get; private set; }

        /// <summary>수직 속도(m/s). 위쪽이 양수.</summary>
        public float VelocityY { get; private set; }

        /// <summary>수평 속도 z 성분(칸/초).</summary>
        public float VelocityZ { get; private set; }

        /// <summary>머물고 있는 row. 판 밖이면 -1.</summary>
        public int StayRow { get; private set; } = -1;

        /// <summary><see cref="StayRow"/> 에 머문 시간(초). 줄이 바뀌거나 판 밖이면 0 부터 다시 센다.</summary>
        public float RowStaySeconds { get; private set; }

        /// <summary>머물고 있는 col. 판 밖이면 -1.</summary>
        public int StayCol { get; private set; } = -1;

        /// <summary><see cref="StayCol"/> 에 머문 시간(초). 줄이 바뀌거나 판 밖이면 0 부터 다시 센다.</summary>
        public float ColStaySeconds { get; private set; }
        #endregion

        #region Constructors
        /// <param name="gravity">중력 가속도(m/s², 양수)</param>
        /// <param name="tileSize">칸 한 변 길이(m)</param>
        public PlayerArenaTracker(float gravity, float tileSize)
        {
            _gravity = gravity;
            _tileSize = tileSize;
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// 플레이어 표본을 넣는다. 직전 표본과의 차이로 속도를 구한다(첫 표본은 0).
        /// </summary>
        /// <param name="time">표본 시각(초)</param>
        /// <param name="x">아레나 로컬 x(m)</param>
        /// <param name="z">아레나 로컬 z(m)</param>
        /// <param name="y">플레이어 높이(m)</param>
        /// <param name="groundY">아레나 바닥 높이(m)</param>
        /// <param name="isGrounded">접지 여부</param>
        public void Sample(float time, float x, float z, float y, float groundY, bool isGrounded)
        {
            float tileX = x / _tileSize;
            float tileZ = z / _tileSize;
            float deltaTime = Math.Max(0.0f, time - _lastTime);

            if (!_hasSample)
            {
                VelocityX = 0.0f;
                VelocityY = 0.0f;
                VelocityZ = 0.0f;
                deltaTime = 0.0f;
            }
            else if (deltaTime > 0.0f)
            {
                VelocityX = (tileX - _x) / deltaTime;
                VelocityY = (y - _y) / deltaTime;
                VelocityZ = (tileZ - _z) / deltaTime;
            }

            _hasSample = true;
            _lastTime = time;
            _x = tileX;
            _z = tileZ;
            _y = y;
            _groundY = groundY;

            TileIndex = GetTile(tileX, tileZ);
            IsJumping = !isGrounded;

            int row = IsOnArena ? ArenaGrid.GetRow(TileIndex) : -1;
            int col = IsOnArena ? ArenaGrid.GetCol(TileIndex) : -1;
            RowStaySeconds = UpdateStay(row, StayRow, RowStaySeconds, deltaTime);
            ColStaySeconds = UpdateStay(col, StayCol, ColStaySeconds, deltaTime);
            StayRow = row;
            StayCol = col;
        }

        /// <summary>
        /// 착지할 칸을 예측한다. 남은 체공 시간 t = (vy + √(vy² + 2gh)) / g 동안 지금 수평 속도로 간 지점의 칸이다.
        /// 착지점이 판 밖이면 가장 가까운 칸. 접지 중이면 지금 칸(판 밖이면 -1).
        /// </summary>
        public int PredictLandingTile()
        {
            if (!IsJumping) return TileIndex;

            float height = Math.Max(0.0f, _y - _groundY);
            float airSeconds = (VelocityY + (float)Math.Sqrt(VelocityY * VelocityY + 2.0f * _gravity * height)) / _gravity;
            float landingX = _x + VelocityX * airSeconds;
            float landingZ = _z + VelocityZ * airSeconds;

            int row = Clamp((int)Math.Floor(landingZ), 0, ArenaGrid.SIZE - 1);
            int col = Clamp((int)Math.Floor(landingX), 0, ArenaGrid.SIZE - 1);
            return ArenaGrid.GetIndex(row, col);
        }

        /// <summary>
        /// 표본이 없는 처음 상태로 되돌린다.
        /// </summary>
        public void Reset()
        {
            _hasSample = false;
            _lastTime = 0.0f;
            _x = 0.0f;
            _z = 0.0f;
            _y = 0.0f;
            _groundY = 0.0f;

            TileIndex = -1;
            IsJumping = false;
            VelocityX = 0.0f;
            VelocityY = 0.0f;
            VelocityZ = 0.0f;
            StayRow = -1;
            RowStaySeconds = 0.0f;
            StayCol = -1;
            ColStaySeconds = 0.0f;
        }
        #endregion

        #region Private Methods
        private static int GetTile(float tileX, float tileZ)
        {
            return ArenaGrid.GetIndex((int)Math.Floor(tileZ), (int)Math.Floor(tileX));
        }

        private static float UpdateStay(int line, int lastLine, float staySeconds, float deltaTime)
        {
            if (line < 0 || line != lastLine) return 0.0f;

            return staySeconds + deltaTime;
        }

        private static int Clamp(int value, int min, int max)
        {
            return Math.Min(Math.Max(value, min), max);
        }
        #endregion
    }
}
