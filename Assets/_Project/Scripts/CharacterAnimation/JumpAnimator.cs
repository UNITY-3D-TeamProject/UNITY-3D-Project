using UnityEngine;

namespace CharacterAnimation
{
    /// <summary>
    /// 점프 애니메이션이 필요한 객체(보행자 등)만 붙이는 Animator 파라미터 설정 컴포넌트.
    /// </summary>
    public class JumpAnimator : MonoBehaviour
    {
        #region Private Fields
        private Animator _animator;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            _animator = GetComponentInParent<Animator>();

            if (_animator == null)
            {
                Debug.LogError($"[{name}] Animator를 찾지 못했습니다. 모델 루트에 추가하세요.", this);
                enabled = false;
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// 점프 시작을 알린다. 점프 애니메이션을 처음부터 재생한다.
        /// </summary>
        public void PlayJump()
        {
            if (!enabled) return;

            _animator.SetBool(AnimHash.IsJumping, true);
            _animator.SetTrigger(AnimHash.Jump);
        }

        /// <summary>
        /// 착지를 알린다. 점프 상태에서 벗어나 Idle/Move 로 돌아간다.
        /// </summary>
        public void PlayLand()
        {
            if (!enabled) return;

            _animator.SetBool(AnimHash.IsJumping, false);
            // 착지 직전 프레임에 들어온 Trigger 가 남아 착지 후 다시 재생되지 않게 한다
            _animator.ResetTrigger(AnimHash.Jump);
        }
        #endregion

        #region Nested Types
        private static class AnimHash
        {
            public static readonly int IsJumping = Animator.StringToHash("IsJumping");
            public static readonly int Jump = Animator.StringToHash("Jump");
        }
        #endregion
    }
}
