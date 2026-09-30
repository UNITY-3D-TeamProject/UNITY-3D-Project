using UnityEngine;
using UnityEditor;
using Map.Spiral;

namespace Map.Editor
{
    [CustomEditor(typeof(SpiralMapGenerator))]
    public class SpiralMapGeneratorEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            SpiralMapGenerator generator = (SpiralMapGenerator)target;

            GUILayout.Space(15);

            GUIStyle btnStyle = new GUIStyle(GUI.skin.button);
            btnStyle.fontStyle = FontStyle.Bold;

            if (GUILayout.Button("Generate Map", btnStyle, GUILayout.Height(35)))
            {
                generator.GenerateMap();
            }

            GUILayout.Space(5);

            if (GUILayout.Button("Clear Generated Map", GUILayout.Height(25)))
            {
                generator.ClearMap();
            }
            
            GUILayout.Space(10);
            EditorGUILayout.HelpBox("1. Inspector에서 Sections 리스트를 추가하여 각 구간(90도)의 기믹을 설정합니다.\n2. [Generate Map] 버튼을 눌러 회오리 맵을 생성합니다.\n3. 생성된 Section 오브젝트들을 Project 창으로 드래그하면 개별 구간을 프리팹으로 저장할 수 있습니다.", MessageType.Info);
        }
    }
}
