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
        [SerializeField] private MoveSpeedReducer _moveSpeedReducer;
        [Header("Settings")]
        [Tooltip("CharacterCombat.Health 로 연결할 어트리뷰트 이름")]
        [SerializeField] private string _healthValueKey;
        [Tooltip("MoveSpeedReducer 의 감속량을 차감할 어트리뷰트 이름")]
        [SerializeField] private string _moveSpeedValueKey;
        #endregion

        #region Events
        /// <summary>CharacterCombat 이 피격되었을 때 발생한다.</summary>
        public event Action OnHit;
        /// <summary>CharacterCombat 이 사망했을 때 발생한다.</summary>
        public event Action OnDeath;
        /// <summary>
        /// 이동속도 감속량이 바뀌었을 때 (어트리뷰트 이름, 차감량)을 알린다. 차감량이 음수면 그만큼 복구한다.
        /// </summary>
        public event Action<string, float> OnMoveSpeedReduceRequested;
        #endregion

        #region Unity Lifecycle

        protected override void Awake()
        {
            base.Awake();
            ResolveComponent(ref _combatComponent);
            ResolveComponent(ref _moveSpeedReducer);
        }

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

        /// <summary>
        /// 사격 차지 상태를 MoveSpeedReducer 로 전달한다.
        /// </summary>
        /// <param name="isCharging">차지 중이면 true</param>
        public void NotifyFireCharge(bool isCharging)
        {
            if (_moveSpeedReducer == null) return;

            _moveSpeedReducer.SetCharging(isCharging);
        }

        /// <summary>
        /// 조준 상태를 MoveSpeedReducer 로 전달한다.
        /// </summary>
        /// <param name="isAiming">조준 중이면 true</param>
        public void NotifyAim(bool isAiming)
        {
            if (_moveSpeedReducer == null) return;

            _moveSpeedReducer.SetAiming(isAiming);
        }
        #endregion
        
        #region Protected Methods
        protected override void InitAttributeCallback()
        {
            AttributeCallback.TryAdd(_healthValueKey, data =>
            {
                if (_combatComponent != null) _combatComponent.Health = data.NewValue;
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
            if (_moveSpeedReducer != null) _moveSpeedReducer.OnReductionChanged += OnReductionChangedCallback;
            if (_combatComponent == null) return;
            
            _combatComponent.OnHit += OnHitCallback;
            _combatComponent.OnDeath += OnDeathCallback;
        }

        /// <summary>
        /// 컴포넌트 이벤트 에 대한 언바인딩 실행
        /// </summary>
        private void UnBindRequest()
        {
            if (_moveSpeedReducer != null) _moveSpeedReducer.OnReductionChanged -= OnReductionChangedCallback;
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

        /// <summary>
        /// 감속량 변화분을 이동속도 어트리뷰트 차감 요청으로 바꿔 알린다.
        /// </summary>
        /// <param name="newReduction">변경 후 감속량</param>
        /// <param name="oldReduction">변경 전 감속량</param>
        private void OnReductionChangedCallback(float newReduction, float oldReduction)
        {
            OnMoveSpeedReduceRequested?.Invoke(_moveSpeedValueKey, newReduction - oldReduction);
        }
        #endregion
    }
}
