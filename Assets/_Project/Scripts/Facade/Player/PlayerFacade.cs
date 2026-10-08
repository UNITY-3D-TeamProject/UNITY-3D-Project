using System;
using UnityEngine;
using UnityEngine.InputSystem;
using Attribute.Core;
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
        #endregion

        #region Properties
        /// <summary>효과 적용 대상/주체로 사용할 플레이어 어트리뷰트. 없으면 null.</summary>
        public IEffectTarget EffectTarget => _characterMediator ? _characterMediator.EffectTarget : null;
        #endregion

        #region Events
        /// <summary>플레이어 어트리뷰트 값이 변경될 때 (이름, 변경 후 값, 변경 전 값)을 알린다.</summary>
        public event Action<string, float, float> OnAttributeChanged;
        /// <summary>플레이어가 피격되었을 때 발생한다.</summary>
        public event Action OnHit;
        /// <summary>플레이어가 사망했을 때 발생한다.</summary>
        public event Action OnDeath;
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
