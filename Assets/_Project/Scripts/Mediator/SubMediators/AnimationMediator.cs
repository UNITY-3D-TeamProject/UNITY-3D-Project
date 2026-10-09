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
        /// 이동 방향을 받아 이동 중 여부와 이동 방향을 CharacterAnimator 에 전달한다.
        /// </summary>
        /// <param name="direction">이동 입력으로 계산된 월드 이동 방향</param>
        public void SetMoveDirection(Vector3 direction)
        {
            if (!_characterAnimator) return;

            _characterAnimator.SetMoving(direction.sqrMagnitude > MIN_SQR_MAGNITUDE);
            _characterAnimator.SetMoveDirection(direction);
        }

        /// <summary>
        /// 카메라가 보는 방향을 CharacterAnimator 로 전달한다. 이동 방향 블렌드의 기준이 된다.
        /// </summary>
        /// <param name="viewForward">카메라가 보는 방향 (월드 기준)</param>
        public void SetViewForward(Vector3 viewForward)
        {
            if (!_characterAnimator) return;

            _characterAnimator.SetViewForward(viewForward);
        }

        /// <summary>
        /// 사격 요청을 CharacterAnimator 로 전달해 사격 상체 애니메이션을 재생한다.
        /// </summary>
        /// <param name="firePosition">총알 생성 위치 (사용하지 않음)</param>
        public void NotifyFire(Vector3 firePosition)
        {
            if (!_characterAnimator) return;

            _characterAnimator.PlayFire();
        }

        /// <summary>
        /// 점프 시작을 CharacterAnimator 로 전달한다.
        /// </summary>
        public void NotifyJumpStarted()
        {
            if (!_characterAnimator) return;

            _characterAnimator.PlayJump();
        }

        /// <summary>
        /// 착지를 CharacterAnimator 로 전달한다.
        /// </summary>
        public void NotifyJumpEnded()
        {
            if (!_characterAnimator) return;

            _characterAnimator.PlayLand();
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
