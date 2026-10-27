using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Unity.Behavior;
using Unity.Behavior.GraphFramework;
using Core;
using NodeRegistry = Unity.Behavior.NodeRegistry;

namespace AI.Editor
{
    /// <summary>
    /// SOMonsterData 에 등록된 몬스터들의 밸런스 수치(SO 어트리뷰트·프리팹 컴포넌트·BT)를
    /// 행 = 수치, 열 = 몬스터 표로 비교하고 바로 수정한다.
    /// BT 는 Behavior 패키지 내부 API(InternalsVisibleTo Assembly-CSharp-Editor)로 저작 그래프를 고친 뒤 런타임 그래프를 다시 만든다.
    /// </summary>
    public class AIBalanceWindow : EditorWindow
    {
        #region Constants
        private const string PROJECT_ROOT = "Assets/_Project/";
        private const string DATA_PREFIX = "SOMonsterData_";
        private const string GROUP_ATTRIBUTE = "어트리뷰트 (SOMonsterData)";
        private const string GROUP_BLACKBOARD = "BT 블랙보드";
        private const string GROUP_BT_NODE = "BT 노드 값";
        private const string UNDO_NAME = "AI Balance";
        private const float LABEL_WIDTH = 260.0f;
        private const float COLUMN_WIDTH = 110.0f;
        #endregion

        #region Static Fields
        /// <summary>
        /// 행 이름에 마우스를 올리면 보여줄 설명. 컴포넌트 값은 필드의 [Tooltip] 을 쓰므로 여기에는
        /// 설명을 붙일 곳이 없는 어트리뷰트 이름·블랙보드 변수 이름·"노드(조건) 이름 / 필드 이름"만 적는다.
        /// </summary>
        private static readonly Dictionary<string, string> Descriptions = new Dictionary<string, string>
        {
            // 어트리뷰트
            { "MaxHp", "최대 체력" },
            { "CurrentHp", "스폰할 때의 체력" },
            { "MoveSpeed", "이동 속도" },
            { "AttackDamage", "공격 피해량" },

            // 블랙보드
            { "IsGroggy", "그로기 상태인지. 전투 중 BossEncounter 가 바꾸는 값" },
            { "ComboRange", "보스 B 가 근접 콤보를 고르는 거리 (m)" },
            { "ChargeReadyTime", "다음 돌진이 가능한 시각. 전투 중 자동으로 바뀌는 값" },
            { "LeapReadyTime", "다음 도약이 가능한 시각. 전투 중 자동으로 바뀌는 값" },
            { "AttackRange", "대상이 이 거리 안에 들어오면 공격 (m)" },
            { "RetreateRange", "대상이 이 거리 안에 들어오면 후퇴 (m). 특수몹은 연결된 노드가 없어 효과 없음" },
            { "RetreatExitRange", "후퇴할 때 대상과 벌릴 거리이자 한 번에 물러나는 거리 (m)" },
            { "WindRange", "대상이 이 거리 안에 들어오면 바람 공격 시도 (m)" },
            { "WindReach", "바람 스킬을 쓸 수 있는 최대 거리 (m). 이보다 멀면 스킬을 쓰지 않음" },
            { "WindCooldown", "바람 공격 후 다음 사용까지 기다리는 시간 (초)" },
            { "WindReadyTime", "다음 바람 공격이 가능한 시각. 전투 중 자동으로 바뀌는 값" },
            { "RetreateMoveDistance", "연결된 노드가 없어 바꿔도 효과 없음" },
            { "AttackExitRange", "연결된 노드가 없어 바꿔도 효과 없음" },

            // BT 노드 값
            { "Variable Comparison / ComparisonValue", "블랙보드 변수와 비교할 값" },
            { "Time Out / Duration", "아래 노드를 이 시간이 지나면 강제로 끝냄 (초)" },
            { "Wait (Seconds) / SecondsToWait", "기다리는 시간 (초)" },
            { "스킬 사용 후 대기 / Range", "대상이 이 거리 안에 있을 때만 스킬 사용 (m)" },
            { "준비 시각 설정 / Delay", "스킬이 끝난 뒤 다음 사용까지 기다리는 시간 (쿨타임, 초)" },
            { "공격 / Range", "대상이 이 거리 안에 있으면 공격 (m)" },
            { "공격 / ExitRange", "공격을 멈추는 거리 (m). Range 보다 크게 두면 경계에서 공격·추격이 번갈아 바뀌지 않음. Range 이하면 Range 사용" },
            { "후퇴 / Range", "대상과 이 거리 이상 벌어질 때까지 후퇴 (m)" },
            { "후퇴 / MoveDistance", "한 번에 물러나는 거리 (m)" },
            { "후퇴 / SpeedRatio", "후퇴할 때 이동 속도 배율" },
            { "원거리 공격 / Range", "대상이 이 거리 안에 있으면 사격 (m)" },
            { "원거리 공격 / MinInterval", "사격 사이 대기 시간의 최솟값 (초). 최솟값~최댓값 사이에서 무작위" },
            { "원거리 공격 / MaxInterval", "사격 사이 대기 시간의 최댓값 (초)" },
            { "원거리 공격 / WindupDuration", "조준선을 보여주는 예고 시간 (초). 사격 모션 지연보다 짧으면 그만큼 늘어남" },
            { "원거리 공격 / KeepMoving", "켜면 사격 중에도 이동을 멈추지 않음 (후퇴하며 사격)" },
            { "원거리 공격 / AttackMotionDuration", "발사 직후 조준 방향을 유지하는 발사 모션 시간 (초)" },
            { "원거리 공격 / FireDelay", "사격 모션 시작부터 총알이 나가기까지의 시간 (초). 사격 스킬의 Fire Delay 와 같게 둠" },
            { "사선이 트여 있다 / IsClear", "켜면 대상까지 사선이 트였을 때, 끄면 막혔을 때 통과" },
            { "대상이 거리 안에 있다 / Range", "대상이 이 거리 안에 있으면 통과 (m)" },
        };
        #endregion

