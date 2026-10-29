using System.Collections;
using UnityEngine;
using AI;
using Attribute.Core;
using Attribute.Effect;
using Combat;
using CharacterSound;
using Hit;
using Skill.Core;

namespace Skill.Skills
{
    /// <summary>
    /// 적의 근접 공격 스킬. 발동 시 공격 지점에 근접 판정(MeleeHitController)을 만들고,
    /// 판정이 범위 안 대상에게 효과를 적용한다. CooldownCost 를 같이 붙이면 그 Cooldown 값이 공격 간격이 된다.
    /// Hold Duration 이 0 보다 크면 발동 순간 대상 쪽을 바라본 채 그 시간 동안 몸을 붙잡아(AIController.Hold)
    /// 공격 모션 도중 추격으로 미끄러지지 않게 한다.
    /// Hit Delay 가 0 보다 크면 모션은 바로 재생하고 판정만 그 시간 뒤에 만든다. 그 사이 사망하면 판정을 만들지 않는다.
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

        [Header("Timing")]
        [Tooltip("발동 후 판정까지 시간(초). 공격 모션에서 타격이 닿는 시점에 맞춘다. 0 이면 발동 즉시 판정한다.")]
        [SerializeField, Min(0.0f)] private float _hitDelay;

        [Header("Hold")]
        [Tooltip("발동 후 제자리에 붙잡아 둘 시간(초). 공격 모션 길이에 맞춘다. 0 이면 붙잡지 않는다. 붙잡힌 동안에는 경직하지 않는다.")]
        [SerializeField, Min(0.0f)] private float _holdDuration;
        #endregion

        #region Private Fields
        private IEffectTarget _cursor;
        private AIController _controller;
        private CharacterCombat _combat;
        private SoundController _soundController;
        #endregion

        #region Unity Lifecycle
        protected override void Awake()
        {
            base.Awake();
            _cursor = GetComponentInParent<IEffectTarget>();
            _controller = GetComponentInParent<AIController>();
            // CharacterCombat 은 스킬의 부모가 아니라 캐릭터 루트 아래 다른 자식에 있다
            if (_controller)
            {
                _combat = _controller.GetComponentInChildren<CharacterCombat>();
                _soundController = _controller.GetComponentInChildren<SoundController>();
            }

            Debug.Assert(_hitPrefab != null, $"[{name}] 근접 판정 프리팹이 연결되지 않았습니다.");
            Debug.Assert(_hitPoint != null, $"[{name}] 판정 생성 지점이 연결되지 않았습니다.");
            Debug.Assert(_hitEffect != null, $"[{name}] 피격 효과가 연결되지 않았습니다.");
        }

        private void OnDisable()
        {
            // 풀로 돌아가는 동안 대기 중이던 판정이 다음 생성에 남지 않게 한다
            StopAllCoroutines();
        }

        private void OnDrawGizmosSelected()
        {
            // 판정 오브젝트는 생성 직후 사라지므로, 생성될 자리와 반지름을 여기서 미리 보여준다
            if (!_hitPrefab || !_hitPoint) return;

            Gizmos.color = new Color(0.6f, 0.2f, 1.0f);
            Gizmos.DrawWireSphere(_hitPoint.position, _hitPrefab.Radius);
        }
        #endregion

        #region Protected Methods
        protected override void Execute()
        {
            if (_hitDelay > 0.0f) StartCoroutine(CoSpawnHit());
            else SpawnHit();

            if ((_holdDuration > 0.0f) && _controller && _controller.HasVisibleTarget)
            {
                _controller.Hold(_controller.VisibleTarget.position - _controller.Position, _holdDuration);
            }

            base.Execute();
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// 판정 생성 지점에 근접 판정을 만든다. 공격자가 사망했으면 만들지 않는다.
        /// </summary>
        private void SpawnHit()
        {
            if (!_hitPrefab || !_hitPoint) return;
            if (_combat && _combat.IsDead) return;

            MeleeHitController hit = Instantiate(_hitPrefab, _hitPoint.position, _hitPoint.rotation);
            hit.Initialize(_hitEffect, _cursor);
            if (_soundController) _soundController.PlaySkillSound(SkillName, ESoundPhase.Impact, _hitPoint.position);
        }
        #endregion

        #region Coroutines
        private IEnumerator CoSpawnHit()
        {
            yield return new WaitForSeconds(_hitDelay);

            SpawnHit();
        }
        #endregion
    }
}
