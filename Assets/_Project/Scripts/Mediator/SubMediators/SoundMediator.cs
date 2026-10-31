using System;
using UnityEngine;
using CharacterSound;

namespace Mediator.SubMediators
{
    /// <summary>
    /// 조율 계층이 걸러서 보낸 이동·스킬·피격 신호를 SoundController 로 전달하는 접착 컴포넌트.
    /// 어떤 소리를 낼지는 SoundController 가, 생존/사망에 따른 전달 여부는 조율 계층(CharacterMediator)이 정한다.
    /// 이 컴포넌트는 생존/사망을 저장하거나 판단하지 않고 신호와 이동 상태 델리게이트만 넘긴다.
    /// 이 컴포넌트가 꺼져 있는 동안에는 이동 상태 델리게이트를 빼서 SoundController 가 표본을 만들지 않게 한다.
    /// </summary>
    public class SoundMediator : MediatorBase
    {
        #region Serialized Fields
        [Header("References")]
        [SerializeField] private SoundController _soundController;
        #endregion

        #region Private Fields
        // 다시 켜질 때 SoundController 에 다시 넣기 위해 마지막으로 받은 이동 상태 델리게이트를 기억한다
        private Func<bool> _getHasMoveIntent;
        private Func<float> _getHorizontalSpeed;
        private Func<bool> _getGrounded;
        private Func<bool> _getRolling;
        #endregion

        #region Events
        /// <summary>
        /// 이 중재자나 SoundController 가 켜져 소리를 낼 준비가 되었을 때 발생한다.
        /// 받는 쪽(조율 계층)은 현재 실제 상태를 다시 전달해 비활성 중 잃은 상태를 맞춘다.
        /// </summary>
        public event Action OnSyncRequested;
        #endregion

        #region Unity Lifecycle
        protected override void Awake()
        {
            base.Awake();
            ResolveComponent(ref _soundController);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            if (_soundController) _soundController.OnReady += HandleControllerReady;
            ForwardMoveState();

            // 컨트롤러가 아직 꺼져 있으면 아래 알림은 무시되고, 컨트롤러가 켜질 때 HandleControllerReady 로 다시 맞춘다
            OnSyncRequested?.Invoke();
        }

