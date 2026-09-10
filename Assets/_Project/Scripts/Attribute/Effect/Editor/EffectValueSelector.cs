using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SOAttributeEffect))]
public class EffectValueSelector : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawPropertiesExcluding(serializedObject, "_valueSource", "_amount", "_cursorAttributeSet", "_cursorAttribute");

        SerializedProperty typeProp = serializedObject.FindProperty("_valueSource");
        var previousType = (ValueSource)typeProp.enumValueIndex;

        EditorGUILayout.PropertyField(typeProp);
        var currentType = (ValueSource)typeProp.enumValueIndex;

        if (currentType != previousType)
        {
            if (currentType == ValueSource.Float)
                serializedObject.FindProperty("_amount").floatValue = 0f;
            else
            {
                string[] guids = AssetDatabase.FindAssets($"t:{nameof(SOAttributeData)}");
                var firstSet = guids.Length > 0
                    ? AssetDatabase.LoadAssetAtPath<SOAttributeData>(AssetDatabase.GUIDToAssetPath(guids[0]))
                    : null;
                serializedObject.FindProperty("_cursorAttributeSet").objectReferenceValue = firstSet;
                serializedObject.FindProperty("_cursorAttribute").stringValue = "";
            }
        }

        if (currentType == ValueSource.Float)
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_amount"));
        }
        else if (currentType == ValueSource.Attribute)
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_cursorAttributeSet"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_cursorAttribute"));
        }

        serializedObject.ApplyModifiedProperties();
    }
}