        #region Private Fields
        private readonly List<SOMonsterData> _monsters = new List<SOMonsterData>();
        private readonly List<string> _groups = new List<string>();
        private readonly Dictionary<string, List<Row>> _rowsByGroup = new Dictionary<string, List<Row>>();
        private readonly HashSet<string> _expandedGroups = new HashSet<string> { GROUP_ATTRIBUTE };
        private Vector2 _scroll;
        private string _search = string.Empty;
        #endregion

        #region Nested Types
        private sealed class Row
        {
            public string Label;
            public string Tooltip;
            public Cell[] Cells;
        }

        private sealed class Cell
        {
            public object Value;
            public Action<object> Write;
        }
        #endregion

        #region Unity Lifecycle
        private void OnEnable()
        {
            Undo.undoRedoPerformed += OnUndoRedo;
            Refresh();
        }

        private void OnDisable()
        {
            Undo.undoRedoPerformed -= OnUndoRedo;
        }

        private void OnFocus()
        {
            Refresh();
        }

        private void OnGUI()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                if (GUILayout.Button("새로고침", EditorStyles.toolbarButton, GUILayout.Width(70.0f))) Refresh();
                _search = EditorGUILayout.TextField(_search, EditorStyles.toolbarSearchField);
            }

            EditorGUILayout.HelpBox(
                "값을 바꾸고 Enter(또는 다른 칸 클릭)하면 SO·프리팹·BT 에셋에 바로 저장됩니다.\n" +
                "SO 수정만 Undo 됩니다. 프리팹·BT 값은 같은 프리팹·BT 를 쓰는 모든 몬스터에 적용됩니다. '-' 는 그 몬스터에 없는 항목입니다.",
                MessageType.Info);

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            DrawHeader();
            foreach (string group in _groups) DrawGroup(group);
            EditorGUILayout.EndScrollView();
        }
        #endregion

        #region Public Methods
        [MenuItem("Tools/AI/밸런스 수치 표")]
        public static void Open()
        {
            GetWindow<AIBalanceWindow>("AI Balance");
        }
        #endregion

        #region Private Methods - Collect
        private void Refresh()
        {
            _monsters.Clear();
            _groups.Clear();
            _rowsByGroup.Clear();

            foreach (string guid in AssetDatabase.FindAssets("t:SOMonsterData"))
            {
                SOMonsterData data = AssetDatabase.LoadAssetAtPath<SOMonsterData>(AssetDatabase.GUIDToAssetPath(guid));
                if (data) _monsters.Add(data);
            }
            _monsters.Sort((a, b) => string.CompareOrdinal(a.name, b.name));

            for (int i = 0; i < _monsters.Count; i++)
            {
                CollectAttributes(i);
                GameObject prefab = _monsters[i].MonsterPrefab;
                if (!prefab) continue;
                CollectBehaviorGraph(i, prefab);
                CollectComponents(i, prefab);
            }

            // 고정 그룹(어트리뷰트·BT)을 컴포넌트 그룹보다 위에 둔다.
            string[] fixedGroups = { GROUP_BT_NODE, GROUP_BLACKBOARD, GROUP_ATTRIBUTE };
            foreach (string group in fixedGroups)
            {
                if (_groups.Remove(group)) _groups.Insert(0, group);
            }
            Repaint();
        }

        private void CollectAttributes(int column)
        {
            SOMonsterData data = _monsters[column];
            SerializedProperty attributes = new SerializedObject(data).FindProperty("_attributes");
            if (attributes == null) return;

            for (int i = 0; i < attributes.arraySize; i++)
            {
                SerializedProperty element = attributes.GetArrayElementAtIndex(i);
                SerializedProperty valueProperty = element.FindPropertyRelative("Value");
                string path = valueProperty.propertyPath;
                string attributeName = element.FindPropertyRelative("AttributeName").stringValue;
                AddCell(GROUP_ATTRIBUTE, attributeName, FindDescription(attributeName), column,
                    ReadProperty(valueProperty), value =>
                    {
                        SerializedObject target = new SerializedObject(data);
                        WriteProperty(target.FindProperty(path), value);
                        target.ApplyModifiedProperties();
                        AssetDatabase.SaveAssetIfDirty(data);
                    });
            }
        }

        private void CollectComponents(int column, GameObject prefab)
        {
            string prefabPath = AssetDatabase.GetAssetPath(prefab);
            MonoBehaviour[] behaviours = prefab.GetComponentsInChildren<MonoBehaviour>(true);

            Dictionary<Type, int> typeCounts = new Dictionary<Type, int>();
            foreach (MonoBehaviour behaviour in behaviours)
            {
                if (!behaviour) continue;
                typeCounts.TryGetValue(behaviour.GetType(), out int count);
                typeCounts[behaviour.GetType()] = count + 1;
            }

            for (int i = 0; i < behaviours.Length; i++)
            {
                MonoBehaviour behaviour = behaviours[i];
                if (!behaviour || !IsProjectScript(behaviour)) continue;

                // 같은 컴포넌트가 여러 개면 자식 경로로 구분한다.
                string group = behaviour.GetType().Name;
                if (typeCounts[behaviour.GetType()] > 1)
                {
                    group += " / " + AnimationUtility.CalculateTransformPath(behaviour.transform, prefab.transform);
                }

                int componentIndex = i;
                SerializedProperty property = new SerializedObject(behaviour).GetIterator();
                bool shouldEnterChildren = true;
                while (property.NextVisible(shouldEnterChildren))
                {
                    shouldEnterChildren = (property.propertyType == SerializedPropertyType.Generic) && !property.isArray;
                    if (!IsEditableType(property.propertyType)) continue;

                    string path = property.propertyPath;
                    AddCell(group, ToLabel(path), property.tooltip, column, ReadProperty(property),
                        value => WritePrefab(prefabPath, componentIndex, path, value));
                }
            }
        }

        private void CollectBehaviorGraph(int column, GameObject prefab)
        {
            BehaviorGraphAgent agent = prefab.GetComponentInChildren<BehaviorGraphAgent>(true);
            if (!agent || !agent.Graph) return;

            BehaviorAuthoringGraph authoring =
                AssetDatabase.LoadAssetAtPath<BehaviorAuthoringGraph>(AssetDatabase.GetAssetPath(agent.Graph));
            if (!authoring) return;

            if (authoring.Blackboard)
            {
                foreach (VariableModel variable in authoring.Blackboard.Variables)
                {
                    if (!IsEditableType(variable.Type)) continue;
                    AddCell(GROUP_BLACKBOARD, variable.Name, FindDescription(variable.Name), column, variable.ObjectValue, value =>
                    {
                        authoring.Blackboard.MarkUndo(UNDO_NAME);
                        variable.ObjectValue = value;
                        RebuildGraph(authoring);
                    });
                }
            }

            // 같은 이름의 노드가 여러 개면 순서 번호를 붙인다.
            Dictionary<string, int> nameCounts = new Dictionary<string, int>();
            foreach (NodeModel node in authoring.Nodes)
            {
                if (!(node is BehaviorGraphNodeModel behaviorNode)) continue;

                string baseName = NodeRegistry.GetInfoFromTypeID(behaviorNode.NodeTypeID)?.Name ?? node.GetType().Name;
                nameCounts.TryGetValue(baseName, out int count);
                nameCounts[baseName] = ++count;
                string nodeName = (count > 1) ? baseName + " #" + count : baseName;

                AddFieldCells(column, authoring, nodeName, baseName, behaviorNode.Fields);
                if (!(node is IConditionalNodeModel conditional)) continue;

                foreach (ConditionModel condition in conditional.ConditionModels)
                {
                    string conditionName = NodeRegistry.GetConditionInfoFromTypeID(condition.ConditionTypeID)?.Name ?? "Condition";
                    AddFieldCells(column, authoring, nodeName + " / " + conditionName, conditionName, condition.Fields);
                }
            }
        }

        /// <summary>
        /// 블랙보드에 연결되지 않은 노드 로컬 값만 수집한다. 연결된 값은 블랙보드 행에서 고친다.
        /// 설명은 순서 번호·부모 노드와 상관없이 "필드 주인(노드·조건) 이름 / 필드 이름"으로 찾는다.
        /// </summary>
        private void AddFieldCells(
            int column,
            BehaviorAuthoringGraph authoring,
            string prefix,
            string ownerName,
            IEnumerable<BehaviorGraphNodeModel.FieldModel> fields)
        {
            foreach (BehaviorGraphNodeModel.FieldModel field in fields)
            {
                if ((field.LinkedVariable != null) ||
                    (field.LocalValue == null) ||
                    !IsEditableType(field.LocalValue.Type))
                {
                    continue;
                }

                BlackboardVariable local = field.LocalValue;
                AddCell(GROUP_BT_NODE, prefix + " / " + field.FieldName, FindDescription(ownerName + " / " + field.FieldName),
                    column, local.ObjectValue, value =>
                {
                    authoring.MarkUndo(UNDO_NAME);
                    local.ObjectValue = value;
                    RebuildGraph(authoring);
                });
            }
        }

        private void AddCell(string group, string label, string description, int column, object value, Action<object> write)
        {
            if (!_rowsByGroup.TryGetValue(group, out List<Row> rows))
            {
                rows = new List<Row>();
                _rowsByGroup.Add(group, rows);
                _groups.Add(group);
            }

            Row row = rows.Find(r => r.Label == label);
            if (row == null)
            {
                // 잘린 라벨도 읽을 수 있도록 툴팁 첫 줄에 라벨 전체를 둔다.
                string tooltip = string.IsNullOrEmpty(description) ? label : label + "\n" + description;
                row = new Row { Label = label, Tooltip = tooltip, Cells = new Cell[_monsters.Count] };
                rows.Add(row);
            }
            row.Cells[column] = new Cell { Value = value, Write = write };
        }
        #endregion

        #region Private Methods - Draw
        private void DrawHeader()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label(string.Empty, GUILayout.Width(LABEL_WIDTH));
                foreach (SOMonsterData data in _monsters)
                {
                    if (GUILayout.Button(data.name.Replace(DATA_PREFIX, string.Empty), EditorStyles.miniButton,
                            GUILayout.Width(COLUMN_WIDTH)))
                    {
                        Selection.activeObject = data;
                        EditorGUIUtility.PingObject(data);
                    }
                }
            }

            // 같은 프리팹을 쓰는 열은 프리팹 값을 공유하므로 프리팹 이름을 보여준다.
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label("프리팹", GUILayout.Width(LABEL_WIDTH));
                foreach (SOMonsterData data in _monsters)
                {
                    string prefabName = data.MonsterPrefab ? data.MonsterPrefab.name : "(없음)";
                    GUILayout.Label(prefabName, EditorStyles.centeredGreyMiniLabel, GUILayout.Width(COLUMN_WIDTH));
                }
            }
        }

        private void DrawGroup(string group)
        {
            bool isSearching = !string.IsNullOrEmpty(_search);
            List<Row> rows = _rowsByGroup[group];
            if (isSearching && !IsMatch(group)) rows = rows.FindAll(r => IsMatch(r.Label));
            if (rows.Count == 0) return;

            bool isExpanded = isSearching || _expandedGroups.Contains(group);
            bool shouldExpand = EditorGUILayout.Foldout(isExpanded, $"{group} ({rows.Count})", true, EditorStyles.foldoutHeader);
            if (!isSearching && (shouldExpand != isExpanded))
            {
                if (shouldExpand) _expandedGroups.Add(group);
                else _expandedGroups.Remove(group);
            }
            if (!shouldExpand) return;

            foreach (Row row in rows)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField(new GUIContent(row.Label, row.Tooltip), GUILayout.Width(LABEL_WIDTH));
                    foreach (Cell cell in row.Cells) DrawCell(cell);
                }
            }
        }

        private void DrawCell(Cell cell)
        {
            GUILayoutOption width = GUILayout.Width(COLUMN_WIDTH);
            if (cell == null)
            {
                GUILayout.Label("-", EditorStyles.centeredGreyMiniLabel, width);
                return;
            }

            EditorGUI.BeginChangeCheck();
            object newValue = cell.Value switch
            {
                float f => EditorGUILayout.DelayedFloatField(f, width),
                int i => EditorGUILayout.DelayedIntField(i, width),
                bool b => EditorGUILayout.Toggle(b, width),
                _ => cell.Value,
            };
            if (!EditorGUI.EndChangeCheck()) return;

            // 그리는 도중 에셋을 저장·재수집하지 않도록 다음 에디터 틱으로 미룬다.
            cell.Value = newValue;
            EditorApplication.delayCall += () =>
            {
                cell.Write(newValue);
                Refresh();
            };
        }
        #endregion

        #region Private Methods - Utility
        private void OnUndoRedo()
        {
            Refresh();
        }

        private bool IsMatch(string text)
        {
            return text.IndexOf(_search, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static string FindDescription(string key)
        {
            return Descriptions.TryGetValue(key, out string description) ? description : null;
        }

        private static bool IsProjectScript(MonoBehaviour behaviour)
        {
            MonoScript script = MonoScript.FromMonoBehaviour(behaviour);
            return script && AssetDatabase.GetAssetPath(script).StartsWith(PROJECT_ROOT, StringComparison.Ordinal);
        }

        private static bool IsEditableType(SerializedPropertyType type)
        {
            return (type == SerializedPropertyType.Float) ||
                   (type == SerializedPropertyType.Integer) ||
                   (type == SerializedPropertyType.Boolean);
        }

        private static bool IsEditableType(Type type)
        {
            return (type == typeof(float)) || (type == typeof(int)) || (type == typeof(bool));
        }

        /// <summary>"_settings._range" → "Settings / Range"</summary>
        private static string ToLabel(string propertyPath)
        {
            string[] parts = propertyPath.Split('.');
            for (int i = 0; i < parts.Length; i++) parts[i] = ObjectNames.NicifyVariableName(parts[i]);
            return string.Join(" / ", parts);
        }

        private static object ReadProperty(SerializedProperty property)
        {
            switch (property.propertyType)
            {
                case SerializedPropertyType.Float: return property.floatValue;
                case SerializedPropertyType.Integer: return property.intValue;
                case SerializedPropertyType.Boolean: return property.boolValue;
                default: return null;
            }
        }

        private static void WriteProperty(SerializedProperty property, object value)
        {
            switch (value)
            {
                case float f: property.floatValue = f; break;
                case int i: property.intValue = i; break;
                case bool b: property.boolValue = b; break;
            }
        }

        /// <summary>중첩 프리팹 컴포넌트도 바깥 프리팹의 오버라이드로 저장되도록 프리팹 내용을 열어 고친다.</summary>
        private static void WritePrefab(string prefabPath, int componentIndex, string propertyPath, object value)
        {
            using (PrefabUtility.EditPrefabContentsScope scope = new PrefabUtility.EditPrefabContentsScope(prefabPath))
            {
                MonoBehaviour[] behaviours = scope.prefabContentsRoot.GetComponentsInChildren<MonoBehaviour>(true);
                SerializedObject target = new SerializedObject(behaviours[componentIndex]);
                WriteProperty(target.FindProperty(propertyPath), value);
                target.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void RebuildGraph(BehaviorAuthoringGraph authoring)
        {
            authoring.RebuildAndSave();
            authoring.Blackboard.SaveAsset();
        }
        #endregion
    }
}
