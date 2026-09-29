namespace Skill.Core
{
    /// <summary>
    /// 스킬 사용 조건. 스킬과 같은 GameObject 에 붙이면 SkillBase 가 자동으로 참조한다.
    /// </summary>
    public interface ISkillCondition
    {
        /// <summary>현재 조건을 만족하는지 여부.</summary>
        bool IsCanExecute();
    }
}
