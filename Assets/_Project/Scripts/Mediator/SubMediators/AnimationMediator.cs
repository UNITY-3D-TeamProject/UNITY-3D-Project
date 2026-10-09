using UnityEngine;
using CharacterAnimation;

namespace Mediator.SubMediators
{
    /// <summary>
    /// 다른 Mediator 에서 온 캐릭터 상태를 CharacterAnimator 로 전달하는 접착 컴포넌트.
    /// </summary>
    public class AnimationMediator : MediatorBase
    {
        #region Constants
        private const float MIN_SQR_MAGNITUDE = 0.0001f;
        #endregion

        #region Serialized Fields
        [Header("References")]
        [SerializeField] private CharacterAnimator _characterAnimator;
        #endregion

        #region Unity Lifecycle
        protected override void Awake()
        {
            base.Awake();
            ResolveComponent(ref _characterAnimator);
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// 이동 방향을 받아 이동 중 여부를 CharacterAnimator 에 전달한다.
        /// </summary>
        /// <param name="direction">이동 입력으로 계산된 월드 이동 방향</param>
        public void SetMoveDirection(Vector3 direction)
        {
            if (!_characterAnimator) return;

            _characterAnimator.SetMoving(direction.sqrMagnitude > MIN_SQR_MAGNITUDE);
        }
        #endregion

        #region Protected Methods
        protected override void InitAttributeCallback()
        {
        }

        protected override void InitValue()
        {
        }
        #endregion
    }
}
