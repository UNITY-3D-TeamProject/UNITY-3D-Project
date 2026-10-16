using System;
using UnityEngine;
using CharacterAnimation;

namespace Mediator.SubMediators
{
    /// <summary>
    /// 다른 Mediator 에서 온 AI 상태를 EnemyAnimator(와 선택 컴포넌트)로 전달하는 접착 컴포넌트.
    /// </summary>
    public class EnemyAnimationMediator : MediatorBase
    {
        #region Constants
        private const float MIN_SQR_MAGNITUDE = 0.0001f;
        #endregion

        #region Serialized Fields
        [Header("References")]
        [SerializeField] private EnemyAnimator _enemyAnimator;
        [Tooltip("점프 애니메이션이 필요한 객체(보행자 등)만 연결한다. 비어 있으면 점프 알림을 무시한다.")]
        [SerializeField] private JumpAnimator _jumpAnimator;
        [Header("Settings")]
        [Tooltip("EnemyAnimator.SetMoveSpeed 로 연결할 이동속도 어트리뷰트 이름")]
        [SerializeField] private string _speedValueKey;
        #endregion

        #region Events
        /// <summary>EnemyAnimator 의 사망 애니메이션이 끝났을 때 발생한다.</summary>
        public event Action OnDeathAnimationEnded;
        #endregion

        #region Unity Lifecycle
        protected override void Awake()
        {
            base.Awake();
            ResolveComponent(ref _enemyAnimator);
        }

        protected override void OnEnable()
        {
            if (_enemyAnimator) _enemyAnimator.OnDeathAnimationEnded += OnDeathAnimationEndedCallback;
            base.OnEnable();
        }

        private void OnDisable()
        {
            if (_enemyAnimator) _enemyAnimator.OnDeathAnimationEnded -= OnDeathAnimationEndedCallback;
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// 이동 방향을 받아 이동 중 여부를 EnemyAnimator 에 전달한다.
        /// </summary>
        /// <param name="direction">월드 이동 방향</param>
        public void SetMoveDirection(Vector3 direction)
        {
            if (!_enemyAnimator) return;

            _enemyAnimator.SetMoving(direction.sqrMagnitude > MIN_SQR_MAGNITUDE);
        }

        /// <summary>
        /// 점프 시작을 JumpAnimator 로 전달한다.
        /// </summary>
        public void NotifyJumpStarted()
        {
            if (!_jumpAnimator) return;

            _jumpAnimator.PlayJump();
        }

        /// <summary>
        /// 착지를 JumpAnimator 로 전달한다.
        /// </summary>
        public void NotifyJumpEnded()
        {
            if (!_jumpAnimator) return;

            _jumpAnimator.PlayLand();
        }

        /// <summary>
        /// 스킬 사용을 EnemyAnimator 로 전달해 스킬별 모션을 재생한다.
        /// </summary>
        /// <param name="skillName">사용한 스킬 이름</param>
        /// <param name="cooldown">쿨타임 (사용하지 않음)</param>
        public void NotifySkillUsed(string skillName, float cooldown)
        {
            if (!_enemyAnimator) return;

            _enemyAnimator.PlaySkill(skillName);
        }

        /// <summary>
        /// 스킬 정지를 EnemyAnimator 로 전달한다. 유지형 스킬 모션이 풀린다.
        /// </summary>
        /// <param name="skillName">정지한 스킬 이름</param>
        public void NotifySkillStopped(string skillName)
        {
            if (!_enemyAnimator) return;

            _enemyAnimator.StopSkill(skillName);
        }

        /// <summary>
        /// 피격을 EnemyAnimator 로 전달해 피격 애니메이션을 재생한다.
        /// </summary>
        /// <param name="hitSource">피격 방향 기준 월드 위치 (사용하지 않음)</param>
        public void NotifyHit(Vector3? hitSource)
        {
            if (!_enemyAnimator) return;

            _enemyAnimator.PlayHit();
        }

        /// <summary>
        /// 사망을 EnemyAnimator 로 전달해 사망 애니메이션을 재생한다.
        /// EnemyAnimator 가 없으면 바로 사망 종료를 알린다.
        /// </summary>
        public void NotifyDeath()
        {
            if (!_enemyAnimator)
            {
                OnDeathAnimationEnded?.Invoke();
                return;
            }

            _enemyAnimator.PlayDeath();
        }
        #endregion

        #region Protected Methods
        protected override void InitAttributeCallback()
        {
            AttributeCallback.TryAdd(_speedValueKey, data =>
            {
                if (_enemyAnimator) _enemyAnimator.SetMoveSpeed(data.NewValue);
            });
        }

        protected override void InitValue()
        {
        }
        #endregion

        #region Private Methods
        private void OnDeathAnimationEndedCallback()
        {
            OnDeathAnimationEnded?.Invoke();
        }
        #endregion
    }
}
