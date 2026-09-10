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
    public Modifier modifier;
    [EffectTargetDropdown(typeof(SOAttributeData))]
    public SOAttributeData targetAttributeSet;
    [AttributeTargetDropdown(nameof(targetAttributeSet))]
    public string targetAttribute;

    public ValueSource valueSource;
    public float amount;
    [EffectTargetDropdown(typeof(SOAttributeData))]
    public SOAttributeData cursorAttributeSet;
    [AttributeTargetDropdown(nameof(cursorAttributeSet))]
    public string cursorAttribute;
}
