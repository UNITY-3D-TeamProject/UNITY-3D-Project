using System;
using System.Collections.Generic;
using Attribute.Core;
using CameraControl;
using CharacterSound;
using Rendering;
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
        private Fire _fireSkill;
        private Interact _interactSkill;
        private SpawnVehicle _spawnVehicleSkill;
        private Roll _rollSkill;
        private Scan _scanSkill;
        // 점프 시작 ~ 착지. 이 동안 탑승하면 착지할 때 탈것을 생성한다
        private bool _isJumping;
        private bool _isRolling;
        // 구르기 시작 직전 Fire 활성화 상태. 구르기가 끝날 때 원래 켜져 있었으면 다시 켠다
        private bool _wasFireEnabledOnRoll;
        // 탑승 시작 ~ 탈것 생성. 이 동안 사격·구르기·공중 점프를 막는다
        private bool _isMounting;
        // 탑승 연출로 막기 직전의 활성화 상태. 풀 때 원래 켜져 있던 스킬만 다시 켠다
        private bool _wasFireEnabled;
        private bool _wasRollEnabled;
        private bool _wasMultiJumpEnabled;
        // 펫이 꺼져 있는 동안 스캔 실행 요청을 막는다. 스킬 활성화 상태와는 별개로 관리한다
        private bool _isScanBlocked;
        #endregion

        #region Properties
        /// <summary>사격 차지 진행도(0~1). 차지 중이 아니거나 Fire 스킬이 없으면 0.</summary>
        public float FireChargeRatio => _fireSkill ? _fireSkill.ChargeRatio : 0.0f;
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

        /// <summary>정지 요청으로 스킬을 멈췄을 때 발생한다. (스킬 이름)</summary>
        public event Action<string> OnSkillStopped;

        /// <summary>상호작용 대상을 바라보는지 여부가 바뀔 때 발생한다. 대상이 있으면 true.</summary>
        public event Action<bool> OnInteractableChanged;

        /// <summary>버프 배율이 바뀔 때 대상별 배율 목록과 함께 발생한다. 해제 시에는 같은 대상에 배율 1 이 담긴다.</summary>
        public event Action<IReadOnlyList<SBuffRatio>> OnBuffRatiosChanged;

        /// <summary>탈것에 탑승하거나 하차했을 때 발생한다. 탑승이면 true.</summary>
        public event Action<bool> OnRidingChanged;

        /// <summary>지상에서 탈것에 탑승할 때 뛰어오르기 요청. (점프 파워 비율)</summary>
        public event Action<float> OnMountJumpRequested;

        /// <summary>탈것 오브젝트가 생성되었을 때 그 Transform 과 함께, 하차로 제거되었을 때 null 과 함께 발생한다.</summary>
        public event Action<Transform> OnVehicleAttached;

        /// <summary>탑승 중 자동 전진을 켜거나 끌 때 발생한다. (켜면 true, 전진 방향이 시점 방향을 따라 도는 속도(도/초))</summary>
        public event Action<bool, float> OnAutoForwardChanged;

        /// <summary>요시 점프를 시작하거나 끝냈을 때 발생한다. (시작이면 true, 상승 속도)</summary>
        public event Action<bool, float> OnHoverChanged;

        /// <summary>스캔 스킬이 발동했을 때 발생한다. 받는 쪽은 발동 위치가 정해지면 BeginScan 을 호출한다.</summary>
        public event Action OnScanRequested;

        /// <summary>스킬이 실제로 동작한 단계의 단발음 요청. (실제 스킬 이름, 단계, 소리 위치. null 이면 캐릭터 위치)</summary>
        public event Action<string, ESoundPhase, Vector3?> OnSkillSound;

        /// <summary>스킬 반복음 시작·종료 요청. (실제 스킬 이름, 단계, 시작이면 true)</summary>
        public event Action<string, ESoundPhase, bool> OnSkillSoundLoop;

        /// <summary>실제 탈것이 붙거나 떨어졌을 때 발생한다. (탈것 스킬의 실제 이름, 붙었으면 true)</summary>
        public event Action<string, bool> OnVehicleSoundChanged;
        #endregion

        #region Unity Lifecycle

        protected override void Awake()
        {
            base.Awake();
            ResolveComponent(ref _skillController);
            _skillRequestController = GetComponentInParent<ISkillRequestController>();
            if (_skillController) _fireSkill = _skillController.GetComponentInChildren<Fire>(true);
            if (_skillController) _interactSkill = _skillController.GetComponentInChildren<Interact>(true);
            if (_skillController) _spawnVehicleSkill = _skillController.GetComponentInChildren<SpawnVehicle>(true);
            if (_skillController) _rollSkill = _skillController.GetComponentInChildren<Roll>(true);
            if (_skillController) _scanSkill = _skillController.GetComponentInChildren<Scan>(true);
            BindRequest();
        }

        protected override void OnEnable()
        {
            BindRequest();
            if (_spawnVehicleSkill) _spawnVehicleSkill.OnHoverChanged += OnHoverChangedCallback;
            base.OnEnable();
        }

        private void OnDisable()
        {
            // Stop 요청 경로가 끊기기 전에 진행 중인 지속형 스킬(연사 등)을 멈춘다
            if (_skillController) _skillController.StopAllSkills();
            if (_spawnVehicleSkill) _spawnVehicleSkill.OnHoverChanged -= OnHoverChangedCallback;
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
        /// 구르기 스킬의 카메라 추종 설정을 반환한다. 스킬 활성 여부와 무관하게 설정값을 읽는다.
        /// </summary>
        /// <returns>카메라 추종 설정. 구르기 스킬이 없으면 기본값(효과 없음)</returns>
        public SRollFollowSettings GetRollFollowSettings()
        {
            if (!_rollSkill) return default;

            return new SRollFollowSettings
            {
                Damping = _rollSkill.CameraFollowDamping,
                RecoverTime = _rollSkill.CameraDampingRecoverTime,
            };
        }

        /// <summary>
        /// 탑승 스킬의 시야·가장자리 블러 설정을 반환한다. 스킬 활성 여부와 무관하게 설정값을 읽는다.
        /// </summary>
        /// <returns>시야·블러 설정. 탑승 스킬이 없으면 기본값(효과 없음)</returns>
        public SRidingViewSettings GetRidingViewSettings()
        {
            if (!_spawnVehicleSkill) return default;

            return new SRidingViewSettings
            {
                FovOffset = _spawnVehicleSkill.FovOffset,
                TransitionTime = _spawnVehicleSkill.SpeedFeedbackTransitionTime,
                EdgeBlur = new SSpeedEdgeBlurSettings
                {
                    Strength = _spawnVehicleSkill.EdgeBlurStrength,
                    CenterRadius = _spawnVehicleSkill.EdgeBlurCenterRadius,
                    Feather = _spawnVehicleSkill.EdgeBlurFeather,
                    SampleCount = _spawnVehicleSkill.EdgeBlurSampleCount,
                    Center = _spawnVehicleSkill.EdgeBlurCenter,
                },
            };
        }

        /// <summary>
        /// 탑승 중인지 반환한다. 이 중재자가 꺼져 있으면 탑승 이벤트를 받지 못하므로 탑승 중이 아닌 것으로 본다.
        /// </summary>
        /// <returns>탑승 중이면 true. 탑승 스킬이 없거나 이 중재자가 비활성이면 false</returns>
        public bool IsRiding()
        {
            return isActiveAndEnabled && _spawnVehicleSkill && _spawnVehicleSkill.IsRiding;
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
        /// 공중 점프를 다 써서 발동하지 못했으면 탑승 중일 때 요시 점프를 시작한다.
        /// </summary>
        public void RequestMultiJump()
        {
            bool isJumped = _skillController &&
                            _skillController.HasSkill(MULTI_JUMP_SKILL_NAME) &&
                            _skillController.TryExecuteSkill(MULTI_JUMP_SKILL_NAME);
            if (isJumped) return;

            if (_spawnVehicleSkill) _spawnVehicleSkill.TryStartHover();
        }

        /// <summary>
        /// 점프 입력을 뗀 것을 알린다. 요시 점프 중이면 끝낸다.
        /// </summary>
        public void NotifyJumpReleased()
        {
            if (_spawnVehicleSkill) _spawnVehicleSkill.StopHover();
        }

        /// <summary>
        /// 점프 시작을 알린다. 지상에서 점프했으면 공중 구르기 횟수를 초기화한다.
        /// </summary>
        public void NotifyJumpStarted()
        {
            _isJumping = true;
            if (IsGrounded()) ResetRollCount();
        }

        /// <summary>
        /// 착지를 알린다. 공중 점프·공중 구르기 횟수와 요시 점프 사용 여부를 초기화하고, 탑승 중이면 탈것을 생성한다.
        /// </summary>
        public void NotifyLanded()
        {
            _isJumping = false;
            if (_spawnVehicleSkill)
            {
                _spawnVehicleSkill.ResetHover();
                AttachVehicle(_spawnVehicleSkill);
            }
            if (!_skillController) return;

            if (_skillController.HasSkill(MULTI_JUMP_SKILL_NAME)) _skillController.ResetSkill(MULTI_JUMP_SKILL_NAME);
            ResetRollCount();
        }

        /// <summary>
        /// 구르기 시작을 알린다. 구르는 동안 사격할 수 없도록 Fire 를 비활성화한다 (진행 중인 차지는 취소된다). 요시 점프 중이면 끝낸다.
        /// 비활성화 직전 Fire 상태를 기억해 두어 구르기가 끝날 때 되돌린다.
        /// </summary>
        /// <param name="direction">구르기 방향 (사용하지 않음)</param>
        public void NotifyRollStarted(Vector3 direction)
        {
            _isRolling = true;
            if (_rollSkill) OnSkillSound?.Invoke(_rollSkill.SkillName, ESoundPhase.Start, null);
            _wasFireEnabledOnRoll = IsSkillEnabled(FIRE_SKILL_NAME);
            if (_spawnVehicleSkill) _spawnVehicleSkill.StopHover();
            SetSkillEnabled(FIRE_SKILL_NAME, false);
        }

        /// <summary>
        /// 구르기 종료를 알린다. 구르기 전에 Fire 가 켜져 있었고 탑승 연출 중이 아니면 다시 활성화한다.
        /// </summary>
        public void NotifyRollEnded()
        {
            _isRolling = false;
            // 공중에서 구르다 탑승하면 구르기가 끝나도 탈것 생성 전까지 사격을 막아 둔다 (탑승 연출이 끝날 때 되돌린다)
            if (_wasFireEnabledOnRoll && !_isMounting) SetSkillEnabled(FIRE_SKILL_NAME, true);
        }

        /// <summary>
        /// 스캔 실행 요청을 막거나 푼다. 막는 동안 스캔은 발동하지 않으며 쿨타임·코스트도 쓰지 않는다.
        /// SetSkillEnabled 로 정한 활성화 상태는 바꾸지 않는다.
        /// </summary>
        /// <param name="isBlocked">막으면 true</param>
        public void SetScanBlocked(bool isBlocked)
        {
            _isScanBlocked = isBlocked;
        }

        /// <summary>
        /// origin 에서 스캔 파동을 시작한다. OnScanRequested 를 받은 쪽이 발동 위치를 정한 뒤 호출한다.
        /// </summary>
        /// <param name="origin">파동의 중심(월드 좌표)</param>
        public void BeginScan(Vector3 origin)
        {
            if (!_scanSkill) return;

            // 파동이 실제로 시작됐을 때만 소리를 낸다
            if (_scanSkill.TryBeginWave(origin)) OnSkillSound?.Invoke(_scanSkill.SkillName, ESoundPhase.Wave, origin);
        }

        /// <summary>
        /// 지금 차지 중인지와 탈것이 붙어 있는지를 사운드 알림으로 다시 전달한다.
        /// 구독이 늦어 놓친 상태를 맞출 때 호출한다. 스킬을 다시 실행하지 않는다.
        /// </summary>
        public void SyncSoundState()
        {
            if (_fireSkill) OnSkillSoundLoop?.Invoke(_fireSkill.SkillName, ESoundPhase.Charge, _fireSkill.IsCharging);
            if (_spawnVehicleSkill) OnVehicleSoundChanged?.Invoke(_spawnVehicleSkill.SkillName, _spawnVehicleSkill.Vehicle);
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
            _skillRequestController.SetRequestStopSkill(OnRequestStopSkill);
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
            if (_isScanBlocked && IsScanSkill(skillName)) return;

            if (!_skillController.TryExecuteSkill(skillName)) return;

            // 탈것 스킬은 하차할 때 쿨타임이 시작되므로 OnSkillExecuted 에서 하차 시에만 알린다
            if (IsSpawnVehicleSkill(skillName)) return;

            OnSkillUsed?.Invoke(skillName, _skillController.GetSkillCooldown(skillName));
        }

        /// <summary>
        /// 스킬 정지 요청 처리. 스킬을 멈추고 정지를 알린다.
        /// </summary>
        /// <param name="skillName">정지할 스킬 이름</param>
        private void OnRequestStopSkill(string skillName)
        {
            _skillController.StopSkill(skillName);
            OnSkillStopped?.Invoke(skillName);
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
                    OnRidingChanged?.Invoke(spawnVehicle.IsRiding);
                    if (spawnVehicle.IsRiding)
                    {
                        BeginMount(spawnVehicle);
                    }
                    else
                    {
                        if (_isMounting) SetMountBlocked(false);
                        OnVehicleAttached?.Invoke(null);
                        OnAutoForwardChanged?.Invoke(false, 0.0f);
                        OnVehicleSoundChanged?.Invoke(spawnVehicle.SkillName, false);
                    }
                    // 강제 하차(자원 고갈 등)도 쿨타임이 시작되므로 하차는 여기서 알린다
                    if (!spawnVehicle.IsRiding) OnSkillUsed?.Invoke(spawnVehicle.SkillName, spawnVehicle.Cooldown);
                    break;
                case EnemyRangedAttack:
                    // 실제 발사 시점에 스킬이 소리를 요청한다.
                    break;
                case Scan:
                    OnScanRequested?.Invoke();
                    break;
                default:
                    // 위의 특화 스킬은 실제 동작 시점에 따로 소리를 내므로 일반 실행음은 나머지 스킬만 낸다
                    OnSkillSound?.Invoke(skill.SkillName, ESoundPhase.Execute, null);
                    break;
            }
        }

        /// <summary>
        /// 탑승 연출을 시작한다. 지상이면 뛰어오르고, 점프 중이면 그대로 두어 착지(NotifyLanded) 때 탈것을 생성한다.
        /// 착지를 기다리는 동안에는 사격·구르기·공중 점프를 막는다.
        /// 뛰어오를 수 없거나(구르는 중) 점프 없이 떨어지는 중이면 착지 알림이 오지 않으므로 바로 생성한다.
        /// </summary>
        /// <param name="spawnVehicle">탑승한 탈것 스킬</param>
        private void BeginMount(SpawnVehicle spawnVehicle)
        {
            // 공중 탑승으로 추가 점프를 얻지 않도록 지상에서 탑승할 때만 뛰어오른다
            if (IsGrounded() && !_isRolling)
            {
                SetMountBlocked(true);
                OnMountJumpRequested?.Invoke(spawnVehicle.MountJumpPowerRatio);
                return;
            }

            if (_isJumping)
            {
                SetMountBlocked(true);
                return;
            }

            AttachVehicle(spawnVehicle);
        }

        /// <summary>
        /// 탈것을 생성하고, 새로 생성되었으면 알린 뒤 자동 전진을 켠다.
        /// </summary>
        /// <param name="spawnVehicle">탈것 스킬</param>
        private void AttachVehicle(SpawnVehicle spawnVehicle)
        {
            bool hadVehicle = spawnVehicle.Vehicle;
            spawnVehicle.AttachVehicle();
            if (hadVehicle || !spawnVehicle.Vehicle) return;

            if (_isMounting) SetMountBlocked(false);
            OnVehicleAttached?.Invoke(spawnVehicle.Vehicle.transform);
            OnAutoForwardChanged?.Invoke(true, spawnVehicle.ForwardTurnSpeed);
            OnVehicleSoundChanged?.Invoke(spawnVehicle.SkillName, true);
            OnSkillSound?.Invoke(spawnVehicle.SkillName, ESoundPhase.Mount, null);
        }

        /// <summary>
        /// 탑승 연출 중 사격·구르기·공중 점프를 막거나 푼다. 사격을 막으면 진행 중인 차지는 취소된다.
        /// 막기 직전의 활성화 상태를 기억해 두고, 풀 때는 원래 켜져 있던 스킬만 다시 켠다 (외부에서 꺼 둔 스킬 유지).
        /// </summary>
        /// <param name="isBlocked">막으면 true</param>
        private void SetMountBlocked(bool isBlocked)
        {
            _isMounting = isBlocked;
            if (isBlocked)
            {
                // 구르는 중에는 구르기 때문에 Fire 가 꺼져 있으므로 구르기 전 상태를 쓴다
                _wasFireEnabled = _isRolling ? _wasFireEnabledOnRoll : IsSkillEnabled(FIRE_SKILL_NAME);
                _wasRollEnabled = IsSkillEnabled(ROLL_SKILL_NAME);
                _wasMultiJumpEnabled = IsSkillEnabled(MULTI_JUMP_SKILL_NAME);
                SetSkillEnabled(FIRE_SKILL_NAME, false);
                SetSkillEnabled(ROLL_SKILL_NAME, false);
                SetSkillEnabled(MULTI_JUMP_SKILL_NAME, false);
                return;
            }

            // 아직 구르는 중이면 구르기가 끝날 때(NotifyRollEnded) 켜진다
            if (_wasFireEnabled && !_isRolling) SetSkillEnabled(FIRE_SKILL_NAME, true);
            if (_wasRollEnabled) SetSkillEnabled(ROLL_SKILL_NAME, true);
            if (_wasMultiJumpEnabled) SetSkillEnabled(MULTI_JUMP_SKILL_NAME, true);
        }

        /// <summary>
        /// 탈것 스킬의 요시 점프 시작·종료를 상승 속도와 함께 알린다.
        /// </summary>
        /// <param name="isHovering">시작이면 true</param>
        private void OnHoverChangedCallback(bool isHovering)
        {
            OnHoverChanged?.Invoke(isHovering, _spawnVehicleSkill.HoverRiseSpeed);
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
                    OnSkillSoundLoop?.Invoke(fire.SkillName, ESoundPhase.Charge, true);
                    break;
                case EFirePhase.Fired:
                    OnFireChargeChanged?.Invoke(false);
                    OnSkillSoundLoop?.Invoke(fire.SkillName, ESoundPhase.Charge, false);
                    OnSkillSound?.Invoke(fire.SkillName, ESoundPhase.Shot, null);
                    OnFireRequested?.Invoke(fire.FirePosition, fire.IsCharged);
                    break;
                case EFirePhase.Canceled:
                    OnFireChargeChanged?.Invoke(false);
                    OnSkillSoundLoop?.Invoke(fire.SkillName, ESoundPhase.Charge, false);
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
        /// 스캔 스킬 이름인지 반환한다.
        /// </summary>
        /// <param name="skillName">확인할 스킬 이름</param>
        /// <returns>스캔 스킬이면 true. 스캔 스킬이 없으면 false</returns>
        private bool IsScanSkill(string skillName)
        {
            return _scanSkill && string.Equals(skillName, _scanSkill.SkillName, StringComparison.OrdinalIgnoreCase);
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
