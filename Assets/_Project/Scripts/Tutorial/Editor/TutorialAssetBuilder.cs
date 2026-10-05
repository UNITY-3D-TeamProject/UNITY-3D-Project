using UnityEditor;
using UnityEngine;
using Map.Platforms;

namespace Tutorial.Editor
{
    /// <summary>
    /// 튜토리얼 씬의 발판 프리팹 3종을 만든다. 이미 있는 프리팹과 머티리얼은 건드리지 않는다.
    /// - Platform_DataPacket : 그라인더를 지난 발판 (정돈된 데이터 패킷). 그라인더의 스포너에 연결한다.
    /// - Platform_DataClump  : 그라인더에 들어가기 전 발판 (앱 색이 섞인 데이터 뭉치)
    /// - Platform_DataJunk   : 떠다니는 데이터 쓰레기 (고정 발판)
    /// 메뉴: Project > Tutorial > Build Platform Prefabs
    /// </summary>
    public static class TutorialAssetBuilder
    {
        #region Constants
        private const string PREFAB_DIR          = "Assets/_Project/Prefabs/Map/Platform";
        private const string MATERIAL_DIR        = "Assets/_Project/Art/Tutorial";
        private const string SHARED_MATERIAL_DIR = "Assets/_Project/Art/Incinerator/Materials";
        private const string GRINDER_PREFAB_PATH = "Assets/_Project/Prefabs/Map/Pipeline/DataGrinder.prefab";

        private const string PACKET_PREFAB_PATH = PREFAB_DIR + "/Platform_DataPacket.prefab";
        private const string CLUMP_PREFAB_PATH  = PREFAB_DIR + "/Platform_DataClump.prefab";
        private const string JUNK_PREFAB_PATH   = PREFAB_DIR + "/Platform_DataJunk.prefab";

        // 모양이 매번 같게 나오도록 고정한 시드
        private const int CLUMP_SEED = 7;
        private const int JUNK_SEED  = 23;

        private const float APP_EMISSION_INTENSITY  = 0.8f;
        private const float JUNK_EMISSION_INTENSITY = 0.6f;
        #endregion

        #region Private Fields
        private static Material _metalMaterial;
        private static Material _cyanMaterial;
        private static Material _orangeMaterial;
        private static Material _junkMaterial;
        private static Material _junkGlowMaterial;
        private static Material[] _appMaterials;
        #endregion

        #region Public Methods
        [MenuItem("Project/Tutorial/Build Platform Prefabs")]
        public static void BuildPlatformPrefabs()
        {
            EnsureFolder(PREFAB_DIR);
            EnsureFolder(MATERIAL_DIR);

            if (!LoadMaterials()) return;

            GameObject packetPrefab = GetOrCreatePrefab(PACKET_PREFAB_PATH, BuildDataPacket);
            GetOrCreatePrefab(CLUMP_PREFAB_PATH, BuildDataClump);
            GetOrCreatePrefab(JUNK_PREFAB_PATH, BuildDataJunk);

            AssignGrinderPlatform(packetPrefab);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[TutorialAssetBuilder] 발판 프리팹 3종을 준비했습니다. ({PREFAB_DIR})");
        }
        #endregion

        #region Private Methods - Materials
        private static bool LoadMaterials()
        {
            _metalMaterial  = LoadSharedMaterial("M_IncineratorMetal");
            _cyanMaterial   = LoadSharedMaterial("M_IncineratorEdgeLight");
            _orangeMaterial = LoadSharedMaterial("M_IncineratorEmberLight");

            if ((_metalMaterial == null) || (_cyanMaterial == null) || (_orangeMaterial == null)) return false;

            // 앱 아이콘과 같은 계열의 색. 분쇄 전 데이터라는 것을 보여 준다.
            _appMaterials = new[]
            {
                GetOrCreateMaterial("M_DataClump_Green",  new Color(0.25f, 0.85f, 0.40f), APP_EMISSION_INTENSITY, 0.2f, 0.5f),
                GetOrCreateMaterial("M_DataClump_Pink",   new Color(1.00f, 0.35f, 0.60f), APP_EMISSION_INTENSITY, 0.2f, 0.5f),
                GetOrCreateMaterial("M_DataClump_Blue",   new Color(0.25f, 0.50f, 1.00f), APP_EMISSION_INTENSITY, 0.2f, 0.5f),
                GetOrCreateMaterial("M_DataClump_Violet", new Color(0.65f, 0.40f, 1.00f), APP_EMISSION_INTENSITY, 0.2f, 0.5f),
            };

            // 버려진 데이터: 광택이 죽은 금속 + 꺼져 가는 시안
            _junkMaterial     = GetOrCreateMaterial("M_DataJunk",     new Color(0.16f, 0.17f, 0.20f), 0.0f, 0.4f, 0.25f);
            _junkGlowMaterial = GetOrCreateMaterial("M_DataJunkGlow", new Color(0.10f, 0.90f, 1.00f), JUNK_EMISSION_INTENSITY, 0.0f, 0.3f);
            return true;
        }

