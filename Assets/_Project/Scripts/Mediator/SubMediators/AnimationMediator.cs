using System;
using UnityEngine;
using CharacterAnimation;
using Visual;

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
        [Tooltip("피격 후 무적 동안 캐릭터를 깜빡이게 하는 컴포넌트")]
        [SerializeField] private RendererBlinker _blinker;
        [Header("Settings")]
        [Tooltip("CharacterAnimator.SetMoveSpeed 로 연결할 이동속도 어트리뷰트 이름")]
        [SerializeField] private string _speedValueKey;
        #endregion

        #region Events
        /// <summary>CharacterAnimator 의 사망 애니메이션이 끝났을 때 발생한다.</summary>
        public event Action OnDeathAnimationEnded;
        #endregion

        #region Unity Lifecycle
        protected override void Awake()
        {
            base.Awake();
            ResolveComponent(ref _characterAnimator);
            ResolveComponent(ref _blinker);
        }

        protected override void OnEnable()
        {
            if (_characterAnimator) _characterAnimator.OnDeathAnimationEnded += OnDeathAnimationEndedCallback;
            base.OnEnable();
        }

        private void OnDisable()
        {
            if (_characterAnimator) _characterAnimator.OnDeathAnimationEnded -= OnDeathAnimationEndedCallback;
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
        /// 조준점 델리게이트를 CharacterAnimator 로 전달한다.
        /// </summary>
        /// <param name="getAimPoint">조준점을 반환하는 함수</param>
        public void SetGetAimPoint(Func<Vector3> getAimPoint)
        {
            if (!_characterAnimator) return;

            _characterAnimator.SetGetAimPoint(getAimPoint);
        }

        /// <summary>
        /// CharacterAnimator 의 조준점 델리게이트를 제거한다.
        /// </summary>
        public void ClearGetAimPoint()
        {
            if (!_characterAnimator) return;

            _characterAnimator.ClearGetAimPoint();
        }

        /// <summary>
        /// 사격 요청을 CharacterAnimator 로 전달해 사격 상체 애니메이션을 재생한다.
        /// </summary>
        /// <param name="firePosition">총알 생성 위치 (사용하지 않음)</param>
        /// <param name="isCharged">차지샷 여부 (사용하지 않음)</param>
        public void NotifyFire(Vector3 firePosition, bool isCharged)
        {
            if (!_characterAnimator) return;

            _characterAnimator.PlayFire();
        }

        /// <summary>
        /// 사격 차지 상태를 CharacterAnimator 로 전달한다. 차지 중에는 사격 자세를 유지한다.
        /// </summary>
        /// <param name="isCharging">차지 중이면 true</param>
        public void NotifyFireCharge(bool isCharging)
        {
            if (!_characterAnimator) return;

            _characterAnimator.SetCharging(isCharging);
        }

        /// <summary>
        /// 조준 상태를 CharacterAnimator 로 전달한다. 조준 중에는 사격 자세를 유지한다.
        /// </summary>
        /// <param name="isAiming">조준 중이면 true</param>
        public void NotifyAim(bool isAiming)
        {
            if (!_characterAnimator) return;

            _characterAnimator.SetAiming(isAiming);
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

        /// <summary>
        /// 구르기 시작을 CharacterAnimator 로 전달한다.
        /// </summary>
        /// <param name="direction">구르기 방향 (탑승 중 보드 방향에 사용)</param>
        public void NotifyRollStarted(Vector3 direction)
        {
            if (!_characterAnimator) return;

            _characterAnimator.PlayRoll(direction);
        }

        /// <summary>
        /// 구르기 종료를 CharacterAnimator 로 전달한다.
        /// </summary>
        public void NotifyRollEnded()
        {
            if (!_characterAnimator) return;

            _characterAnimator.StopRoll();
        }

        /// <summary>
        /// 탈것 탑승 상태를 CharacterAnimator 로 전달한다.
        /// </summary>
        /// <param name="isRiding">탑승 중이면 true</param>
        public void NotifyRidingChanged(bool isRiding)
        {
            if (!_characterAnimator) return;

            _characterAnimator.SetRiding(isRiding);
        }

        /// <summary>
        /// 탑승 중 이동 방향으로 돌릴 보드(탈것)를 CharacterAnimator 로 전달한다.
        /// </summary>
        /// <param name="board">탈것 Transform. 하차해 제거되었으면 null</param>
        public void SetRidingBoard(Transform board)
        {
            if (!_characterAnimator) return;

            _characterAnimator.SetRidingBoard(board);
        }

        /// <summary>
        /// 피격을 전달받아 깜빡임을 시작하고 방향별 피격 애니메이션을 재생한다. 이미 깜빡이는 중이면 그대로 이어간다.
        /// </summary>
        /// <param name="hitSource">피격 방향 기준 월드 위치(맞은 지점 또는 공격자 위치). null 이면 전방 피격</param>
        public void NotifyHit(Vector3? hitSource)
        {
            if (_blinker) _blinker.Play();
            if (!_characterAnimator) return;

            _characterAnimator.PlayHit(hitSource);
        }

        /// <summary>
        /// 피격 후 무적이 끝났음을 전달받아 깜빡임을 멈춘다.
        /// </summary>
        public void NotifyInvincibilityEnded()
        {
            if (!_blinker) return;

            _blinker.Stop();
        }

        /// <summary>
        /// 사망을 CharacterAnimator 로 전달해 사망 애니메이션을 재생한다. 깜빡임은 멈춘다.
        /// </summary>
        public void NotifyDeath()
        {
            if (_blinker) _blinker.Stop();
            if (!_characterAnimator) return;

            _characterAnimator.PlayDeath();
        }
        #endregion

        #region Protected Methods
        protected override void InitAttributeCallback()
        {
            AttributeCallback.TryAdd(_speedValueKey, data =>
            {
                if (_characterAnimator) _characterAnimator.SetMoveSpeed(data.NewValue);
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
