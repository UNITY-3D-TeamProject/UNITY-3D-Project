using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

[DefaultExecutionOrder(-100)]
public class AttributeSet : MonoBehaviour
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

    /// <summary>
    /// Value 변경 전 발생할 콜백 Set
    /// </summary>
    /// <param name="callback">void(AttributeData, ref float, float) 시그니쳐 callback</param>
    public void SetPreAttributeChangedCallback(OnAttributeChangeWithRef callback)
    {
        if (callback == null) return;
        _preAttributeChangedEvent = callback;
    }

    /// <summary>
    /// Value 변경 시 발생할 콜백 Set
    /// </summary>
    /// <param name="callback">void(AttributeData, float, float) 시그니쳐 callback</param>
    public void SetOnAttributeChangedCallback(OnAttributeChange callback)
    {
        if (callback == null) return;
        _onAttributeChangedEvent = callback;
    }
    
    /// <summary>
    /// Value 변경 후 발생할 콜백 Set
    /// </summary>
    /// <param name="callback">void(AttributeData, float, float) 시그니쳐 callback</param>
    public void SetPostAttributeChangedCallback(OnAttributeChange callback)
    {
        if (callback == null) return;
        _postAttributeChangedEvent = callback;
    }
    
    /// <summary>
    /// 해당 Name 을 가진 AttributeData 존재여부 체크
    /// </summary>
    /// <param name="attributeName">확인하고자 하는 Name</param>
    /// <returns>존재 여부</returns>
    public bool IsValidAttribute(string attributeName)
    {
        return _attributes.ContainsKey(attributeName);
    }

    /// <summary>
    /// 해당 Name 을 가진 AttributeData Value 값 읽기
    /// </summary>
    /// <param name="attributeName">값을 읽을 attributeData의 Name</param>
    /// <returns>attributeData Value, 존재하지 않는 경우 0</returns>
    public float GetValue(string attributeName)
    {
        if (!IsValidAttribute(attributeName))
        {
            Assert.IsTrue(false, $"[{attributeName}] : is invalid attribute name");
            return 0.0f;
        }
        
        return _attributes[attributeName].Value;
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
