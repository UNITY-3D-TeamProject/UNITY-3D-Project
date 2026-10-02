using System;
using Combat;
using UnityEngine;

namespace Mediator.SubMediators
{
    public class CombatMediator : MediatorBase
    {
        #region Serialized Fields
        [Header("References")]
        [SerializeField] private CharacterCombat _combatComponent;
        [Header("Settings")]
        [Tooltip("CharacterCombat.Health 로 연결할 어트리뷰트 이름")]
        [SerializeField] private string _healthValueKey;
        #endregion

        #region Events
        /// <summary>CharacterCombat 이 피격되었을 때 발생한다.</summary>
        public event Action OnHit;
        /// <summary>CharacterCombat 이 사망했을 때 발생한다.</summary>
        public event Action OnDeath;
        #endregion

        #region Unity Lifecycle

        protected override void OnEnable()
        {
            BindRequest();
            base.OnEnable();
        }

        private void OnDisable()
        {
            UnBindRequest();
        }
        #endregion
        
        #region Protected Methods

        public bool IsDead()
        {
            if (_combatComponent == null) return true;
            
            return _combatComponent.IsDead;
        }
        #endregion
        
        #region Protected Methods
        protected override void InitAttributeCallback()
        {
            AttributeCallback.TryAdd(_healthValueKey, (float newValue, float oldValue) =>
            {
                if (_combatComponent != null) _combatComponent.Health = newValue;
            });
        }

        protected override void InitValue()
        {
            if (_combatComponent == null || AttributeGetter == null) return;

            _combatComponent.Health = AttributeGetter.Invoke(_healthValueKey);
        }
        #endregion
        
        #region Private Methods
        /// <summary>
        /// 컴포넌트 이벤트 에 대한 바인딩 실행
        /// </summary>
        private void BindRequest()
        {
            if (_combatComponent == null) return;
            
            _combatComponent.OnHit += OnHitCallback;
            _combatComponent.OnDeath += OnDeathCallback;
        }

        /// <summary>
        /// 컴포넌트 이벤트 에 대한 언바인딩 실행
        /// </summary>
        private void UnBindRequest()
        {
            if (_combatComponent == null) return;
            
            _combatComponent.OnHit -= OnHitCallback;
            _combatComponent.OnDeath -= OnDeathCallback;
        }

        private void OnHitCallback()
        {
            OnHit?.Invoke();
        }

        private void OnDeathCallback()
        {
            OnDeath?.Invoke();
        }
        #endregion
    }
}
