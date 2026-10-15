using UnityEngine;

namespace Pet
{
    /// <summary>
    /// 펫 Animator 에 이동 속도, 점프, 피격, 특수 Idle, 사망을 전달한다.
    /// 점프는 클립 하나를 구간별 배속으로 재생해 캐릭터의 점프 시작·체공·착지 길이에 맞춘다.
    /// </summary>
    public class PetAnimator : MonoBehaviour
    {
        #region Serialized Fields
        [Header("Move")]
        [Tooltip("달리기 클립의 원래 이동 속도(m/s). 이보다 빠르면 이동 애니메이션을 그 비율만큼 빠르게 재생한다")]
        [SerializeField, Min(0.01f)] private float _runAnimSpeed = 0.8f;
        [Tooltip("이동 애니메이션 재생 배속 상한")]
        [SerializeField, Min(1.0f)] private float _maxMoveAnimSpeed = 3.0f;

        [Header("Jump")]
        [Tooltip("Jump 상태에서 재생하는 클립. 구간별 배속 계산에 길이를 쓴다")]
        [SerializeField] private AnimationClip _jumpClip;
        [Tooltip("점프 시작부터 최고점까지 재생할 시간(초). 캐릭터 Jump_Start 가 체공으로 넘어가는 시간")]
        [SerializeField, Min(0.01f)] private float _jumpStartDuration = 0.45f;
        [Tooltip("착지 구간(최고점 이후)을 재생할 시간(초). 캐릭터 Jump_End 길이")]
        [SerializeField, Min(0.01f)] private float _jumpLandDuration = 0.5f;
        [Tooltip("점프 클립에서 최고점 위치(정규화 시간). 체공 중에는 이 위치에서 멈춘다")]
        [SerializeField, Range(0.0f, 1.0f)] private float _jumpApexNormalizedTime = 0.5f;

        [Header("References")]
        [SerializeField] private Animator _animator;
        #endregion

        #region Private Fields
        private bool _isAirborne;
        private bool _isDead;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            if (!_animator) _animator = GetComponentInChildren<Animator>();
        }

