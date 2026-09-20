using UnityEditor;

namespace Attribute.Effect.Editor
{
    /// <summary>
    /// SOAttributeEffect 커스텀 인스펙터.
    /// ValueSource 에 따라 Amount / CursorAttribute 중 해당하는 필드만 표시한다.
    /// </summary>
    [CustomEditor(typeof(SOAttributeEffect))]
    public class SOAttributeEffectEditor : UnityEditor.Editor
    {
        #region Private Fields
        private SerializedProperty _modifierProp;
        private SerializedProperty _targetAttributeProp;
        private SerializedProperty _valueSourceProp;
        private SerializedProperty _amountProp;
        private SerializedProperty _cursorAttributeProp;
        #endregion

        #region Unity Lifecycle
        private void OnEnable()
        {
            _modifierProp = serializedObject.FindProperty("_modifier");
            _targetAttributeProp = serializedObject.FindProperty("_targetAttribute");
            _valueSourceProp = serializedObject.FindProperty("_valueSource");
            _amountProp = serializedObject.FindProperty("_amount");
            _cursorAttributeProp = serializedObject.FindProperty("_cursorAttribute");
        }
        #endregion

        #region Public Methods
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(_modifierProp);
            EditorGUILayout.PropertyField(_targetAttributeProp);
            EditorGUILayout.PropertyField(_valueSourceProp);

            // ValueSource 에 맞는 입력 필드만 노출
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
        #endregion
    }
}
