using System;
using UnityEngine;
using UnityEngine.InputSystem;
using Attribute.Core;
using Attribute.Effect;
using Mediator;

namespace Facade.Player
{
    /// <summary>
    /// 플레이어와 외부(GameManager, Stage, UI 등) 사이의 단일 소통 창구.
    /// 캐릭터 내부와는 CharacterMediator 를 통해서만 소통하고, 설정창 입력은 PlayerInput 에서 받아 전달한다.
    /// </summary>
    public class PlayerFacade : MonoBehaviour
    {
        #region Constants
        private const string ACTION_MAP_PLAYER = "Player";
        private const string ACTION_MAP_UI = "UI";
        #endregion

        #region Serialized Fields
        [Header("References")]
        [SerializeField] private CharacterMediator _characterMediator;
        [SerializeField] private UnityEngine.InputSystem.PlayerInput _playerInput;
        [Header("Settings")]
        [Tooltip("설정창 열기 입력 액션 이름")]
        [SerializeField] private string _openSettingsActionName = "Player/OpenSettings";
        [Tooltip("설정창 닫기 입력 액션 이름")]
        [SerializeField] private string _closeSettingsActionName = "UI/CloseSettings";
        #endregion

        #region Private Fields
        private InputAction _openSettingsAction;
        private InputAction _closeSettingsAction;
        private bool _isSpawnEnded = false;
        #endregion

        #region Properties
        /// <summary>효과 적용 대상/주체로 사용할 플레이어 어트리뷰트. 없으면 null.</summary>
        public IEffectTarget EffectTarget => _characterMediator ? _characterMediator.EffectTarget : null;

        /// <summary>사격 차지 진행도(0~1). 차지 중이 아니면(발사·취소 후, 재차지 대기 중) 0.</summary>
        public float FireChargeRatio => _characterMediator ? _characterMediator.FireChargeRatio : 0.0f;
        
        public bool IsSpawnEnded => _isSpawnEnded;
        #endregion

        #region Events
        /// <summary>플레이어 어트리뷰트 값이 변경될 때 (이름, 변경 후 값, 변경 전 값)을 알린다.</summary>
        public event Action<string, float, float> OnAttributeChanged;
        /// <summary>플레이어가 피격되었을 때 발생한다.</summary>
        public event Action OnHit;
        /// <summary>플레이어가 사망한 즉시 발생한다.</summary>
        public event Action OnDeath;
        /// <summary>플레이어 사망 후 처리(사망 애니메이션)가 끝나 플레이어가 파괴되기 직전에 발생한다.</summary>
        public event Action OnDeathEnded;
        /// <summary>플레이어가 스킬을 사용했을 때 (스킬 이름, 쿨타임)을 알린다. 사용 1회당 1번 발생한다.</summary>
        public event Action<string, float> OnSkillUsed;
        /// <summary>카메라가 상호작용 대상을 바라보는지 여부가 바뀔 때 발생한다. 대상이 있으면 true.</summary>
        public event Action<bool> OnInteractableChanged;
        /// <summary>
        /// 사격 차지 상태가 바뀔 때 발생한다. 차지 시작이면 true, 발사·취소로 끝나면 false.
        /// true 를 받으면 false 를 받을 때까지 FireChargeRatio 를 읽어 게이지를 갱신한다.
        /// </summary>
        public event Action<bool> OnFireChargeChanged;
        /// <summary>
        /// 스폰 이펙트가 끝나 이동·스킬·시점 조작이 다시 켜진 직후 한 번 발생한다.
        /// 이펙트가 정상적으로 끝나지 않으면(준비 실패·도중 비활성화) 건너뛰고 발생한다.
        /// </summary>
        public event Action OnSpawnEffectEnded;
        /// <summary>설정창 열기 입력이 들어왔을 때 발생한다.</summary>
        public event Action OnOpenSettingsRequested;
        /// <summary>설정창 닫기 입력이 들어왔을 때 발생한다.</summary>
        public event Action OnCloseSettingsRequested;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            ResolveComponent(ref _characterMediator);
            ResolveComponent(ref _playerInput);
        }

        private void OnEnable()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// 어트리뷰트 존재 여부를 반환한다.
        /// </summary>
        /// <param name="key">어트리뷰트 이름</param>
        /// <returns>어트리뷰트가 정의되어 있으면 true</returns>
        public bool HasAttribute(string key)
        {
            return _characterMediator && _characterMediator.IsValidAttribute(key);
        }