        private static Material LoadSharedMaterial(string materialName)
        {
            string path = $"{SHARED_MATERIAL_DIR}/{materialName}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                Debug.LogError($"[TutorialAssetBuilder] 머티리얼을 찾지 못했습니다: {path}");
            }

            return material;
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
        /// <summary>그라인더를 지난 발판. 어두운 금속판에 시안 테두리, 윗면에 패킷 헤더 무늬.</summary>
        private static GameObject BuildDataPacket()
        {
            var size = new Vector3(2.8f, 0.5f, 2.8f);
            GameObject root = CreateRoot("Platform_DataPacket", size);

            CreatePart("Body", root.transform, Vector3.zero, size, Vector3.zero, _metalMaterial);

            // 윗면 테두리
            float top = (size.y * 0.5f) + 0.01f;
            float edge = (size.x * 0.5f) - 0.06f;
            CreatePart("Edge_Front", root.transform, new Vector3(0.0f, top, edge),  new Vector3(2.8f, 0.04f, 0.08f),  Vector3.zero, _cyanMaterial);
            CreatePart("Edge_Back",  root.transform, new Vector3(0.0f, top, -edge), new Vector3(2.8f, 0.04f, 0.08f),  Vector3.zero, _cyanMaterial);
            CreatePart("Edge_Right", root.transform, new Vector3(edge, top, 0.0f),  new Vector3(0.08f, 0.04f, 2.64f), Vector3.zero, _cyanMaterial);
            CreatePart("Edge_Left",  root.transform, new Vector3(-edge, top, 0.0f), new Vector3(0.08f, 0.04f, 2.64f), Vector3.zero, _cyanMaterial);

            // 패킷 헤더: 긴 줄 하나 + 비트 3개 (마지막 비트만 주황)
            CreatePart("Header", root.transform, new Vector3(-0.35f, top, -0.75f), new Vector3(1.3f, 0.04f, 0.12f), Vector3.zero, _cyanMaterial);
            CreatePart("Bit_0", root.transform, new Vector3(-0.6f, top, 0.6f), new Vector3(0.24f, 0.04f, 0.24f), Vector3.zero, _cyanMaterial);
            CreatePart("Bit_1", root.transform, new Vector3(0.0f, top, 0.6f),  new Vector3(0.24f, 0.04f, 0.24f), Vector3.zero, _cyanMaterial);
            CreatePart("Bit_2", root.transform, new Vector3(0.6f, top, 0.6f),  new Vector3(0.24f, 0.04f, 0.24f), Vector3.zero, _orangeMaterial);

            AddRideable(root);
            root.AddComponent<PlatformMover>();
            return root;
        }

        /// <summary>그라인더에 들어가기 전 발판. 평평한 윗판 아래로 앱 색 덩어리가 뭉쳐 있다.</summary>
        private static GameObject BuildDataClump()
        {
            var size = new Vector3(3.0f, 0.4f, 3.0f);
            GameObject root = CreateRoot("Platform_DataClump", size);
            var random = new System.Random(CLUMP_SEED);

            CreatePart("Top", root.transform, Vector3.zero, size, Vector3.zero, _metalMaterial);

            // 윗면에 박힌 앱 색 타일 4장
            float top = (size.y * 0.5f) + 0.01f;
            for (int i = 0; i < _appMaterials.Length; i++)
            {
                float x = ((i % 2) == 0) ? -0.65f : 0.65f;
                float z = (i < 2) ? -0.65f : 0.65f;
                var tileEuler = new Vector3(0.0f, Range(random, -12.0f, 12.0f), 0.0f);
                CreatePart($"Tile_{i}", root.transform, new Vector3(x, top, z), new Vector3(0.9f, 0.04f, 0.9f), tileEuler, _appMaterials[i]);
            }

            // 아래로 뭉친 덩어리: 앱 색과 금속을 섞는다
            const int CHUNK_COUNT = 16;
            for (int i = 0; i < CHUNK_COUNT; i++)
            {
                var position = new Vector3(Range(random, -1.2f, 1.2f), Range(random, -1.1f, -0.3f), Range(random, -1.2f, 1.2f));
                var scale = new Vector3(Range(random, 0.5f, 1.1f), Range(random, 0.5f, 1.1f), Range(random, 0.5f, 1.1f));
                var euler = new Vector3(Range(random, 0.0f, 360.0f), Range(random, 0.0f, 360.0f), Range(random, 0.0f, 360.0f));

                // 3개 중 1개는 금속
                bool isMetal = (i % 3) == 2;
                Material material = isMetal ? _metalMaterial : _appMaterials[i % _appMaterials.Length];
                CreatePart($"Chunk_{i}", root.transform, position, scale, euler, material);
            }

            AddRideable(root);
            return root;
        }

