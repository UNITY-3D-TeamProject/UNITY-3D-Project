using Attribute.Core;
using Attribute.Effect;

namespace Combat
{
    public readonly struct SHitInfo
    {
        public readonly SOAttributeEffect Effect;
        public readonly IEffectTarget Attacker;

        public SHitInfo(SOAttributeEffect effect, IEffectTarget attacker)
        {
            Effect = effect;
            Attacker = attacker;
        }
    }
}
