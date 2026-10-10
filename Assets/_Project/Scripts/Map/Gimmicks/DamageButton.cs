using System;
using UnityEngine;
using UnityEngine.Events;
using Attribute.Core;

namespace Map.Gimmicks
{
    /// <summary>
    /// 공격을 받아 HP가 0 이하가 되면 연결된 동작을 실행하는 버튼. 멀리서 쏴서 기믹을 작동시키는 데 쓴다.
    /// 예: On Pressed 에 FallingObstacle.Drop 을 연결
    /// 같은 오브젝트의 AttributeSet 에 CurrentHp 가 있어야 하고(값이 버튼의 내구도),
    /// 콜라이더는 Is Trigger를 끄고 레이어를 공격이 맞는 레이어(Obstacle)로 둔다.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    [RequireComponent(typeof(AttributeSet))]
    public class DamageButton : MonoBehaviour
    {
        #region Constants
        private const string HP_ATTRIBUTE = "CurrentHp";
        #endregion

        #region Serialized Fields
        [Header("Event")]
        [Tooltip("HP가 0 이하가 됐을 때 실행할 동작")]
        [SerializeField] private UnityEvent _onPressed = new UnityEvent();

        [Tooltip("켜면 처음 한 번만 실행한다. 끄면 실행한 뒤 HP를 처음 값으로 되돌려 다시 누를 수 있다.")]
        [SerializeField] private bool _isOneShot = true;
        #endregion

        #region Private Fields
        private AttributeSet _attributeSet;
        private float _initialHp;
        #endregion

        #region Properties
        /// <summary>HP가 0 이하가 됐을 때 실행되는 이벤트.</summary>
        public UnityEvent OnPressed => _onPressed;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            // AttributeSet 은 실행 순서가 앞이라 이 시점에 값이 준비되어 있다
            _attributeSet = GetComponent<AttributeSet>();

            Debug.Assert(_attributeSet.IsValidTarget(HP_ATTRIBUTE),
                $"[{name}] AttributeSet에 {HP_ATTRIBUTE}가 없습니다.");

            _initialHp = _attributeSet.GetValue(HP_ATTRIBUTE);
        }

        private void OnEnable()
        {
            _attributeSet.AddPostAttributeChangedCallback(HandleAttributeChanged);
        }

        private void OnDisable()
        {
            _attributeSet.RemovePostAttributeChangedCallback(HandleAttributeChanged);
        }
        #endregion

        #region Private Methods
        private void HandleAttributeChanged(SAttributeChangeData data)
        {
            if (!string.Equals(data.AttributeName, HP_ATTRIBUTE, StringComparison.OrdinalIgnoreCase)) return;

            // 0 이하로 내려가는 순간에만 실행한다 (이미 0 이하인 상태에서 더 맞는 것은 무시)
            if ((data.OldValue <= 0.0f) || (data.NewValue > 0.0f)) return;

            _onPressed.Invoke();

            if (!_isOneShot)
            {
                _attributeSet.SetValue(HP_ATTRIBUTE, _initialHp);
            }
        }
        #endregion
    }
}
