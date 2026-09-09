using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Assertions;

public abstract class AttributeSetBase : MonoBehaviour
{
    public delegate void OnAttributeChange(AttributeData targetData, float newValue, float oldValue);
    
    private event OnAttributeChange PreAttributeChangedEvent;
    private event OnAttributeChange OnAttributeChangedEvent;
    private event OnAttributeChange PostAttributeChangedEvent;
    
    private readonly Dictionary<string, AttributeData> _attributes = new(StringComparer.OrdinalIgnoreCase);

    private static readonly Dictionary<Type, FieldInfo[]> _fieldCache = new();
    private void Awake()
    {
        Type concreteType = GetType();
        
        if (!_fieldCache.TryGetValue(concreteType, out FieldInfo[] fields))
        {
            var list = new List<FieldInfo>();
            Type type = concreteType;
            //type이 AttributeSetBase 일때까지 반복
            while (type != null && type != typeof(AttributeSetBase))
            {
                //DeclaredOnly : 상속받은건 제외, 중복처리에 대한 예외
                list.AddRange(type.GetFields(
                    BindingFlags.Instance | BindingFlags.Public |
                    BindingFlags.NonPublic | BindingFlags.DeclaredOnly));
                type = type.BaseType;
            }
            fields = list.ToArray();
            _fieldCache[concreteType] = fields;
        }
        
        foreach (FieldInfo field in fields)
        {
            if (!typeof(AttributeData).IsAssignableFrom(field.FieldType))
                continue;

            var attribute = field.GetValue(this) as AttributeData;
            if (attribute == null)
            {
                attribute = new AttributeData();
                field.SetValue(this, attribute);
            }

            _attributes[field.Name] = attribute;

            attribute.SetPreValueChangedCallback(NativePreAttributeChanged);
            attribute.SetOnValueChangedCallback(NativeOnAttributeChanged);
            attribute.SetPostValueChangedCallback(NativePostAttributeChanged);
        }
    }

    public void SetPreAttributeChangedCallback(OnAttributeChange callback)
    {
        if (callback == null) return;
        PreAttributeChangedEvent = callback;
    }

    public void SetOnAttributeChangedCallback(OnAttributeChange callback)
    {
        if (callback == null) return;
        OnAttributeChangedEvent = callback;
    }
    public void SetPostAttributeChangedCallback(OnAttributeChange callback)
    {
        if (callback == null) return;
        PostAttributeChangedEvent = callback;
    }
    
    public bool IsValidAttribute(string name)
    {
        return _attributes.ContainsKey(name);
    }

    public float GetValue(string name)
    {
        Assert.IsTrue(IsValidAttribute(name), "is invalid attribute name");
        if (!IsValidAttribute(name))
        {
            throw new ArgumentException($"attribute {name} is not valid");
        }
        return _attributes[name].Value;
    }

    private void NativePreAttributeChanged(AttributeData target, float newValue, float oldValue)
    {
        PreAttributeChangedEvent?.Invoke(target, newValue, oldValue);
        PreAttributeChanged(target, newValue, oldValue);
    }
    private void NativeOnAttributeChanged(AttributeData target, float newValue, float oldValue)
    {
        OnAttributeChangedEvent?.Invoke(target, newValue, oldValue);
        OnAttributeChanged(target, newValue, oldValue);
    }
    private void NativePostAttributeChanged(AttributeData target, float newValue, float oldValue)
    {
        PostAttributeChangedEvent?.Invoke(target, newValue, oldValue);
        PostAttributeChanged(target, newValue, oldValue);
    }
    
    protected virtual void PreAttributeChanged (AttributeData target, float newValue, float oldValue) {}
    protected virtual void OnAttributeChanged (AttributeData target, float newValue, float oldValue) {}
    protected virtual void PostAttributeChanged (AttributeData target, float newValue, float oldValue) {}
}
