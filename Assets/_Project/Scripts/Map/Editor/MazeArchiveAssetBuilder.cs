using UnityEditor;
using UnityEngine;
using Map.Maze;

namespace Map.Editor
{
    /// <summary>
    /// 파일 스테이지의 이어진 미로(MazeChainGenerator)에 쓰는 프리팹을 만든다. 이미 있는 프리팹과 머티리얼은 건드리지 않는다.
    /// 분위기: 어두운 자료 보관소. 벽은 서랍이 층층이 쌓인 보관함이고, 미로마다 포인트 색(남색 / 보라 / 호박색)이 다르다.
    /// - MazeFloor            : 바닥 블록 (1 x 1 x 1, 생성기가 미로 크기로 늘린다)
    /// - MazeWall_A / B / C   : 벽 한 칸 (길이 방향 X축)
    /// - MazeGate_A / B / C   : 출구 (출구 칸 중앙에 놓임, +Z 쪽이 문, 바닥에 빨간 도착 표시)
    /// - MazeBridgeSegment    : 다리 한 칸 (길이 방향 Z축)
    /// - MazeStartMarker      : 입구 칸 초록 표시 (테스트용)
    /// 메뉴: Project > Map > Build Archive Maze Prefabs
    /// </summary>
    public static class MazeArchiveAssetBuilder
    {
        #region Constants
        private const string PREFAB_DIR   = "Assets/_Project/Prefabs/Map/Maze/Archive";
        private const string MATERIAL_DIR = "Assets/_Project/Art/Map/Maze";

        // MazeChainGenerator 의 Tile Size / Wall Thickness 와 맞춘다.
        private const float TILE_SIZE      = 5.0f;
        private const float WALL_THICKNESS = 0.5f;
        private const float WALL_HEIGHT    = 4.0f;

        // 벽끼리 만나는 모서리가 비지 않도록 두께만큼 더 길게 만든다.
        private const float WALL_LENGTH = TILE_SIZE + WALL_THICKNESS;

        private const float BRIDGE_WIDTH     = 4.0f;
        private const float BRIDGE_THICKNESS = 0.4f;
        private const float RAIL_HEIGHT      = 0.6f;

        private const float MARKER_SIZE = 3.5f;

        private const float ACCENT_EMISSION_INTENSITY = 1.6f;
        private const float MARKER_EMISSION_INTENSITY = 2.0f;
        private const float BRIDGE_EMISSION_INTENSITY = 1.2f;
        #endregion

        #region Private Fields
        private static readonly string[] ThemeNames = { "A", "B", "C" };

        // 미로별 포인트 색: 남색 / 보라 / 호박색
        private static readonly Color[] ThemeColors =
        {
            new Color(0.25f, 0.45f, 1.00f),
            new Color(0.75f, 0.30f, 0.95f),
            new Color(1.00f, 0.65f, 0.15f),
        };

        private static Material _bodyMaterial;
        private static Material _shelfMaterial;
        private static Material _floorMaterial;
        private static Material _bridgeMaterial;
        private static Material _bridgeLightMaterial;
        private static Material _startMaterial;
        private static Material _goalMaterial;
        #endregion

