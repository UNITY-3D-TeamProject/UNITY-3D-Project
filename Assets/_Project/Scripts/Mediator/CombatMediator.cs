using Combat;
using UnityEngine;

namespace Mediator
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
        protected override void InitAttributeCallback()
        {
            AttributeCallback.Add(_healthValueKey, (float newValue, float oldValue) =>
            {
                if (_combatComponent != null) _combatComponent.Health = newValue;
            });
        }

        protected override void InitValue()
        {
            if (AttributeGetter == null) return;

            _combatComponent.Health = AttributeGetter.Invoke(_healthValueKey);
        }
        #endregion
        
        #region Private Methods
        /// <summary>
        /// Controller 에 대한 바인딩 실행
        /// </summary>
        private void BindRequest()
        {
            if (_combatComponent == null) return;
            
            _combatComponent.OnHit += OnHitCallback;
            _combatComponent.OnDeath += OnDeathCallback;
        }

        /// <summary>
        /// Controller 에 대한 언바인딩 실행
        /// </summary>
        private void UnBindRequest()
        {
            if (_combatComponent == null) return;
            
            _combatComponent.OnHit -= OnHitCallback;
            _combatComponent.OnDeath -= OnDeathCallback;
        }

        private void OnHitCallback()
        {
        }

        private void OnDeathCallback()
        {
        }
        #endregion
    }
}
