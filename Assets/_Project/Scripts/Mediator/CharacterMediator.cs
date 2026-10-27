using System;
using System.Collections.Generic;
using Attribute.Core;
using Attribute.Effect;
using CharacterSound;
using Mediator.SubMediators;
using Skill.Buff;
using UnityEngine;
using UnityEngine.Serialization;
using Visual;

namespace Mediator
{
    /// // Awake 가 늦게 실행되어 다른 Mediator 에 대한 조정 실행
    [DefaultExecutionOrder(100)]
    public class CharacterMediator : MonoBehaviour
    {
        #region Serialized Fields
        [Header("References")]
        [SerializeField] private AttributeMediator _attributeMediator;
        [FormerlySerializedAs("moveMediator")]
        [SerializeField] private MoveMediator _moveMediator;
        [FormerlySerializedAs("cameraMediator")]
        [SerializeField] private CameraMediator _cameraMediator;
        [SerializeField] private CombatMediator _combatMediator;
        [SerializeField] private SkillMediator _skillMediator;
        [SerializeField] private RotateMediator _rotateMediator;
        [SerializeField] private SpawnMediator _spawnMediator;
        [SerializeField] private PlayerAnimationMediator _playerAnimationMediator;
        [Tooltip("AI(적·군중) 전용 애니메이션 중재자. 플레이어는 비워 둔다.")]
        [SerializeField] private EnemyAnimationMediator _enemyAnimationMediator;
        [SerializeField] private PetMediator _petMediator;
        [Tooltip("생성 연출. 끝날 때까지 이동·스킬·시점 프리팹을 꺼 둔다. AI(적·군중)는 비워 둔다.")]
        [SerializeField] private SpawnEffectDriver _spawnEffectDriver;
        [Tooltip("소리 중재자. 비워 두면 자식에서 찾는다.")]
        [SerializeField] private SoundMediator _soundMediator;
        #endregion

        #region Private Fields
        private MediatorBase[] _mediators;
        // _attributeMediator 가 지정되지 않았을 때 직접 사용할 AttributeSet
        private AttributeSet _attributeSet;
        private bool _isDead;
        private bool _isRiding;
        private bool _isAutoForward;
        // 생성 연출 때문에 이동·스킬·시점 프리팹을 꺼 둔 상태인지 여부
        private bool _isSpawnLocked;
        private bool _isSyncingSound;
        // 가장 최근 어트리뷰트 변경의 출처 정보. 피격은 Health 변경 알림 도중 동기로 오므로 피격 방향 계산에 쓴다
        private SEffectContext _lastChangeContext;
        // 피격 시 조종부(AI)에 경직을 요청하는 콜백. 경직이 걸렸으면 true 를 돌려준다
        private Func<bool> _requestHitStun;
        #endregion

        #region Properties
        /// <summary>효과 적용 대상/주체로 사용할 어트리뷰트. 없으면 null.</summary>
        public IEffectTarget EffectTarget
        {
            get
            {
                if (_attributeMediator) return _attributeMediator.EffectTarget;
                return _attributeSet ? _attributeSet : null;
            }
        }

        /// <summary>사격 차지 진행도(0~1). 차지 중이 아니면 0.</summary>
        public float FireChargeRatio => _skillMediator ? _skillMediator.FireChargeRatio : 0.0f;
        #endregion

        #region Events
        /// <summary>어트리뷰트 값이 변경될 때 (이름, 변경 후 값, 변경 전 값)을 알린다.</summary>
        public event Action<string, float, float> OnAttributeChanged;
        /// <summary>캐릭터가 피격되었을 때 발생한다.</summary>
        public event Action OnHit;
        /// <summary>약점에 명중했을 때 맞은 지점을 알린다. OnHit 보다 먼저 발생한다.</summary>
        public event Action<Vector3?> OnCriticalHit;
        /// <summary>
        /// 피격 방향 기준 월드 위치(맞은 지점 또는 공격자 위치)를 알린다. OnHit 보다 먼저 발생한다.
        /// 기준 위치가 없는 피격이나 사망을 일으킨 피격에는 발생하지 않는다.
        /// </summary>
        public event Action<Vector3> OnHitFrom;
        /// <summary>캐릭터가 사망한 즉시 한 번 발생한다.</summary>
        public event Action OnDeath;
        /// <summary>사망 후 처리(사망 애니메이션)가 끝나 GameObject 를 파괴하기 직전에 한 번 발생한다.</summary>
        public event Action OnDeathEnded;
        /// <summary>스킬이 사용되었을 때 (스킬 이름, 쿨타임)을 알린다. 요청 1회당 1번 발생한다.</summary>
        public event Action<string, float> OnSkillUsed;
        /// <summary>상호작용 대상을 바라보는지 여부가 바뀔 때 발생한다. 대상이 있으면 true.</summary>
        public event Action<bool> OnInteractableChanged;
        /// <summary>사격 차지 상태 변경. 차지 시작이면 true, 발사·취소로 끝나면 false.</summary>
        public event Action<bool> OnFireChargeChanged;
        /// <summary>생성 연출이 끝나 이동·스킬·시점 프리팹을 다시 켠 직후 한 번 발생한다. 연출을 건너뛴 경우에도 발생한다.</summary>
        public event Action OnSpawnEffectEnded;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            _mediators = GetComponentsInChildren<MediatorBase>();
            if (!_attributeMediator) _attributeSet = GetComponentInChildren<AttributeSet>();
            if (!_soundMediator) _soundMediator = GetComponentInChildren<SoundMediator>();
        }

        private void OnEnable()
        {
            BindCallbacks();
            Subscribe();

            // 시점 입력 전에도 이동이 카메라 기준이 되도록 초기 방향을 한 번 전달
            if (_cameraMediator) _cameraMediator.PublishViewForward();

            // 비활성 중 놓친 구르기·탑승 종료와 초기 탑승 상태를 카메라에 맞춘다
            SyncCameraSpeedFeedback();

            // 구독 전에 시작된 차지·탑승과 사망 상태를 소리에 맞춘다
            SyncSoundState();
        }

        private void Start()
        {
            if (!_spawnEffectDriver) return;

            // 준비 실패 등으로 연출을 이미 건너뛰었으면 조작을 막지 않고 바로 알린다
            if (_spawnEffectDriver.IsEnded)
            {
                OnSpawnEffectEnded?.Invoke();
                return;
            }

            // 자식들의 초기화(Awake·OnEnable·Start)가 끝난 뒤 끈다
            _isSpawnLocked = true;
            SetSpawnLockedObjectsActive(false);
        }