        private void Update()
        {
            if (!_isAirborne || !_animator) return;
            // 다시 점프한 직후에는 아직 이전 Jump 상태라 최고점으로 판정되므로 트리거가 소비될 때까지 기다린다
            if (_animator.GetBool(AnimHash.Jump)) return;
            if (!TryGetJumpNormalizedTime(out float normalizedTime)) return;

            // 체공 중 최고점에 닿으면 착지할 때까지 멈춘다
            if (normalizedTime >= _jumpApexNormalizedTime) _animator.SetFloat(AnimHash.JumpAnimSpeed, 0.0f);
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// 펫의 실제 이동 속도로 걷기/달리기 블렌드와 이동 애니메이션 배속을 갱신한다.
        /// </summary>
        /// <param name="moveSpeed">수평 이동 속도(m/s)</param>
        public void SetMoveSpeed(float moveSpeed)
        {
            if (_isDead || !_animator) return;

            _animator.SetFloat(AnimHash.Speed, moveSpeed);
            _animator.SetFloat(AnimHash.MoveAnimSpeed, Mathf.Clamp(moveSpeed / _runAnimSpeed, 1.0f, _maxMoveAnimSpeed));
        }

        /// <summary>
        /// 점프 애니메이션을 처음부터 재생한다. 최고점까지 _jumpStartDuration 동안 재생한 뒤 착지할 때까지 멈춘다.
        /// </summary>
        public void PlayJump()
        {
            if (_isDead || !_animator || !_jumpClip) return;

            _isAirborne = true;
            _animator.SetFloat(AnimHash.JumpAnimSpeed, (_jumpApexNormalizedTime * _jumpClip.length) / _jumpStartDuration);
            _animator.SetTrigger(AnimHash.Jump);
        }

        /// <summary>
        /// 점프 애니메이션의 남은 구간을 _jumpLandDuration 동안 재생한다. 점프 상태가 아니면 무시한다.
        /// </summary>
        public void PlayLand()
        {
            if (_isDead || !_isAirborne || !_animator || !_jumpClip) return;

            _isAirborne = false;
            if (!TryGetJumpNormalizedTime(out float normalizedTime)) return;

            float remainingTime = Mathf.Max(0.0f, 1.0f - normalizedTime) * _jumpClip.length;
            _animator.SetFloat(AnimHash.JumpAnimSpeed, remainingTime / _jumpLandDuration);
        }

        /// <summary>
        /// 방향별 피격 애니메이션을 재생한다.
        /// </summary>
        /// <param name="direction">캐릭터 기준 피격 방향. 0: 앞, 1: 뒤, 2: 왼쪽, 3: 오른쪽</param>
        public void PlayHit(int direction)
        {
            if (_isDead || !_animator) return;

            _isAirborne = false;
            _animator.SetInteger(AnimHash.HitDirection, direction);
            _animator.SetTrigger(AnimHash.Hit);
        }

        /// <summary>
        /// 특수 Idle 애니메이션을 재생한다. 펫이 움직이면 이동 애니메이션으로 돌아간다.
        /// </summary>
        /// <param name="specialIdleIndex">캐릭터가 재생한 특수 Idle 인덱스</param>
        public void PlaySpecialIdle(int specialIdleIndex)
        {
            if (_isDead || !_animator) return;

            _animator.SetInteger(AnimHash.SpecialIdleIndex, specialIdleIndex);
            _animator.SetTrigger(AnimHash.SpecialIdle);
        }

        /// <summary>
        /// 사망 애니메이션을 재생한다. 애니메이션은 마지막 프레임에서 멈추고, 이후 알림은 무시한다.
        /// </summary>
        public void PlayDeath()
        {
            if (_isDead) return;
            _isDead = true;
            _isAirborne = false;

            if (!_animator) return;
            _animator.SetFloat(AnimHash.Speed, 0.0f);
            _animator.ResetTrigger(AnimHash.Jump);
            _animator.ResetTrigger(AnimHash.Hit);
            _animator.ResetTrigger(AnimHash.SpecialIdle);
            _animator.SetTrigger(AnimHash.Death);
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Jump 상태의 정규화 시간을 얻는다. Jump 로 전환 중이면 전환 대상 기준이다.
        /// </summary>
        /// <param name="normalizedTime">Jump 상태의 정규화 시간</param>
        /// <returns>현재(또는 전환 대상) 상태가 Jump 면 true</returns>
        private bool TryGetJumpNormalizedTime(out float normalizedTime)
        {
            AnimatorStateInfo stateInfo = _animator.IsInTransition(0)
                ? _animator.GetNextAnimatorStateInfo(0)
                : _animator.GetCurrentAnimatorStateInfo(0);

            normalizedTime = stateInfo.normalizedTime;
            return stateInfo.shortNameHash == AnimHash.JumpState;
        }
        #endregion

        #region Nested Types
        private static class AnimHash
        {
            public static readonly int Speed = Animator.StringToHash("Speed");
            public static readonly int MoveAnimSpeed = Animator.StringToHash("MoveAnimSpeed");
            public static readonly int Jump = Animator.StringToHash("Jump");
            public static readonly int JumpAnimSpeed = Animator.StringToHash("JumpAnimSpeed");
            public static readonly int Hit = Animator.StringToHash("Hit");
            public static readonly int HitDirection = Animator.StringToHash("HitDirection");
            public static readonly int SpecialIdle = Animator.StringToHash("SpecialIdle");
            public static readonly int SpecialIdleIndex = Animator.StringToHash("SpecialIdleIndex");
            public static readonly int Death = Animator.StringToHash("Death");
            public static readonly int JumpState = Animator.StringToHash("Jump");
        }
        #endregion
    }
}
