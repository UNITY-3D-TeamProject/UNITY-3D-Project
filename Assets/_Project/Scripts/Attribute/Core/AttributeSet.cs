using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace Attribute.Core
{
    //Awake 가 먼저 실행되어 다른 곳에서 attribute 를 참조할 때 문제가 없도록 순서 조정
    [DefaultExecutionOrder(-100)]
    public class AttributeSet : MonoBehaviour, IEffectTarget
    {
        [SerializeField] private SOAttributeData _initData;
        private readonly Dictionary<string, AttributeData> _attributes = new(StringComparer.OrdinalIgnoreCase);

        public delegate void OnAttributeChangeWithRef(string attributeName, ref float newValue, float oldValue);

        public delegate void OnAttributeChange(string attributeName, float newValue, float oldValue);

        private event OnAttributeChangeWithRef _preAttributeChangedEvent;
        private event OnAttributeChange _onAttributeChangedEvent;
        private event OnAttributeChange _postAttributeChangedEvent;

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

                var attribute = new AttributeData(entry.Value, entry.AttributeName);
                attribute.SetPreValueChangedCallback(NativePreAttributeChanged);
                attribute.SetOnValueChangedCallback(NativeOnAttributeChanged);
                attribute.SetPostValueChangedCallback(NativePostAttributeChanged);

                _attributes[entry.AttributeName] = attribute;
            }
        }

        #region IEffectTarget functions
        //begin IEffectTarget
        public bool IsValidTarget(string targetName)
        {
            return _attributes.ContainsKey(targetName);
        }
        
        public float GetValue(string targetName)
        {
            if (!IsValidTarget(targetName))
            {
                Assert.IsTrue(false, $"[{targetName}] : is invalid attribute name");
                return 0.0f;
            }

            return _attributes[targetName].Value;
        }

        public void SetValue(string targetName, float value)
        {
            if (!IsValidTarget(targetName))
            {
                Assert.IsTrue(false, $"[{targetName}] : is invalid attribute name");
                return;
            }
            _attributes[targetName].Value = value;
        }
        //end IEffectTarget
        #endregion
        
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
    }
}
