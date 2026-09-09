using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

[DefaultExecutionOrder(-100)]
public class AttributeSet : MonoBehaviour
{
    [SerializeField] private SOAttributeData _initData;
    private readonly Dictionary<string, AttributeData> _attributes = new(StringComparer.OrdinalIgnoreCase);
    
    public delegate void OnAttributeChangeWithRef(AttributeData targetData, ref float newValue, float oldValue);
    public delegate void OnAttributeChange(AttributeData targetData, float newValue, float oldValue);
    
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
            
            var attribute = new AttributeData(entry.Value);
            attribute.SetPreValueChangedCallback(NativePreAttributeChanged);
            attribute.SetOnValueChangedCallback(NativeOnAttributeChanged);
            attribute.SetPostValueChangedCallback(NativePostAttributeChanged);
            
            _attributes[entry.AttributeName] = attribute;
        }
    }

    public void SetPreAttributeChangedCallback(OnAttributeChangeWithRef callback)
    {
        if (callback == null) return;
        _preAttributeChangedEvent = callback;
    }

    public void SetOnAttributeChangedCallback(OnAttributeChange callback)
    {
        if (callback == null) return;
        _onAttributeChangedEvent = callback;
    }
    public void SetPostAttributeChangedCallback(OnAttributeChange callback)
    {
        if (callback == null) return;
        _postAttributeChangedEvent = callback;
    }
    
    public bool IsValidAttribute(string attributeName)
    {
        return _attributes.ContainsKey(attributeName);
    }

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
        _preAttributeChangedEvent?.Invoke(target, ref newValue, oldValue);
    }
    private void NativeOnAttributeChanged(AttributeData target, float newValue, float oldValue)
    {
        _onAttributeChangedEvent?.Invoke(target, newValue, oldValue);
    }
    private void NativePostAttributeChanged(AttributeData target, float newValue, float oldValue)
    {
        _postAttributeChangedEvent?.Invoke(target, newValue, oldValue);
    }
}
