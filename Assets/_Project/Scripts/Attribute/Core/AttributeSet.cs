using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

[DefaultExecutionOrder(-100)]
public class AttributeSet : MonoBehaviour
{
    [SerializeField] private SOAttributeData _initData;
    private readonly Dictionary<string, AttributeData> _attributes = new(StringComparer.OrdinalIgnoreCase);
    
    public delegate void OnAttributeChangeWithRef(string attributeName, ref float newValue, float oldValue);
    public delegate void OnAttributeChange(string attributeName, float newValue, float oldValue);
    
    private event OnAttributeChangeWithRef _preAttributeChangedEvent;
    private event OnAttributeChange _onAttributeChangedEvent;
    private event OnAttributeChange _postAttributeChangedEvent;
    
    private void Awake()
    {
        if (_initData == null)
        {
            throw new InvalidOperationException($"[{name}] : SOAttributeData is not set");
        }
        
        foreach (var entry in _initData.Attributes)
        {
            if (_attributes.ContainsKey(entry.AttributeName))
            {
                Assert.IsTrue(false, $"[{entry.AttributeName}] : already defined, skipped");
                continue;
            }
            
            var attribute = new AttributeData(entry.Value, entry.AttributeName);
            attribute.SetPreValueChangedCallback(NativePreAttributeChanged);
            attribute.SetOnValueChangedCallback(NativeOnAttributeChanged);
            attribute.SetPostValueChangedCallback(NativePostAttributeChanged);
            
            _attributes[entry.AttributeName] = attribute;
        }
    }

    /// <summary>
    /// Value 변경 전 발생할 콜백 Set, newValue 에 대해 클램핑이 필요한 경우 진행
    /// </summary>
    /// <param name="callback">void(string, ref float, float) 시그니쳐 callback</param>
    public void SetPreAttributeChangedCallback(OnAttributeChangeWithRef callback)
    {
        if (callback == null) return;
        _preAttributeChangedEvent = callback;
    }

    /// <summary>
    /// Value 변경 시 발생할 콜백 Set
    /// </summary>
    /// <param name="callback">void(string, float, float) 시그니쳐 callback</param>
    public void SetOnAttributeChangedCallback(OnAttributeChange callback)
    {
        if (callback == null) return;
        _onAttributeChangedEvent = callback;
    }
    
    /// <summary>
    /// Value 변경 후 발생할 콜백 Set
    /// </summary>
    /// <param name="callback">void(string, float, float) 시그니쳐 callback</param>
    public void SetPostAttributeChangedCallback(OnAttributeChange callback)
    {
        if (callback == null) return;
        _postAttributeChangedEvent = callback;
    }
    
    /// <summary>
    /// 해당 Name 을 가진 AttributeData 존재여부 체크
    /// </summary>
    /// <param name="attributeName">확인하고자 하는 Name</param>
    /// <returns>존재 여부</returns>
    public bool IsValidAttribute(string attributeName)
    {
        return _attributes.ContainsKey(attributeName);
    }

    /// <summary>
    /// 해당 Name 을 가진 AttributeData Value 값 읽기
    /// </summary>
    /// <param name="attributeName">값을 읽을 attributeData의 Name</param>
    /// <returns>attributeData Value, 존재하지 않는 경우 0</returns>
    public float GetValue(string attributeName)
    {
        if (!IsValidAttribute(attributeName))
        {
            Assert.IsTrue(false, $"[{attributeName}] : is invalid attribute name");
            return 0.0f;
        }
        
        return _attributes[attributeName].Value;
    }

    private void NativePreAttributeChanged(AttributeData target, ref float newValue, float oldValue)
    {
        _preAttributeChangedEvent?.Invoke(target.Name, ref newValue, oldValue);
    }
    private void NativeOnAttributeChanged(AttributeData target, float newValue, float oldValue)
    {
        _onAttributeChangedEvent?.Invoke(target.Name, newValue, oldValue);
    }
    private void NativePostAttributeChanged(AttributeData target, float newValue, float oldValue)
    {
        _postAttributeChangedEvent?.Invoke(target.Name, newValue, oldValue);
    }
}

/*
using UnityEngine;

public class SoTypeDropdownAttribute : PropertyAttribute
{
    public readonly System.Type baseType;
    public SoTypeDropdownAttribute(System.Type baseType)
    {
        this.baseType = baseType;
    }
}

public abstract class ListSourceSO : ScriptableObject
{
    public string[] values;
}

// 이걸 상속한 여러 SO를 프로젝트에 만들어둠
public class WeaponNamesSO : ListSourceSO { }
public class ArmorNamesSO : ListSourceSO { }

[CreateAssetMenu]
public class SomeConsumerSO : ScriptableObject
{
    [SoTypeDropdown(typeof(ListSourceSO))]
    public ListSourceSO source; // ListSourceSO를 상속한 모든 에셋 중에서 드롭다운으로 선택

    [SoStringDropdown(nameof(source))] // 지난번 드로어 재사용
    public string selectedValue;
}

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(SoTypeDropdownAttribute))]
public class SoTypeDropdownDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        var attr = (SoTypeDropdownAttribute)attribute;

        if (property.propertyType != SerializedPropertyType.ObjectReference)
        {
            EditorGUI.LabelField(position, label.text, "Object Reference 필드에만 사용하세요.");
            return;
        }

        // baseType을 상속한 모든 에셋 검색 (t: 검색은 서브클래스도 포함)
        string[] guids = AssetDatabase.FindAssets($"t:{attr.baseType.Name}");

        var assets = new List<Object>();
        var names = new List<string>();

        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var obj = AssetDatabase.LoadAssetAtPath(path, attr.baseType);
            if (obj != null)
            {
                assets.Add(obj);
                names.Add(obj.name);
            }
        }

        if (assets.Count == 0)
        {
            EditorGUI.LabelField(position, label.text, $"{attr.baseType.Name} 타입 에셋이 없습니다.");
            return;
        }

        int currentIndex = assets.IndexOf(property.objectReferenceValue);
        if (currentIndex < 0) currentIndex = 0;

        EditorGUI.BeginChangeCheck();
        int selected = EditorGUI.Popup(position, label.text, currentIndex, names.ToArray());
        if (EditorGUI.EndChangeCheck())
        {
            property.objectReferenceValue = assets[selected];
        }
    }
}
 */
