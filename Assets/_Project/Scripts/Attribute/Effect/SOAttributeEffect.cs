using System;
using UnityEngine;
using UnityEngine.Assertions;

[Serializable]
public enum EValueSource
{
    Float,
    Attribute
}

[CreateAssetMenu(fileName = "SOAttributeEffect", menuName = "Attribute/AttributeEffect")]
public class SOAttributeEffect : ScriptableObject
{
    [SerializeField] private EModifier _modifier;
    [SerializeField] private string _targetAttribute;

    [SerializeField] private EValueSource _valueSource;
    [SerializeField] private float _amount;
    [SerializeField] private string _cursorAttribute;

    public EModifier Modifier => _modifier;
    public string TargetAttribute => _targetAttribute;
    public EValueSource ValueSource => _valueSource;
    public float Amount => _amount;
    public string CursorAttribute => _cursorAttribute;

    public void Apply(IEffectTarget cursor, IEffectTarget target)
    {
        if (!cursor.IsValidTarget(_cursorAttribute))
        {
            throw new InvalidOperationException($"[{_cursorAttribute}] : is not set in cursor");
        }
        if (!target.IsValidTarget(_targetAttribute))
        {
            throw new InvalidOperationException($"[{_targetAttribute}] : is not set in target");
        }
        
        float amount = _valueSource == EValueSource.Attribute ? cursor.GetValue(_cursorAttribute) : _amount;
        float newValue = Modifiers.Modify(_modifier, target.GetValue(_targetAttribute), amount);
        target.SetValue(_targetAttribute, newValue);
    }
}
