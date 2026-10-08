using System;
using System.Collections.Generic;
using Attribute.Core;
using Mediator.SubMediators;
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
        #endregion

        #region Private Fields
        private MediatorBase[] _mediators;
        // _attributeMediator 가 지정되지 않았을 때 직접 사용할 AttributeSet
        private AttributeSet _attributeSet;
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
        #endregion

        #region Events
        /// <summary>어트리뷰트 값이 변경될 때 (이름, 변경 후 값, 변경 전 값)을 알린다.</summary>
        public event Action<string, float, float> OnAttributeChanged;
        /// <summary>캐릭터가 피격되었을 때 발생한다.</summary>
        public event Action OnHit;
        /// <summary>캐릭터가 사망했을 때 GameObject 파괴 직전에 발생한다.</summary>
        public event Action OnDeath;
        /// <summary>스킬이 사용되었을 때 (스킬 이름, 쿨타임)을 알린다. 요청 1회당 1번 발생한다.</summary>
        public event Action<string, float> OnSkillUsed;
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
            }
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
            }
        }
        /// <summary>
        /// 중재자들이 원하는 값 변경시 알림 발송
        /// </summary>
        private void OnAttributeChangeCallback(string attributeName, float newValue, float oldValue, SEffectContext context)
        {
            foreach (var mediator in _mediators)
            {
                mediator.NotifyAttributeChanged(attributeName, newValue, oldValue, context);
            }
            OnAttributeChanged?.Invoke(attributeName, newValue, oldValue);
        }
        /// <summary>
        /// 카메라 중재자가 알린 시점 방향을 이동·회전 중재자로 전달
        /// </summary>
        private void SendViewForward(Vector3 viewForward)
        {
            if (_moveMediator) _moveMediator.SetViewForward(viewForward);
            if (_rotateMediator) _rotateMediator.SetViewForward(viewForward);
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

        private void OnSkillUsedCallback(string skillName, float cooldown)
        {
            OnSkillUsed?.Invoke(skillName, cooldown);
        }

        private void OnHitCallback()
        {
            Debug.Log($"{name} is hit!");
            OnHit?.Invoke();
        }

        private void OnDeathCallback()
        {
            Debug.Log($"{name} is Death!");
            OnDeath?.Invoke();
            Destroy(gameObject);
        }
        /// <summary>
        /// 사격 요청을 어트리뷰트 중재자에 전달해 회복을 정지한다.
        /// </summary>
        /// <param name="position">총알 생성 위치 (사용하지 않음)</param>
        private void OnFireCallback(Vector3 position)
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
            if (_combatMediator) _combatMediator.OnHit += OnHitCallback;
            if (_combatMediator) _combatMediator.OnDeath += OnDeathCallback;
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
            if (_combatMediator) _combatMediator.OnHit -= OnHitCallback;
            if (_combatMediator) _combatMediator.OnDeath -= OnDeathCallback;
        }
        #endregion
    }
}
