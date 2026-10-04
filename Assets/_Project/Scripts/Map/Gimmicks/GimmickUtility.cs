using UnityEngine;
using Attribute.Core;
using Attribute.Effect;
using PlayerInput;

namespace Map.Gimmicks
{
    /// <summary>
    /// 맵 기믹들이 공통으로 쓰는 플레이어 판별과 Effect 적용.
    /// </summary>
    internal static class GimmickUtility
    {
        #region Public Methods
        /// <summary>
        /// source가 플레이어(또는 그 자식)인지 확인하고 플레이어 루트 컴포넌트를 돌려준다.
        /// </summary>
        /// <param name="source">트리거에 닿은 콜라이더 등</param>
        /// <param name="player">찾은 플레이어</param>
        /// <returns>플레이어면 true</returns>
        public static bool TryGetPlayer(Component source, out PlayerInputComponent player)
        {
            player = source.GetComponentInParent<PlayerInputComponent>();
            return player != null;
        }

        /// <summary>
        /// effects를 순서대로 target에 적용한다. target에 없는 어트리뷰트를 가리키는 Effect는 건너뛴다.
        /// </summary>
        /// <param name="effects">적용할 Effect 목록 (ValueSource는 Float만 지원)</param>
        /// <param name="target">적용 대상</param>
        public static void ApplyEffects(SOAttributeEffect[] effects, IEffectTarget target)
        {
            if ((effects == null) || (target == null)) return;

            foreach (SOAttributeEffect effect in effects)
            {
                if ((effect == null) || !target.IsValidTarget(effect.TargetAttribute)) continue;

                effect.Apply(target);
            }
        }
        #endregion
    }
}
