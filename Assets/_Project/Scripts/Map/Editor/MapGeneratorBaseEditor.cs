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
    // 이 네임스페이스(Map.Editor)와 이름이 겹치므로 부모 클래스는 UnityEditor.Editor 로 전체 이름을 쓴다.
    [CustomEditor(typeof(MapGeneratorBase), true)]
    public class MapGeneratorBaseEditor : UnityEditor.Editor
    {
        private const string UNDO_NAME_GENERATE = "Generate Map";
        private const string UNDO_NAME_CLEAR    = "Clear Generated Map";

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            MapGeneratorBase generator = (MapGeneratorBase)target;

            GUILayout.Space(15);

            if (GUILayout.Button("Generate Map", GUILayout.Height(35)))
            {
                GenerateWithUndo(generator);
                MarkChanged(generator);
            }

            GUILayout.Space(5);

            if (GUILayout.Button("Clear Generated Map", GUILayout.Height(25)))
            {
                ClearWithUndo(generator);
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

        // 새 맵을 만들되 Ctrl+Z 로 이전 맵으로 되돌릴 수 있게 한다. 플레이 중에는 Undo 대상이 아니다.
        private static void GenerateWithUndo(MapGeneratorBase generator)
        {
            if (Application.isPlaying)
            {
                generator.Generate();
                return;
            }

            DestroyGeneratedRootWithUndo(generator, UNDO_NAME_GENERATE);
            generator.Generate();

            if (generator.GeneratedRoot != null)
            {
                Undo.RegisterCreatedObjectUndo(generator.GeneratedRoot.gameObject, UNDO_NAME_GENERATE);
            }
        }

        // 맵을 지우되 Ctrl+Z 로 되살릴 수 있게 한다.
        private static void ClearWithUndo(MapGeneratorBase generator)
        {
            if (!Application.isPlaying)
            {
                DestroyGeneratedRootWithUndo(generator, UNDO_NAME_CLEAR);
            }

            generator.Clear();
        }

        // 생성기가 직접 지우면 되돌릴 수 없으므로, 이전 맵은 Undo 에 등록하면서 먼저 지운다.
        // 생성기의 저장 값(맵 루트, 마지막 시드)도 함께 기록해 되돌릴 때 같이 돌아오게 한다.
        private static void DestroyGeneratedRootWithUndo(MapGeneratorBase generator, string undoName)
        {
            Undo.RecordObject(generator, undoName);

            if (generator.GeneratedRoot != null)
            {
                Undo.DestroyObjectImmediate(generator.GeneratedRoot.gameObject);
            }
        }

        // 에디터에서 만든 결과가 씬에 저장되도록 변경을 알린다. 플레이 중에는 저장 대상이 아니므로 건너뛴다.
        private static void MarkChanged(MapGeneratorBase generator)
        {
            if (Application.isPlaying) return;

            EditorUtility.SetDirty(generator);
            EditorSceneManager.MarkSceneDirty(generator.gameObject.scene);
        }
    }
}
