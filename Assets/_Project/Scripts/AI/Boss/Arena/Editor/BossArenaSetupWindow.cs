using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using AI.Boss.Logic;

namespace AI.Boss.Arena.Editor
{
    /// <summary>맵 담당이 배치한 25개 타일에 보스전 참조를 연결한다. 지형은 이동하지 않는다.</summary>
    public class BossArenaSetupWindow : EditorWindow
    {
        #region Constants
        private const string BOSS_PREFABS = "Assets/_Project/Prefabs/AI/Boss/";
        private const string MATERIAL_PATH = "Assets/_Project/Art/Materials/Boss/M_BossTelegraph.mat";
        private const string UNDO_NAME = "Setup Boss Arena Tiles";
        #endregion

        #region Serialized Fields
        [SerializeField] private BossArena _arena;
        [SerializeField] private GameObject[] _tileRoots = Array.Empty<GameObject>();
        [SerializeField] private Transform _supportRoot;
        [SerializeField] private Material _telegraphMaterial;
        [SerializeField] private float _tileSize = 12.0f;
        #endregion

        #region Private Fields
        private string _message = string.Empty;
        private MessageType _messageType = MessageType.Info;
        private Vector2 _scroll;
        private bool _isTileListOpen;
        #endregion

        #region Unity Lifecycle
        private void OnEnable()
        {
            if (!_telegraphMaterial) _telegraphMaterial = AssetDatabase.LoadAssetAtPath<Material>(MATERIAL_PATH);
        }

        private void OnGUI()
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            EditorGUILayout.HelpBox(
                "1. 지형과 분리된 빈 오브젝트에 BossArena를 붙여 지정합니다.\n" +
                "2. Hierarchy에서 바닥 25개를 선택하고 선택 가져오기를 누릅니다.\n" +
                "3. 타일 크기와 아레나 회전을 맞춘 뒤 검사 → 일괄 설정합니다.\n" +
                "타일은 이동하지 않고 아레나 원점만 0번 칸 바깥 모서리로 맞춥니다.", MessageType.Info);

            EditorGUI.BeginChangeCheck();
            BossArena arena = (BossArena)EditorGUILayout.ObjectField("Boss Arena", _arena, typeof(BossArena), true);
            if (EditorGUI.EndChangeCheck())
            {
                _arena = arena;
                if (_arena) _tileSize = _arena.TileSize;
                _message = string.Empty;
            }

            _tileSize = EditorGUILayout.FloatField("타일 한 변 (월드 m)", _tileSize);
            _telegraphMaterial = (Material)EditorGUILayout.ObjectField(
                "타일 예고 머터리얼", _telegraphMaterial, typeof(Material), false);
            _supportRoot = (Transform)EditorGUILayout.ObjectField(
                new GUIContent("기둥 검색 루트 (선택)", "별도 배치된 기둥만 모인 부모를 지정합니다. 여러 칸에 걸친 지형은 자동 연결하지 않습니다."),
                _supportRoot, typeof(Transform), true);

            if (GUILayout.Button("선택한 바닥 25개 가져오기"))
            {
                _tileRoots = Selection.gameObjects;
                _message = $"{_tileRoots.Length}개를 가져왔습니다. 선택 순서 대신 아레나의 +X / +Z 방향으로 정렬합니다.";
                _messageType = MessageType.Info;
            }

            _isTileListOpen = EditorGUILayout.Foldout(_isTileListOpen, $"등록된 바닥 ({_tileRoots.Length})", true);
            if (_isTileListOpen)
            {
                for (int i = 0; i < _tileRoots.Length; i++)
                {
                    _tileRoots[i] = (GameObject)EditorGUILayout.ObjectField(
                        i.ToString(), _tileRoots[i], typeof(GameObject), true);
                }
            }

            using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
            {
                if (GUILayout.Button("배치 / 참조 검사")) Run(false);
                if (GUILayout.Button("25개 타일 일괄 설정 (Undo 가능)", GUILayout.Height(32))) Run(true);
            }

