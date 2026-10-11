namespace Skill.Core
{
    /// <summary>
    /// 지불량에 배수를 적용할 수 있는 코스트. ISkillCost 와 함께 구현하며, 배수가 필요한 코스트만 구현한다.
    /// </summary>
    public interface IScalableCost
    {
        /// <summary>다음 지불에 적용할 배수. SkillBase 가 지불 후 1로 되돌린다.</summary>
        void SetScale(float scale);
    }
}
