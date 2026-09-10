using System;
using UnityEngine;

[Serializable]
public enum ValueSource
{
    Float,
    Attribute
}

[Serializable]
public enum Modifier
{
    Add,
    Multiply
}

[CreateAssetMenu(fileName = "SOAttributeEffect", menuName = "Attribute/AttributeEffect")]
public class SOAttributeEffect : ScriptableObject
{
    [SerializeField] private Modifier _modifier;
    [SerializeField, EffectTargetDropdown(typeof(SOAttributeData))]
    private SOAttributeData _targetAttributeSet;
    [SerializeField, AttributeTargetDropdown(nameof(_targetAttributeSet))]
    private string _targetAttribute;

    [SerializeField] private ValueSource _valueSource;
    [SerializeField] private float _amount;
    [SerializeField, EffectTargetDropdown(typeof(SOAttributeData))]
    private SOAttributeData _cursorAttributeSet;
    [SerializeField, AttributeTargetDropdown(nameof(_cursorAttributeSet))]
    private string _cursorAttribute;

    public Modifier Modifier => _modifier;
    public SOAttributeData TargetAttributeSet => _targetAttributeSet;
    public string TargetAttribute => _targetAttribute;
    public ValueSource ValueSource => _valueSource;
    public float Amount => _amount;
    public SOAttributeData CursorAttributeSet => _cursorAttributeSet;
    public string CursorAttribute => _cursorAttribute;
}
