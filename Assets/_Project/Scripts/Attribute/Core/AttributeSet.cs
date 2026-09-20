using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace Attribute.Core
{
    /// <summary>
    /// SOAttributeData 로 초기화되는 어트리뷰트 컬렉션 컴포넌트.
    /// 이름(대소문자 무시)으로 어트리뷰트를 조회/수정하며, 개별 AttributeData 의 콜백을
    /// 이름 기반 콜백으로 묶어 외부에 노출한다. IEffectTarget 을 구현해 SOAttributeEffect 의 대상이 된다.
    /// </summary>
    // Awake 가 먼저 실행되어 다른 곳에서 attribute 를 참조할 때 문제가 없도록 순서 조정
    [DefaultExecutionOrder(-100)]
    public class AttributeSet : MonoBehaviour, IEffectTarget
    {
        #region Delegates
        /// <summary>값 변경 전 호출. newValue 를 수정하면 실제 반영되는 값이 바뀐다.</summary>
        public delegate void OnAttributeChangeWithRef(string attributeName, ref float newValue, float oldValue);

        /// <summary>값 변경 시 / 변경 후 호출.</summary>
        public delegate void OnAttributeChange(string attributeName, float newValue, float oldValue);
        #endregion

        #region Serialized Fields
        [Tooltip("초기 어트리뷰트 목록. 비어 있으면 Awake 에서 예외를 던진다.")]
        [SerializeField] private SOAttributeData _initData;
        #endregion

        #region Private Fields
        private readonly Dictionary<string, AttributeData> _attributes = new(StringComparer.OrdinalIgnoreCase);
        #endregion

        #region Events
        private event OnAttributeChangeWithRef _preAttributeChangedEvent;
        private event OnAttributeChange _onAttributeChangedEvent;
        private event OnAttributeChange _postAttributeChangedEvent;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            if (_initData == null)
            {
                throw new InvalidOperationException($"[{name}] : SOAttributeData is not set");
            }

            foreach (var entry in _initData.Attributes)
            {
                if (_attributes.ContainsKey(entry.AttributeName))
                {
                    Assert.IsTrue(false, $"[{entry.AttributeName}] : already defined, skipped");
                    continue;
                }

                // 개별 AttributeData 의 콜백을 이 컴포넌트의 이름 기반 이벤트로 중계
                var attribute = new AttributeData(entry.Value, entry.AttributeName);
                attribute.SetPreValueChangedCallback(NativePreAttributeChanged);
                attribute.SetOnValueChangedCallback(NativeOnAttributeChanged);
                attribute.SetPostValueChangedCallback(NativePostAttributeChanged);

                _attributes[entry.AttributeName] = attribute;
            }
        }
        #endregion

        #region IEffectTarget
        /// <inheritdoc />
        public bool IsValidTarget(string targetName)
        {
            return _attributes.ContainsKey(targetName);
        }

        /// <inheritdoc />
        public float GetValue(string targetName)
        {
            if (!IsValidTarget(targetName))
            {
                Assert.IsTrue(false, $"[{targetName}] : is invalid attribute name");
                return 0.0f;
            }

            return _attributes[targetName].Value;
        }

        /// <inheritdoc />
        public void SetValue(string targetName, float value)
        {
            if (!IsValidTarget(targetName))
            {
                Assert.IsTrue(false, $"[{targetName}] : is invalid attribute name");
                return;
            }

            _attributes[targetName].Value = value;
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Value 변경 전 발생할 콜백 Set
        /// newValue 에 대해 클램핑이 가능하므로 단일구독을 위해 Set
        /// </summary>
        /// <param name="callback">void(string, ref float, float) 시그니쳐 callback</param>
        public void SetPreAttributeChangedCallback(OnAttributeChangeWithRef callback)
        {
            if (callback == null) return;
            _preAttributeChangedEvent = callback;
        }

        /// <summary>
        /// Value 변경 전 발생할 콜백 Clear
        /// </summary>
        public void ClearPreAttributeChangedCallback()
        {
            _preAttributeChangedEvent = null;
        }

        /// <summary>
        /// Value 변경 시 발생할 콜백 Add
        /// </summary>
        /// <param name="callback">void(string, float, float) 시그니쳐 callback</param>
        public void AddOnAttributeChangedCallback(OnAttributeChange callback)
        {
            if (callback == null) return;
            _onAttributeChangedEvent += callback;
        }

        /// <summary>
        /// Value 변경 시 발생할 콜백 Remove
        /// </summary>
        /// <param name="callback">void(string, float, float) 시그니쳐 callback</param>
        public void RemoveOnAttributeChangedCallback(OnAttributeChange callback)
        {
            if (callback == null) return;
            _onAttributeChangedEvent -= callback;
        }

        /// <summary>
        /// Value 변경 후 발생할 콜백 Add
        /// </summary>
        /// <param name="callback">void(string, float, float) 시그니쳐 callback</param>
        public void AddPostAttributeChangedCallback(OnAttributeChange callback)
        {
            if (callback == null) return;
            _postAttributeChangedEvent += callback;
        }

        /// <summary>
        /// Value 변경 후 발생할 콜백 Remove
        /// </summary>
        /// <param name="callback">void(string, float, float) 시그니쳐 callback</param>
        public void RemovePostAttributeChangedCallback(OnAttributeChange callback)
        {
            if (callback == null) return;
            _postAttributeChangedEvent -= callback;
        }
        #endregion

        #region Private Methods
        private void NativePreAttributeChanged(AttributeData target, ref float newValue, float oldValue)
        {
            _preAttributeChangedEvent?.Invoke(target.Name, ref newValue, oldValue);
        }

        private void NativeOnAttributeChanged(AttributeData target, float newValue, float oldValue)
        {
            _onAttributeChangedEvent?.Invoke(target.Name, newValue, oldValue);
        }

        private void NativePostAttributeChanged(AttributeData target, float newValue, float oldValue)
        {
            _postAttributeChangedEvent?.Invoke(target.Name, newValue, oldValue);
        }
        #endregion
    }
}
