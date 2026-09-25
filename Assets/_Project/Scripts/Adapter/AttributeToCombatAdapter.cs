using System;
using Attribute.Core;
using Combat;
using UnityEngine;

namespace Scripts.Adapter
{
    /// <summary>
    /// AttributeSet의 HP 변경을 구독해 CharacterCombat.Health에 전달하는 어댑터.
    /// CharacterCombat이 속성 시스템을 모르게 하기 위해 대신 구독해준다.
    /// </summary>
    [RequireComponent(typeof(CharacterCombat))]
    public class AttributeToCombatAdapter : MonoBehaviour
    {
        #region Serialized Fields
        [SerializeField] private AttributeSet _attributeSet;
        [Tooltip("사망 판정에 쓸 체력 속성 이름.")]
        [SerializeField] private string _hpAttributeName = "CurrentHp";
        #endregion

        #region Private Fields
        private CharacterCombat _combat;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            _combat = GetComponent<CharacterCombat>();

            if (!_attributeSet) _attributeSet = GetComponentInParent<AttributeSet>();

            if (!_attributeSet)
            {
                Debug.LogError($"[{name}] AttributeSet 미연결 — 체력이 전달되지 않아 사망 판정이 동작하지 않는다.", this);
            }
        }

        private void OnEnable()
        {
            if (!_attributeSet) return;

            // AttributeSet은 '변경'만 통지하고 초기값은 통지하지 않는다.
            // 초기값은 어댑터가 직접 당겨온다 (Pull).
            _combat.Health = _attributeSet.GetValue(_hpAttributeName);
            _attributeSet.AddPostAttributeChangedCallback(HandleAttributeChanged);
        }

        private void OnDisable()
        {
            if (!_attributeSet) return;

            _attributeSet.RemovePostAttributeChangedCallback(HandleAttributeChanged);
        }
        #endregion

        #region Private Methods
        private void HandleAttributeChanged(string attributeName, float newValue, float oldValue)
        {
            if (!string.Equals(attributeName, _hpAttributeName, StringComparison.OrdinalIgnoreCase)) return;

            _combat.Health = newValue;
        }
        #endregion
    }
}
