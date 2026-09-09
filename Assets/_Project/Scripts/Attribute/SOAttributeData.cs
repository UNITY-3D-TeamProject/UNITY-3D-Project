using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SOAttributeData", menuName = "Attribute/AttributeData")]
public class SOAttributeData : ScriptableObject
{
    [Serializable]
    public struct SAttribute
    {
        public string AttributeName;
        public float Value;
    }

    [SerializeField] private SAttribute[] _attributes;
    
    public IReadOnlyList<SAttribute> Attributes => _attributes;
}
