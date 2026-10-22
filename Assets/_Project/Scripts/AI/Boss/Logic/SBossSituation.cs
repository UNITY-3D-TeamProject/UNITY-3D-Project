namespace AI.Boss.Logic
{
    /// <summary>
    /// A 패턴 선택기에 넣는 상황 스냅샷. 진행 쪽이 패턴을 고를 때마다 채운다.
    /// </summary>
    public struct SBossSituation
    {
        public EBossStage Stage;

        /// <summary>칸 상태를 읽을 아레나 그리드.</summary>
        public ArenaGrid Grid;

        /// <summary>플레이어가 서 있는 칸. 판 밖이면 -1.</summary>
        public int PlayerTile;

        public bool IsPlayerJumping;

        /// <summary>플레이어가 착지할 칸(<see cref="PlayerArenaTracker.PredictLandingTile"/>).</summary>
        public int LandingTile;

        public int StayRow;
        public float RowStaySeconds;
        public int StayCol;
        public float ColStaySeconds;

        public bool IsBChargePreparing;

        /// <summary>B 돌진 방향이 row 축(row 인덱스가 바뀌는 방향, 아레나 로컬 z)인지. false 면 col 축(로컬 x).</summary>
        public bool ChargeDirectionIsRow;

        /// <summary>B 돌진 시작점과 수평 방향(칸 단위 좌표, x → col, z → row). 방향이 0 이면 직선 위 칸을 빼지 않는다.</summary>
        public float ChargeOriginX;
        public float ChargeOriginZ;
        public float ChargeDirectionX;
        public float ChargeDirectionZ;
    }
}
