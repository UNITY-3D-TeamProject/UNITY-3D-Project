using System;
using System.Collections.Generic;
using Attribute.Core;
using Skill.Buff;
using Skill.Core;
using Skill.Skills;
using UnityEngine;

namespace Mediator.SubMediators
{
    public interface ISkillRequestController
    {
        public void SetRequestExecuteSkill(Action<string> requestExecuteSkill);
        public void ClearRequestExecuteSkill();
        public void SetRequestStopSkill(Action<string> requestStopSkill);
        public void ClearRequestStopSkill();
    }
    
    public class SkillMediator : MediatorBase
    {
        #region Constants
        private const string MULTI_JUMP_SKILL_NAME = "MultiJump";
        private const string ROLL_SKILL_NAME = "Roll";
        private const string FIRE_SKILL_NAME = "Attack";
        #endregion

        #region Serialized Fields
        [Header("References")]
        [SerializeField] private SkillController _skillController;
        #endregion
    
        #region Private Fields
        private ISkillRequestController _skillRequestController;
        private Func<bool> _getGrounded;
        private Interact _interactSkill;
        private SpawnVehicle _spawnVehicleSkill;
        #endregion

        #region Events
        /// <summary>
        /// 코스트 지불 요청. (어트리뷰트 이름, 소모량)을 받아 지불 성공 여부를 반환한다.
        /// 반환값은 마지막 구독자의 값만 남으므로 단일 구독(CharacterMediator)을 전제로 한다.
        /// </summary>
        public event Func<string, float, bool> OnPayRequested;

        /// <summary>구르기 요청. (이동 거리, 이동 시간)</summary>
        public event Action<float, float> OnRollRequested;

        /// <summary>사격 요청. (총알 생성 위치, 차지샷 여부)</summary>
        public event Action<Vector3, bool> OnFireRequested;

        /// <summary>사격 차지 상태 변경. 차지 시작이면 true, 발사·취소로 끝나면 false.</summary>
        public event Action<bool> OnFireChargeChanged;

        /// <summary>공중 점프 요청. (점프 파워 비율)</summary>
        public event Action<float> OnMultiJumpRequested;

        /// <summary>
        /// 실행 요청으로 스킬이 발동했을 때 발생한다. (스킬 이름, 쿨타임)
        /// 요청 1회당 1번만 발생하며, 연사처럼 스킬 내부에서 반복 발동하는 경우는 알리지 않는다.
        /// </summary>
        public event Action<string, float> OnSkillUsed;

        /// <summary>상호작용 대상을 바라보는지 여부가 바뀔 때 발생한다. 대상이 있으면 true.</summary>
        public event Action<bool> OnInteractableChanged;

        /// <summary>버프 배율이 바뀔 때 대상별 배율 목록과 함께 발생한다. 해제 시에는 같은 대상에 배율 1 이 담긴다.</summary>
        public event Action<IReadOnlyList<SBuffRatio>> OnBuffRatiosChanged;
        #endregion

        #region Unity Lifecycle

        protected override void Awake()
        {
            base.Awake();
            ResolveComponent(ref _skillController);
            _skillRequestController = GetComponentInParent<ISkillRequestController>();
            if (_skillController) _interactSkill = _skillController.GetComponentInChildren<Interact>(true);
            if (_skillController) _spawnVehicleSkill = _skillController.GetComponentInChildren<SpawnVehicle>(true);
            BindRequest();
        }

        protected override void OnEnable()
        {
            BindRequest();
            base.OnEnable();
        }

