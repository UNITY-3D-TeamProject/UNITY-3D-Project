using UnityEngine;
using Attribute.Core;
using Attribute.Effect;
using Core.Audio;
using Core.Effect;
using PlayerInput;

namespace Map.Gimmicks
{
    /// <summary>
    /// 맵 기믹들이 공통으로 쓰는 플레이어 판별, Effect 적용, 효과음·VFX 재생.
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

        /// <summary>
        /// 3D 효과음을 position에서 한 번 낸다. 소리가 비어 있거나 씬에 AudioManager가 없으면 무시한다.
        /// </summary>
        /// <param name="sound">재생할 소리 (비우면 무음)</param>
        /// <param name="position">소리가 나는 위치</param>
        public static void PlaySfx(SOAudioData sound, Vector3 position)
        {
            if ((sound == null) || (AudioManager.Instance == null)) return;

            AudioManager.Instance.PlaySfx(sound, position);
        }

        /// <summary>
        /// VFX를 position에서 한 번 재생한다. 효과가 비어 있거나 씬에 EffectManager가 없으면 무시한다.
        /// </summary>
        /// <param name="effect">재생할 효과 (비우면 없음)</param>
        /// <param name="position">효과가 나는 위치</param>
        public static void PlayVfx(SOEffectData effect, Vector3 position)
        {
            if ((effect == null) || (EffectManager.Instance == null)) return;

            EffectManager.Instance.Play(effect, position, Quaternion.identity);
        }

        /// <summary>
        /// 위치 없는 UI 소리로 한 번 낸다. 대화·일시정지로 3D 소리가 멈춰도 들린다. (E 상호작용음)
        /// </summary>
        /// <param name="sound">재생할 소리 (비우면 무음)</param>
        public static void PlayUi(SOAudioData sound)
        {
            if ((sound == null) || (AudioManager.Instance == null)) return;

            AudioManager.Instance.PlayUi(sound);
        }

        /// <summary>
        /// 3D 반복음을 target을 따라가며 낸다. 이미 재생 중인 핸들이면 그대로 둔다.
        /// </summary>
        /// <param name="sound">반복할 소리 (비우면 무음)</param>
        /// <param name="target">따라갈 대상</param>
        /// <param name="handle">재생 중인 소리 핸들. 끝낼 때 StopSfxLoop에 넘긴다</param>
        public static void PlaySfxLoop(SOAudioData sound, Transform target, ref SAudioHandle handle)
        {
            if (handle.IsValid) return;
            if ((sound == null) || (AudioManager.Instance == null)) return;

            handle = AudioManager.Instance.PlaySfxLoop(sound, target);
        }

        /// <summary>
        /// PlaySfxLoop로 시작한 반복음을 끊고 핸들을 비운다.
        /// </summary>
        /// <param name="handle">끊을 소리 핸들</param>
        public static void StopSfxLoop(ref SAudioHandle handle)
        {
            if (!handle.IsValid) return;

            AudioManager.Instance?.StopSfx(handle);
            handle = default;
        }
        #endregion
    }
}
