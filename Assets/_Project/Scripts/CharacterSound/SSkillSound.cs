using System;
using UnityEngine;
using Core.Audio;

namespace CharacterSound
{
    /// <summary>
    /// Inspector 에서 채우는 특수 소리 한 행. 스킬 이름과 단계가 키, 소리가 값이다.
    /// </summary>
    [Serializable]
    public struct SSkillSound
    {
        [Tooltip("실제 스킬 이름 (대소문자 무시)")]
        [SerializeField] private string _skillName;
        [SerializeField] private ESoundPhase _phase;
        [SerializeField] private SOAudioData _audioData;

        public string SkillName => _skillName;
        public ESoundPhase Phase => _phase;
        public SOAudioData AudioData => _audioData;
    }
}
