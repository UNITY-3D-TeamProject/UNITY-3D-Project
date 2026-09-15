using UnityEditor;

namespace Attribute.Effect.Editor
{
    [CustomEditor(typeof(SOAttributeEffect))]
    public class SOAttributeEffectEditor : UnityEditor.Editor
    {
        private SerializedProperty _modifierProp;
        private SerializedProperty _targetAttributeProp;
        private SerializedProperty _valueSourceProp;
        private SerializedProperty _amountProp;
        private SerializedProperty _cursorAttributeProp;

        private void OnEnable()
        {
            _modifierProp = serializedObject.FindProperty("_modifier");
            _targetAttributeProp = serializedObject.FindProperty("_targetAttribute");
            _valueSourceProp = serializedObject.FindProperty("_valueSource");
            _amountProp = serializedObject.FindProperty("_amount");
            _cursorAttributeProp = serializedObject.FindProperty("_cursorAttribute");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(_modifierProp);
            EditorGUILayout.PropertyField(_targetAttributeProp);
            EditorGUILayout.PropertyField(_valueSourceProp);

            var valueSource = (EValueSource)_valueSourceProp.enumValueIndex;
            if (valueSource == EValueSource.Float)
            {
                EditorGUILayout.PropertyField(_amountProp);
            }
            else
            {
                EditorGUILayout.PropertyField(_cursorAttributeProp);
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
