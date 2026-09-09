using UnityEngine;

public class AttributeData
{
    public delegate void OnValueChange(AttributeData targetData, float newValue, float oldValue);
    
    private event OnValueChange PreValueChangedEvent;
    private event OnValueChange OnValueChangedEvent;
    private event OnValueChange PostValueChangedEvent;
    
    private float _value;

    public float Value
    {
        get => _value;
        set
        {
            PreValueChangedEvent?.Invoke(this, value, _value);
            float oldValue= _value;
            _value = value;
            OnValueChangedEvent?.Invoke(this, _value, oldValue);
            PostValueChangedEvent?.Invoke(this, _value, oldValue);
        }
    }

    public void SetPreValueChangedCallback(OnValueChange callback)
    {
        if (callback == null) return;
        PreValueChangedEvent = callback;
    }

    public void SetOnValueChangedCallback(OnValueChange callback)
    {
        if (callback == null) return;
        OnValueChangedEvent = callback;
    }
    
    public void SetPostValueChangedCallback(OnValueChange callback)
    {
        if (callback == null) return;
        PostValueChangedEvent = callback;
    }
}
