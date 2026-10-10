namespace Skill.Core
{
    /// <summary>
    /// 재사용 대기 시간을 가진 코스트. SkillBase 가 스킬의 쿨타임을 구할 때 참조한다.
    /// </summary>
    public interface ICooldownCost
    {
        /// <summary>재사용 대기 시간(초).</summary>
        float Cooldown { get; }
    }
}
