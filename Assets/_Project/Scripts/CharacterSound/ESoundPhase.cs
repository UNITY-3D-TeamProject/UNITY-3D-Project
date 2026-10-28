namespace CharacterSound
{
    /// <summary>
    /// 스킬 소리를 구분하는 단계. 스킬 이름과 함께 SoundController 의 매핑 키가 된다.
    /// </summary>
    public enum ESoundPhase
    {
        /// <summary>일반 스킬 실행.</summary>
        Execute,
        /// <summary>동작이 실제로 시작되었다 (구르기).</summary>
        Start,
        /// <summary>한 발을 쐈다.</summary>
        Shot,
        /// <summary>차지 중 (반복).</summary>
        Charge,
        /// <summary>탈것이 실제로 부착되었다.</summary>
        Mount,
        /// <summary>이동 중 (반복). 탈것이 붙어 있는 동안 계속 내는 탈것 소리에 쓴다.</summary>
        Move,
        /// <summary>스캔 파동이 실제로 시작되었다.</summary>
        Wave,
        /// <summary>공격이 적중하거나 착지했다.</summary>
        Impact,
        /// <summary>탈것이 붙어 있는 동안 점프를 시작했다. 기본 점프음 대신 낸다.</summary>
        Jump,
        /// <summary>요시 점프로 공중에서 버티는 중 (반복).</summary>
        Hover,
    }
}
