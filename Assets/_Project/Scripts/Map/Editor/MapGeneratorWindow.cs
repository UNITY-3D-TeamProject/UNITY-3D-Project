using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Map.Common;

namespace Map.Editor
{
    /// <summary>
    /// 맵 생성기를 한곳에서 관리하는 창. (메뉴: Tools > Map Generator)
    /// 왼쪽에서 씬의 생성기(MapGeneratorBase 를 상속한 컴포넌트)나 설정 에셋(SOMapSettings)을 고르면,
    /// 오른쪽에서 설정 에셋 교체·복제, Generate / Clear, 시드 고정, 설정 편집을 할 수 있다.
    /// 나선·미로 등 생성기의 종류와 상관없이 같은 창을 쓴다.
    /// </summary>
    // 이 네임스페이스(Map.Editor)와 이름이 겹치므로 에디터 클래스는 UnityEditor.Editor 로 전체 이름을 쓴다.
    public class MapGeneratorWindow : EditorWindow
    {
        #region Constants
        private const string MENU_PATH = "Tools/Map Generator";
        private const string WINDOW_TITLE = "Map Generator";

        // 생성기들이 설정 에셋을 담는 필드 이름. 생성기를 새로 만들 때도 이 이름을 쓴다.
        private const string SETTINGS_PROPERTY = "_settings";
        private const string RANDOM_SEED_PROPERTY = "_shouldUseRandomSeed";
        private const string SEED_PROPERTY = "_seed";
        private const string SCRIPT_PROPERTY = "m_Script";

        private const float LIST_WIDTH = 230.0f;
        #endregion

        #region Private Fields
        private MapGeneratorBase[] _generators = Array.Empty<MapGeneratorBase>();
        private SOMapSettings[] _settingsAssets = Array.Empty<SOMapSettings>();

        // 둘 중 하나만 고른 상태다. 생성기를 고르면 그 생성기의 설정 에셋을 함께 보여 준다.
        private MapGeneratorBase _selectedGenerator;
        private SOMapSettings _selectedAsset;

        private UnityEditor.Editor _settingsEditor;
        private string _summary = string.Empty;
        private Vector2 _listScroll;
        private Vector2 _detailScroll;
        private bool _isSceneSettingsOpen = true;
        private bool _isMapSettingsOpen = true;
        #endregion

        #region Unity Lifecycle
        private void OnEnable()
        {
            EditorApplication.hierarchyChanged += Refresh;
            EditorApplication.projectChanged += Refresh;
            Refresh();
        }

        private void OnDisable()
        {
            EditorApplication.hierarchyChanged -= Refresh;
            EditorApplication.projectChanged -= Refresh;

            if (_settingsEditor != null)
            {
                DestroyImmediate(_settingsEditor);
            }
        }

        private void OnGUI()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                DrawList();
                DrawDetail();
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// 창을 연다.
        /// </summary>
        [MenuItem(MENU_PATH)]
        public static void Open()
        {
            Open(null);
        }

        /// <summary>
        /// 창을 열고 generator 를 고른 상태로 만든다.
        /// </summary>
        /// <param name="generator">고를 생성기. null 이면 고르지 않는다.</param>
        public static void Open(MapGeneratorBase generator)
        {
            MapGeneratorWindow window = GetWindow<MapGeneratorWindow>(WINDOW_TITLE);
            window.minSize = new Vector2(700.0f, 360.0f);
            window.Refresh();

            if (generator != null)
            {
                window.SelectGenerator(generator);
            }

            window.Show();
        }
        #endregion

        #region Private Methods
        // 씬의 생성기와 프로젝트의 설정 에셋 목록을 다시 모은다.
        private void Refresh()
        {
            _generators = FindObjectsByType<MapGeneratorBase>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Array.Sort(_generators, (a, b) => string.CompareOrdinal(a.name, b.name));

            string[] guids = AssetDatabase.FindAssets($"t:{nameof(SOMapSettings)}");
            _settingsAssets = new SOMapSettings[guids.Length];
            for (int i = 0; i < guids.Length; i++)
            {
                _settingsAssets[i] = AssetDatabase.LoadAssetAtPath<SOMapSettings>(AssetDatabase.GUIDToAssetPath(guids[i]));
            }

            UpdateSummary();
            Repaint();
        }

