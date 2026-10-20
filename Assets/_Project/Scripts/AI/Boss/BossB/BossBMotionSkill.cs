using Skill.Core;

namespace AI.Boss.BossB
{
    /// <summary>
    /// 모션 알림 전용 스킬. 발동해도 하는 동작은 없고, 발동을 알리는 것만으로
    /// 스킬 이름에 매핑된 애니메이션(EnemyAnimator)이 재생된다. 다른 B 스킬이 구간 모션을 바꿀 때 요청한다.
    /// </summary>
    public class BossBMotionSkill : SkillBase
    {
    }
}