        #region Public Methods
        [MenuItem("Project/Map/Build Archive Maze Prefabs")]
        public static void BuildPrefabs()
        {
            EnsureFolder(PREFAB_DIR);
            EnsureFolder(MATERIAL_DIR);
            CreateMaterials();

            GetOrCreatePrefab("MazeFloor", BuildFloor);
            GetOrCreatePrefab("MazeBridgeSegment", BuildBridgeSegment);
            GetOrCreatePrefab("MazeStartMarker", BuildStartMarker);

            for (int i = 0; i < ThemeNames.Length; i++)
            {
                Material accentMaterial = GetOrCreateMaterial($"M_Archive_Accent_{ThemeNames[i]}", ThemeColors[i], ACCENT_EMISSION_INTENSITY, 0.0f, 0.4f);
                string wallName = $"MazeWall_{ThemeNames[i]}";
                string gateName = $"MazeGate_{ThemeNames[i]}";

                GetOrCreatePrefab(wallName, () => BuildWall(wallName, accentMaterial));
                GetOrCreatePrefab(gateName, () => BuildGate(gateName, accentMaterial));
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[MazeArchiveAssetBuilder] 미로 프리팹을 준비했습니다. ({PREFAB_DIR})");
        }
        #endregion

        #region Private Methods - Materials
        private static void CreateMaterials()
        {
            // 넓은 면은 거의 검게 눌러 두고, 빛나는 것은 서랍 표식과 윗단 선뿐이다.
            _bodyMaterial   = GetOrCreateMaterial("M_Archive_Body",   new Color(0.06f, 0.07f, 0.09f), 0.0f, 0.3f, 0.35f);
            _shelfMaterial  = GetOrCreateMaterial("M_Archive_Shelf",  new Color(0.13f, 0.14f, 0.17f), 0.0f, 0.5f, 0.45f);
            _floorMaterial  = GetOrCreateMaterial("M_Archive_Floor",  new Color(0.04f, 0.045f, 0.06f), 0.0f, 0.2f, 0.65f);
            _bridgeMaterial = GetOrCreateMaterial("M_Archive_Bridge", new Color(0.10f, 0.11f, 0.13f), 0.0f, 0.5f, 0.5f);

            _bridgeLightMaterial = GetOrCreateMaterial("M_Archive_BridgeLight", new Color(0.85f, 0.90f, 1.00f), BRIDGE_EMISSION_INTENSITY, 0.0f, 0.4f);
            _startMaterial       = GetOrCreateMaterial("M_Maze_Start", new Color(0.20f, 0.90f, 0.35f), MARKER_EMISSION_INTENSITY, 0.0f, 0.4f);
            _goalMaterial        = GetOrCreateMaterial("M_Maze_Goal",  new Color(0.95f, 0.20f, 0.20f), MARKER_EMISSION_INTENSITY, 0.0f, 0.4f);
        }

        private static Material GetOrCreateMaterial(
            string materialName,
            Color color,
            float emissionIntensity,
            float metallic,
            float smoothness)
        {
            string path = $"{MATERIAL_DIR}/{materialName}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;

            material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Metallic", metallic);
            material.SetFloat("_Smoothness", smoothness);

            if (emissionIntensity > 0.0f)
            {
                material.EnableKeyword("_EMISSION");
                material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
                material.SetColor("_EmissionColor", color * emissionIntensity);
            }

            AssetDatabase.CreateAsset(material, path);
            return material;
        }
        #endregion

        #region Private Methods - Prefabs
        private static GameObject BuildFloor()
        {
            GameObject root = GameObject.CreatePrimitive(PrimitiveType.Cube);
            root.name = "MazeFloor";
            root.GetComponent<MeshRenderer>().sharedMaterial = _floorMaterial;
            return root;
        }

        /// <summary>자료 보관함처럼 보이는 벽. 서랍 층을 나누는 선반 턱, 서랍 표식, 윗단 발광선.</summary>
        private static GameObject BuildWall(string wallName, Material accentMaterial)
        {
            var root = new GameObject(wallName);
            BoxCollider box = root.AddComponent<BoxCollider>();
            box.center = new Vector3(0.0f, WALL_HEIGHT * 0.5f, 0.0f);
            box.size = new Vector3(WALL_LENGTH, WALL_HEIGHT, WALL_THICKNESS);

            AddArchiveBody(root.transform, accentMaterial);

            // 서랍 표식: 벽을 관통해 양쪽 면에서 모두 보인다.
            float labelDepth = WALL_THICKNESS + 0.06f;
            CreatePart("Label_0", root.transform, new Vector3(-1.4f, 2.0f, 0.0f), new Vector3(0.5f, 0.12f, labelDepth), accentMaterial);
            CreatePart("Label_1", root.transform, new Vector3(1.1f, 0.7f, 0.0f),  new Vector3(0.5f, 0.12f, labelDepth), accentMaterial);
            return root;
        }

        /// <summary>출구. 출구 칸 중앙이 원점이고 +Z 쪽 벽 자리에 문이 선다. 바닥의 빨간 판은 테스트용 도착 표시.</summary>
        private static GameObject BuildGate(string gateName, Material accentMaterial)
        {
            var root = new GameObject(gateName);

            // 출구 칸에 들어온 플레이어를 감지하는 영역
            BoxCollider trigger = root.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.center = new Vector3(0.0f, 1.5f, 0.0f);
            trigger.size = new Vector3(3.0f, 3.0f, 3.0f);

            var door = new GameObject("Door");
            door.transform.SetParent(root.transform, false);
            door.transform.localPosition = new Vector3(0.0f, 0.0f, TILE_SIZE * 0.5f);

            BoxCollider doorBox = door.AddComponent<BoxCollider>();
            doorBox.center = new Vector3(0.0f, WALL_HEIGHT * 0.5f, 0.0f);
            doorBox.size = new Vector3(WALL_LENGTH, WALL_HEIGHT, WALL_THICKNESS);

            AddArchiveBody(door.transform, accentMaterial);

            // 잠긴 파일처럼 보이도록 문틀과 잠금 장치를 포인트 색으로 강조한다.
            float frameDepth = WALL_THICKNESS + 0.08f;
            CreatePart("Frame_L", door.transform, new Vector3(-1.6f, WALL_HEIGHT * 0.5f, 0.0f), new Vector3(0.15f, WALL_HEIGHT, frameDepth), accentMaterial);
            CreatePart("Frame_R", door.transform, new Vector3(1.6f, WALL_HEIGHT * 0.5f, 0.0f),  new Vector3(0.15f, WALL_HEIGHT, frameDepth), accentMaterial);
            CreatePart("Lock",    door.transform, new Vector3(0.0f, 1.7f, 0.0f),                new Vector3(0.7f, 0.7f, frameDepth + 0.06f), accentMaterial);

            CreatePart("GoalMarker", root.transform, new Vector3(0.0f, 0.02f, 0.0f), new Vector3(MARKER_SIZE, 0.04f, MARKER_SIZE), _goalMaterial);

            MazeExitGate gate = root.AddComponent<MazeExitGate>();
            var serialized = new SerializedObject(gate);
            serialized.FindProperty("_door").objectReferenceValue = door;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return root;
        }

        /// <summary>다리 한 칸. 낮은 난간과 희미한 흰빛 유도선. 난간이 낮아 뛰어내리면 떨어진다.</summary>
        private static GameObject BuildBridgeSegment()
        {
            var root = new GameObject("MazeBridgeSegment");
            BoxCollider box = root.AddComponent<BoxCollider>();
            box.center = new Vector3(0.0f, -BRIDGE_THICKNESS * 0.5f, 0.0f);
            box.size = new Vector3(BRIDGE_WIDTH, BRIDGE_THICKNESS, TILE_SIZE);

            CreatePart("Deck", root.transform, new Vector3(0.0f, -BRIDGE_THICKNESS * 0.5f, 0.0f), new Vector3(BRIDGE_WIDTH, BRIDGE_THICKNESS, TILE_SIZE), _bridgeMaterial);

            float railX = (BRIDGE_WIDTH * 0.5f) - 0.1f;
            float lightX = railX - 0.2f;
            foreach (float side in new[] { -1.0f, 1.0f })
            {
                string suffix = (side < 0.0f) ? "L" : "R";
                CreatePart($"Rail_{suffix}", root.transform, new Vector3(side * railX, RAIL_HEIGHT, 0.0f),        new Vector3(0.1f, 0.08f, TILE_SIZE), _bridgeMaterial);
                CreatePart($"Post_{suffix}", root.transform, new Vector3(side * railX, RAIL_HEIGHT * 0.5f, 0.0f), new Vector3(0.1f, RAIL_HEIGHT, 0.1f), _bridgeMaterial);
                CreatePart($"Light_{suffix}", root.transform, new Vector3(side * lightX, 0.01f, 0.0f),            new Vector3(0.06f, 0.03f, TILE_SIZE * 0.6f), _bridgeLightMaterial);
            }

            return root;
        }

        private static GameObject BuildStartMarker()
        {
            var root = new GameObject("MazeStartMarker");
            CreatePart("Panel", root.transform, new Vector3(0.0f, 0.02f, 0.0f), new Vector3(MARKER_SIZE, 0.04f, MARKER_SIZE), _startMaterial);
            return root;
        }

        // 벽과 문이 함께 쓰는 보관함 몸통: 어두운 몸통 + 서랍 층을 나누는 선반 턱 2줄 + 윗단 발광선
        private static void AddArchiveBody(Transform parent, Material accentMaterial)
        {
            CreatePart("Body", parent, new Vector3(0.0f, WALL_HEIGHT * 0.5f, 0.0f), new Vector3(WALL_LENGTH, WALL_HEIGHT, WALL_THICKNESS), _bodyMaterial);

            float shelfDepth = WALL_THICKNESS + 0.08f;
            CreatePart("Shelf_0", parent, new Vector3(0.0f, WALL_HEIGHT / 3.0f, 0.0f),          new Vector3(WALL_LENGTH, 0.06f, shelfDepth), _shelfMaterial);
            CreatePart("Shelf_1", parent, new Vector3(0.0f, (WALL_HEIGHT * 2.0f) / 3.0f, 0.0f), new Vector3(WALL_LENGTH, 0.06f, shelfDepth), _shelfMaterial);

            CreatePart("TopLine", parent, new Vector3(0.0f, WALL_HEIGHT, 0.0f), new Vector3(WALL_LENGTH, 0.06f, 0.2f), accentMaterial);
        }
        #endregion

        #region Private Methods - Helpers
        // 이미 있는 프리팹은 다시 만들지 않는다. (직접 다듬은 내용을 덮어쓰지 않기 위함)
        private static void GetOrCreatePrefab(string prefabName, System.Func<GameObject> build)
        {
            string path = $"{PREFAB_DIR}/{prefabName}.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null) return;

            GameObject instance = build();
            PrefabUtility.SaveAsPrefabAsset(instance, path);
            Object.DestroyImmediate(instance);
        }

        // 모양만 담당하는 부품. 충돌은 루트의 BoxCollider 가 맡는다.
        private static void CreatePart(string partName, Transform parent, Vector3 localPosition, Vector3 localScale, Material material)
        {
            GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube);
            part.name = partName;
            Object.DestroyImmediate(part.GetComponent<Collider>());

            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPosition;
            part.transform.localScale = localScale;
            part.GetComponent<MeshRenderer>().sharedMaterial = material;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;

            int separatorIndex = path.LastIndexOf('/');
            string parent = path.Substring(0, separatorIndex);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, path.Substring(separatorIndex + 1));
        }
        #endregion
    }
}
