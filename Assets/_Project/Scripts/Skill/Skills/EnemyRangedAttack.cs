using System.Collections;
using UnityEngine;
using AI;
using Attribute.Core;
using Attribute.Effect;
using Combat;
using CharacterSound;
using Core.Effect;
using Core.ObjectPool;
using Projectile;
using Skill.Core;

namespace Skill.Skills
{
    /// <summary>
    /// 적의 원거리 공격 스킬. 발동 시 발사 지점에서 발사 지점 정면으로 총알(BulletController)을 만든다.
    /// 총알은 ObjectPoolManager 에 등록된 프리팹이면 풀에서 꺼내고, 매니저가 없는 씬이면 Instantiate 한다.
    /// CooldownCost 를 같이 붙이면 그 Cooldown 값이 사격 간격이 된다.
    /// 발동 시 모션을 재생하고 Fire Delay 뒤에 발동 순간의 방향으로 총알과 총구 이펙트를 낸다. 그 사이 사망하면 쏘지 않는다.
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
        [Tooltip("총구에서 재생할 발사 이펙트")]
        [SerializeField] private SOEffectData _muzzleVfx;
        [Tooltip("발사 이펙트를 표시할 총구들. 비어 있으면 총알 발사 지점을 사용한다")]
        [SerializeField] private Transform[] _muzzlePoints;

        [Header("Timing")]
        [Tooltip("발동 후 발사까지 시간(초). 공격 모션에서 총을 쏘는 시점에 맞춘다. 0 이면 발동 즉시 쏜다.")]
        [SerializeField, Min(0.0f)] private float _fireDelay;
        #endregion

        #region Private Fields
        private IEffectTarget _cursor;
        private CharacterCombat _combat;
        private SoundController _soundController;
        #endregion

        #region Unity Lifecycle
        protected override void Awake()
        {
            base.Awake();
            _cursor = GetComponentInParent<IEffectTarget>();
            // CharacterCombat 은 스킬의 부모가 아니라 캐릭터 루트 아래 다른 자식에 있다
            AIController controller = GetComponentInParent<AIController>();
            if (controller)
            {
                _combat = controller.GetComponentInChildren<CharacterCombat>();
                _soundController = controller.GetComponentInChildren<SoundController>();
            }

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
                Quaternion rotation = _firePoint.rotation;
                if (_fireDelay > 0.0f) StartCoroutine(CoFire(rotation));
                else Fire(rotation);
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

            BulletController bullet = SpawnBullet(rotation);
            bullet.Initialize(rotation * Vector3.forward, _hitEffect, _cursor);
            PlayMuzzleVfx(rotation);
            if (_soundController) _soundController.PlaySkillSound(SkillName, ESoundPhase.Shot, _firePoint.position);
        }

        /// <summary>
        /// 발사 지점에 총알을 만든다. 풀에서 꺼내고, 매니저가 없거나 풀에 등록되지 않은 프리팹이면 Instantiate 로 대신한다.
        /// </summary>
        private BulletController SpawnBullet(Quaternion rotation)
        {
            ObjectPoolManager pool = ObjectPoolManager.Instance;
            if (pool != null)
            {
                // 등록되지 않은 프리팹이면 매니저가 에러를 남기고 null 을 돌려준다
                GameObject pooled = pool.Get(_bulletPrefab.gameObject, _firePoint.position, rotation);
                if (pooled && pooled.TryGetComponent(out BulletController pooledBullet))
                    return pooledBullet;
            }

            return Instantiate(_bulletPrefab, _firePoint.position, rotation);
        }

        /// <summary>총알이 나가는 순간 총구 이펙트를 재생한다.</summary>
        private void PlayMuzzleVfx(Quaternion rotation)
        {
            if (_muzzleVfx && _muzzleVfx.Prefab)
            {
                Quaternion effectRotation = rotation * _muzzleVfx.Prefab.transform.localRotation;
                if ((_muzzlePoints == null) || (_muzzlePoints.Length == 0))
                {
                    EffectManager.Instance?.Play(_muzzleVfx, _firePoint.position, effectRotation);
                }
                else
                {
                    foreach (Transform muzzlePoint in _muzzlePoints)
                    {
                        if (muzzlePoint)
                        {
                            EffectManager.Instance?.Play(
                                _muzzleVfx,
                                muzzlePoint.position,
                                muzzlePoint.rotation * _muzzleVfx.Prefab.transform.localRotation);
                        }
                    }
                }
            }
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