        private void OnDisable()
        {
            UnbindCallbacks();
            Unsubscribe();

            // 이벤트 경로가 끊기므로 반복음과 탈것 반복음 설정을 정리한다
            if (_soundMediator) _soundMediator.StopAllSounds();

            // 이벤트 경로가 끊기므로 카메라 속도감 효과를 해제한다
            if (_cameraMediator) _cameraMediator.SyncSpeedFeedback(false, false);
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// 어트리뷰트 존재 여부를 반환한다.
        /// </summary>
        /// <param name="key">어트리뷰트 이름</param>
        /// <returns>어트리뷰트가 정의되어 있으면 true</returns>
        public bool IsValidAttribute(string key)
        {
            IEffectTarget target = EffectTarget;
            return target != null && target.IsValidTarget(key);
        }

        /// <summary>
        /// 어트리뷰트 값을 반환한다.
        /// </summary>
        /// <param name="key">어트리뷰트 이름</param>
        /// <returns>어트리뷰트 값. 어트리뷰트가 없으면 0</returns>
        public float GetAttribute(string key)
        {
            IEffectTarget target = EffectTarget;
            return target != null ? target.GetValue(key) : 0.0f;
        }

        /// <summary>
        /// 어트리뷰트 값을 value 로 설정한다. (차감이 아닌 절대값 설정)
        /// </summary>
        /// <param name="key">어트리뷰트 이름</param>
        /// <param name="value">설정할 값</param>
        public void SetAttribute(string key, float value)
        {
            EffectTarget?.SetValue(key, value);
        }

        /// <summary>
        /// 출처 정보 없이 effect 를 이 캐릭터에 적용한다.
        /// </summary>
        /// <param name="effect">적용할 효과</param>
        /// <returns>적용했으면 true. 효과·대상 어트리뷰트가 없거나 cursor 가 필요한 효과면 false</returns>
        public bool ApplyEffect(SOAttributeEffect effect)
        {
            return ApplyEffect(new SEffectContext(null, effect));
        }

        /// <summary>
        /// context.Effect 를 출처 정보와 함께 이 캐릭터에 적용한다. context 는 어트리뷰트 변경 콜백까지 전달된다.
        /// </summary>
        /// <param name="context">적용할 효과(Effect)와 출처 정보. ValueSource 가 Attribute 인 효과는 Cursor 가 필요하다</param>
        /// <returns>적용했으면 true. 효과·대상 어트리뷰트가 없거나 필요한 cursor 가 없으면 false</returns>
        public bool ApplyEffect(SEffectContext context)
        {
            SOAttributeEffect effect = context.Effect;
            IEffectTarget target = EffectTarget;
            if (!effect || target == null || !target.IsValidTarget(effect.TargetAttribute)) return false;

            bool needsCursor = effect.ValueSource == EValueSource.Attribute;
            if (needsCursor && (context.Cursor == null || !context.Cursor.IsValidTarget(effect.CursorAttribute))) return false;

            effect.Apply(target, context);
            return true;
        }

        /// <summary>
        /// 스킬을 활성화/비활성화한다.
        /// </summary>
        /// <param name="skillName">대상 스킬 이름</param>
        /// <param name="isEnabled">활성화 여부</param>
        public void SetSkillEnabled(string skillName, bool isEnabled)
        {
            if (!_skillMediator) return;

            _skillMediator.SetSkillEnabled(skillName, isEnabled);
        }

        /// <summary>
        /// 스킬의 활성화 여부를 반환한다.
        /// </summary>
        /// <param name="skillName">대상 스킬 이름</param>
        /// <returns>스킬이 활성화되어 있으면 true</returns>
        public bool IsSkillEnabled(string skillName)
        {
            return _skillMediator && _skillMediator.IsSkillEnabled(skillName);
        }

        /// <summary>
        /// 피격 후 무적 지속 시간을 바꾼다. 진행 중인 무적에는 적용하지 않고 다음 피격부터 적용한다.
        /// </summary>
        /// <param name="duration">무적 지속 시간(초). 음수는 0 으로 처리한다</param>
        public void SetHitInvincibilityDuration(float duration)
        {
            if (!_combatMediator) return;

            _combatMediator.SetHitInvincibilityDuration(duration);
        }

        /// <summary>
        /// 펫을 켜거나 끈다. 펫이 꺼져 있는 동안에는 스캔을 쓸 수 없다.
        /// </summary>
        /// <param name="isEnabled">켜려면 true</param>
        public void SetPetEnabled(bool isEnabled)
        {
            if (!_petMediator) return;

            _petMediator.SetPetEnabled(isEnabled);
            if (_skillMediator) _skillMediator.SetScanBlocked(!isEnabled);
        }

        /// <summary>
        /// 펫이 켜져 있는지 반환한다.
        /// </summary>
        /// <returns>펫이 켜져 있으면 true. 펫 중재자가 없으면 false</returns>
        public bool IsPetEnabled()
        {
            return _petMediator && _petMediator.IsPetEnabled();
        }

        /// <summary>
        /// 피격 시 경직을 요청할 조종부 콜백을 등록한다. 등록되어 있으면 경직이 걸린 피격에만 적 피격 모션을 재생한다.
        /// 비어 있으면 매 피격마다 재생한다.
        /// </summary>
        /// <param name="requestHitStun">경직을 시도하고, 걸렸으면 true 를 돌려주는 콜백</param>
        public void SetRequestHitStun(Func<bool> requestHitStun)
        {
            if (requestHitStun == null) return;
            _requestHitStun = requestHitStun;
        }

        /// <summary>
        /// 경직 요청 콜백 등록을 해제한다.
        /// </summary>
        public void ClearRequestHitStun()
        {
            _requestHitStun = null;
        }

        /// <summary>
        /// 이동·점프·구르기 입력을 잠그거나 푼다. 잠긴 동안에도 넉백 등 외부 변위와 중력은 적용된다.
        /// </summary>
        /// <param name="isLocked">잠그려면 true</param>
        public void SetMoveLocked(bool isLocked)
        {
            if (!_moveMediator) return;

            _moveMediator.SetMoveLocked(isLocked);
        }

        /// <summary>
        /// 붙잡힌 상태로 두거나 푼다. 붙잡힌 동안은 중력이 멈추고(상승 속도 0 인 일정 속도 상승) 외부 변위로만 움직인다.
        /// </summary>
        /// <param name="isHeld">붙잡으려면 true</param>
        public void SetHeld(bool isHeld)
        {
            if (!_moveMediator) return;

            _moveMediator.SetHover(isHeld, 0.0f);
        }

        /// <summary>
        /// 사격 쉐이크와 같은 임펄스로 카메라를 흔든다. 카메라 중재자가 없으면 무시한다.
        /// </summary>
        /// <param name="force">차지샷 쉐이크(1) 대비 세기</param>
        public void PlayCameraShake(float force)
        {
            if (!_cameraMediator) return;

            _cameraMediator.PlayShake(force);
        }

        /// <summary>
        /// 바라볼 방향으로 몸을 즉시 돌리고 카메라 피벗도 같은 방향으로 맞춘다.
        /// 생성 연출 중이면 연출이 끝날 때까지 방향을 고정하고, 끝나면 자동으로 풀린다(OnSpawnEffectEndedCallback).
        /// 연출이 이미 끝났거나 생성 연출 드라이버가 없으면 정렬 직후 고정을 풀어 정상 카메라 추종으로 복귀한다.
        /// 수평 성분이 없는 방향은 무시한다.
        /// </summary>
        /// <param name="direction">바라볼 월드 방향</param>
        public void SetLookDirection(Vector3 direction)
        {
            if (!_rotateMediator || !_cameraMediator) return;

            // 하위 중재자의 무시 규칙과 맞춘다. 잘못된 방향으로 기존 고정까지 풀지 않는다
            if (Vector3.ProjectOnPlane(direction, Vector3.up).sqrMagnitude <= Mathf.Epsilon) return;

            _rotateMediator.LockLookDirection(direction);
            _cameraMediator.AlignYaw(direction);

            // 연출 대기 중이면 종료 이벤트(OnSpawnEffectEndedCallback)가 풀 때까지 고정을 유지한다
            if (_spawnEffectDriver && !_spawnEffectDriver.IsEnded) return;

            _cameraMediator.PublishViewForward();
            _rotateMediator.UnlockLookDirection();
        }

        /// <summary>
        /// 사망 처리로 바뀐 상태를 되돌린다. 오브젝트 풀에서 다시 꺼낸 캐릭터가 재사용될 때 호출한다.
        /// </summary>
        public void ResetForReuse()
        {
            _isDead = false;
            if (_moveMediator) _moveMediator.enabled = true;
            if (_skillMediator) _skillMediator.enabled = true;
            if (_rotateMediator) _rotateMediator.enabled = true;
            SyncCameraSpeedFeedback();
            // 이전 반복음과 착지 판정 기준을 비우고, 활성 상태면 현재 상태로 다시 연결한다
            if (_soundMediator) _soundMediator.StopAllSounds();
            if (isActiveAndEnabled) BindSoundMoveState();
            SyncSoundState();
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// 필요한 바인딩 실행
        /// </summary>
        private void BindCallbacks()
        {
            if (_attributeMediator)
            {
                _attributeMediator.OnAttributeChanged += OnAttributeChangeCallback;
                foreach (var mediator in _mediators)
                {
                    mediator.SetGetAttribute(_attributeMediator.GetValue);
                }
            }
            else if (_attributeSet)
            {
                _attributeSet.AddOnAttributeChangedCallback(OnAttributeChangeCallback);
                foreach (var mediator in _mediators)
                {
                    mediator.SetGetAttribute(_attributeSet.GetValue);
                }
            }
            if (_spawnMediator)
            {
                if (_attributeMediator) _spawnMediator.SetEffectCursor(_attributeMediator.EffectTarget);
                else if (_attributeSet) _spawnMediator.SetEffectCursor(_attributeSet);
                if (_moveMediator) _spawnMediator.SetGetFireDirection(_moveMediator.GetViewDirection);
                if (_cameraMediator) _spawnMediator.SetGetAimPoint(_cameraMediator.GetAimPoint);
            }
            if (_playerAnimationMediator && _cameraMediator) _playerAnimationMediator.SetGetAimPoint(_cameraMediator.GetAimPoint);
            if (_skillMediator && _cameraMediator) _cameraMediator.SetGetRollFollowSettings(_skillMediator.GetRollFollowSettings);
            if (_skillMediator && _cameraMediator) _cameraMediator.SetGetRidingViewSettings(_skillMediator.GetRidingViewSettings);
            if (_skillMediator && _moveMediator) _skillMediator.SetGetGrounded(_moveMediator.IsGrounded);
            if (_skillMediator && _cameraMediator) _skillMediator.SetGetAimRay(_cameraMediator.GetAimRay);
            if (_attributeMediator && _combatMediator) _attributeMediator.SetIsDamageBlocked(_combatMediator.IsDamageBlocked);
            if (_attributeMediator && _combatMediator) _attributeMediator.SetDamageModifier(_combatMediator.ModifyDamage);
            BindSoundMoveState();
        }
        /// <summary>
        /// 이동 상태 조회 델리게이트를 소리 중재자로 전달한다. 사망 상태에서는 전달하지 않는다.
        /// </summary>
        private void BindSoundMoveState()
        {
            if (_isDead || !_moveMediator || !_soundMediator) return;

            _soundMediator.SetGetMoveState(
                _moveMediator.HasMoveIntent,
                _moveMediator.GetHorizontalSpeed,
                _moveMediator.IsGrounded,
                _moveMediator.IsRolling);
        }
        /// <summary>
        /// 등록된 바인딩 해제
        /// </summary>
        private void UnbindCallbacks()
        {
            if (_attributeMediator) _attributeMediator.OnAttributeChanged -= OnAttributeChangeCallback;
            else if (_attributeSet) _attributeSet.RemoveOnAttributeChangedCallback(OnAttributeChangeCallback);
            foreach (var mediator in _mediators)
            {
                mediator.ClearGetAttribute();
            }
            if (_spawnMediator)
            {
                _spawnMediator.ClearEffectCursor();
                _spawnMediator.ClearGetFireDirection();
                _spawnMediator.ClearGetAimPoint();
            }
            if (_playerAnimationMediator) _playerAnimationMediator.ClearGetAimPoint();
            if (_cameraMediator) _cameraMediator.ClearGetRollFollowSettings();
            if (_cameraMediator) _cameraMediator.ClearGetRidingViewSettings();
            if (_skillMediator) _skillMediator.ClearGetGrounded();
            if (_skillMediator) _skillMediator.ClearGetAimRay();
            if (_attributeMediator) _attributeMediator.ClearIsDamageBlocked();
            if (_attributeMediator) _attributeMediator.ClearDamageModifier();
            if (_soundMediator) _soundMediator.ClearGetMoveState();
        }
        /// <summary>
        /// 중재자들이 원하는 값 변경시 알림 발송
        /// </summary>
        private void OnAttributeChangeCallback(SAttributeChangeData data)
        {
            _lastChangeContext = data.Context;
            foreach (var mediator in _mediators)
            {
                mediator.NotifyAttributeChanged(data);
            }
            OnAttributeChanged?.Invoke(data.AttributeName, data.NewValue, data.OldValue);
        }
        /// <summary>
        /// 카메라 중재자가 알린 시점 방향을 이동·회전·애니메이션 중재자로 전달
        /// </summary>
        private void SendViewForward(Vector3 viewForward)
        {
            // 사망 후에는 시점을 돌려도 몸이 따라 돌지 않게 한다
            if (_isDead) return;

            if (_moveMediator) _moveMediator.SetViewForward(viewForward);
            if (_rotateMediator) _rotateMediator.SetViewForward(viewForward);
            if (_playerAnimationMediator) _playerAnimationMediator.SetViewForward(viewForward);
        }
        /// <summary>
        /// 스킬 중재자의 코스트 지불 요청을 어트리뷰트에 반영
        /// </summary>
        /// <param name="key">소모할 어트리뷰트 이름</param>
        /// <param name="amount">소모량</param>
        /// <returns>지불에 성공했으면 true</returns>
        private bool PayAttribute(string key, float amount)
        {
            if (_attributeMediator)
            {
                if (!_attributeMediator.IsValidTarget(key)) return false;

                _attributeMediator.SetValue(key, amount);
                return true;
            }

            if (!_attributeSet || !_attributeSet.IsValidTarget(key)) return false;

            float current = _attributeSet.GetValue(key);

            _attributeSet.SetValue(key, current - amount);
            return true;
        }

        /// <summary>
        /// 전투 중재자의 이동속도 감속 요청을 어트리뷰트에 반영한다.
        /// </summary>
        /// <param name="key">이동속도 어트리뷰트 이름</param>
        /// <param name="amount">차감량. 음수면 복구</param>
        private void OnMoveSpeedReduceCallback(string key, float amount)
        {
            PayAttribute(key, amount);
        }

        private void OnSkillUsedCallback(string skillName, float cooldown)
        {
            OnSkillUsed?.Invoke(skillName, cooldown);
        }

        private void OnInteractableChangedCallback(bool hasTarget)
        {
            OnInteractableChanged?.Invoke(hasTarget);
        }

        private void OnFireChargeChangedCallback(bool isCharging)
        {
            OnFireChargeChanged?.Invoke(isCharging);
        }

        /// <summary>
        /// 스캔 발동 위치를 펫에게 정하게 하고, 정해지면 그 위치에서 스캔을 시작한다. 펫이 없으면 캐릭터 위치에서 바로 시작한다.
        /// </summary>
        private void OnScanRequestedCallback()
        {
            if (_petMediator)
            {
                _petMediator.RequestScan(_skillMediator.BeginScan);
                return;
            }

            _skillMediator.BeginScan(transform.position);
        }

        /// <summary>
        /// 스킬 중재자가 보낸 버프 배율을 대상에 맞는 중재자로 전달한다.
        /// </summary>
        /// <param name="buffRatios">대상별 배율 목록</param>
        private void OnBuffRatiosChangedCallback(IReadOnlyList<SBuffRatio> buffRatios)
        {
            if (!_moveMediator) return;

            foreach (SBuffRatio buffRatio in buffRatios)
            {
                switch (buffRatio.Target)
                {
                    case EBuffTarget.MoveSpeed:
                        _moveMediator.SetMoveSpeedRatio(buffRatio.Ratio);
                        break;
                    case EBuffTarget.JumpPower:
                        _moveMediator.SetJumpPowerRatio(buffRatio.Ratio);
                        break;
                    case EBuffTarget.Gravity:
                        _moveMediator.SetGravityRatio(buffRatio.Ratio);
                        break;
                }
            }
        }

        private void OnRidingChangedCallback(bool isRiding)
        {
            _isRiding = isRiding;
        }

        /// <summary>
        /// 생성 연출이 끝나면 꺼 둔 이동·스킬·시점 프리팹을 켠 뒤 종료를 알린다.
        /// </summary>
        private void OnSpawnEffectEndedCallback()
        {
            // 캐릭터가 꺼지는 중(파괴·씬 종료)에 온 알림은 무시한다
            if (!_isSpawnLocked || !isActiveAndEnabled) return;

            _isSpawnLocked = false;
            SetSpawnLockedObjectsActive(true);

            // SetLookDirection 으로 고정해 둔 방향이 있으면, 카메라가 이미 그 방향을 보고 있는 상태로 전달한 뒤 풀어 스냅 없이 넘긴다
            if (_cameraMediator) _cameraMediator.PublishViewForward();
            if (_rotateMediator) _rotateMediator.UnlockLookDirection();

            OnSpawnEffectEnded?.Invoke();
        }

        /// <summary>
        /// 생성 연출 동안 조작을 막도록 이동·스킬·시점 중재자가 있는 프리팹을 끄거나 켠다.
        /// </summary>
        /// <param name="isActive">켜려면 true</param>
        private void SetSpawnLockedObjectsActive(bool isActive)
        {
            if (_moveMediator) _moveMediator.gameObject.SetActive(isActive);
            if (_skillMediator) _skillMediator.gameObject.SetActive(isActive);
            if (_cameraMediator) _cameraMediator.gameObject.SetActive(isActive);
        }

        /// <summary>
        /// 구르기·탑승의 실제 상태를 카메라 중재자에 즉시 맞춘다. 사망 상태에서는 모두 해제한다.
        /// </summary>
        private void SyncCameraSpeedFeedback()
        {
            if (!_cameraMediator) return;

            bool isRolling = !_isDead && _moveMediator && _moveMediator.IsRolling();
            bool isRiding = !_isDead && _skillMediator && _skillMediator.IsRiding();
            _cameraMediator.SyncSpeedFeedback(isRolling, isRiding);
        }

        /// <summary>
        /// 소리 중재자를 현재 상태에 맞춘다. 사망 상태면 아무것도 전달하지 않고, 아니면 차지 중 여부와 실제 탈것 부착 여부를 다시 전달한다.
        /// </summary>
        private void SyncSoundState()
        {
            // 동기화 중 되돌아온 요청은 무시한다
            if (!_soundMediator || _isSyncingSound || _isDead) return;

            _isSyncingSound = true;
            if (_skillMediator) _skillMediator.SyncSoundState();
            _isSyncingSound = false;
        }

        /// <summary>
        /// 점프 시작을 소리 중재자로 전달한다. 사망 후에는 전달하지 않는다.
        /// </summary>
        private void OnJumpStartedSoundCallback()
        {
            if (_isDead) return;

            _soundMediator.NotifyJumpStarted();
        }

        /// <summary>
        /// 이동 애니메이션의 발 디딤을 소리 중재자로 전달한다. 사망 후에는 전달하지 않는다.
        /// </summary>
        private void OnFootstepSoundCallback()
        {
            if (_isDead) return;

            _soundMediator.NotifyFootstep();
        }

        /// <summary>
        /// 스킬 단발음을 소리 중재자로 전달한다. 사망 후에는 전달하지 않는다.
        /// </summary>
        private void OnSkillSoundCallback(string skillName, ESoundPhase phase, Vector3? position)
        {
            if (_isDead) return;

            _soundMediator.NotifySkillSound(skillName, phase, position);
        }

        /// <summary>
        /// 스킬 반복음 시작·종료를 소리 중재자로 전달한다. 사망 후 시작은 전달하지 않고 종료는 항상 전달한다.
        /// </summary>
        private void OnSkillSoundLoopCallback(string skillName, ESoundPhase phase, bool isOn)
        {
            if (_isDead && isOn) return;

            _soundMediator.NotifySkillSoundLoop(skillName, phase, isOn);
        }

        /// <summary>
        /// 탈것 부착·해제를 소리 중재자로 전달한다. 사망 후 부착은 전달하지 않고 해제는 항상 전달한다.
        /// </summary>
        private void OnVehicleSoundChangedCallback(string skillName, bool isAttached)
        {
            if (_isDead && isAttached) return;

            _soundMediator.NotifyVehicleSound(skillName, isAttached);
        }

        /// <summary>
        /// 자동 전진 상태를 기록하고 이동 중재자로 전달한다.
        /// </summary>
        /// <param name="isEnabled">켜면 true</param>
        /// <param name="turnSpeed">전진 방향이 시점 방향을 따라 도는 속도(도/초)</param>
        private void OnAutoForwardChangedCallback(bool isEnabled, float turnSpeed)
        {
            _isAutoForward = isEnabled;
            if (_moveMediator) _moveMediator.SetAutoForward(isEnabled, turnSpeed);
        }

        /// <summary>
        /// 구르기 시작을 애니메이션 중재자로 전달한다. 탑승 중이면 몸·보드가 볼 방향으로 바꿔 전달한다.
        /// </summary>
        /// <param name="direction">구르기 방향 (월드 기준)</param>
        private void OnRollStartedCallback(Vector3 direction)
        {
            _playerAnimationMediator.NotifyRollStarted(GetRollLookDirection(direction));
        }

        /// <summary>
        /// 구르는 동안 몸이 볼 방향을 회전 중재자로 전달한다. 탑승 중이면 뒤쪽 구르기도 앞쪽을 보게 바꾼다.
        /// </summary>
        /// <param name="direction">구르기 방향 (월드 기준)</param>
        private void OnRollLookCallback(Vector3 direction)
        {
            _rotateMediator.LockLookDirection(GetRollLookDirection(direction));
        }

        /// <summary>
        /// 구르는 동안 몸이 볼 방향을 반환한다. 탑승 중이고 시점 뒤쪽으로 구르면 반대 방향(앞쪽)을 본다.
        /// 보드가 뒤로 갈 때 앞을 본 채 미끄러지는 규칙과 맞춘다. 실제 구르기 이동 방향은 바꾸지 않는다.
        /// 자동 전진 중에는 항상 전진 방향으로 구르므로 바꾸지 않는다 (전진 방향이 시점을 늦게 따라와 뒤쪽으로 판정될 수 있다).
        /// </summary>
        /// <param name="direction">구르기 방향 (월드 기준)</param>
        /// <returns>몸이 볼 방향</returns>
        private Vector3 GetRollLookDirection(Vector3 direction)
        {
            if (!_isRiding || _isAutoForward || !_moveMediator) return direction;

            bool isBackward = Vector3.Dot(direction, _moveMediator.GetViewDirection()) < 0.0f;
            return isBackward ? -direction : direction;
        }

        private void OnCriticalHitCallback(Vector3? hitPoint)
        {
            if (_isDead) return;

            OnCriticalHit?.Invoke(hitPoint);
        }

        private void OnHitCallback()
        {
            if (_isDead) return;

            Debug.Log($"{name} is hit!");
            // 치명타는 같은 프레임에 OnHit 다음 OnDeath 가 오므로, 이미 죽었으면 깜빡임을 시작하지 않는다
            if (!_combatMediator.IsDead())
            {
                Vector3? hitSource = GetHitSource(_lastChangeContext);
                // 경직 요청은 애니메이션 중재자 유무와 상관없이 매 피격마다 보낸다. 힛모션은 경직이 걸렸을 때만 재생한다
                bool shouldPlayEnemyHit = (_requestHitStun == null) || _requestHitStun();
                if (_playerAnimationMediator) _playerAnimationMediator.NotifyHit(hitSource);
                if (_enemyAnimationMediator && shouldPlayEnemyHit) _enemyAnimationMediator.NotifyHit(hitSource);
                if (_petMediator) _petMediator.NotifyHit(_playerAnimationMediator ? _playerAnimationMediator.GetHitDirection(hitSource) : 0);
                if (hitSource.HasValue) OnHitFrom?.Invoke(hitSource.Value);
            }
            if (_soundMediator) _soundMediator.NotifyHit();
            OnHit?.Invoke();
        }

        /// <summary>
        /// 피격 방향 계산에 쓸 기준 위치를 반환한다. 맞은 지점 → 공격자 위치 순으로 고른다.
        /// </summary>
        /// <param name="context">피격을 일으킨 어트리뷰트 변경의 출처 정보</param>
        /// <returns>기준 위치. 맞은 지점과 공격자가 모두 없으면 null</returns>
        private static Vector3? GetHitSource(SEffectContext context)
        {
            if (context.HitPoint.HasValue) return context.HitPoint;
            if (context.Cursor is Component cursor && cursor) return cursor.transform.position;
            return null;
        }

        /// <summary>
        /// 사망을 한 번만 처리한다. 사망 이벤트를 발행하고 조작을 막은 뒤 사망 애니메이션을 재생한다.
        /// 애니메이션 중재자가 없으면 바로 사망 후 처리를 끝낸다.
        /// </summary>
        private void OnDeathCallback()
        {
            if (_isDead) return;

            _isDead = true;
            // 아래에서 스킬 중재자가 꺼지며 Fire.Stop 이 발사할 수 있으므로, 그 전에 이동 표본 연결과 기존 반복음을 정리한다
            if (_soundMediator)
            {
                _soundMediator.ClearGetMoveState();
                _soundMediator.StopAllSounds();
            }
            Debug.Log($"{name} is Death!");
            OnDeath?.Invoke();
            DisableControl();
            SyncCameraSpeedFeedback();
            if (_petMediator) _petMediator.NotifyDeath();

            if (!_playerAnimationMediator && !_enemyAnimationMediator)
            {
                EndDeath();
                return;
            }

            if (_playerAnimationMediator) _playerAnimationMediator.NotifyDeath();
            if (_enemyAnimationMediator) _enemyAnimationMediator.NotifyDeath();
        }

        private void OnDeathAnimationEndedCallback()
        {
            EndDeath();
        }

        /// <summary>
        /// 사망 후 처리 완료를 알리고 GameObject 를 파괴한다.
        /// </summary>
        private void EndDeath()
        {
            OnDeathEnded?.Invoke();
            //Destroy(gameObject);
        }

        /// <summary>
        /// 사망 중 이동·스킬·회전 입력을 막도록 해당 중재자를 끈다.
        /// </summary>
        private void DisableControl()
        {
            if (_moveMediator) _moveMediator.enabled = false;
            if (_skillMediator) _skillMediator.enabled = false;
            if (_rotateMediator) _rotateMediator.enabled = false;
        }
        /// <summary>
        /// 사격 요청을 어트리뷰트 중재자에 전달해 회복을 정지한다.
        /// </summary>
        /// <param name="position">총알 생성 위치 (사용하지 않음)</param>
        /// <param name="isCharged">차지샷 여부 (사용하지 않음)</param>
        private void OnFireCallback(Vector3 position, bool isCharged)
        {
            _attributeMediator.PauseRegenOnFire();
        }
        /// <summary>
        /// 중재자 이벤트 구독
        /// </summary>
        private void Subscribe()
        {
            if (_cameraMediator) _cameraMediator.OnViewForwardChanged += SendViewForward;
            if (_skillMediator) _skillMediator.OnPayRequested += PayAttribute;
            if (_skillMediator && _moveMediator) _skillMediator.OnRollRequested += _moveMediator.CommandRoll;
            if (_skillMediator && _spawnMediator) _skillMediator.OnFireRequested += _spawnMediator.CommandSpawnBullet;
            if (_skillMediator && _attributeMediator) _skillMediator.OnFireRequested += OnFireCallback;
            if (_skillMediator) _skillMediator.OnSkillUsed += OnSkillUsedCallback;
            if (_skillMediator) _skillMediator.OnInteractableChanged += OnInteractableChangedCallback;
            if (_skillMediator) _skillMediator.OnFireChargeChanged += OnFireChargeChangedCallback;
            if (_skillMediator) _skillMediator.OnScanRequested += OnScanRequestedCallback;
            if (_combatMediator) _combatMediator.OnHit += OnHitCallback;
            if (_combatMediator) _combatMediator.OnCriticalHit += OnCriticalHitCallback;
            if (_combatMediator) _combatMediator.OnDeath += OnDeathCallback;
            if (_playerAnimationMediator) _playerAnimationMediator.OnDeathAnimationEnded += OnDeathAnimationEndedCallback;
            if (_moveMediator && _playerAnimationMediator) _moveMediator.OnMoveDirectionChanged += _playerAnimationMediator.SetMoveDirection;
            if (_skillMediator && _playerAnimationMediator) _skillMediator.OnFireRequested += _playerAnimationMediator.NotifyFire;
            if (_skillMediator && _playerAnimationMediator) _skillMediator.OnFireChargeChanged += _playerAnimationMediator.NotifyFireCharge;
            if (_moveMediator && _playerAnimationMediator) _moveMediator.OnJumpStarted += _playerAnimationMediator.NotifyJumpStarted;
            if (_moveMediator && _playerAnimationMediator) _moveMediator.OnJumpEnded += _playerAnimationMediator.NotifyJumpEnded;
            if (_moveMediator && _playerAnimationMediator) _moveMediator.OnFallStarted += _playerAnimationMediator.NotifyFallStarted;
            if (_moveMediator && _playerAnimationMediator) _moveMediator.OnFallEnded += _playerAnimationMediator.NotifyJumpEnded;
            if (_moveMediator && _playerAnimationMediator) _moveMediator.OnRollStarted += OnRollStartedCallback;
            if (_moveMediator && _playerAnimationMediator) _moveMediator.OnRollEnded += _playerAnimationMediator.NotifyRollEnded;
            if (_moveMediator && _rotateMediator) _moveMediator.OnRollStarted += OnRollLookCallback;
            if (_moveMediator && _rotateMediator) _moveMediator.OnRollEnded += _rotateMediator.UnlockLookDirection;
            if (_moveMediator && _skillMediator) _moveMediator.OnAirJumpRequested += _skillMediator.RequestMultiJump;
            if (_skillMediator && _moveMediator) _skillMediator.OnMultiJumpRequested += _moveMediator.CommandAirJump;
            if (_skillMediator) _skillMediator.OnBuffRatiosChanged += OnBuffRatiosChangedCallback;
            if (_skillMediator) _skillMediator.OnRidingChanged += OnRidingChangedCallback;
            if (_skillMediator && _playerAnimationMediator) _skillMediator.OnRidingChanged += _playerAnimationMediator.NotifyRidingChanged;
            if (_skillMediator && _cameraMediator) _skillMediator.OnRidingChanged += _cameraMediator.NotifyRidingChanged;
            if (_skillMediator && _moveMediator) _skillMediator.OnMountJumpRequested += _moveMediator.CommandAirJump;
            if (_skillMediator && _playerAnimationMediator) _skillMediator.OnVehicleAttached += _playerAnimationMediator.SetRidingBoard;
            if (_moveMediator && _skillMediator) _moveMediator.OnJumpEnded += _skillMediator.NotifyLanded;
            if (_moveMediator && _skillMediator) _moveMediator.OnJumpStarted += _skillMediator.NotifyJumpStarted;
            if (_moveMediator && _skillMediator) _moveMediator.OnRollStarted += _skillMediator.NotifyRollStarted;
            if (_moveMediator && _skillMediator) _moveMediator.OnRollEnded += _skillMediator.NotifyRollEnded;
            if (_moveMediator && _skillMediator) _moveMediator.OnJumpReleased += _skillMediator.NotifyJumpReleased;
            if (_skillMediator && _moveMediator) _skillMediator.OnHoverChanged += _moveMediator.SetHover;
            if (_skillMediator) _skillMediator.OnAutoForwardChanged += OnAutoForwardChangedCallback;
            if (_moveMediator && _cameraMediator) _moveMediator.OnRollStarted += _cameraMediator.NotifyRollStarted;
            if (_moveMediator && _cameraMediator) _moveMediator.OnRollEnded += _cameraMediator.NotifyRollEnded;
            if (_skillMediator && _cameraMediator) _skillMediator.OnFireRequested += _cameraMediator.NotifyFire;
            if (_skillMediator && _combatMediator) _skillMediator.OnFireChargeChanged += _combatMediator.NotifyFireCharge;
            if (_cameraMediator && _combatMediator) _cameraMediator.OnAimChanged += _combatMediator.NotifyAim;
            if (_cameraMediator && _playerAnimationMediator) _cameraMediator.OnAimChanged += _playerAnimationMediator.NotifyAim;
            if (_combatMediator && _playerAnimationMediator) _combatMediator.OnInvincibilityEnded += _playerAnimationMediator.NotifyInvincibilityEnded;
            if (_combatMediator) _combatMediator.OnMoveSpeedReduceRequested += OnMoveSpeedReduceCallback;
            if (_moveMediator && _petMediator) _moveMediator.OnJumpStarted += _petMediator.NotifyJumpStarted;
            if (_moveMediator && _petMediator) _moveMediator.OnJumpEnded += _petMediator.NotifyJumpEnded;
            if (_playerAnimationMediator && _petMediator) _playerAnimationMediator.OnSpecialIdleStarted += _petMediator.NotifySpecialIdle;
            if (_enemyAnimationMediator) _enemyAnimationMediator.OnDeathAnimationEnded += OnDeathAnimationEndedCallback;
            if (_moveMediator && _enemyAnimationMediator) _moveMediator.OnMoveDirectionChanged += _enemyAnimationMediator.SetMoveDirection;
            if (_moveMediator && _enemyAnimationMediator) _moveMediator.OnJumpStarted += _enemyAnimationMediator.NotifyJumpStarted;
            if (_moveMediator && _enemyAnimationMediator) _moveMediator.OnJumpEnded += _enemyAnimationMediator.NotifyJumpEnded;
            if (_skillMediator && _enemyAnimationMediator) _skillMediator.OnSkillUsed += _enemyAnimationMediator.NotifySkillUsed;
            if (_skillMediator && _enemyAnimationMediator) _skillMediator.OnSkillStopped += _enemyAnimationMediator.NotifySkillStopped;
            if (_spawnEffectDriver) _spawnEffectDriver.OnSpawnEffectEnded += OnSpawnEffectEndedCallback;
            if (_moveMediator && _soundMediator) _moveMediator.OnJumpStarted += OnJumpStartedSoundCallback;
            if (_playerAnimationMediator && _soundMediator) _playerAnimationMediator.OnFootstep += OnFootstepSoundCallback;
            if (_skillMediator && _soundMediator) _skillMediator.OnSkillSound += OnSkillSoundCallback;
            if (_skillMediator && _soundMediator) _skillMediator.OnSkillSoundLoop += OnSkillSoundLoopCallback;
            if (_skillMediator && _soundMediator) _skillMediator.OnVehicleSoundChanged += OnVehicleSoundChangedCallback;
            if (_soundMediator) _soundMediator.OnSyncRequested += SyncSoundState;
        }
        /// <summary>
        /// 중재자 이벤트 구독 해지
        /// </summary>
        private void Unsubscribe()
        {
            if (_cameraMediator) _cameraMediator.OnViewForwardChanged -= SendViewForward;
            if (_skillMediator) _skillMediator.OnPayRequested -= PayAttribute;
            if (_skillMediator && _moveMediator) _skillMediator.OnRollRequested -= _moveMediator.CommandRoll;
            if (_skillMediator && _spawnMediator) _skillMediator.OnFireRequested -= _spawnMediator.CommandSpawnBullet;
            if (_skillMediator && _attributeMediator) _skillMediator.OnFireRequested -= OnFireCallback;
            if (_skillMediator) _skillMediator.OnSkillUsed -= OnSkillUsedCallback;
            if (_skillMediator) _skillMediator.OnInteractableChanged -= OnInteractableChangedCallback;
            if (_skillMediator) _skillMediator.OnFireChargeChanged -= OnFireChargeChangedCallback;
            if (_skillMediator) _skillMediator.OnScanRequested -= OnScanRequestedCallback;
            if (_combatMediator) _combatMediator.OnHit -= OnHitCallback;
            if (_combatMediator) _combatMediator.OnCriticalHit -= OnCriticalHitCallback;
            if (_combatMediator) _combatMediator.OnDeath -= OnDeathCallback;
            if (_playerAnimationMediator) _playerAnimationMediator.OnDeathAnimationEnded -= OnDeathAnimationEndedCallback;
            if (_moveMediator && _playerAnimationMediator) _moveMediator.OnMoveDirectionChanged -= _playerAnimationMediator.SetMoveDirection;
            if (_skillMediator && _playerAnimationMediator) _skillMediator.OnFireRequested -= _playerAnimationMediator.NotifyFire;
            if (_skillMediator && _playerAnimationMediator) _skillMediator.OnFireChargeChanged -= _playerAnimationMediator.NotifyFireCharge;
            if (_moveMediator && _playerAnimationMediator) _moveMediator.OnJumpStarted -= _playerAnimationMediator.NotifyJumpStarted;
            if (_moveMediator && _playerAnimationMediator) _moveMediator.OnJumpEnded -= _playerAnimationMediator.NotifyJumpEnded;
            if (_moveMediator && _playerAnimationMediator) _moveMediator.OnFallStarted -= _playerAnimationMediator.NotifyFallStarted;
            if (_moveMediator && _playerAnimationMediator) _moveMediator.OnFallEnded -= _playerAnimationMediator.NotifyJumpEnded;
            if (_moveMediator && _playerAnimationMediator) _moveMediator.OnRollStarted -= OnRollStartedCallback;
            if (_moveMediator && _playerAnimationMediator) _moveMediator.OnRollEnded -= _playerAnimationMediator.NotifyRollEnded;
            if (_moveMediator && _rotateMediator) _moveMediator.OnRollStarted -= OnRollLookCallback;
            if (_moveMediator && _rotateMediator) _moveMediator.OnRollEnded -= _rotateMediator.UnlockLookDirection;
            if (_moveMediator && _skillMediator) _moveMediator.OnAirJumpRequested -= _skillMediator.RequestMultiJump;
            if (_skillMediator && _moveMediator) _skillMediator.OnMultiJumpRequested -= _moveMediator.CommandAirJump;
            if (_skillMediator) _skillMediator.OnBuffRatiosChanged -= OnBuffRatiosChangedCallback;
            if (_skillMediator) _skillMediator.OnRidingChanged -= OnRidingChangedCallback;
            if (_skillMediator && _playerAnimationMediator) _skillMediator.OnRidingChanged -= _playerAnimationMediator.NotifyRidingChanged;
            if (_skillMediator && _cameraMediator) _skillMediator.OnRidingChanged -= _cameraMediator.NotifyRidingChanged;
            if (_skillMediator && _moveMediator) _skillMediator.OnMountJumpRequested -= _moveMediator.CommandAirJump;
            if (_skillMediator && _playerAnimationMediator) _skillMediator.OnVehicleAttached -= _playerAnimationMediator.SetRidingBoard;
            if (_moveMediator && _skillMediator) _moveMediator.OnJumpEnded -= _skillMediator.NotifyLanded;
            if (_moveMediator && _skillMediator) _moveMediator.OnJumpStarted -= _skillMediator.NotifyJumpStarted;
            if (_moveMediator && _skillMediator) _moveMediator.OnRollStarted -= _skillMediator.NotifyRollStarted;
            if (_moveMediator && _skillMediator) _moveMediator.OnRollEnded -= _skillMediator.NotifyRollEnded;
            if (_moveMediator && _skillMediator) _moveMediator.OnJumpReleased -= _skillMediator.NotifyJumpReleased;
            if (_skillMediator && _moveMediator) _skillMediator.OnHoverChanged -= _moveMediator.SetHover;
            if (_skillMediator) _skillMediator.OnAutoForwardChanged -= OnAutoForwardChangedCallback;
            if (_moveMediator && _cameraMediator) _moveMediator.OnRollStarted -= _cameraMediator.NotifyRollStarted;
            if (_moveMediator && _cameraMediator) _moveMediator.OnRollEnded -= _cameraMediator.NotifyRollEnded;
            if (_skillMediator && _cameraMediator) _skillMediator.OnFireRequested -= _cameraMediator.NotifyFire;
            if (_skillMediator && _combatMediator) _skillMediator.OnFireChargeChanged -= _combatMediator.NotifyFireCharge;
            if (_cameraMediator && _combatMediator) _cameraMediator.OnAimChanged -= _combatMediator.NotifyAim;
            if (_cameraMediator && _playerAnimationMediator) _cameraMediator.OnAimChanged -= _playerAnimationMediator.NotifyAim;
            if (_combatMediator && _playerAnimationMediator) _combatMediator.OnInvincibilityEnded -= _playerAnimationMediator.NotifyInvincibilityEnded;
            if (_combatMediator) _combatMediator.OnMoveSpeedReduceRequested -= OnMoveSpeedReduceCallback;
            if (_moveMediator && _petMediator) _moveMediator.OnJumpStarted -= _petMediator.NotifyJumpStarted;
            if (_moveMediator && _petMediator) _moveMediator.OnJumpEnded -= _petMediator.NotifyJumpEnded;
            if (_playerAnimationMediator && _petMediator) _playerAnimationMediator.OnSpecialIdleStarted -= _petMediator.NotifySpecialIdle;
            if (_enemyAnimationMediator) _enemyAnimationMediator.OnDeathAnimationEnded -= OnDeathAnimationEndedCallback;
            if (_moveMediator && _enemyAnimationMediator) _moveMediator.OnMoveDirectionChanged -= _enemyAnimationMediator.SetMoveDirection;
            if (_moveMediator && _enemyAnimationMediator) _moveMediator.OnJumpStarted -= _enemyAnimationMediator.NotifyJumpStarted;
            if (_moveMediator && _enemyAnimationMediator) _moveMediator.OnJumpEnded -= _enemyAnimationMediator.NotifyJumpEnded;
            if (_skillMediator && _enemyAnimationMediator) _skillMediator.OnSkillUsed -= _enemyAnimationMediator.NotifySkillUsed;
            if (_skillMediator && _enemyAnimationMediator) _skillMediator.OnSkillStopped -= _enemyAnimationMediator.NotifySkillStopped;
            if (_spawnEffectDriver) _spawnEffectDriver.OnSpawnEffectEnded -= OnSpawnEffectEndedCallback;
            if (_moveMediator && _soundMediator) _moveMediator.OnJumpStarted -= OnJumpStartedSoundCallback;
            if (_playerAnimationMediator && _soundMediator) _playerAnimationMediator.OnFootstep -= OnFootstepSoundCallback;
            if (_skillMediator && _soundMediator) _skillMediator.OnSkillSound -= OnSkillSoundCallback;
            if (_skillMediator && _soundMediator) _skillMediator.OnSkillSoundLoop -= OnSkillSoundLoopCallback;
            if (_skillMediator && _soundMediator) _skillMediator.OnVehicleSoundChanged -= OnVehicleSoundChangedCallback;
            if (_soundMediator) _soundMediator.OnSyncRequested -= SyncSoundState;
        }
        #endregion
    }
}
