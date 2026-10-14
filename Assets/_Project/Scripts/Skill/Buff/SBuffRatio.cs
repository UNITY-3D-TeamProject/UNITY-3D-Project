using System;
using UnityEngine;

namespace Skill.Buff
{
    /// <summary>배율을 적용할 대상.</summary>
    public enum EBuffTarget
    {
        MoveSpeed,
        JumpPower,
        Gravity,
    }

    /// <summary>
    /// 버프 대상과 배율. 스킬이 보내면 CharacterMediator 가 대상에 맞는 중재자로 전달한다.
    /// </summary>
    [Serializable]
    public struct SBuffRatio
    {
        [Tooltip("배율을 적용할 대상")]
        public EBuffTarget Target;
        [Tooltip("기본값에 곱할 배율. 1 이면 변화 없음")]
        public float Ratio;

        public SBuffRatio(EBuffTarget target, float ratio)
        {
            Target = target;
            Ratio = ratio;
        }
    }
}