        private void SelectGenerator(MapGeneratorBase generator)
        {
            _selectedGenerator = generator;
            _selectedAsset = null;
            UpdateSummary();
        }

        private void SelectAsset(SOMapSettings asset)
        {
            _selectedGenerator = null;
            _selectedAsset = asset;
            UpdateSummary();
        }

        // 왼쪽: 씬의 생성기 목록과 설정 에셋 목록
        private void DrawList()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox, GUILayout.Width(LIST_WIDTH)))
            {
                _listScroll = EditorGUILayout.BeginScrollView(_listScroll);

                EditorGUILayout.LabelField("씬의 생성기", EditorStyles.boldLabel);
                if (_generators.Length == 0)
                {
                    EditorGUILayout.LabelField("열린 씬에 생성기가 없습니다.", EditorStyles.wordWrappedMiniLabel);
                }

                foreach (MapGeneratorBase generator in _generators)
                {
                    if (generator == null) continue;

                    string label = $"{generator.name}\n{generator.GetType().Name}";
                    bool isSelected = generator == _selectedGenerator;
                    if (GUILayout.Toggle(isSelected, label, "Button", GUILayout.Height(36.0f)) && !isSelected)
                    {
                        SelectGenerator(generator);
                    }
                }

                EditorGUILayout.Space(10.0f);
                EditorGUILayout.LabelField("설정 에셋", EditorStyles.boldLabel);

                foreach (SOMapSettings asset in _settingsAssets)
                {
                    if (asset == null) continue;

                    bool isSelected = asset == _selectedAsset;
                    if (GUILayout.Toggle(isSelected, asset.name, "Button") && !isSelected)
                    {
                        SelectAsset(asset);
                    }
                }

                EditorGUILayout.EndScrollView();
            }
        }

        // 오른쪽: 고른 생성기 또는 설정 에셋의 내용
        private void DrawDetail()
        {
            using (new EditorGUILayout.VerticalScope())
            {
                _detailScroll = EditorGUILayout.BeginScrollView(_detailScroll);

                if (_selectedGenerator != null)
                {
                    DrawGenerator(_selectedGenerator);
                }
                else if (_selectedAsset != null)
                {
                    DrawAssetOnly(_selectedAsset);
                }
                else
                {
                    EditorGUILayout.HelpBox("왼쪽에서 생성기나 설정 에셋을 고르세요.", MessageType.Info);
                }

                EditorGUILayout.EndScrollView();
            }
        }

        private void DrawGenerator(MapGeneratorBase generator)
        {
            var serializedGenerator = new SerializedObject(generator);
            serializedGenerator.Update();
            SerializedProperty settingsProperty = serializedGenerator.FindProperty(SETTINGS_PROPERTY);

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label($"{generator.name}  ({generator.GetType().Name})", EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("하이어라키에서 보기", GUILayout.Width(130.0f)))
                {
                    Selection.activeGameObject = generator.gameObject;
                    EditorGUIUtility.PingObject(generator.gameObject);
                }
            }

            EditorGUILayout.Space(4.0f);
            DrawSettingsSlot(generator, settingsProperty);

            EditorGUILayout.Space(8.0f);
            DrawGenerateButtons(generator, serializedGenerator);

            EditorGUILayout.Space(8.0f);
            _isSceneSettingsOpen = EditorGUILayout.BeginFoldoutHeaderGroup(_isSceneSettingsOpen, "씬 설정 (이 생성기에만 적용)");
            EditorGUILayout.EndFoldoutHeaderGroup();
            if (_isSceneSettingsOpen)
            {
                DrawSceneSettings(serializedGenerator);
            }

            serializedGenerator.ApplyModifiedProperties();

            EditorGUILayout.Space(8.0f);
            _isMapSettingsOpen = EditorGUILayout.BeginFoldoutHeaderGroup(_isMapSettingsOpen, "맵 설정 (에셋, 이 에셋을 쓰는 모든 생성기에 적용)");
            EditorGUILayout.EndFoldoutHeaderGroup();
            if (_isMapSettingsOpen)
            {
                var settings = (settingsProperty != null) ? settingsProperty.objectReferenceValue as SOMapSettings : null;
                DrawSettingsAsset(settings);
            }
        }

        // 설정 에셋을 고르는 칸과, 복제·새로 만들기 버튼
        private void DrawSettingsSlot(MapGeneratorBase generator, SerializedProperty settingsProperty)
        {
            if (settingsProperty == null)
            {
                EditorGUILayout.HelpBox($"이 생성기에는 설정 에셋 필드({SETTINGS_PROPERTY})가 없습니다.", MessageType.Warning);
                return;
            }

            // PropertyField 로 그리면 필드의 Header 까지 따라 나오므로 ObjectField 로 직접 그린다
            Type settingsType = GetSettingsType(generator);
            settingsProperty.objectReferenceValue = EditorGUILayout.ObjectField(
                "설정 에셋",
                settingsProperty.objectReferenceValue,
                settingsType,
                false);

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();

                using (new EditorGUI.DisabledScope(settingsProperty.objectReferenceValue == null))
                {
                    if (GUILayout.Button(new GUIContent("복제해서 연결", "지금 설정을 복사한 새 에셋을 만들어 이 생성기에 연결한다. 원래 에셋은 그대로 남는다."), GUILayout.Width(110.0f)))
                    {
                        DuplicateSettings(settingsProperty);
                    }
                }

                if (GUILayout.Button(new GUIContent("새로 만들기", "기본값으로 된 새 설정 에셋을 만들어 이 생성기에 연결한다."), GUILayout.Width(90.0f)))
                {
                    CreateSettings(settingsType, settingsProperty);
                }
            }
        }

        // 생성기의 설정 필드가 받는 에셋 타입. 필드를 찾지 못하면 공통 부모 타입을 돌려준다.
        private static Type GetSettingsType(MapGeneratorBase generator)
        {
            FieldInfo field = generator.GetType().GetField(SETTINGS_PROPERTY, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            return (field != null) ? field.FieldType : typeof(SOMapSettings);
        }

        private void DrawGenerateButtons(MapGeneratorBase generator, SerializedObject serializedGenerator)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Generate Map", GUILayout.Height(32.0f)))
                {
                    // 창에서 바꾼 값이 생성에 반영되도록 먼저 적용한다
                    serializedGenerator.ApplyModifiedProperties();
                    MapGeneratorBaseEditor.GenerateWithUndo(generator);
                    MapGeneratorBaseEditor.MarkChanged(generator);
                    serializedGenerator.Update();
                    UpdateSummary();
                }

                if (GUILayout.Button("Clear", GUILayout.Height(32.0f), GUILayout.Width(100.0f)))
                {
                    MapGeneratorBaseEditor.ClearWithUndo(generator);
                    MapGeneratorBaseEditor.MarkChanged(generator);
                    serializedGenerator.Update();
                    UpdateSummary();
                }
            }

            if (!generator.IsGenerated)
            {
                EditorGUILayout.LabelField("생성된 맵이 없습니다. (Play 하면 자동으로 생성됩니다)", EditorStyles.miniLabel);
                return;
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label($"마지막 시드: {generator.LastSeed}");
                GUILayout.FlexibleSpace();

                if (GUILayout.Button("복사", GUILayout.Width(50.0f)))
                {
                    EditorGUIUtility.systemCopyBuffer = generator.LastSeed.ToString();
                }

                if (GUILayout.Button(new GUIContent("이 시드로 고정", "Should Use Random Seed 를 끄고 Seed 에 이 값을 넣어, 항상 같은 맵이 나오게 한다."), GUILayout.Width(100.0f)))
                {
                    serializedGenerator.FindProperty(RANDOM_SEED_PROPERTY).boolValue = false;
                    serializedGenerator.FindProperty(SEED_PROPERTY).intValue = generator.LastSeed;
                }
            }

            EditorGUILayout.LabelField(_summary, EditorStyles.miniLabel);
        }

        // 생성기 컴포넌트에 남아 있는 값(시드, 도착 프리팹, 배치 목록, 씬 오브젝트 참조)을 그린다.
        private static void DrawSceneSettings(SerializedObject serializedGenerator)
        {
            SerializedProperty property = serializedGenerator.GetIterator();
            bool shouldEnterChildren = true;

            while (property.NextVisible(shouldEnterChildren))
            {
                shouldEnterChildren = false;

                if ((property.name == SCRIPT_PROPERTY) || (property.name == SETTINGS_PROPERTY)) continue;

                EditorGUILayout.PropertyField(property, true);
            }
        }

        // 설정 에셋의 인스펙터를 창 안에 그린다.
        private void DrawSettingsAsset(SOMapSettings settings)
        {
            if (settings == null)
            {
                EditorGUILayout.HelpBox("설정 에셋이 연결되지 않았습니다. 위에서 에셋을 넣거나 새로 만드세요.", MessageType.Warning);
                return;
            }

            UnityEditor.Editor.CreateCachedEditor(settings, null, ref _settingsEditor);
            _settingsEditor.OnInspectorGUI();
        }

        private void DrawAssetOnly(SOMapSettings asset)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label($"{asset.name}  ({asset.GetType().Name})", EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("프로젝트에서 보기", GUILayout.Width(130.0f)))
                {
                    EditorGUIUtility.PingObject(asset);
                }
            }

            EditorGUILayout.LabelField(_summary, EditorStyles.miniLabel);
            EditorGUILayout.Space(8.0f);
            DrawSettingsAsset(asset);
        }

        // 지금 연결된 설정 에셋을 같은 폴더에 복사하고, 복사본을 생성기에 연결한다.
        private void DuplicateSettings(SerializedProperty settingsProperty)
        {
            string sourcePath = AssetDatabase.GetAssetPath(settingsProperty.objectReferenceValue);
            string targetPath = AssetDatabase.GenerateUniqueAssetPath(sourcePath);

            if (!AssetDatabase.CopyAsset(sourcePath, targetPath))
            {
                Debug.LogError($"설정 에셋을 복제하지 못했습니다: {sourcePath}");
                return;
            }

            settingsProperty.objectReferenceValue = AssetDatabase.LoadAssetAtPath<SOMapSettings>(targetPath);
        }

        // settingsType 의 새 에셋을 만들고 생성기에 연결한다.
        private void CreateSettings(Type settingsType, SerializedProperty settingsProperty)
        {
            if (settingsType.IsAbstract) return;

            string path = EditorUtility.SaveFilePanelInProject(
                "새 설정 에셋",
                settingsType.Name,
                "asset",
                "설정 에셋을 저장할 위치를 고르세요.");
            if (string.IsNullOrEmpty(path)) return;

            ScriptableObject asset = CreateInstance(settingsType);
            AssetDatabase.CreateAsset(asset, path);
            settingsProperty.objectReferenceValue = asset;

            // 저장 창을 닫고 돌아오면 그리던 레이아웃이 어긋나므로 이번 OnGUI 는 여기서 끝낸다
            settingsProperty.serializedObject.ApplyModifiedProperties();
            GUIUtility.ExitGUI();
        }

        // 고른 대상에 대한 한 줄 요약을 만든다. 매번 다시 세면 느리므로 고르거나 생성할 때만 갱신한다.
        private void UpdateSummary()
        {
            _summary = string.Empty;

            if ((_selectedGenerator != null) && _selectedGenerator.IsGenerated)
            {
                Transform root = _selectedGenerator.GeneratedRoot;
                Renderer[] renderers = root.GetComponentsInChildren<Renderer>();
                int objectCount = root.GetComponentsInChildren<Transform>(true).Length - 1;

                if (renderers.Length == 0)
                {
                    _summary = $"오브젝트 {objectCount}개";
                    return;
                }

                Bounds bounds = renderers[0].bounds;
                foreach (Renderer renderer in renderers)
                {
                    bounds.Encapsulate(renderer.bounds);
                }

                _summary = $"오브젝트 {objectCount}개, 크기 {bounds.size.x:F0} x {bounds.size.y:F0} x {bounds.size.z:F0} (가로 x 높이 x 세로)";
            }
            else if (_selectedAsset != null)
            {
                int userCount = 0;
                foreach (MapGeneratorBase generator in _generators)
                {
                    if (generator == null) continue;

                    SerializedProperty property = new SerializedObject(generator).FindProperty(SETTINGS_PROPERTY);
                    if ((property != null) && (property.objectReferenceValue == _selectedAsset)) userCount++;
                }

                _summary = $"열린 씬에서 이 에셋을 쓰는 생성기: {userCount}개";
            }
        }
        #endregion
    }
}
