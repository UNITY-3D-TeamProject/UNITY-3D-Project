using Unity.Behavior;

namespace AI.Boss.BT
{
    /// <summary>
    /// B 가 고른 공격을 BT 의 Switch 로 가르기 위한 블랙보드 열거형. Logic 의 EBossBAttack 과 값과 순서가 같다.
    /// Logic 은 Unity 를 참조하지 않아 BlackboardEnum 을 붙일 수 없으므로 BT 쪽에 따로 둔다.
    /// </summary>
    [BlackboardEnum]
    public enum EBossBTAttack
    {
        None,
        Combo,
        Charge,
        Leap,
    }

    /// <summary>
    /// 블랙보드에 담는 보스전 단계. Logic 의 EBossStage 와 값과 순서가 같다. 진행이 (EBossBTStage)(int)stage 로 쓴다.
    /// </summary>
    [BlackboardEnum]
    public enum EBossBTStage
    {
        Stage1_1,
        Stage1_2,
        Final,
        Defeated,
    }
}
