using UnityEngine;

public class AttributeTargetDropdownAttribute : PropertyAttribute
{
    public readonly string soFieldName;

    public AttributeTargetDropdownAttribute(string soFieldName)
    {
        this.soFieldName = soFieldName;
    }
}