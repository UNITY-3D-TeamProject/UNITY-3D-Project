using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SOAttributeEffect))]
public class EffectValueSelector : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawPropertiesExcluding(serializedObject, "valueSource", "amount", "cursorAttributeSet", "cursorAttribute");
        
        SerializedProperty typeProp = serializedObject.FindProperty("valueSource");
        var previousType = (ValueSource)typeProp.enumValueIndex;

        EditorGUILayout.PropertyField(typeProp);
        var currentType = (ValueSource)typeProp.enumValueIndex;
        
        if (currentType != previousType)
        {
            if (currentType == ValueSource.Float)
                serializedObject.FindProperty("amount").floatValue = 0f;
            else
            {
                string[] guids = AssetDatabase.FindAssets($"t:{nameof(SOAttributeData)}");
                var firstSet = guids.Length > 0
                    ? AssetDatabase.LoadAssetAtPath<SOAttributeData>(AssetDatabase.GUIDToAssetPath(guids[0]))
                    : null;
                serializedObject.FindProperty("cursorAttributeSet").objectReferenceValue = firstSet;
                serializedObject.FindProperty("cursorAttribute").stringValue = "";
            }
        }

        if (currentType == ValueSource.Float)
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("amount"));
        }
        else if (currentType == ValueSource.Attribute)
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("cursorAttributeSet"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("cursorAttribute"));
        }

        serializedObject.ApplyModifiedProperties();
    }
}
