using UnityEngine;
using Attribute.Core;
using Attribute.Effect;
using Projectile;
using Skill.Core;

namespace Skill.Skills
{
    /// <summary>
    /// 적의 원거리 공격 스킬. 발동 시 발사 지점에서 발사 지점 정면으로 총알(BulletController)을 만든다.
    /// CooldownCost 를 같이 붙이면 그 Cooldown 값이 사격 간격이 된다.
    /// </summary>
    public class EnemyRangedAttack : SkillBase
    {
        #region Serialized Fields
        [Header("References")]
        [Tooltip("생성할 총알 프리팹")]
        [SerializeField] private BulletController _bulletPrefab;
        [Tooltip("총알이 생성될 위치. 이 Transform 의 정면으로 발사한다.")]
        [SerializeField] private Transform _firePoint;
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

            Debug.Assert(_bulletPrefab != null, $"[{name}] 총알 프리팹이 연결되지 않았습니다.");
            Debug.Assert(_firePoint != null, $"[{name}] 발사 지점이 연결되지 않았습니다.");
            Debug.Assert(_hitEffect != null, $"[{name}] 피격 효과가 연결되지 않았습니다.");
        }
        #endregion

        #region Protected Methods
        protected override void Execute()
        {
            if (_bulletPrefab && _firePoint)
            {
                BulletController bullet = Instantiate(_bulletPrefab, _firePoint.position, _firePoint.rotation);
                bullet.Initialize(_firePoint.forward, _hitEffect, _cursor);
            }

            base.Execute();
        }
        #endregion
    }
}
