using System;

namespace CharacterSound
{
    /// <summary>
    /// 스킬 이름(대소문자 무시)과 단계로 이루어진 소리 키. 매핑 조회와 루프 상태가 모두 같은 비교 규칙을 쓰도록 한다.
    /// </summary>
    public readonly struct SSoundKey : IEquatable<SSoundKey>
    {
        /// <summary>공통 Footstep 칸의 루프를 가리키는 예약 키. 설정 행은 빈 스킬 이름을 허용하지 않으므로 겹치지 않는다.</summary>
        public static readonly SSoundKey Footstep = new SSoundKey(string.Empty, ESoundPhase.Move);

        private readonly string _skillName;

        public string SkillName => _skillName ?? string.Empty;
        public ESoundPhase Phase { get; }

        public SSoundKey(string skillName, ESoundPhase phase)
        {
            _skillName = skillName ?? string.Empty;
            Phase = phase;
        }

        public bool Equals(SSoundKey other)
        {
            return (Phase == other.Phase) &&
                   string.Equals(SkillName, other.SkillName, StringComparison.OrdinalIgnoreCase);
        }

        public override bool Equals(object obj)
        {
            return (obj is SSoundKey other) && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(StringComparer.OrdinalIgnoreCase.GetHashCode(SkillName), Phase);
        }

        public static bool operator ==(SSoundKey left, SSoundKey right) => left.Equals(right);

        public static bool operator !=(SSoundKey left, SSoundKey right) => !left.Equals(right);
    }
}
