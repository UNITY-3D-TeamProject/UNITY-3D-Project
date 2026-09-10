using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(EffectTargetDropdownAttribute))]
public class SoTypeDropdownDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        var attr = (EffectTargetDropdownAttribute)attribute;

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