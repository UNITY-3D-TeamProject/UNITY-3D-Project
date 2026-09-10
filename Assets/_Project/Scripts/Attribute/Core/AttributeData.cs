
public class AttributeData
{
    public delegate void OnValueChangeWithRef(AttributeData targetData, ref float newValue, float oldValue);
    public delegate void OnValueChange(AttributeData targetData, float newValue, float oldValue);
    
    private event OnValueChangeWithRef _preValueChangedEvent;
    private event OnValueChange _onValueChangedEvent;
    private event OnValueChange _postValueChangedEvent;

    public string Name { get; private set; }
    private float _value;

    public AttributeData(float value, string name)
    {
        _value = value;
        Name = name;
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
    
    /// <summary>
    /// Value 변경 전 발생할 콜백 Set
    /// </summary>
    /// <param name="callback">void(AttributeData, ref float, float) 시그니쳐 callback</param>
    public void SetPreValueChangedCallback(OnValueChangeWithRef callback)
    {
        if (callback == null) return;
        _preValueChangedEvent = callback;
    }

    /// <summary>
    /// Value 변경 시 발생할 콜백 Set
    /// </summary>
    /// <param name="callback">void(AttributeData, float, float) 시그니쳐 callback</param>
    public void SetOnValueChangedCallback(OnValueChange callback)
    {
        if (callback == null) return;
        _onValueChangedEvent = callback;
    }
    
    /// <summary>
    /// Value 변경 후 발생할 콜백 Set
    /// </summary>
    /// <param name="callback">void(AttributeData, float, float) 시그니쳐 callback</param>
    public void SetPostValueChangedCallback(OnValueChange callback)
    {
        if (callback == null) return;
        _postValueChangedEvent = callback;
    }
}