            EditorGUILayout.HelpBox(
                "기둥이 바닥의 자식이면 함께 연결합니다. 별도 기둥은 검색 루트를 지정하세요.\n" +
                "검사 시 Scene 뷰에 칸 번호가 표시됩니다. 0~24 순서는 row × 5 + col입니다.\n" +
                "체크포인트·라운드·스폰 위치·낙사 구역·NavMesh Bake는 이 도구의 설정 범위가 아닙니다.", MessageType.Info);
            if (!string.IsNullOrEmpty(_message)) EditorGUILayout.HelpBox(_message, _messageType);
            EditorGUILayout.EndScrollView();
        }
        #endregion

        #region Public Methods
        [MenuItem("Tools/Boss/타일 일괄 설정")]
        public static void Open()
        {
            Open(Selection.activeGameObject ? Selection.activeGameObject.GetComponent<BossArena>() : null);
        }

        public static void Open(BossArena arena)
        {
            BossArenaSetupWindow window = GetWindow<BossArenaSetupWindow>("Boss 타일 설정");
            window.minSize = new Vector2(440.0f, 440.0f);
            if (arena)
            {
                window._arena = arena;
                window._tileSize = arena.TileSize;
            }
        }

        /// <summary>먼저 전체 배치를 검사한 뒤 한 Undo 그룹으로 설정한다. 실패 시 변경을 되돌린다.</summary>
        public static void Configure(BossArena arena, GameObject[] roots, float tileSize, Transform supportRoot, Material material)
        {
            TileSetup[] tiles = BuildPlan(arena, roots, tileSize, supportRoot, material, out Vector3 origin);
            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName(UNDO_NAME);
            try
            {
                Undo.RecordObject(arena.transform, UNDO_NAME);
                arena.transform.position = origin;
                SerializedObject arenaData = new SerializedObject(arena);
                SerializedProperty entries = arenaData.FindProperty("_tiles");
                entries.arraySize = ArenaGrid.COUNT;
                for (int i = 0; i < tiles.Length; i++)
                {
                    BossTile tile = ConfigureTile(arena, tiles[i], i, tileSize, material);
                    entries.GetArrayElementAtIndex(i).objectReferenceValue = tile;
                }

                arenaData.FindProperty("_tileSize").floatValue = tileSize;
                SetPrefabIfEmpty(arenaData, "_circlePrefab", "BossCircleTelegraph.prefab");
                SetPrefabIfEmpty(arenaData, "_linePrefab", "BossLineTelegraph.prefab");
                arenaData.ApplyModifiedProperties();
                PrefabUtility.RecordPrefabInstancePropertyModifications(arena.transform);
                EditorSceneManager.MarkSceneDirty(arena.gameObject.scene);
                Undo.CollapseUndoOperations(group);
            }
            catch
            {
                Undo.RevertAllDownToGroup(group);
                throw;
            }
        }
        #endregion

        #region Private Methods
        private void Run(bool shouldApply)
        {
            try
            {
                TileSetup[] tiles = BuildPlan(_arena, _tileRoots, _tileSize, _supportRoot, _telegraphMaterial, out Vector3 origin);
                if (shouldApply) Configure(_arena, _tileRoots, _tileSize, _supportRoot, _telegraphMaterial);
                _message = (shouldApply ? "설정 완료. Ctrl+Z로 한 번에 되돌릴 수 있습니다. 씬을 저장하세요.\n" : "검사 통과. 아직 씬은 변경하지 않았습니다.\n") +
                    $"25칸 / 원점 {origin:F2}\n" +
                    string.Join("\n", tiles.Select((tile, i) => $"{i:00}: {tile.Root.name} (Renderer {tile.Renderers.Count}, Collider {tile.Colliders.Count})"));
                _messageType = MessageType.Info;
                SceneView.RepaintAll();
            }
            catch (ArgumentException exception)
            {
                _message = exception.Message;
                _messageType = MessageType.Error;
            }
        }

        private void OnFocus()
        {
            SceneView.duringSceneGui -= DrawTileLabels;
            SceneView.duringSceneGui += DrawTileLabels;
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= DrawTileLabels;
            SceneView.RepaintAll();
        }

