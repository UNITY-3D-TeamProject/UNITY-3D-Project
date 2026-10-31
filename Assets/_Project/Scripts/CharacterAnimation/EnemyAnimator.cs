using System;
using UnityEngine;

namespace CharacterAnimation
{
    /// <summary>
    /// AI(적·군중) Animator 파라미터를 설정하는 컴포넌트.
    /// 공통 동작(이동·피격·사망)과, 스킬 이름 → 파라미터 매핑으로 처리하는 스킬별 모션을 맡는다.
    /// 객체 전용 로직(예: 점프)은 별도 컴포넌트로 둔다.
    /// </summary>
    public class EnemyAnimator : MonoBehaviour
    {
        #region Nested Types
        /// <summary>스킬 이름 하나에 대응하는 Animator 파라미터.</summary>
        [Serializable]
        public struct SSkillAnimation
        {
            [Tooltip("SkillMediator 가 알리는 스킬 이름 (대소문자 무시)")]
            public string SkillName;
            [Tooltip("재생할 Animator 파라미터 이름")]
            public string Parameter;
            [Tooltip("true 면 Bool 파라미터를 스킬 사용 중 켜 둔다 (예: 예비동작). false 면 Trigger 로 1회 재생한다.")]
            public bool IsHold;
        }
        #endregion

        #region Serialized Fields
        [Header("Locomotion")]
        [Tooltip("이동 애니메이션이 1배속으로 재생될 때의 이동속도. 현재 이동속도 / 이 값이 Move 재생 배율이 된다.")]
        [SerializeField, Min(0.01f)] private float _referenceMoveSpeed = 3.0f;

        [Tooltip("이동 중 이 거리(m)만큼 움직일 때마다 발 디딤을 알린다. 0 이면 알리지 않는다.")]
        [SerializeField, Min(0.0f)] private float _footstepStride = 0.0f;

        [Header("Skill")]
        [Tooltip("스킬 이름별 모션. 객체마다 프리팹에서 항목을 채운다.")]
        [SerializeField] private SSkillAnimation[] _skillAnimations =
        {
            new SSkillAnimation { SkillName = "Attack", Parameter = "Attack", IsHold = false },
        };

        [Header("Death")]
        [Tooltip("사망 상태(Death)를 이 횟수만큼 재생하면 사망 애니메이션 종료를 알린다.")]
        [SerializeField, Min(0.0f)] private float _deathPlayCount = 1.0f;
        #endregion

        #region Private Fields
        private Animator _animator;
        private int[] _skillParameterHashes;
        private bool _isDead;
        private bool _hasDeathEnded;
        private bool _isMoving;
        private Vector3 _lastPosition;
        private float _stepDistance;
        #endregion

        #region Events
        /// <summary>사망 애니메이션이 끝났을 때 한 번 발생한다.</summary>
        public event Action OnDeathAnimationEnded;

        /// <summary>이동 중 보폭만큼 움직일 때마다 발생한다. 보폭이 0 이면 발생하지 않는다.</summary>
        public event Action OnFootstep;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            // GetComponentInParent는 자기 자신을 먼저 검사하므로 루트/자식 배치를 모두 커버한다
            _animator = GetComponentInParent<Animator>();

            if (_animator == null)
            {
                Debug.LogError($"[{name}] Animator를 찾지 못했습니다. 모델 루트에 추가하세요.", this);
                enabled = false;
                return;
            }

            CacheSkillParameters();
        }

        private void OnEnable()
        {
            // 풀에서 다시 꺼낼 때 이전 사망 상태가 남지 않게 한다
            _isDead = false;
            _hasDeathEnded = false;
            _lastPosition = transform.position;
            _stepDistance = 0.0f;
        }