        /// <summary>
        /// 어트리뷰트 값을 반환한다.
        /// </summary>
        /// <param name="key">어트리뷰트 이름</param>
        /// <returns>어트리뷰트 값. 어트리뷰트가 없으면 0</returns>
        public float GetAttribute(string key)
        {
            return _characterMediator ? _characterMediator.GetAttribute(key) : 0.0f;
        }

        /// <summary>
        /// 어트리뷰트 값을 value 로 설정한다.
        /// </summary>
        /// <param name="key">어트리뷰트 이름</param>
        /// <param name="value">설정할 값</param>
        public void SetAttribute(string key, float value)
        {
            if (!_characterMediator) return;

            _characterMediator.SetAttribute(key, value);
        }

        /// <summary>
        /// 출처 정보 없이 effect 를 플레이어에 적용한다.
        /// </summary>
        /// <param name="effect">적용할 효과</param>
        /// <returns>적용했으면 true. 효과·대상 어트리뷰트가 없거나 cursor 가 필요한 효과면 false</returns>
        public bool ApplyEffect(SOAttributeEffect effect)
        {
            return _characterMediator && _characterMediator.ApplyEffect(effect);
        }

        /// <summary>
        /// context.Effect 를 출처 정보와 함께 플레이어에 적용한다.
        /// </summary>
        /// <param name="context">적용할 효과(Effect)와 출처 정보. ValueSource 가 Attribute 인 효과는 Cursor 가 필요하다</param>
        /// <returns>적용했으면 true. 효과·대상 어트리뷰트가 없거나 필요한 cursor 가 없으면 false</returns>
        public bool ApplyEffect(SEffectContext context)
        {
            return _characterMediator && _characterMediator.ApplyEffect(context);
        }

        /// <summary>
        /// 스킬을 활성화/비활성화한다.
        /// </summary>
        /// <param name="skillName">대상 스킬 이름</param>
        /// <param name="isEnabled">활성화 여부</param>
        public void SetSkillEnabled(string skillName, bool isEnabled)
        {
            if (!_characterMediator) return;

            _characterMediator.SetSkillEnabled(skillName, isEnabled);
        }

        /// <summary>
        /// 스킬의 활성화 여부를 반환한다.
        /// </summary>
        /// <param name="skillName">대상 스킬 이름</param>
        /// <returns>스킬이 활성화되어 있으면 true</returns>
        public bool IsSkillEnabled(string skillName)
        {
            return _characterMediator && _characterMediator.IsSkillEnabled(skillName);
        }

        /// <summary>
        /// 피격 후 무적 지속 시간을 바꾼다. 진행 중인 무적에는 적용하지 않고 다음 피격부터 적용한다.
        /// </summary>
        /// <param name="duration">무적 지속 시간(초). 음수는 0 으로 처리한다</param>
        public void SetHitInvincibilityDuration(float duration)
        {
            if (!_characterMediator) return;

            _characterMediator.SetHitInvincibilityDuration(duration);
        }

        /// <summary>
        /// 펫을 켜거나 끈다. 펫이 꺼져 있는 동안에는 스캔을 쓸 수 없다. 펫 생성 전에 호출하면 생성 직후 적용한다.
        /// </summary>
        /// <param name="isEnabled">켜려면 true</param>
        public void SetPetEnabled(bool isEnabled)
        {
            if (!_characterMediator) return;

            _characterMediator.SetPetEnabled(isEnabled);
        }

        /// <summary>
        /// 펫이 켜져 있는지 반환한다.
        /// </summary>
        /// <returns>펫이 켜져 있으면 true</returns>
        public bool IsPetEnabled()
        {
            return _characterMediator && _characterMediator.IsPetEnabled();
        }

        /// <summary>
        /// 입력 맵을 UI 용으로 전환한다.
        /// </summary>
        public void SwitchToUIInput()
        {
            SwitchActionMap(ACTION_MAP_UI);
        }

