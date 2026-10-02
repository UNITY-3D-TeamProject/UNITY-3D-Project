using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Map.Common;

namespace Map.Editor
{
    /// <summary>
    /// 모든 맵 생성기(MapGeneratorBase 를 상속한 컴포넌트)에 공통으로 쓰이는 인스펙터.
    /// 에디터에서 미리 생성해 보거나 지울 수 있는 버튼을 붙인다.
    /// </summary>
    [CustomEditor(typeof(MapGeneratorBase), true)]
    public class MapGeneratorBaseEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            MapGeneratorBase generator = (MapGeneratorBase)target;

            GUILayout.Space(15);

            if (GUILayout.Button("Generate Map", GUILayout.Height(35)))
            {
                generator.Generate();
                MarkChanged(generator);
            }

            GUILayout.Space(5);

            if (GUILayout.Button("Clear Generated Map", GUILayout.Height(25)))
            {
                generator.Clear();
                MarkChanged(generator);
            }

            GUILayout.Space(10);

            if (generator.IsGenerated)
            {
                EditorGUILayout.LabelField("마지막 시드", generator.LastSeed.ToString());
            }

            EditorGUILayout.HelpBox(
                "플레이 중에는 씬이 로드될 때 자동으로 생성됩니다 (Should Generate On Awake).\n" +
                "같은 맵을 다시 만들려면 Should Use Random Seed 를 끄고 Seed 에 마지막 시드를 넣습니다.",
                MessageType.Info);
        }

        // 에디터에서 만든 결과가 씬에 저장되도록 변경을 알린다
        private static void MarkChanged(MapGeneratorBase generator)
        {
            if (Application.isPlaying) return;

            EditorUtility.SetDirty(generator);
            EditorSceneManager.MarkSceneDirty(generator.gameObject.scene);
        }
    }
}
