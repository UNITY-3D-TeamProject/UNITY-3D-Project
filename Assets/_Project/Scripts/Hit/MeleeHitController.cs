using System.Collections.Generic;
using UnityEngine;
using Attribute.Core;
using Attribute.Effect;

namespace Hit
{
    /// <summary>
    /// 근접 공격의 판정 영역. 총알과 달리 이동하지 않는다.
    /// 생성된 자리에서 반지름 안의 대상을 한 번씩 찾아 SOAttributeEffect 를 적용하고, 수명이 지나면 소멸한다.
    /// </summary>
    public class MeleeHitController : MonoBehaviour
    {
        #region Serialized Fields
        [Header("Hit")]
        [Tooltip("피격 대상으로 감지할 레이어")]
        [SerializeField] private LayerMask _hitLayers = Physics.DefaultRaycastLayers;
        [Tooltip("판정 반지름(m)")]
        [SerializeField, Min(0.0f)] private float _radius = 1.0f;
        [Tooltip("판정 후 오브젝트가 남아 있는 시간(초). 판정 자체는 생성 즉시 한 번만 한다.")]
        [SerializeField, Min(0.0f)] private float _lifeSpan = 0.2f;
        #endregion

        #region Unity Lifecycle
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, _radius);
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// 생성 직후 호출한다. 범위 안의 대상에 효과를 적용하고 수명 뒤 소멸한다.
        /// </summary>
        /// <param name="hitEffect">피격 대상에 적용할 효과. null 이면 효과 없이 소멸만 한다.</param>
        /// <param name="cursor">공격자의 IEffectTarget. 효과의 ValueSource 가 Attribute 일 때 필요하며, 공격자 자신은 피격 대상에서 제외된다.</param>
        public void Initialize(SOAttributeEffect hitEffect, IEffectTarget cursor = null)
        {
            ApplyHitEffect(hitEffect, cursor);
            Destroy(gameObject, _lifeSpan);
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// 범위 안 콜라이더에서 IEffectTarget 을 찾아 효과를 적용한다.
        /// 한 캐릭터에 콜라이더가 여러 개여도 한 번만 적용한다.
        /// </summary>
        private void ApplyHitEffect(SOAttributeEffect hitEffect, IEffectTarget cursor)
        {
            if (hitEffect == null) return;

            Collider[] colliders = Physics.OverlapSphere(
                transform.position,
                _radius,
                _hitLayers,
                QueryTriggerInteraction.Ignore);

            HashSet<IEffectTarget> appliedTargets = new();
            foreach (Collider other in colliders)
            {
                IEffectTarget target = other.GetComponentInParent<IEffectTarget>();
                if (target == null || target == cursor) continue;
                if (!target.IsValidTarget(hitEffect.TargetAttribute)) continue;
                if (!appliedTargets.Add(target)) continue;

                hitEffect.Apply(target, new SEffectContext(cursor));
            }
        }
        #endregion
    }
}