        /// <summary>
        /// 입력 맵을 플레이어 조작용으로 전환한다.
        /// </summary>
        public void SwitchToPlayerInput()
        {
            SwitchActionMap(ACTION_MAP_PLAYER);
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// 중재자 이벤트와 설정창 입력 액션 구독
        /// </summary>
        private void Subscribe()
        {
            if (_characterMediator)
            {
                _characterMediator.OnAttributeChanged += RelayAttributeChanged;
                _characterMediator.OnHit += RelayHit;
                _characterMediator.OnDeath += RelayDeath;
                _characterMediator.OnDeathEnded += RelayDeathEnded;
                _characterMediator.OnSkillUsed += RelaySkillUsed;
                _characterMediator.OnInteractableChanged += RelayInteractableChanged;
                _characterMediator.OnFireChargeChanged += RelayFireChargeChanged;
                _characterMediator.OnSpawnEffectEnded += RelaySpawnEffectEnded;
            }

            if (!_playerInput || _playerInput.actions == null) return;

            _openSettingsAction = _playerInput.actions.FindAction(_openSettingsActionName);
            _closeSettingsAction = _playerInput.actions.FindAction(_closeSettingsActionName);

            if (_openSettingsAction != null) _openSettingsAction.performed += HandleOpenSettings;
            if (_closeSettingsAction != null) _closeSettingsAction.performed += HandleCloseSettings;
        }

        /// <summary>
        /// 중재자 이벤트와 설정창 입력 액션 구독 해지
        /// </summary>
        private void Unsubscribe()
        {
            if (_characterMediator)
            {
                _characterMediator.OnAttributeChanged -= RelayAttributeChanged;
                _characterMediator.OnHit -= RelayHit;
                _characterMediator.OnDeath -= RelayDeath;
                _characterMediator.OnDeathEnded -= RelayDeathEnded;
                _characterMediator.OnSkillUsed -= RelaySkillUsed;
                _characterMediator.OnInteractableChanged -= RelayInteractableChanged;
                _characterMediator.OnFireChargeChanged -= RelayFireChargeChanged;
                _characterMediator.OnSpawnEffectEnded -= RelaySpawnEffectEnded;
            }

            if (_openSettingsAction != null) _openSettingsAction.performed -= HandleOpenSettings;
            if (_closeSettingsAction != null) _closeSettingsAction.performed -= HandleCloseSettings;

            _openSettingsAction = null;
            _closeSettingsAction = null;
        }

        private void RelayAttributeChanged(string attributeName, float newValue, float oldValue)
        {
            OnAttributeChanged?.Invoke(attributeName, newValue, oldValue);
        }

        private void RelayHit()
        {
            OnHit?.Invoke();
        }

        private void RelayDeath()
        {
            OnDeath?.Invoke();
        }

        private void RelayDeathEnded()
        {
            OnDeathEnded?.Invoke();
        }

        private void RelaySkillUsed(string skillName, float cooldown)
        {
            OnSkillUsed?.Invoke(skillName, cooldown);
        }

        private void RelayInteractableChanged(bool hasTarget)
        {
            Debug.Log($"[PlayerFacade] OnInteractableChanged : {hasTarget}"); // TODO: 테스트용 로그, 검증 후 제거
            OnInteractableChanged?.Invoke(hasTarget);
        }

        private void RelayFireChargeChanged(bool isCharging)
        {
            OnFireChargeChanged?.Invoke(isCharging);
        }

        private void RelaySpawnEffectEnded()
        {
            _isSpawnEnded = true;
            OnSpawnEffectEnded?.Invoke();
        }

        private void HandleOpenSettings(InputAction.CallbackContext context)
        {
            OnOpenSettingsRequested?.Invoke();
        }

        private void HandleCloseSettings(InputAction.CallbackContext context)
        {
            OnCloseSettingsRequested?.Invoke();
        }

        private void SwitchActionMap(string mapName)
        {
            if (!_playerInput) return;

            _playerInput.SwitchCurrentActionMap(mapName);
        }

        /// <summary>
        /// 참조가 지정되지 않았으면 같은 GameObject 에서 찾아 채운다. 찾지 못하면 로그를 남긴다.
        /// </summary>
        /// <param name="component">확인할 참조</param>
        private void ResolveComponent<T>(ref T component) where T : Component
        {
            if (component) return;
            if (TryGetComponent(out component)) return;

            Debug.LogWarning($"[{name}] {GetType().Name} : {typeof(T).Name} is not assigned and not found", this);
        }
        #endregion
    }
}
