using UnityEngine;
using Attribute.Core;
using Attribute.Effect;
using Hit;
using Skill.Core;

namespace Skill.Skills
{
    /// <summary>
    /// 적의 근접 공격 스킬. 발동 시 공격 지점에 근접 판정(MeleeHitController)을 만들고,
    /// 판정이 범위 안 대상에게 효과를 적용한다. CooldownCost 를 같이 붙이면 그 Cooldown 값이 공격 간격이 된다.
    /// </summary>
    public class EnemyMeleeAttack : SkillBase
    {
        #region Serialized Fields
        [Header("References")]
        [Tooltip("생성할 근접 판정 프리팹")]
        [SerializeField] private MeleeHitController _hitPrefab;
        [Tooltip("근접 판정이 생성될 위치")]
        [SerializeField] private Transform _hitPoint;
        [Tooltip("피격 대상에 적용할 효과")]
        [SerializeField] private SOAttributeEffect _hitEffect;
        #endregion

        #region Private Fields
        private IEffectTarget _cursor;
        #endregion

        #region Unity Lifecycle
        protected override void Awake()
        {
            base.Awake();
            _cursor = GetComponentInParent<IEffectTarget>();

            Debug.Assert(_hitPrefab != null, $"[{name}] 근접 판정 프리팹이 연결되지 않았습니다.");
            Debug.Assert(_hitPoint != null, $"[{name}] 판정 생성 지점이 연결되지 않았습니다.");
            Debug.Assert(_hitEffect != null, $"[{name}] 피격 효과가 연결되지 않았습니다.");
        }
        #endregion

        #region Protected Methods
        protected override void Execute()
        {
            if (_hitPrefab && _hitPoint)
            {
                MeleeHitController hit = Instantiate(_hitPrefab, _hitPoint.position, _hitPoint.rotation);
                hit.Initialize(_hitEffect, _cursor);
            }

            base.Execute();
        }
        #endregion
    }
}
