using System;
using UnityEngine;
using CharacterAnimation;
using Visual;

namespace Mediator.SubMediators
{
    /// <summary>
    /// 다른 Mediator 에서 온 캐릭터 상태를 PlayerAnimator 로 전달하는 접착 컴포넌트.
    /// </summary>
    public class PlayerAnimationMediator : MediatorBase
    {
        #region Constants
        private const float MIN_SQR_MAGNITUDE = 0.0001f;
        #endregion

        #region Serialized Fields
        [Header("References")]
        [SerializeField] private PlayerAnimator _playerAnimator;
        [Tooltip("피격 후 무적 동안 캐릭터를 깜빡이게 하는 컴포넌트")]
        [SerializeField] private RendererBlinker _blinker;
        [Header("Settings")]
        [Tooltip("PlayerAnimator.SetMoveSpeed 로 연결할 이동속도 어트리뷰트 이름")]
        [SerializeField] private string _speedValueKey;
        #endregion

        #region Events
        /// <summary>PlayerAnimator 의 사망 애니메이션이 끝났을 때 발생한다.</summary>
        public event Action OnDeathAnimationEnded;
        /// <summary>PlayerAnimator 가 특수 Idle 재생을 시작할 때 (특수 Idle 인덱스)를 알린다.</summary>
        public event Action<int> OnSpecialIdleStarted;
        /// <summary>PlayerAnimator 의 이동 애니메이션에서 발이 땅에 닿을 때마다 발생한다.</summary>
        public event Action OnFootstep;
        #endregion

        #region Unity Lifecycle
        protected override void Awake()
        {
            base.Awake();
            ResolveComponent(ref _playerAnimator);
            ResolveComponent(ref _blinker);
        }

        protected override void OnEnable()
        {
            if (_playerAnimator) _playerAnimator.OnDeathAnimationEnded += OnDeathAnimationEndedCallback;
            if (_playerAnimator) _playerAnimator.OnSpecialIdleStarted += OnSpecialIdleStartedCallback;
            if (_playerAnimator) _playerAnimator.OnFootstep += OnFootstepCallback;
            base.OnEnable();
        }

