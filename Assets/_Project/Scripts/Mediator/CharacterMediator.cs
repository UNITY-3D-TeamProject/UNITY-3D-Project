using System;
using System.Collections.Generic;
using Attribute.Core;
using Attribute.Effect;
using Mediator.SubMediators;
using Skill.Buff;
using UnityEngine;
using UnityEngine.Serialization;

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
        [SerializeField] private AnimationMediator _animationMediator;
        [Tooltip("AI(적·군중) 전용 애니메이션 중재자. 플레이어는 비워 둔다.")]
        [SerializeField] private EnemyAnimationMediator _enemyAnimationMediator;
        [SerializeField] private PetMediator _petMediator;
        #endregion

        #region Private Fields
        private MediatorBase[] _mediators;
        // _attributeMediator 가 지정되지 않았을 때 직접 사용할 AttributeSet
        private AttributeSet _attributeSet;
        private bool _isDead;
        private bool _isRiding;
        private bool _isAutoForward;
        // 가장 최근 어트리뷰트 변경의 출처 정보. 피격은 Health 변경 알림 도중 동기로 오므로 피격 방향 계산에 쓴다
        private SEffectContext _lastChangeContext;
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
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            _mediators = GetComponentsInChildren<MediatorBase>();
            if (!_attributeMediator) _attributeSet = GetComponentInChildren<AttributeSet>();
        }

        private void OnEnable()
        {
            BindCallbacks();
            Subscribe();

            // 시점 입력 전에도 이동이 카메라 기준이 되도록 초기 방향을 한 번 전달
            if (_cameraMediator) _cameraMediator.PublishViewForward();
        }

        private void OnDisable()
        {
            UnbindCallbacks();
            Unsubscribe();
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
            if (_animationMediator && _cameraMediator) _animationMediator.SetGetAimPoint(_cameraMediator.GetAimPoint);
            if (_skillMediator && _moveMediator) _skillMediator.SetGetGrounded(_moveMediator.IsGrounded);
            if (_skillMediator && _cameraMediator) _skillMediator.SetGetAimRay(_cameraMediator.GetAimRay);
            if (_attributeMediator && _combatMediator) _attributeMediator.SetIsDamageBlocked(_combatMediator.IsDamageBlocked);
            if (_attributeMediator && _combatMediator) _attributeMediator.SetDamageModifier(_combatMediator.ModifyDamage);
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
            if (_animationMediator) _animationMediator.ClearGetAimPoint();
            if (_skillMediator) _skillMediator.ClearGetGrounded();
            if (_skillMediator) _skillMediator.ClearGetAimRay();
            if (_attributeMediator) _attributeMediator.ClearIsDamageBlocked();
            if (_attributeMediator) _attributeMediator.ClearDamageModifier();
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
            if (_animationMediator) _animationMediator.SetViewForward(viewForward);
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
            _animationMediator.NotifyRollStarted(GetRollLookDirection(direction));
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
                if (_animationMediator) _animationMediator.NotifyHit(hitSource);
                if (_enemyAnimationMediator) _enemyAnimationMediator.NotifyHit(hitSource);
                if (_petMediator) _petMediator.NotifyHit(_animationMediator ? _animationMediator.GetHitDirection(hitSource) : 0);
            }
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
            Debug.Log($"{name} is Death!");
            OnDeath?.Invoke();
            DisableControl();
            if (_petMediator) _petMediator.NotifyDeath();

            if (!_animationMediator && !_enemyAnimationMediator)
            {
                EndDeath();
                return;
            }

            if (_animationMediator) _animationMediator.NotifyDeath();
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
            if (_animationMediator) _animationMediator.OnDeathAnimationEnded += OnDeathAnimationEndedCallback;
            if (_moveMediator && _animationMediator) _moveMediator.OnMoveDirectionChanged += _animationMediator.SetMoveDirection;
            if (_skillMediator && _animationMediator) _skillMediator.OnFireRequested += _animationMediator.NotifyFire;
            if (_skillMediator && _animationMediator) _skillMediator.OnFireChargeChanged += _animationMediator.NotifyFireCharge;
            if (_moveMediator && _animationMediator) _moveMediator.OnJumpStarted += _animationMediator.NotifyJumpStarted;
            if (_moveMediator && _animationMediator) _moveMediator.OnJumpEnded += _animationMediator.NotifyJumpEnded;
            if (_moveMediator && _animationMediator) _moveMediator.OnRollStarted += OnRollStartedCallback;
            if (_moveMediator && _animationMediator) _moveMediator.OnRollEnded += _animationMediator.NotifyRollEnded;
            if (_moveMediator && _rotateMediator) _moveMediator.OnRollStarted += OnRollLookCallback;
            if (_moveMediator && _rotateMediator) _moveMediator.OnRollEnded += _rotateMediator.UnlockLookDirection;
            if (_moveMediator && _skillMediator) _moveMediator.OnAirJumpRequested += _skillMediator.RequestMultiJump;
            if (_skillMediator && _moveMediator) _skillMediator.OnMultiJumpRequested += _moveMediator.CommandAirJump;
            if (_skillMediator) _skillMediator.OnBuffRatiosChanged += OnBuffRatiosChangedCallback;
            if (_skillMediator) _skillMediator.OnRidingChanged += OnRidingChangedCallback;
            if (_skillMediator && _animationMediator) _skillMediator.OnRidingChanged += _animationMediator.NotifyRidingChanged;
            if (_skillMediator && _moveMediator) _skillMediator.OnMountJumpRequested += _moveMediator.CommandAirJump;
            if (_skillMediator && _animationMediator) _skillMediator.OnVehicleAttached += _animationMediator.SetRidingBoard;
            if (_moveMediator && _skillMediator) _moveMediator.OnJumpEnded += _skillMediator.NotifyLanded;
            if (_moveMediator && _skillMediator) _moveMediator.OnJumpStarted += _skillMediator.NotifyJumpStarted;
            if (_moveMediator && _skillMediator) _moveMediator.OnRollStarted += _skillMediator.NotifyRollStarted;
            if (_moveMediator && _skillMediator) _moveMediator.OnRollEnded += _skillMediator.NotifyRollEnded;
            if (_moveMediator && _skillMediator) _moveMediator.OnJumpReleased += _skillMediator.NotifyJumpReleased;
            if (_skillMediator && _moveMediator) _skillMediator.OnHoverChanged += _moveMediator.SetHover;
            if (_skillMediator) _skillMediator.OnAutoForwardChanged += OnAutoForwardChangedCallback;
            if (_moveMediator && _cameraMediator) _moveMediator.OnRollStarted += _cameraMediator.NotifyRollStarted;
            if (_moveMediator && _cameraMediator) _moveMediator.OnRollEnded += _cameraMediator.NotifyRollEnded;
            if (_skillMediator && _combatMediator) _skillMediator.OnFireChargeChanged += _combatMediator.NotifyFireCharge;
            if (_cameraMediator && _combatMediator) _cameraMediator.OnAimChanged += _combatMediator.NotifyAim;
            if (_cameraMediator && _animationMediator) _cameraMediator.OnAimChanged += _animationMediator.NotifyAim;
            if (_combatMediator && _animationMediator) _combatMediator.OnInvincibilityEnded += _animationMediator.NotifyInvincibilityEnded;
            if (_combatMediator) _combatMediator.OnMoveSpeedReduceRequested += OnMoveSpeedReduceCallback;
            if (_moveMediator && _petMediator) _moveMediator.OnJumpStarted += _petMediator.NotifyJumpStarted;
            if (_moveMediator && _petMediator) _moveMediator.OnJumpEnded += _petMediator.NotifyJumpEnded;
            if (_animationMediator && _petMediator) _animationMediator.OnSpecialIdleStarted += _petMediator.NotifySpecialIdle;
            if (_enemyAnimationMediator) _enemyAnimationMediator.OnDeathAnimationEnded += OnDeathAnimationEndedCallback;
            if (_moveMediator && _enemyAnimationMediator) _moveMediator.OnMoveDirectionChanged += _enemyAnimationMediator.SetMoveDirection;
            if (_moveMediator && _enemyAnimationMediator) _moveMediator.OnJumpStarted += _enemyAnimationMediator.NotifyJumpStarted;
            if (_moveMediator && _enemyAnimationMediator) _moveMediator.OnJumpEnded += _enemyAnimationMediator.NotifyJumpEnded;
            if (_skillMediator && _enemyAnimationMediator) _skillMediator.OnSkillUsed += _enemyAnimationMediator.NotifySkillUsed;
            if (_skillMediator && _enemyAnimationMediator) _skillMediator.OnSkillStopped += _enemyAnimationMediator.NotifySkillStopped;
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
            if (_animationMediator) _animationMediator.OnDeathAnimationEnded -= OnDeathAnimationEndedCallback;
            if (_moveMediator && _animationMediator) _moveMediator.OnMoveDirectionChanged -= _animationMediator.SetMoveDirection;
            if (_skillMediator && _animationMediator) _skillMediator.OnFireRequested -= _animationMediator.NotifyFire;
            if (_skillMediator && _animationMediator) _skillMediator.OnFireChargeChanged -= _animationMediator.NotifyFireCharge;
            if (_moveMediator && _animationMediator) _moveMediator.OnJumpStarted -= _animationMediator.NotifyJumpStarted;
            if (_moveMediator && _animationMediator) _moveMediator.OnJumpEnded -= _animationMediator.NotifyJumpEnded;
            if (_moveMediator && _animationMediator) _moveMediator.OnRollStarted -= OnRollStartedCallback;
            if (_moveMediator && _animationMediator) _moveMediator.OnRollEnded -= _animationMediator.NotifyRollEnded;
            if (_moveMediator && _rotateMediator) _moveMediator.OnRollStarted -= OnRollLookCallback;
            if (_moveMediator && _rotateMediator) _moveMediator.OnRollEnded -= _rotateMediator.UnlockLookDirection;
            if (_moveMediator && _skillMediator) _moveMediator.OnAirJumpRequested -= _skillMediator.RequestMultiJump;
            if (_skillMediator && _moveMediator) _skillMediator.OnMultiJumpRequested -= _moveMediator.CommandAirJump;
            if (_skillMediator) _skillMediator.OnBuffRatiosChanged -= OnBuffRatiosChangedCallback;
            if (_skillMediator) _skillMediator.OnRidingChanged -= OnRidingChangedCallback;
            if (_skillMediator && _animationMediator) _skillMediator.OnRidingChanged -= _animationMediator.NotifyRidingChanged;
            if (_skillMediator && _moveMediator) _skillMediator.OnMountJumpRequested -= _moveMediator.CommandAirJump;
            if (_skillMediator && _animationMediator) _skillMediator.OnVehicleAttached -= _animationMediator.SetRidingBoard;
            if (_moveMediator && _skillMediator) _moveMediator.OnJumpEnded -= _skillMediator.NotifyLanded;
            if (_moveMediator && _skillMediator) _moveMediator.OnJumpStarted -= _skillMediator.NotifyJumpStarted;
            if (_moveMediator && _skillMediator) _moveMediator.OnRollStarted -= _skillMediator.NotifyRollStarted;
            if (_moveMediator && _skillMediator) _moveMediator.OnRollEnded -= _skillMediator.NotifyRollEnded;
            if (_moveMediator && _skillMediator) _moveMediator.OnJumpReleased -= _skillMediator.NotifyJumpReleased;
            if (_skillMediator && _moveMediator) _skillMediator.OnHoverChanged -= _moveMediator.SetHover;
            if (_skillMediator) _skillMediator.OnAutoForwardChanged -= OnAutoForwardChangedCallback;
            if (_moveMediator && _cameraMediator) _moveMediator.OnRollStarted -= _cameraMediator.NotifyRollStarted;
            if (_moveMediator && _cameraMediator) _moveMediator.OnRollEnded -= _cameraMediator.NotifyRollEnded;
            if (_skillMediator && _combatMediator) _skillMediator.OnFireChargeChanged -= _combatMediator.NotifyFireCharge;
            if (_cameraMediator && _combatMediator) _cameraMediator.OnAimChanged -= _combatMediator.NotifyAim;
            if (_cameraMediator && _animationMediator) _cameraMediator.OnAimChanged -= _animationMediator.NotifyAim;
            if (_combatMediator && _animationMediator) _combatMediator.OnInvincibilityEnded -= _animationMediator.NotifyInvincibilityEnded;
            if (_combatMediator) _combatMediator.OnMoveSpeedReduceRequested -= OnMoveSpeedReduceCallback;
            if (_moveMediator && _petMediator) _moveMediator.OnJumpStarted -= _petMediator.NotifyJumpStarted;
            if (_moveMediator && _petMediator) _moveMediator.OnJumpEnded -= _petMediator.NotifyJumpEnded;
            if (_animationMediator && _petMediator) _animationMediator.OnSpecialIdleStarted -= _petMediator.NotifySpecialIdle;
            if (_enemyAnimationMediator) _enemyAnimationMediator.OnDeathAnimationEnded -= OnDeathAnimationEndedCallback;
            if (_moveMediator && _enemyAnimationMediator) _moveMediator.OnMoveDirectionChanged -= _enemyAnimationMediator.SetMoveDirection;
            if (_moveMediator && _enemyAnimationMediator) _moveMediator.OnJumpStarted -= _enemyAnimationMediator.NotifyJumpStarted;
            if (_moveMediator && _enemyAnimationMediator) _moveMediator.OnJumpEnded -= _enemyAnimationMediator.NotifyJumpEnded;
            if (_skillMediator && _enemyAnimationMediator) _skillMediator.OnSkillUsed -= _enemyAnimationMediator.NotifySkillUsed;
            if (_skillMediator && _enemyAnimationMediator) _skillMediator.OnSkillStopped -= _enemyAnimationMediator.NotifySkillStopped;
        }
        #endregion
    }
}
