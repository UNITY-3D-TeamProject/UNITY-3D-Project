using UnityEngine;

public class EffectTargetDropdownAttribute : PropertyAttribute
{
    public readonly System.Type baseType;
    public EffectTargetDropdownAttribute(System.Type baseType)
    {
        this.baseType = baseType;
    }
}
