using System.Linq;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(AttributeTargetDropdownAttribute))]
public class AttributeSelector : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        var attr = (AttributeTargetDropdownAttribute)attribute;

        if (property.propertyType != SerializedPropertyType.String)
        {
            EditorGUI.LabelField(position, label.text, "string 필드에만 사용하세요.");
            return;
        }

        SerializedProperty soProp = property.serializedObject.FindProperty(attr.soFieldName);
        if (soProp == null || soProp.objectReferenceValue is not SOAttributeData soAttributeData)
        {
            EditorGUI.LabelField(position, label.text, $"{attr.soFieldName}을(를) 먼저 선택하세요.");
            return;
        }

        string[] names = soAttributeData.Attributes.Select(a => a.AttributeName).ToArray();
        if (names.Length == 0)
        {
            EditorGUI.LabelField(position, label.text, $"{soAttributeData.name}에 attribute가 없습니다.");
            return;
        }

        int currentIndex = System.Array.IndexOf(names, property.stringValue);
        if (currentIndex < 0) currentIndex = 0;

        EditorGUI.BeginChangeCheck();
        int selected = EditorGUI.Popup(position, label.text, currentIndex, names);
        if (EditorGUI.EndChangeCheck())
        {
            property.stringValue = names[selected];
        }
    }
}