        private void DrawTileLabels(SceneView sceneView)
        {
            if (!_arena || _tileRoots.Length != ArenaGrid.COUNT) return;
            try
            {
                TileSetup[] tiles = BuildPlan(_arena, _tileRoots, _tileSize, _supportRoot, _telegraphMaterial, out _);
                foreach (TileSetup tile in tiles)
                {
                    Handles.Label(tile.Center, Array.IndexOf(tiles, tile).ToString("00"));
                }
            }
            catch (ArgumentException) { }
        }

        private static TileSetup[] BuildPlan(BossArena arena, GameObject[] roots, float size, Transform supportRoot, Material material, out Vector3 origin)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new ArgumentException("Play를 종료한 뒤 설정하세요.");
            if (!arena || EditorUtility.IsPersistent(arena)) throw new ArgumentException("씬에 있는 BossArena를 지정하세요.");
            if (!material || !material.HasProperty("_BaseColor")) throw new ArgumentException("_BaseColor가 있는 타일 예고 머터리얼을 지정하세요.");
            if (float.IsNaN(size) || float.IsInfinity(size) || size <= 0.1f) throw new ArgumentException("타일 크기는 0.1m보다 커야 합니다.");
            if (roots == null || roots.Length != ArenaGrid.COUNT || roots.Distinct().Count() != ArenaGrid.COUNT)
                throw new ArgumentException("중복 없이 바닥 25개를 선택하세요. 이름과 선택 순서는 상관없습니다.");
            if (roots.Any(root => !root)) throw new ArgumentException("등록된 바닥에 비어 있는 항목이 있습니다. 다시 선택하세요.");
            if ((arena.transform.lossyScale - Vector3.one).sqrMagnitude > 0.0001f || Vector3.Dot(arena.transform.up, Vector3.up) < 0.9999f)
                throw new ArgumentException("BossArena는 부모를 포함한 월드 스케일이 (1,1,1)이고 수평이어야 합니다. Y축 회전은 가능합니다.");
            if (supportRoot && (EditorUtility.IsPersistent(supportRoot) || supportRoot.gameObject.scene != arena.gameObject.scene || supportRoot.IsChildOf(arena.transform)))
                throw new ArgumentException("기둥 검색 루트는 같은 씬에서 BossArena 바깥에 있는 지형을 지정하세요.");

