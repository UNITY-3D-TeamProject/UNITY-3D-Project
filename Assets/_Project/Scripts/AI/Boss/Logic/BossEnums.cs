namespace AI.Boss.Logic
{
    /// <summary>
    /// 보스 아레나 칸 하나의 상태.
    /// </summary>
    public enum ETileState
    {
        Normal,
        Telegraphed,
        Collapsed,
    }

    /// <summary>
    /// 보스전 진행 단계.
    /// </summary>
    public enum EBossStage
    {
        Stage1_1,
        Stage1_2,
        Final,
        Defeated,
    }

    /// <summary>
    /// B 가 고르는 공격. None 이면 추격한다.
    /// </summary>
    public enum EBossBAttack
    {
        None,
        Combo,
        Charge,
        Leap,
    }

    /// <summary>
    /// A 가 고르는 패턴. Rest 면 잠깐 쉰다.
    /// </summary>
    public enum EBossAPattern
    {
        Rest,
        A1,
        A1Double,
        A2,
        A4,
        A5Plus,
        A5Cross,
        A6,
    }

    /// <summary>
    /// A 가 받는 피격 범위. 중계(BossHitRelay)마다 허용 여부를 정한다.
    /// </summary>
    public enum EBossDamageMode
    {
        /// <summary>전부 차단 (1페이즈)</summary>
        Blocked,

        /// <summary>손만 허용 (그로기). HP 하한이 걸린다</summary>
        HandsOnly,

        /// <summary>손·몸·머리 전부 허용 (최종)</summary>
        All,
    }

    /// <summary>
    /// A 의 피격 중계가 붙은 부위.
    /// </summary>
    public enum EBossHitPart
    {
        Hand,
        Body,
        Head,
    }
}