        private void Update()
        {
            UpdateDeath();
            UpdateFootstep();
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// 이동 중 여부를 설정한다. true 면 Move, false 면 Idle 로 전환된다.
        /// </summary>
        /// <param name="isMoving">이동 중이면 true</param>
        public void SetMoving(bool isMoving)
        {
            if (!enabled || _isDead) return;

            _isMoving = isMoving;
            _animator.SetBool(AnimHash.IsMoving, isMoving);
        }

        /// <summary>
        /// 현재 이동속도를 설정한다. 기준 속도 대비 비율을 MoveAnimSpeed 에 넣어 Move 재생 속도를 맞춘다.
        /// </summary>
        /// <param name="moveSpeed">현재 이동속도</param>
        public void SetMoveSpeed(float moveSpeed)
        {
            if (!enabled) return;

            _animator.SetFloat(AnimHash.MoveAnimSpeed, moveSpeed / _referenceMoveSpeed);
        }

        /// <summary>
        /// 스킬 사용을 알린다. 매핑된 스킬이면 Trigger 를 켜거나 Bool 을 켠다. 매핑이 없으면 무시한다.
        /// </summary>
        /// <param name="skillName">사용한 스킬 이름</param>
        public void PlaySkill(string skillName)
        {
            if (!enabled || _isDead) return;
            if (!TryFindSkill(skillName, out int index)) return;

            SSkillAnimation skill = _skillAnimations[index];
            int hash = _skillParameterHashes[index];
            if (skill.IsHold) _animator.SetBool(hash, true);
            else _animator.SetTrigger(hash);
        }

        /// <summary>
        /// 스킬 정지를 알린다. 유지형(IsHold) 스킬이면 Bool 을 끈다.
        /// </summary>
        /// <param name="skillName">정지한 스킬 이름</param>
        public void StopSkill(string skillName)
        {
            if (!enabled) return;
            if (!TryFindSkill(skillName, out int index)) return;
            if (!_skillAnimations[index].IsHold) return;

            _animator.SetBool(_skillParameterHashes[index], false);
        }

        /// <summary>
        /// 피격을 알린다. 피격 애니메이션을 재생한다.
        /// </summary>
        public void PlayHit()
        {
            if (!enabled || _isDead) return;

            _animator.SetTrigger(AnimHash.Hit);
        }

        /// <summary>
        /// 사망을 알린다. 유지형 스킬 자세를 내리고 사망 애니메이션을 재생한다. 이후 알림은 무시한다.
        /// Animator 가 없으면 바로 종료를 알린다.
        /// </summary>
        public void PlayDeath()
        {
            if (_isDead) return;

            _isDead = true;
            if (!enabled)
            {
                EndDeath();
                return;
            }

            ReleaseHoldSkills();
            _animator.SetBool(AnimHash.IsMoving, false);
            _animator.ResetTrigger(AnimHash.Hit);
            _animator.SetTrigger(AnimHash.Death);
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// 매핑된 파라미터 이름을 해시로 바꿔 캐싱한다.
        /// </summary>
        private void CacheSkillParameters()
        {
            _skillParameterHashes = new int[_skillAnimations.Length];
            for (int i = 0; i < _skillAnimations.Length; i++)
            {
                _skillParameterHashes[i] = Animator.StringToHash(_skillAnimations[i].Parameter);
            }
        }

        /// <summary>
        /// 스킬 이름에 해당하는 매핑 항목의 인덱스를 찾는다.
        /// </summary>
        /// <param name="skillName">스킬 이름</param>
        /// <param name="index">찾은 항목 인덱스</param>
        /// <returns>매핑이 있으면 true</returns>
        private bool TryFindSkill(string skillName, out int index)
        {
            for (int i = 0; i < _skillAnimations.Length; i++)
            {
                if (!string.Equals(_skillAnimations[i].SkillName, skillName, StringComparison.OrdinalIgnoreCase)) continue;

                index = i;
                return true;
            }

            index = -1;
            return false;
        }

        /// <summary>
        /// 켜져 있는 유지형 스킬 Bool 을 모두 끈다.
        /// </summary>
        private void ReleaseHoldSkills()
        {
            for (int i = 0; i < _skillAnimations.Length; i++)
            {
                if (_skillAnimations[i].IsHold) _animator.SetBool(_skillParameterHashes[i], false);
            }
        }

        /// <summary>
        /// 사망 상태(전환 중 아님)가 정해진 횟수만큼 재생되면 사망 애니메이션 종료를 한 번 알린다.
        /// </summary>
        private void UpdateDeath()
        {
            if (!_isDead || _hasDeathEnded) return;
            if (_animator.IsInTransition(0)) return;

            AnimatorStateInfo stateInfo = _animator.GetCurrentAnimatorStateInfo(0);
            if (stateInfo.shortNameHash != AnimHash.DeathState) return;
            if (stateInfo.normalizedTime < _deathPlayCount) return;

            EndDeath();
        }

        /// <summary>
        /// 이동 중 실제로 움직인 수평 거리를 모아 보폭마다 발 디딤을 알린다. 이동을 멈추면 모은 거리를 비운다.
        /// </summary>
        private void UpdateFootstep()
        {
            Vector3 position = transform.position;
            Vector3 delta = position - _lastPosition;
            _lastPosition = position;
            delta.y = 0.0f;

            bool canStep = (_footstepStride > 0.0f) && _isMoving && !_isDead;
            if (!canStep)
            {
                _stepDistance = 0.0f;
                return;
            }

            _stepDistance += delta.magnitude;
            if (_stepDistance < _footstepStride) return;

            _stepDistance %= _footstepStride;
            OnFootstep?.Invoke();
        }

        private void EndDeath()
        {
            if (_hasDeathEnded) return;

            _hasDeathEnded = true;
            OnDeathAnimationEnded?.Invoke();
        }
        #endregion

        #region Nested Types
        private static class AnimHash
        {
            public static readonly int IsMoving = Animator.StringToHash("IsMoving");
            public static readonly int MoveAnimSpeed = Animator.StringToHash("MoveAnimSpeed");
            public static readonly int Hit = Animator.StringToHash("Hit");
            public static readonly int Death = Animator.StringToHash("Death");
            public static readonly int DeathState = Animator.StringToHash("Death");
        }
        #endregion
    }
}