        private void OnDisable()
        {
            if (_soundController)
            {
                _soundController.OnReady -= HandleControllerReady;
                _soundController.ClearGetMoveState();
            }
            StopAllSounds();
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// 이동 상태를 읽기 위한 델리게이트 주입. 이동 입력 여부, 실제 수평 속도, 접지 여부, 구르는 중 여부.
        /// 켜져 있으면 바로 SoundController 에 넘기고, 꺼져 있으면 기억해 두었다가 켜질 때 넘긴다.
        /// </summary>
        public void SetGetMoveState(
            Func<bool> getHasMoveIntent,
            Func<float> getHorizontalSpeed,
            Func<bool> getGrounded,
            Func<bool> getRolling)
        {
            if ((getHasMoveIntent == null) || (getHorizontalSpeed == null) || (getGrounded == null) || (getRolling == null)) return;

            _getHasMoveIntent = getHasMoveIntent;
            _getHorizontalSpeed = getHorizontalSpeed;
            _getGrounded = getGrounded;
            _getRolling = getRolling;
            if (isActiveAndEnabled) ForwardMoveState();
        }

        /// <summary>
        /// 이동 상태 델리게이트 제거. SoundController 에서도 뺀다.
        /// </summary>
        public void ClearGetMoveState()
        {
            _getHasMoveIntent = null;
            _getHorizontalSpeed = null;
            _getGrounded = null;
            _getRolling = null;
            if (_soundController) _soundController.ClearGetMoveState();
        }

        /// <summary>
        /// 점프 시작을 전달한다.
        /// </summary>
        public void NotifyJumpStarted()
        {
            if (!_soundController || !isActiveAndEnabled) return;

            _soundController.PlayJump();
        }

        /// <summary>
        /// 이동 애니메이션의 발 디딤을 전달한다.
        /// </summary>
        public void NotifyFootstep()
        {
            if (!_soundController || !isActiveAndEnabled) return;

            _soundController.PlayFootstep();
        }

        /// <summary>
        /// 피격을 전달한다.
        /// </summary>
        public void NotifyHit()
        {
            if (!_soundController || !isActiveAndEnabled) return;

            _soundController.PlayHit();
        }

        /// <summary>
        /// 사망음을 전달한다.
        /// </summary>
        public void NotifyDeathSound()
        {
            if (!_soundController || !isActiveAndEnabled) return;

            _soundController.PlayDeath();
        }

        public void FireInOverHit()
        {
            if (!_soundController || !isActiveAndEnabled) return;

            _soundController.PlaySkillSound("Attack", ESoundPhase.Failed, null);
        }
        
        /// <summary>
        /// 스킬이 실제로 동작한 단계의 단발음을 전달한다.
        /// </summary>
        /// <param name="skillName">실제 스킬 이름</param>
        /// <param name="phase">소리 단계</param>
        /// <param name="position">소리 위치. null 이면 캐릭터 위치</param>
        public void NotifySkillSound(string skillName, ESoundPhase phase, Vector3? position)
        {
            if (!_soundController || !isActiveAndEnabled) return;

            _soundController.PlaySkillSound(skillName, phase, position);
        }

        /// <summary>
        /// 스킬 반복음의 시작·종료를 전달한다.
        /// </summary>
        /// <param name="skillName">실제 스킬 이름</param>
        /// <param name="phase">소리 단계</param>
        /// <param name="isOn">시작이면 true</param>
        public void NotifySkillSoundLoop(string skillName, ESoundPhase phase, bool isOn)
        {
            // 꺼진 동안 새 시작은 막고 해제는 항상 처리한다. 켜질 때 현재 상태로 다시 맞춘다
            if (!_soundController || (isOn && !isActiveAndEnabled)) return;

            _soundController.SetSkillLoop(skillName, phase, isOn);
        }

        /// <summary>
        /// 실제 탈것이 붙거나 떨어진 것을 전달한다. 붙어 있는 동안 그 스킬의 탈것 소리를 계속 내고 발소리는 내지 않는다.
        /// </summary>
        /// <param name="skillName">탈것 스킬의 실제 이름</param>
        /// <param name="isAttached">탈것이 붙어 있으면 true</param>
        public void NotifyVehicleSound(string skillName, bool isAttached)
        {
            if (!_soundController || (isAttached && !isActiveAndEnabled)) return;

            _soundController.SetVehicleSound(isAttached ? skillName : string.Empty);
        }

        /// <summary>
        /// API 호환용. 사망 상태를 저장하거나 판단하지 않고 일반 반복음 정리만 한다.
        /// 정상 경로에서는 CharacterMediator 가 사망 시 StopAllSounds 를 직접 호출하므로 쓰이지 않는다.
        /// </summary>
        public void NotifyDeath()
        {
            if (!_soundController) return;

            _soundController.NotifyDeath();
        }

        /// <summary>
        /// API 호환용. 사망 상태를 저장하거나 판단하지 않고 착지 판정 기준 초기화만 한다.
        /// 정상 경로에서는 CharacterMediator 가 재사용 시 StopAllSounds 를 직접 호출하므로 쓰이지 않는다.
        /// </summary>
        public void ResetForReuse()
        {
            if (!_soundController) return;

            _soundController.ResetForReuse();
        }

        /// <summary>
        /// 모든 반복음과 탈것 반복음 설정을 정리하고 착지 판정 기준을 비운다.
        /// </summary>
        public void StopAllSounds()
        {
            if (!_soundController) return;

            _soundController.StopAllSounds();
            _soundController.ResetLocomotionTracking();
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

        #region Private Methods
        /// <summary>
        /// SoundController 가 켜졌을 때 조율 계층에 상태 재전달을 요청한다.
        /// </summary>
        private void HandleControllerReady()
        {
            OnSyncRequested?.Invoke();
        }

        /// <summary>
        /// 기억해 둔 이동 상태 델리게이트를 SoundController 에 넘긴다. 하나라도 없으면 SoundController 가 무시한다.
        /// </summary>
        private void ForwardMoveState()
        {
            if (!_soundController) return;

            _soundController.SetGetMoveState(_getHasMoveIntent, _getHorizontalSpeed, _getGrounded, _getRolling);
        }
        #endregion
    }
}