        private void OnDisable()
        {
            if (_playerAnimator) _playerAnimator.OnDeathAnimationEnded -= OnDeathAnimationEndedCallback;
            if (_playerAnimator) _playerAnimator.OnSpecialIdleStarted -= OnSpecialIdleStartedCallback;
            if (_playerAnimator) _playerAnimator.OnFootstep -= OnFootstepCallback;
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// 이동 방향을 받아 이동 중 여부와 이동 방향을 PlayerAnimator 에 전달한다.
        /// </summary>
        /// <param name="direction">이동 입력으로 계산된 월드 이동 방향</param>
        public void SetMoveDirection(Vector3 direction)
        {
            if (!_playerAnimator) return;

            _playerAnimator.SetMoving(direction.sqrMagnitude > MIN_SQR_MAGNITUDE);
            _playerAnimator.SetMoveDirection(direction);
        }

        /// <summary>
        /// 카메라가 보는 방향을 PlayerAnimator 로 전달한다. 이동 방향 블렌드의 기준이 된다.
        /// </summary>
        /// <param name="viewForward">카메라가 보는 방향 (월드 기준)</param>
        public void SetViewForward(Vector3 viewForward)
        {
            if (!_playerAnimator) return;

            _playerAnimator.SetViewForward(viewForward);
        }

        /// <summary>
        /// 조준점 델리게이트를 PlayerAnimator 로 전달한다.
        /// </summary>
        /// <param name="getAimPoint">조준점을 반환하는 함수</param>
        public void SetGetAimPoint(Func<Vector3> getAimPoint)
        {
            if (!_playerAnimator) return;

            _playerAnimator.SetGetAimPoint(getAimPoint);
        }

        /// <summary>
        /// PlayerAnimator 의 조준점 델리게이트를 제거한다.
        /// </summary>
        public void ClearGetAimPoint()
        {
            if (!_playerAnimator) return;

            _playerAnimator.ClearGetAimPoint();
        }

        /// <summary>
        /// 사격 요청을 PlayerAnimator 로 전달해 사격 상체 애니메이션을 재생한다.
        /// </summary>
        /// <param name="firePosition">총알 생성 위치 (사용하지 않음)</param>
        /// <param name="isCharged">차지샷 여부 (사용하지 않음)</param>
        public void NotifyFire(Vector3 firePosition, bool isCharged)
        {
            if (!_playerAnimator) return;

            _playerAnimator.PlayFire();
        }

        /// <summary>
        /// 사격 차지 상태를 PlayerAnimator 로 전달한다. 차지 중에는 사격 자세를 유지한다.
        /// </summary>
        /// <param name="isCharging">차지 중이면 true</param>
        public void NotifyFireCharge(bool isCharging)
        {
            if (!_playerAnimator) return;

            _playerAnimator.SetCharging(isCharging);
        }

        /// <summary>
        /// 조준 상태를 PlayerAnimator 로 전달한다. 조준 중에는 사격 자세를 유지한다.
        /// </summary>
        /// <param name="isAiming">조준 중이면 true</param>
        public void NotifyAim(bool isAiming)
        {
            if (!_playerAnimator) return;

            _playerAnimator.SetAiming(isAiming);
        }

        /// <summary>
        /// 점프 시작을 PlayerAnimator 로 전달한다.
        /// </summary>
        public void NotifyJumpStarted()
        {
            if (!_playerAnimator) return;

            _playerAnimator.PlayJump();
        }

        /// <summary>
        /// 점프 없는 낙하 시작을 PlayerAnimator 로 전달한다.
        /// </summary>
        public void NotifyFallStarted()
        {
            if (!_playerAnimator) return;

            _playerAnimator.PlayFall();
        }

        /// <summary>
        /// 착지를 PlayerAnimator 로 전달한다.
        /// </summary>
        public void NotifyJumpEnded()
        {
            if (!_playerAnimator) return;

            _playerAnimator.PlayLand();
        }

        /// <summary>
        /// 구르기 시작을 PlayerAnimator 로 전달한다.
        /// </summary>
        /// <param name="direction">구르기 방향 (탑승 중 보드 방향에 사용)</param>
        public void NotifyRollStarted(Vector3 direction)
        {
            if (!_playerAnimator) return;

            _playerAnimator.PlayRoll(direction);
        }

        /// <summary>
        /// 구르기 종료를 PlayerAnimator 로 전달한다.
        /// </summary>
        public void NotifyRollEnded()
        {
            if (!_playerAnimator) return;

            _playerAnimator.StopRoll();
        }

        /// <summary>
        /// 탈것 탑승 상태를 PlayerAnimator 로 전달한다.
        /// </summary>
        /// <param name="isRiding">탑승 중이면 true</param>
        public void NotifyRidingChanged(bool isRiding)
        {
            if (!_playerAnimator) return;

            _playerAnimator.SetRiding(isRiding);
        }

        /// <summary>
        /// 탑승 중 이동 방향으로 돌릴 보드(탈것)를 PlayerAnimator 로 전달한다.
        /// </summary>
        /// <param name="board">탈것 Transform. 하차해 제거되었으면 null</param>
        public void SetRidingBoard(Transform board)
        {
            if (!_playerAnimator) return;

            _playerAnimator.SetRidingBoard(board);
        }

        /// <summary>
        /// 피격을 전달받아 깜빡임을 시작하고 방향별 피격 애니메이션을 재생한다. 이미 깜빡이는 중이면 그대로 이어간다.
        /// </summary>
        /// <param name="hitSource">피격 방향 기준 월드 위치(맞은 지점 또는 공격자 위치). null 이면 전방 피격</param>
        public void NotifyHit(Vector3? hitSource)
        {
            if (_blinker) _blinker.Play();
            if (!_playerAnimator) return;

            _playerAnimator.PlayHit(hitSource);
        }

        /// <summary>
        /// 기준 위치가 몸 정면 기준 어느 쪽인지 PlayerAnimator 로 판정한 피격 방향을 반환한다.
        /// </summary>
        /// <param name="hitSource">피격 방향 기준 월드 위치(맞은 지점 또는 공격자 위치). null 이면 전방 피격</param>
        /// <returns>0: 앞, 1: 뒤, 2: 왼쪽, 3: 오른쪽. PlayerAnimator 가 없으면 0</returns>
        public int GetHitDirection(Vector3? hitSource)
        {
            return _playerAnimator ? _playerAnimator.GetHitDirection(hitSource) : 0;
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
        /// 사망을 PlayerAnimator 로 전달해 사망 애니메이션을 재생한다. 깜빡임은 멈춘다.
        /// </summary>
        public void NotifyDeath()
        {
            if (_blinker) _blinker.Stop();
            if (!_playerAnimator) return;

            _playerAnimator.PlayDeath();
        }
        #endregion

        #region Protected Methods
        protected override void InitAttributeCallback()
        {
            AttributeCallback.TryAdd(_speedValueKey, data =>
            {
                if (_playerAnimator) _playerAnimator.SetMoveSpeed(data.NewValue);
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

        private void OnSpecialIdleStartedCallback(int specialIdleIndex)
        {
            OnSpecialIdleStarted?.Invoke(specialIdleIndex);
        }

        private void OnFootstepCallback()
        {
            OnFootstep?.Invoke();
        }
        #endregion
    }
}
