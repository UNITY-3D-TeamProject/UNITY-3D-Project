using UnityEngine;
using CharacterSound;
using Skill.Core;

namespace AI.Boss
{
    /// <summary>보스가 최종 사망했을 때 사망 소리를 재생한다.</summary>
    public class BossDeathSkill : SkillBase
    {
        private SoundController _soundController;

        protected override void Awake()
        {
            base.Awake();
            _soundController = GetComponentInChildren<SoundController>();
        }

        protected override void Execute()
        {
            // 사망 상태에서는 일반 스킬 소리 중재가 차단되므로 직접 전달한다.
            if (_soundController) _soundController.PlaySkillSound(SkillName, ESoundPhase.Execute, null);
        }
    }
}