        /// <summary>떠다니는 데이터 쓰레기. 깨진 판 조각과 부스러기, 꺼져 가는 불빛. 움직이지 않는다.</summary>
        private static GameObject BuildDataJunk()
        {
            GameObject root = CreateRoot("Platform_DataJunk", new Vector3(2.6f, 0.3f, 2.6f));
            var random = new System.Random(JUNK_SEED);

            // 세 조각으로 깨진 판. 윗면 높이는 거의 같게 둔다.
            CreatePart("Slab_A", root.transform, new Vector3(-0.55f, 0.0f, 0.0f),    new Vector3(1.5f, 0.3f, 2.6f),  Vector3.zero, _junkMaterial);
            CreatePart("Slab_B", root.transform, new Vector3(0.78f, 0.0f, -0.6f),    new Vector3(1.0f, 0.3f, 1.4f),  new Vector3(0.0f, 4.0f, 0.0f), _junkMaterial);
            CreatePart("Slab_C", root.transform, new Vector3(0.82f, -0.03f, 0.78f),  new Vector3(0.9f, 0.26f, 1.0f), new Vector3(3.0f, -9.0f, 2.0f), _junkMaterial);

            // 끊어진 회로선
            CreatePart("Trace_0", root.transform, new Vector3(-0.8f, 0.16f, -0.4f), new Vector3(0.06f, 0.03f, 1.2f), Vector3.zero, _junkGlowMaterial);
            CreatePart("Trace_1", root.transform, new Vector3(-0.3f, 0.16f, 0.7f),  new Vector3(0.7f, 0.03f, 0.06f), Vector3.zero, _junkGlowMaterial);
            CreatePart("Ember",   root.transform, new Vector3(0.75f, 0.16f, -0.7f), new Vector3(0.16f, 0.03f, 0.16f), Vector3.zero, _orangeMaterial);

            // 아래에 매달린 부스러기
            const int DEBRIS_COUNT = 7;
            for (int i = 0; i < DEBRIS_COUNT; i++)
            {
                var position = new Vector3(Range(random, -1.2f, 1.2f), Range(random, -0.9f, -0.25f), Range(random, -1.2f, 1.2f));
                float debrisSize = Range(random, 0.2f, 0.55f);
                var euler = new Vector3(Range(random, 0.0f, 360.0f), Range(random, 0.0f, 360.0f), Range(random, 0.0f, 360.0f));
                CreatePart($"Debris_{i}", root.transform, position, Vector3.one * debrisSize, euler, _junkMaterial);
            }

            return root;
        }

        // 그라인더 출구의 스포너가 내보내는 발판을 데이터 패킷으로 바꾼다.
        private static void AssignGrinderPlatform(GameObject packetPrefab)
        {
            GameObject contents = PrefabUtility.LoadPrefabContents(GRINDER_PREFAB_PATH);
            PlatformSpawner spawner = contents.GetComponentInChildren<PlatformSpawner>(true);

            if (spawner == null)
            {
                Debug.LogError($"[TutorialAssetBuilder] {GRINDER_PREFAB_PATH} 에서 PlatformSpawner를 찾지 못했습니다.");
            }
            else
            {
                var serialized = new SerializedObject(spawner);
                serialized.FindProperty("_platformPrefab").objectReferenceValue = packetPrefab.GetComponent<PlatformMover>();
                serialized.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(contents, GRINDER_PREFAB_PATH);
            }

            PrefabUtility.UnloadPrefabContents(contents);
        }
        #endregion

        #region Private Methods - Helpers
        // 이미 있는 프리팹은 다시 만들지 않는다. (직접 다듬은 내용을 덮어쓰지 않기 위함)
        private static GameObject GetOrCreatePrefab(string path, System.Func<GameObject> build)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null) return prefab;

            GameObject instance = build();
            prefab = PrefabUtility.SaveAsPrefabAsset(instance, path);
            Object.DestroyImmediate(instance);
            return prefab;
        }

        // 루트는 스케일 1로 두고 밟는 면만 콜라이더로 만든다. 모양은 자식 오브젝트가 담당한다.
        private static GameObject CreateRoot(string rootName, Vector3 colliderSize)
        {
            var root = new GameObject(rootName);
            root.AddComponent<BoxCollider>().size = colliderSize;
            return root;
        }

        private static void CreatePart(
            string partName,
            Transform parent,
            Vector3 localPosition,
            Vector3 localScale,
            Vector3 localEuler,
            Material material)
        {
            GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube);
            part.name = partName;
            Object.DestroyImmediate(part.GetComponent<Collider>());

            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPosition;
            part.transform.localRotation = Quaternion.Euler(localEuler);
            part.transform.localScale = localScale;
            part.GetComponent<MeshRenderer>().sharedMaterial = material;
        }

        private static void AddRideable(GameObject root)
        {
            // TransformMotor 와 Rigidbody 는 RequireComponent 로 함께 붙는다
            root.AddComponent<RideablePlatform>();
            Rigidbody body = root.GetComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
        }

        private static float Range(System.Random random, float min, float max)
        {
            return min + ((float)random.NextDouble() * (max - min));
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
