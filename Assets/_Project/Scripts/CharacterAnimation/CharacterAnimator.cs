using UnityEngine;

namespace CharacterAnimation
{
    /// <summary>
    /// 캐릭터 Animator 파라미터를 설정하는 컴포넌트.
    /// Animator는 캐릭터 루트에 두고, 이 스크립트는 자식에 있어도 된다.
    /// </summary>
    public class CharacterAnimator : MonoBehaviour
    {
        #region Private Fields
        private Animator _animator;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            // GetComponentInParent는 자기 자신을 먼저 검사하므로 루트/자식 배치를 모두 커버한다
            _animator = GetComponentInParent<Animator>();

            if (_animator == null)
            {
                Debug.LogError($"[{name}] Animator를 찾지 못했습니다. 캐릭터 루트에 추가하세요.", this);
                enabled = false;
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// 이동 중 여부를 설정한다. true 면 Run, false 면 Idle 로 전환된다.
        /// </summary>
        /// <param name="isMoving">이동 중이면 true</param>
        public void SetMoving(bool isMoving)
        {
            if (!enabled) return;

            _animator.SetBool(AnimHash.IsMoving, isMoving);
        }
        #endregion

        #region Nested Types
        private static class AnimHash
        {
            public static readonly int IsMoving = Animator.StringToHash("IsMoving");
        }
        #endregion
    }
}
