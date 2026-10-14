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
        [Tooltip("피격 후 무적. 없으면 무적을 쓰지 않는다 (자동 탐색 안 함).")]
        [SerializeField] private HitInvincibility _hitInvincibility;
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
        /// <summary>피격 후 무적 시간이 끝났을 때 발생한다. 사망으로 멈춘 경우에는 발생하지 않는다.</summary>
        public event Action OnInvincibilityEnded;
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

        /// <summary>
        /// 어트리뷰트 감소를 막아야 하는지 반환한다. 무적 중인 체력 어트리뷰트만 막는다.
        /// </summary>
        /// <param name="key">감소하려는 어트리뷰트 이름</param>
        /// <returns>막아야 하면 true</returns>
        public bool IsDamageBlocked(string key)
        {
            if (_hitInvincibility == null || !_hitInvincibility.IsInvincible) return false;

            return string.Equals(key, _healthValueKey, StringComparison.OrdinalIgnoreCase);
        }
        #endregion
        
        #region Protected Methods
        protected override void InitAttributeCallback()
        {
            AttributeCallback.TryAdd(_healthValueKey, data =>
            {
                if (_combatComponent == null) return;

                // TODO: 치명타 피해 곱 적용 전, 판정 결과만 로그로 확인한다
                if (data.NewValue < data.OldValue)
                {
                    bool isCritical = _combatComponent.IsCriticalHit(data.Context.HitPoint, data.Context.Direction);
                    Debug.Log($"[{name}] {(isCritical ? "Critical" : "Normal")} Hit (HitPoint: {data.Context.HitPoint})", this);
                }

                _combatComponent.Health = data.NewValue;
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
            if (_hitInvincibility != null) _hitInvincibility.OnEnded += OnInvincibilityEndedCallback;
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
            if (_hitInvincibility != null) _hitInvincibility.OnEnded -= OnInvincibilityEndedCallback;
        }

        private void OnHitCallback()
        {
            // 치명타는 같은 프레임에 OnHit 다음 OnDeath 가 오므로, 이미 죽었으면 무적을 시작하지 않는다
            if (!_combatComponent.IsDead && _hitInvincibility != null) _hitInvincibility.Begin();

            OnHit?.Invoke();
        }

        private void OnDeathCallback()
        {
            if (_hitInvincibility != null) _hitInvincibility.Stop();

            OnDeath?.Invoke();
        }

        private void OnInvincibilityEndedCallback()
        {
            OnInvincibilityEnded?.Invoke();
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