        private void OnDisable()
        {
            // Stop 요청 경로가 끊기기 전에 진행 중인 지속형 스킬(연사 등)을 멈춘다
            if (_skillController) _skillController.StopAllSkills();
            UnBindRequest();
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// 스킬을 활성화/비활성화한다.
        /// </summary>
        /// <param name="skillName">대상 스킬 이름</param>
        /// <param name="isEnabled">활성화 여부</param>
        public void SetSkillEnabled(string skillName, bool isEnabled)
        {
            if (!_skillController) return;

            _skillController.SetSkillEnabled(skillName, isEnabled);
        }

        /// <summary>
        /// 스킬의 활성화 여부를 반환한다.
        /// </summary>
        /// <param name="skillName">대상 스킬 이름</param>
        /// <returns>스킬이 활성화되어 있으면 true</returns>
        public bool IsSkillEnabled(string skillName)
        {
            return _skillController && _skillController.IsSkillEnabled(skillName);
        }

        /// <summary>
        /// 접지 여부를 얻기 위한 델리게이트 주입
        /// </summary>
        public void SetGetGrounded(Func<bool> getGrounded)
        {
            if (getGrounded == null) return;
            _getGrounded = getGrounded;
        }

        /// <summary>
        /// 접지 여부 델리게이트 제거
        /// </summary>
        public void ClearGetGrounded()
        {
            _getGrounded = null;
        }

        /// <summary>
        /// 카메라 조준 레이를 얻기 위한 델리게이트를 상호작용 스킬에 주입
        /// </summary>
        public void SetGetAimRay(Func<Ray> getAimRay)
        {
            if (getAimRay == null || !_interactSkill) return;
            _interactSkill.SetGetAimRay(getAimRay);
        }

        /// <summary>
        /// 상호작용 스킬의 카메라 조준 레이 델리게이트 제거
        /// </summary>
        public void ClearGetAimRay()
        {
            if (!_interactSkill) return;
            _interactSkill.ClearGetAimRay();
        }

        /// <summary>
        /// 공중 점프 스킬 발동을 요청한다. 발동하면 OnMultiJumpRequested 가 발생한다.
        /// </summary>
        public void RequestMultiJump()
        {
            if (!_skillController || !_skillController.HasSkill(MULTI_JUMP_SKILL_NAME)) return;

            _skillController.TryExecuteSkill(MULTI_JUMP_SKILL_NAME);
        }

        /// <summary>
        /// 점프 시작을 알린다. 지상에서 점프했으면 공중 구르기 횟수를 초기화한다.
        /// </summary>
        public void NotifyJumpStarted()
        {
            if (IsGrounded()) ResetRollCount();
        }

        /// <summary>
        /// 착지를 알린다. 공중 점프와 공중 구르기 횟수를 초기화한다.
        /// </summary>
        public void NotifyLanded()
        {
            if (!_skillController) return;

            if (_skillController.HasSkill(MULTI_JUMP_SKILL_NAME)) _skillController.ResetSkill(MULTI_JUMP_SKILL_NAME);
            ResetRollCount();
        }

        /// <summary>
        /// 구르기 시작을 알린다. 구르는 동안 사격할 수 없도록 Fire 를 비활성화한다 (진행 중인 차지는 취소된다).
        /// </summary>
        /// <param name="direction">구르기 방향 (사용하지 않음)</param>
        public void NotifyRollStarted(Vector3 direction)
        {
            SetSkillEnabled(FIRE_SKILL_NAME, false);
        }

        /// <summary>
        /// 구르기 종료를 알린다. Fire 를 다시 활성화한다.
        /// </summary>
        public void NotifyRollEnded()
        {
            SetSkillEnabled(FIRE_SKILL_NAME, true);
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
        /// 컴포넌트 이벤트 에 대한 바인딩 실행
        /// </summary>
        private void BindRequest()
        {
            if (_skillController == null || _skillRequestController == null) return;

            _skillRequestController.SetRequestExecuteSkill(OnRequestExecuteSkill);
            _skillRequestController.SetRequestStopSkill(_skillController.StopSkill);
            _skillController.SetGetAttribute(key => AttributeGetter?.Invoke(key) ?? 0.0f);
            _skillController.SetRequestPay(OnRequestPay);
            _skillController.SetNotifyExecuted(OnSkillExecuted);
            if (_interactSkill) _interactSkill.SetNotifyInteractableChanged(OnInteractableChangedCallback);
        }

        /// <summary>
        /// 컴포넌트 이벤트 에 대한 언바인딩 실행
        /// </summary>
        private void UnBindRequest()
        {
            if (_skillRequestController == null) return;
            
            _skillRequestController.ClearRequestExecuteSkill();
            _skillRequestController.ClearRequestStopSkill();
        }

        /// <summary>
        /// 스킬 실행 요청 처리. 발동에 성공하면 사용된 스킬과 쿨타임을 알린다.
        /// </summary>
        /// <param name="skillName">실행할 스킬 이름</param>
        private void OnRequestExecuteSkill(string skillName)
        {
            // 지상 구르기는 공중 횟수와 무관하므로, 걸어서 떨어졌다 착지해 남은 횟수를 발동 전에 지운다
            bool isRoll = string.Equals(skillName, ROLL_SKILL_NAME, StringComparison.OrdinalIgnoreCase);
            if (isRoll && IsGrounded()) ResetRollCount();

            if (!_skillController.TryExecuteSkill(skillName)) return;

            // 탈것 스킬은 하차할 때 쿨타임이 시작되므로 OnSkillExecuted 에서 하차 시에만 알린다
            if (IsSpawnVehicleSkill(skillName)) return;

            OnSkillUsed?.Invoke(skillName, _skillController.GetSkillCooldown(skillName));
        }

        /// <summary>
        /// 코스트 지불 요청 처리
        /// </summary>
        /// <param name="key">소모할 어트리뷰트 이름</param>
        /// <param name="amount">소모량</param>
        /// <returns>지불에 성공했으면 true</returns>
        private bool OnRequestPay(string key, float amount)
        {
            // 구독자가 없으면 지불 실패로 처리
            return OnPayRequested?.Invoke(key, amount) ?? false;
        }

        /// <summary>
        /// 발동한 스킬 종류에 맞는 요청 이벤트 발행
        /// </summary>
        /// <param name="skill">발동한 스킬</param>
        private void OnSkillExecuted(SkillBase skill)
        {
            switch (skill)
            {
                case Roll roll:
                    // 지상 구르기는 공중 횟수에서 제외한다
                    if (IsGrounded()) ResetRollCount();
                    OnRollRequested?.Invoke(roll.Distance, roll.Duration);
                    break;
                case Fire fire:
                    NotifyFirePhase(fire);
                    break;
                case MultiJump multiJump:
                    OnMultiJumpRequested?.Invoke(multiJump.JumpPowerRatio);
                    break;
                case SpawnVehicle spawnVehicle:
                    OnBuffRatiosChanged?.Invoke(spawnVehicle.BuffRatios);
                    // 강제 하차(자원 고갈 등)도 쿨타임이 시작되므로 하차는 여기서 알린다
                    if (!spawnVehicle.IsRiding) OnSkillUsed?.Invoke(spawnVehicle.SkillName, spawnVehicle.Cooldown);
                    break;
            }
        }

        /// <summary>
        /// 상호작용 스킬의 대상 유무 변경을 알린다.
        /// </summary>
        /// <param name="hasTarget">대상이 있으면 true</param>
        private void OnInteractableChangedCallback(bool hasTarget)
        {
            OnInteractableChanged?.Invoke(hasTarget);
        }

        /// <summary>
        /// Fire 의 진행 단계에 맞는 사격 이벤트 발행
        /// </summary>
        /// <param name="fire">발동 알림을 보낸 Fire 스킬</param>
        private void NotifyFirePhase(Fire fire)
        {
            switch (fire.Phase)
            {
                case EFirePhase.ChargeStarted:
                    OnFireChargeChanged?.Invoke(true);
                    break;
                case EFirePhase.Fired:
                    OnFireChargeChanged?.Invoke(false);
                    OnFireRequested?.Invoke(fire.FirePosition, fire.IsCharged);
                    break;
                case EFirePhase.Canceled:
                    OnFireChargeChanged?.Invoke(false);
                    break;
            }
        }

        /// <summary>
        /// skillName 이 탈것 스킬의 이름인지 확인한다.
        /// </summary>
        /// <param name="skillName">확인할 스킬 이름</param>
        /// <returns>탈것 스킬이면 true</returns>
        private bool IsSpawnVehicleSkill(string skillName)
        {
            return _spawnVehicleSkill && string.Equals(skillName, _spawnVehicleSkill.SkillName, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 접지 여부를 반환한다. 델리게이트가 없으면 지상으로 간주한다.
        /// </summary>
        /// <returns>지면에 닿아 있으면 true</returns>
        private bool IsGrounded()
        {
            return _getGrounded?.Invoke() ?? true;
        }

        /// <summary>
        /// 공중 구르기 횟수를 초기화한다.
        /// </summary>
        private void ResetRollCount()
        {
            if (!_skillController || !_skillController.HasSkill(ROLL_SKILL_NAME)) return;

            _skillController.ResetSkill(ROLL_SKILL_NAME);
        }
        #endregion
    }
}
