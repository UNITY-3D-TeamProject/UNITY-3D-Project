using System;
using Attribute.Core;
using UnityEngine;

namespace Combat
{
    /// <summary>
    /// 피격·사망 체크를 담당하는 공용 전투 컴포넌트.
    /// 자신의 속성 접근은 IEffectTarget으로만 하며 AttributeSet을 직접 참조하지 않는다.
    /// 피격(ReceiveHit)과 속성 변경 알림(NotifyAttributeChanged) 두 입구로 사망 판정을 트리거하고,
    /// 결과는 이벤트로만 방출한다 — 중재자 형태를 몰라도 된다.
    /// </summary>
    public class CharacterCombat : MonoBehaviour, IHitReceiver
    {
        #region Serialized Fields
        [Header("Attribute Keys")]
        [Tooltip("사망 판정에 쓸 체력 속성 이름. 프로젝트마다 다를 수 있어 인스펙터로 노출한다.")]
        [SerializeField] private string _hpAttributeName = "CurrentHp";
        #endregion

        #region Private Fields
        private IEffectTarget _self;
        private bool _isDead;
        #endregion

        #region Properties
        public bool IsAlive => _self.GetValue(_hpAttributeName) > 0.0f;
        #endregion

        #region Events
        public event Action<SHitInfo> OnHit;
        public event Action OnDeath;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            _self = GetComponent<IEffectTarget>();
            Debug.Assert(_self != null, $"[{name}] : IEffectTarget component missing");
        }
        #endregion

        #region Public Methods
        public void ReceiveHit(SHitInfo hitInfo)
        {
            // Apply가 속성 변경 이벤트를 유발해 NotifyAttributeChanged → OnDeath로 이어질 수 있으므로
            // OnHit을 먼저 쏴서 "피격 → 사망" 순서를 보장한다.
            OnHit?.Invoke(hitInfo);
            hitInfo.Effect.Apply(_self, hitInfo.Attacker);
            CheckDeath();
        }

        /// <summary>
        /// 피격 외 경로로 체력이 바뀌었을 때(틱데미지, 낙하 대미지, 체력회복 등) 사망 판정을 걸기 위한 입구.
        /// AttributeSet.OnAttributeChange(string, float, float)와 시그니처가 동일해 델리게이트로 바로 등록 가능하다.
        /// </summary>
        public void NotifyAttributeChanged(string attributeName, float newValue, float oldValue)
        {
            if (!string.Equals(attributeName, _hpAttributeName, StringComparison.OrdinalIgnoreCase)) return;
            CheckDeath();
        }
        #endregion

        #region Private Methods
        private void CheckDeath()
        {
            if (_isDead) return;
            if (!_self.IsValidTarget(_hpAttributeName)) return;
            if (IsAlive) return;

            _isDead = true;
            OnDeath?.Invoke();
        }
        #endregion
    }
}
