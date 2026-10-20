using UnityEditor;
using UnityEngine;

namespace AI.Boss.Arena.Editor
{
    [CustomEditor(typeof(BossArena))]
    public class BossArenaEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            GUILayout.Space(10.0f);
            if (GUILayout.Button("타일 일괄 설정 창 열기", GUILayout.Height(28.0f)))
            {
                BossArenaSetupWindow.Open((BossArena)target);
            }
        }
    }
}