            Matrix4x4 frame = Matrix4x4.Rotate(Quaternion.Inverse(arena.transform.rotation));
            float tolerance = Mathf.Max(0.02f, size * 0.05f);
            List<TileSetup> candidates = new List<TileSetup>();
            HashSet<UnityEngine.Object> geometry = new HashSet<UnityEngine.Object>();
            HashSet<Transform> telegraphs = new HashSet<Transform>();
            HashSet<NavMeshObstacle> obstacles = new HashSet<NavMeshObstacle>();
            foreach (GameObject root in roots)
            {
                if (!root || EditorUtility.IsPersistent(root) || root.scene != arena.gameObject.scene || !root.activeInHierarchy)
                    throw new ArgumentException("모든 바닥은 BossArena와 같은 씬의 활성 오브젝트여야 합니다.");
                if (root.transform.IsChildOf(arena.transform) || arena.transform.IsChildOf(root.transform) || roots.Any(other => other != root && root.transform.IsChildOf(other.transform)))
                    throw new ArgumentException("바닥끼리 부모·자식으로 중복 선택할 수 없습니다. BossArena는 지형과 분리된 빈 오브젝트에 두세요.");

                BossTile existing = root.GetComponent<BossTile>();
                GameObject telegraph = existing ? (GameObject)new SerializedObject(existing).FindProperty("_telegraph").objectReferenceValue : null;
                if (telegraph && (EditorUtility.IsPersistent(telegraph) || telegraph.scene != arena.gameObject.scene ||
                    !telegraph.GetComponent<Renderer>() || arena.transform.IsChildOf(telegraph.transform) ||
                    roots.Any(other => other.transform.IsChildOf(telegraph.transform)) || !telegraphs.Add(telegraph.transform)))
                    throw new ArgumentException($"{root.name}: 기존 Telegraph는 같은 씬의 독립된 Renderer여야 하며 다른 타일과 공유할 수 없습니다.");
                if (telegraph && telegraph.transform.parent &&
                    Mathf.Abs(telegraph.transform.parent.lossyScale.x * telegraph.transform.parent.lossyScale.y * telegraph.transform.parent.lossyScale.z) < 0.000001f)
                    throw new ArgumentException($"{root.name}: 기존 Telegraph의 부모 스케일이 0입니다.");
                NavMeshObstacle obstacle = existing ? (NavMeshObstacle)new SerializedObject(existing).FindProperty("_holeObstacle").objectReferenceValue : null;
                if (obstacle && (EditorUtility.IsPersistent(obstacle) || obstacle.gameObject.scene != arena.gameObject.scene || !obstacles.Add(obstacle)))
                    throw new ArgumentException($"{root.name}: 기존 Hole Obstacle은 같은 씬에 있어야 하며 다른 타일과 공유할 수 없습니다.");
                Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true).Where(r => !telegraph || !r.transform.IsChildOf(telegraph.transform)).ToArray();
                Collider[] colliders = root.GetComponentsInChildren<Collider>(true).Where(c => !c.isTrigger && (!telegraph || !c.transform.IsChildOf(telegraph.transform))).ToArray();
                if (renderers.Length == 0 || colliders.Length == 0) throw new ArgumentException($"{root.name}: 바닥 Renderer와 물리 Collider가 필요합니다.");
                Bounds bounds = RendererBounds(renderers[0], frame);
                foreach (Renderer renderer in renderers) { bounds.Encapsulate(RendererBounds(renderer, frame)); geometry.Add(renderer); }
                foreach (Collider collider in colliders) geometry.Add(collider);
                if (Mathf.Abs(bounds.size.x - size) > tolerance || Mathf.Abs(bounds.size.z - size) > tolerance)
                    throw new ArgumentException($"{root.name}: 지형 크기 {bounds.size.x:F2} × {bounds.size.z:F2}m가 타일 크기 {size:F2}m와 다릅니다. 아레나 회전·타일 크기·선택 범위를 확인하세요.");
                candidates.Add(new TileSetup(root, bounds, renderers, colliders));
            }

            float minX = candidates.Min(t => t.Bounds.center.x);
            float minZ = candidates.Min(t => t.Bounds.center.z);
            float top = candidates[0].Bounds.max.y;
            Vector3 localOrigin = new Vector3(minX - size * 0.5f, top, minZ - size * 0.5f);
            origin = arena.transform.rotation * localOrigin;
            TileSetup[] tiles = new TileSetup[ArenaGrid.COUNT];
            foreach (TileSetup tile in candidates)
            {
                float x = (tile.Bounds.center.x - minX) / size;
                float z = (tile.Bounds.center.z - minZ) / size;
                int col = Mathf.RoundToInt(x);
                int row = Mathf.RoundToInt(z);
                int index = ArenaGrid.GetIndex(row, col);
                if (index < 0 || tiles[index] != null || Mathf.Abs(x - col) * size > tolerance || Mathf.Abs(z - row) * size > tolerance || Mathf.Abs(tile.Bounds.max.y - top) > tolerance)
                    throw new ArgumentException($"{tile.Root.name}: 같은 높이의 일정한 5×5 격자가 아닙니다. 중복 칸·간격·아레나 회전을 확인하세요.");
                tiles[index] = tile;
                tile.Center = arena.transform.rotation * new Vector3(tile.Bounds.center.x, top, tile.Bounds.center.z);
                foreach (Collider collider in tile.Colliders)
                {
                    Bounds bounds = ColliderBounds(collider, frame);
                    if (bounds.min.x < tile.Bounds.center.x - size * 0.5f - tolerance || bounds.max.x > tile.Bounds.center.x + size * 0.5f + tolerance ||
                        bounds.min.z < tile.Bounds.center.z - size * 0.5f - tolerance || bounds.max.z > tile.Bounds.center.z + size * 0.5f + tolerance)
                        throw new ArgumentException($"{collider.name}: 바닥 Collider가 한 칸의 범위를 벗어납니다. 공용 Collider는 칸별로 분리하세요.");
                }
            }

            if (supportRoot)
            {
                foreach (Renderer renderer in supportRoot.GetComponentsInChildren<Renderer>(true))
                {
                    if (geometry.Contains(renderer) || renderer.transform.IsChildOf(arena.transform) || telegraphs.Any(t => renderer.transform.IsChildOf(t))) continue;
                    int index = FindSupportTile(RendererBounds(renderer, frame), localOrigin, size, tolerance);
                    if (index >= 0) tiles[index].Renderers.Add(renderer);
                }
                foreach (Collider collider in supportRoot.GetComponentsInChildren<Collider>(true))
                {
                    if (geometry.Contains(collider) || collider.isTrigger || collider.transform.IsChildOf(arena.transform) || telegraphs.Any(t => collider.transform.IsChildOf(t))) continue;
                    int index = FindSupportTile(ColliderBounds(collider, frame), localOrigin, size, tolerance);
                    if (index >= 0) tiles[index].Colliders.Add(collider);
                }
            }
            return tiles;
        }

        private static int FindSupportTile(Bounds bounds, Vector3 origin, float size, float tolerance)
        {
            Vector3 center = bounds.center - origin;
            if (bounds.max.y > origin.y + tolerance || center.x < 0 || center.z < 0 || center.x >= size * ArenaGrid.SIZE || center.z >= size * ArenaGrid.SIZE) return -1;
            int col = Mathf.FloorToInt(center.x / size);
            int row = Mathf.FloorToInt(center.z / size);
            if (bounds.min.x < origin.x + col * size - tolerance || bounds.max.x > origin.x + (col + 1) * size + tolerance ||
                bounds.min.z < origin.z + row * size - tolerance || bounds.max.z > origin.z + (row + 1) * size + tolerance)
                throw new ArgumentException("기둥 검색 범위에 여러 칸에 걸친 Renderer 또는 Collider가 있습니다. 칸별로 분리하거나 검색 범위를 줄이세요.");
            return ArenaGrid.GetIndex(row, col);
        }

        private static BossTile ConfigureTile(BossArena arena, TileSetup setup, int index, float size, Material material)
        {
            BossTile tile = setup.Root.GetComponent<BossTile>();
            if (!tile) tile = Undo.AddComponent<BossTile>(setup.Root);
            SerializedObject data = new SerializedObject(tile);
            GameObject telegraph = (GameObject)data.FindProperty("_telegraph").objectReferenceValue;
            if (!telegraph)
            {
                telegraph = GameObject.CreatePrimitive(PrimitiveType.Quad);
                telegraph.name = $"BossTelegraph_{index:00}";
                Undo.RegisterCreatedObjectUndo(telegraph, UNDO_NAME);
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(telegraph, arena.gameObject.scene);
                Undo.SetTransformParent(telegraph.transform, arena.transform, UNDO_NAME);
            }
            foreach (Collider collider in telegraph.GetComponents<Collider>()) Undo.DestroyObjectImmediate(collider);
            Undo.RecordObjects(new UnityEngine.Object[] { telegraph, telegraph.transform, telegraph.GetComponent<Renderer>() }, UNDO_NAME);
            telegraph.transform.SetPositionAndRotation(setup.Center + Vector3.up * 0.03f, arena.transform.rotation * Quaternion.Euler(90.0f, 0.0f, 0.0f));
            // 기존 프리팹 자식은 재부모화하지 않고, 부모 스케일을 환산해 월드 크기를 맞춘다.
            telegraph.transform.localScale = Vector3.one;
            Vector3 scale = telegraph.transform.lossyScale;
            telegraph.transform.localScale = new Vector3(size / Mathf.Abs(scale.x), size / Mathf.Abs(scale.y), 1.0f / Mathf.Abs(scale.z));
            telegraph.GetComponent<Renderer>().sharedMaterial = material;
            telegraph.SetActive(false);
            PrefabUtility.RecordPrefabInstancePropertyModifications(telegraph.transform);
            PrefabUtility.RecordPrefabInstancePropertyModifications(telegraph.GetComponent<Renderer>());
            PrefabUtility.RecordPrefabInstancePropertyModifications(telegraph);

            NavMeshObstacle obstacle = (NavMeshObstacle)data.FindProperty("_holeObstacle").objectReferenceValue;
            if (!obstacle)
            {
                GameObject hole = new GameObject($"BossHole_{index:00}");
                Undo.RegisterCreatedObjectUndo(hole, UNDO_NAME);
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(hole, arena.gameObject.scene);
                Undo.SetTransformParent(hole.transform, arena.transform, UNDO_NAME);
                hole.transform.SetPositionAndRotation(setup.Center, arena.transform.rotation);
                hole.transform.localScale = Vector3.one;
                obstacle = Undo.AddComponent<NavMeshObstacle>(hole);
            }
            Undo.RecordObject(obstacle, UNDO_NAME);
            Bounds holeBounds = TransformBounds(new Bounds(arena.transform.InverseTransformPoint(setup.Center), new Vector3(size, 4.0f, size)), obstacle.transform.worldToLocalMatrix * arena.transform.localToWorldMatrix);
            obstacle.shape = NavMeshObstacleShape.Box;
            obstacle.center = holeBounds.center;
            obstacle.size = holeBounds.size;
            obstacle.carving = true;
            obstacle.enabled = false;
            PrefabUtility.RecordPrefabInstancePropertyModifications(obstacle);

            SetReferences(data.FindProperty("_renderers"), setup.Renderers);
            SetReferences(data.FindProperty("_colliders"), setup.Colliders);
            data.FindProperty("_telegraph").objectReferenceValue = telegraph;
            data.FindProperty("_holeObstacle").objectReferenceValue = obstacle;
            data.ApplyModifiedProperties();
            return tile;
        }

        private static void SetReferences<T>(SerializedProperty property, List<T> values) where T : UnityEngine.Object
        {
            property.arraySize = values.Count;
            for (int i = 0; i < values.Count; i++) property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }

        private static void SetPrefabIfEmpty(SerializedObject data, string field, string filename)
        {
            SerializedProperty property = data.FindProperty(field);
            if (!property.objectReferenceValue) property.objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(BOSS_PREFABS + filename);
        }

        private static Bounds RendererBounds(Renderer renderer, Matrix4x4 frame)
        {
            return TransformBounds(renderer.localBounds, frame * renderer.localToWorldMatrix);
        }

        private static Bounds ColliderBounds(Collider collider, Matrix4x4 frame)
        {
            if (collider is BoxCollider box) return TransformBounds(new Bounds(box.center, box.size), frame * box.transform.localToWorldMatrix);
            if (collider is MeshCollider mesh && mesh.sharedMesh) return TransformBounds(mesh.sharedMesh.bounds, frame * mesh.transform.localToWorldMatrix);
            return TransformBounds(collider.bounds, frame);
        }

        private static Bounds TransformBounds(Bounds bounds, Matrix4x4 matrix)
        {
            Bounds result = new Bounds(matrix.MultiplyPoint3x4(bounds.min), Vector3.zero);
            for (int x = 0; x < 2; x++)
            for (int y = 0; y < 2; y++)
            for (int z = 0; z < 2; z++)
                result.Encapsulate(matrix.MultiplyPoint3x4(new Vector3(x == 0 ? bounds.min.x : bounds.max.x, y == 0 ? bounds.min.y : bounds.max.y, z == 0 ? bounds.min.z : bounds.max.z)));
            return result;
        }
        #endregion

        #region Nested Types
        private sealed class TileSetup
        {
            public GameObject Root { get; }
            public Bounds Bounds { get; }
            public List<Renderer> Renderers { get; }
            public List<Collider> Colliders { get; }
            public Vector3 Center { get; set; }

            public TileSetup(GameObject root, Bounds bounds, Renderer[] renderers, Collider[] colliders)
            {
                Root = root;
                Bounds = bounds;
                Renderers = new List<Renderer>(renderers);
                Colliders = new List<Collider>(colliders);
            }
        }
        #endregion
    }
}
