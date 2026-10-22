using System.Collections;
using UnityEngine;
using AI;
using Attribute.Core;
using Attribute.Effect;
using Combat;
using Projectile;
using Skill.Core;

namespace Skill.Skills
{
    /// <summary>
    /// 적의 원거리 공격 스킬. 발동 시 발사 지점에서 발사 지점 정면으로 총알(BulletController)을 만든다.
    /// CooldownCost 를 같이 붙이면 그 Cooldown 값이 사격 간격이 된다.
    /// Fire Delay 가 0 보다 크면 모션은 바로 재생하고 총알만 그 시간 뒤에 발동 순간의 방향으로 쏜다. 그 사이 사망하면 쏘지 않는다.
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

        [Header("Timing")]
        [Tooltip("발동 후 발사까지 시간(초). 공격 모션에서 총을 쏘는 시점에 맞춘다. 0 이면 발동 즉시 쏜다.")]
        [SerializeField, Min(0.0f)] private float _fireDelay;
        #endregion

        #region Private Fields
        private IEffectTarget _cursor;
        private CharacterCombat _combat;
        #endregion

        #region Unity Lifecycle
        protected override void Awake()
        {
            base.Awake();
            _cursor = GetComponentInParent<IEffectTarget>();
            // CharacterCombat 은 스킬의 부모가 아니라 캐릭터 루트 아래 다른 자식에 있다
            AIController controller = GetComponentInParent<AIController>();
            if (controller) _combat = controller.GetComponentInChildren<CharacterCombat>();

            Debug.Assert(_bulletPrefab != null, $"[{name}] 총알 프리팹이 연결되지 않았습니다.");
            Debug.Assert(_firePoint != null, $"[{name}] 발사 지점이 연결되지 않았습니다.");
            Debug.Assert(_hitEffect != null, $"[{name}] 피격 효과가 연결되지 않았습니다.");
        }

        private void OnDisable()
        {
            // 풀로 돌아가는 동안 대기 중이던 발사가 다음 생성에 남지 않게 한다
            StopAllCoroutines();
        }
        #endregion

        #region Protected Methods
        protected override void Execute()
        {
            if (_firePoint)
            {
                if (_fireDelay > 0.0f) StartCoroutine(CoFire(_firePoint.rotation));
                else Fire(_firePoint.rotation);
            }

            base.Execute();
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// 발사 지점에서 지정한 방향으로 총알을 만든다. 공격자가 사망했으면 쏘지 않는다.
        /// </summary>
        /// <param name="rotation">총알 방향. 발동 순간의 발사 지점 회전이다.</param>
        private void Fire(Quaternion rotation)
        {
            if (!_bulletPrefab || !_firePoint) return;
            if (_combat && _combat.IsDead) return;

            BulletController bullet = Instantiate(_bulletPrefab, _firePoint.position, rotation);
            bullet.Initialize(rotation * Vector3.forward, _hitEffect, _cursor);
        }
        #endregion

        #region Coroutines
        private IEnumerator CoFire(Quaternion rotation)
        {
            yield return new WaitForSeconds(_fireDelay);

            Fire(rotation);
        }
        #endregion
    }
}
