
public class AttributeData
{
    public delegate void OnValueChangeWithRef(AttributeData targetData,ref float newValue, float oldValue);
    public delegate void OnValueChange(AttributeData targetData, float newValue, float oldValue);
    
    private event OnValueChangeWithRef _preValueChangedEvent;
    private event OnValueChange _onValueChangedEvent;
    private event OnValueChange _postValueChangedEvent;
    
    private float _value;

    public AttributeData(float value)
    {
        _value = value;
    }
    
    public float Value
    {
        get => _value;
        set
        {
            float newValue = value;
            _preValueChangedEvent?.Invoke(this, ref newValue, _value);
            float oldValue = _value;
            _value = newValue;
            _onValueChangedEvent?.Invoke(this, _value, oldValue);
            _postValueChangedEvent?.Invoke(this, _value, oldValue);
        }
    }
    
    public void SetPreValueChangedCallback(OnValueChangeWithRef callback)
    {
        if (callback == null) return;
        _preValueChangedEvent = callback;
    }

    public void SetOnValueChangedCallback(OnValueChange callback)
    {
        if (callback == null) return;
        _onValueChangedEvent = callback;
    }
    
    public void SetPostValueChangedCallback(OnValueChange callback)
    {
        if (callback == null) return;
        _postValueChangedEvent = callback;
    }
}
