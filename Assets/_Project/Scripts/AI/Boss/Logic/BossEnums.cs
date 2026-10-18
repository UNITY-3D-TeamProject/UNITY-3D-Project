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
}
