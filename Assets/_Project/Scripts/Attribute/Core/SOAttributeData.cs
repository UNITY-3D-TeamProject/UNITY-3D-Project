using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

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

    private void OnValidate()
    {
        if (_attributes == null) return;

        var seenNames = new HashSet<string>();
        foreach (var entry in _attributes)
        {
            if (string.IsNullOrWhiteSpace(entry.AttributeName))
            {
                Assert.IsTrue(false, $"[{name}] : AttributeName은 비어있을 수 없습니다");
                continue;
            }

            if (!seenNames.Add(entry.AttributeName))
            {
                Assert.IsTrue(false, $"[{name}] : AttributeName '{entry.AttributeName}' 이(가) 중복되었습니다");
            }
        }
    }
}
