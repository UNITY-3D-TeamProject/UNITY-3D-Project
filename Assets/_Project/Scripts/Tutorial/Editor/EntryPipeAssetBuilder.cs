using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Tutorial.Editor
{
    /// <summary>
    /// 플레이어가 걸어 들어갈 수 있는 T자형 파이프 프리팹(DataEntryPipe)을 만든다.
    /// DataSourcePipe 와 같은 규약: 원점 = 입구 중앙, 입구는 +Z 를 보고 몸통은 -Z 로 뻗는다.
    /// 안으로 들어가면(-Z) 갈림길이 나오고, 들어가는 방향 기준 오른쪽(-X)이 긴 길, 왼쪽(+X)이 짧은 길이다.
    /// 이미 있는 프리팹과 메시는 건드리지 않는다. 치수를 바꾸려면 프리팹과 메시를 지우고 다시 만든다.
    /// 메뉴: Project > Tutorial > Build Entry Pipe
    /// </summary>
    public static class EntryPipeAssetBuilder
    {
        #region Constants
        private const string PREFAB_PATH         = "Assets/_Project/Prefabs/Map/Pipeline/DataEntryPipe.prefab";
        private const string MESH_DIR            = "Assets/_Project/Art/Tutorial/Meshes";
        private const string SHARED_MATERIAL_DIR = "Assets/_Project/Art/Incinerator/Materials";
        private const string SHARED_MESH_DIR     = "Assets/_Project/Art/Incinerator/Meshes";

        // DataSourcePipe 와 같은 입구 반지름
        private const float INNER_RADIUS = 4.2f;
        private const float OUTER_RADIUS = 5.2f;

        // 입구에서 갈림길 중심까지 / 왼쪽 길 / 오른쪽 길의 길이
        private const float ENTRY_LENGTH = 20.0f;
        private const float LEFT_LENGTH  = 14.0f;
        private const float RIGHT_LENGTH = 90.0f;

        // 바닥(평평한 발판) 윗면 높이. 파이프 중심 기준.
        private const float FLOOR_TOP_Y     = -2.6f;
        private const float FLOOR_THICKNESS = 0.3f;

        private const int   CIRCLE_SEGMENTS    = 64;
        private const float RING_SPACING       = 4.0f;    // 관 길이 방향 분할 간격
        private const float OVERLAY_OFFSET     = 0.02f;   // 회로 오버레이를 표면에서 띄우는 거리
        private const float LIGHT_BAND_WIDTH   = 0.2f;
        private const float LIGHT_BAND_SPACING = 10.0f;
        private const float COLLAR_SPACING     = 12.0f;
        private const float EDGE_LIGHT_INSET   = 0.15f;
        #endregion

        #region Private Fields
        private static Material _metalMaterial;
        private static Material _cyanMaterial;
        private static Material _circuitMaterial;
        #endregion

        #region Public Methods
        [MenuItem("Project/Tutorial/Build Entry Pipe")]
        public static void BuildEntryPipe()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH) != null)
            {
                Debug.Log($"[EntryPipeAssetBuilder] 이미 프리팹이 있어 다시 만들지 않았습니다. ({PREFAB_PATH})");
                return;
            }

            _metalMaterial   = LoadShared<Material>($"{SHARED_MATERIAL_DIR}/M_IncineratorMetal.mat");
            _cyanMaterial    = LoadShared<Material>($"{SHARED_MATERIAL_DIR}/M_IncineratorEdgeLight.mat");
            _circuitMaterial = LoadShared<Material>($"{SHARED_MATERIAL_DIR}/M_CircuitGlowPipe.mat");
            if ((_metalMaterial == null) || (_cyanMaterial == null) || (_circuitMaterial == null)) return;

            EnsureFolder(MESH_DIR);

            GameObject root = BuildPipe();
            PrefabUtility.SaveAsPrefabAsset(root, PREFAB_PATH);
            Object.DestroyImmediate(root);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[EntryPipeAssetBuilder] T자형 파이프 프리팹을 만들었습니다. ({PREFAB_PATH})");
        }
        #endregion

        #region Private Methods - Prefab
        private static GameObject BuildPipe()
        {
            var root = new GameObject("DataEntryPipe");
            Transform body = CreateGroup("Body", root.transform);

            // 벽: 안쪽 면 + 바깥 면 + 양 끝 마개. 같은 메시를 충돌에도 쓴다.
            Mesh shellMesh = GetOrCreateMesh("EntryPipe_Shell", CreateShellMesh);
            GameObject shell = CreateMeshObject("Shell", body, shellMesh, _metalMaterial);
            shell.AddComponent<MeshCollider>().sharedMesh = shellMesh;

            Mesh overlayMesh = GetOrCreateMesh("EntryPipe_CircuitOverlay", CreateOverlayMesh);
            CreateMeshObject("CircuitOverlay", body, overlayMesh, _circuitMaterial);

            // 입구 발광 링은 DataSourcePipe 의 것을 그대로 쓴다.
            Mesh mouthRingMesh = LoadShared<Mesh>($"{SHARED_MESH_DIR}/SourcePipe_MouthRing.asset");
            if (mouthRingMesh != null)
            {
                CreateMeshObject("MouthRing", body, mouthRingMesh, _cyanMaterial);
            }

            BuildFloor(root.transform);
            BuildLightBands(root.transform);
            BuildCollars(root.transform);
            return root;
        }

        // 평평한 발판: 진입로 + 갈림길(좌우로 이어지는 길). 가장자리에 시안 유도선.
        private static void BuildFloor(Transform parent)
        {
            Transform group = CreateGroup("Floor", parent);

            float halfWidth = Mathf.Sqrt((INNER_RADIUS * INNER_RADIUS) - (FLOOR_TOP_Y * FLOOR_TOP_Y)) - 0.02f;
            float centerY = FLOOR_TOP_Y - (FLOOR_THICKNESS * 0.5f);
            float stripY = FLOOR_TOP_Y + 0.01f;
            float stripOffset = halfWidth - EDGE_LIGHT_INSET;

            // 진입로는 갈림길 바닥이 시작되는 곳까지만 깐다. (겹치면 윗면이 깜빡인다)
            float entryFloorLength = ENTRY_LENGTH - halfWidth;
            float entryCenterZ = -entryFloorLength * 0.5f;
            CreateBox("Floor_Entry", group, new Vector3(0.0f, centerY, entryCenterZ), new Vector3(halfWidth * 2.0f, FLOOR_THICKNESS, entryFloorLength), _metalMaterial, true);
            CreateBox("EdgeLight_Entry_L", group, new Vector3(stripOffset, stripY, entryCenterZ),  new Vector3(0.08f, 0.04f, entryFloorLength), _cyanMaterial, false);
            CreateBox("EdgeLight_Entry_R", group, new Vector3(-stripOffset, stripY, entryCenterZ), new Vector3(0.08f, 0.04f, entryFloorLength), _cyanMaterial, false);

            float crossLength = LEFT_LENGTH + RIGHT_LENGTH;
            float crossCenterX = (LEFT_LENGTH - RIGHT_LENGTH) * 0.5f;
            CreateBox("Floor_Cross", group, new Vector3(crossCenterX, centerY, -ENTRY_LENGTH), new Vector3(crossLength, FLOOR_THICKNESS, halfWidth * 2.0f), _metalMaterial, true);

            // 안쪽 벽 쪽 유도선은 끝에서 끝까지, 입구 쪽 유도선은 진입로가 만나는 구간을 비운다.
            CreateBox("EdgeLight_Cross_Far", group, new Vector3(crossCenterX, stripY, -ENTRY_LENGTH - stripOffset), new Vector3(crossLength, 0.04f, 0.08f), _cyanMaterial, false);

            float leftStripLength = LEFT_LENGTH - halfWidth;
            float rightStripLength = RIGHT_LENGTH - halfWidth;
            float nearZ = -ENTRY_LENGTH + stripOffset;
            CreateBox("EdgeLight_Cross_NearLeft",  group, new Vector3(halfWidth + (leftStripLength * 0.5f), stripY, nearZ),   new Vector3(leftStripLength, 0.04f, 0.08f),  _cyanMaterial, false);
            CreateBox("EdgeLight_Cross_NearRight", group, new Vector3(-halfWidth - (rightStripLength * 0.5f), stripY, nearZ), new Vector3(rightStripLength, 0.04f, 0.08f), _cyanMaterial, false);
        }

        // 안쪽 벽을 따라 일정 간격으로 도는 시안 발광 띠. 긴 길의 거리감을 준다.
        private static void BuildLightBands(Transform parent)
        {
            Transform group = CreateGroup("LightBands", parent);
            Mesh bandMesh = GetOrCreateMesh("EntryPipe_LightBand", CreateLightBandMesh);
            Quaternion crossRotation = Quaternion.Euler(0.0f, 90.0f, 0.0f);

            // 갈림길에서 관이 서로 뚫린 구간에는 놓지 않는다.
            float junctionMargin = INNER_RADIUS + 2.0f;
            int index = 0;

            for (float z = 3.0f; z < ENTRY_LENGTH - junctionMargin; z += LIGHT_BAND_SPACING * 0.5f)
            {
                CreateMeshObject($"Band_{index++}", group, bandMesh, _cyanMaterial).transform.localPosition = new Vector3(0.0f, 0.0f, -z);
            }

            for (float x = junctionMargin; x < LEFT_LENGTH; x += LIGHT_BAND_SPACING)
            {
                Transform band = CreateMeshObject($"Band_{index++}", group, bandMesh, _cyanMaterial).transform;
                band.localPosition = new Vector3(x, 0.0f, -ENTRY_LENGTH);
                band.localRotation = crossRotation;
            }

            for (float x = junctionMargin; x < RIGHT_LENGTH; x += LIGHT_BAND_SPACING)
            {
                Transform band = CreateMeshObject($"Band_{index++}", group, bandMesh, _cyanMaterial).transform;
                band.localPosition = new Vector3(-x, 0.0f, -ENTRY_LENGTH);
                band.localRotation = crossRotation;
            }
        }

        // 바깥 보강 링은 DataSourcePipe 의 것을 그대로 쓴다.
        private static void BuildCollars(Transform parent)
        {
            Mesh collarMesh = LoadShared<Mesh>($"{SHARED_MESH_DIR}/SourcePipe_Collar.asset");
            Mesh collarLightMesh = LoadShared<Mesh>($"{SHARED_MESH_DIR}/SourcePipe_CollarLight.asset");
            if ((collarMesh == null) || (collarLightMesh == null)) return;

            Transform group = CreateGroup("Collars", parent);
            Quaternion crossRotation = Quaternion.Euler(0.0f, 90.0f, 0.0f);
            float junctionMargin = OUTER_RADIUS + 3.0f;
            int index = 0;

            for (float z = 4.0f; z < ENTRY_LENGTH - junctionMargin; z += 4.0f)
            {
                CreateCollar(index++, group, collarMesh, collarLightMesh, new Vector3(0.0f, 0.0f, -z), Quaternion.identity);
            }

            for (float x = junctionMargin; x < LEFT_LENGTH; x += COLLAR_SPACING)
            {
                CreateCollar(index++, group, collarMesh, collarLightMesh, new Vector3(x, 0.0f, -ENTRY_LENGTH), crossRotation);
            }

            for (float x = junctionMargin; x < RIGHT_LENGTH; x += COLLAR_SPACING)
            {
                CreateCollar(index++, group, collarMesh, collarLightMesh, new Vector3(-x, 0.0f, -ENTRY_LENGTH), crossRotation);
            }
        }

        private static void CreateCollar(
            int index,
            Transform parent,
            Mesh collarMesh,
            Mesh collarLightMesh,
            Vector3 localPosition,
            Quaternion localRotation)
        {
            Transform collar = CreateMeshObject($"Collar_{index}", parent, collarMesh, _metalMaterial).transform;
            collar.localPosition = localPosition;
            collar.localRotation = localRotation;

            CreateMeshObject("CollarLight", collar, collarLightMesh, _cyanMaterial);
        }
        #endregion

        #region Private Methods - Meshes
        private static Mesh CreateShellMesh()
        {
            var data = new MeshData();
            AddTee(data, INNER_RADIUS, true);
            AddTee(data, OUTER_RADIUS, false);

            // 입구 단면 (안쪽 반지름 ~ 바깥 반지름)
            AddDisc(data, Vector3.zero, Vector3.forward, Vector3.right, INNER_RADIUS, OUTER_RADIUS);

            // 좌우 길의 막힌 끝. 안에서도 밖에서도 보이도록 양면으로 만든다.
            var crossCenter = new Vector3(0.0f, 0.0f, -ENTRY_LENGTH);
            Vector3 leftEnd = crossCenter + (Vector3.right * LEFT_LENGTH);
            Vector3 rightEnd = crossCenter + (Vector3.left * RIGHT_LENGTH);
            AddDisc(data, leftEnd, Vector3.right, Vector3.forward, 0.0f, OUTER_RADIUS);
            AddDisc(data, leftEnd, Vector3.left, Vector3.forward, 0.0f, OUTER_RADIUS);
            AddDisc(data, rightEnd, Vector3.left, Vector3.forward, 0.0f, OUTER_RADIUS);
            AddDisc(data, rightEnd, Vector3.right, Vector3.forward, 0.0f, OUTER_RADIUS);

            return data.ToMesh();
        }

        private static Mesh CreateOverlayMesh()
        {
            var data = new MeshData();
            AddTee(data, INNER_RADIUS - OVERLAY_OFFSET, true);
            AddTee(data, OUTER_RADIUS + OVERLAY_OFFSET, false);
            return data.ToMesh();
        }

        // Z축을 감싸는 짧은 띠. 안쪽을 본다.
        private static Mesh CreateLightBandMesh()
        {
            var data = new MeshData();
            float radius = INNER_RADIUS - (OVERLAY_OFFSET * 2.0f);
            float halfWidth = LIGHT_BAND_WIDTH * 0.5f;
            var indices = new int[2, CIRCLE_SEGMENTS + 1];

            for (int i = 0; i <= CIRCLE_SEGMENTS; i++)
            {
                float angle = (i / (float)CIRCLE_SEGMENTS) * Mathf.PI * 2.0f;
                var radial = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0.0f);
                float u = i / (float)CIRCLE_SEGMENTS;

                indices[0, i] = data.AddVertex((radial * radius) + (Vector3.back * halfWidth), -radial, new Vector2(u, 0.0f));
                indices[1, i] = data.AddVertex((radial * radius) + (Vector3.forward * halfWidth), -radial, new Vector2(u, 1.0f));
            }

            AddGrid(data, indices);
            return data.ToMesh();
        }

        /// <summary>
        /// 반지름이 같은 두 관이 T자로 만나는 표면을 추가한다.
        /// 진입 관(축 Z)은 교차 관 표면에 맞춰 끝이 말안장 모양으로 잘리고,
        /// 교차 관(축 X)은 진입 관이 뚫고 들어오는 부분만큼 벽이 비어 있다. 두 경계는 같은 곡선이라 틈이 없다.
        /// </summary>
        /// <param name="data">메시를 쌓을 대상</param>
        /// <param name="radius">관 반지름</param>
        /// <param name="isInward">true 면 면이 관 안쪽을 본다</param>
        private static void AddTee(MeshData data, float radius, bool isInward)
        {
            float normalSign = isInward ? -1.0f : 1.0f;

            // 진입 관: 입구(z = 0)에서 갈림길까지
            int entryRings = Mathf.CeilToInt(ENTRY_LENGTH / RING_SPACING);
            var entryIndices = new int[entryRings + 1, CIRCLE_SEGMENTS + 1];

            for (int ring = 0; ring <= entryRings; ring++)
            {
                for (int i = 0; i <= CIRCLE_SEGMENTS; i++)
                {
                    float angle = (i / (float)CIRCLE_SEGMENTS) * Mathf.PI * 2.0f;
                    float cos = Mathf.Cos(angle);
                    float sin = Mathf.Sin(angle);

                    // 이 각도에서 진입 관이 교차 관 표면에 닿는 깊이
                    float endDepth = ENTRY_LENGTH - (radius * Mathf.Abs(cos));
                    float depth = Mathf.Lerp(0.0f, endDepth, ring / (float)entryRings);

                    var position = new Vector3(radius * cos, radius * sin, -depth);
                    var normal = new Vector3(cos, sin, 0.0f) * normalSign;
                    entryIndices[ring, i] = data.AddVertex(position, normal, new Vector2(i / (float)CIRCLE_SEGMENTS, depth));
                }
            }

            AddGrid(data, entryIndices);

            // 교차 관: 왼쪽(+X)과 오른쪽(-X) 두 조각
            AddCrossBranch(data, radius, normalSign, 1.0f, LEFT_LENGTH);
            AddCrossBranch(data, radius, normalSign, -1.0f, RIGHT_LENGTH);
        }

        private static void AddCrossBranch(MeshData data, float radius, float normalSign, float direction, float length)
        {
            int rings = Mathf.CeilToInt(length / RING_SPACING);
            var indices = new int[rings + 1, CIRCLE_SEGMENTS + 1];

            for (int ring = 0; ring <= rings; ring++)
            {
                for (int i = 0; i <= CIRCLE_SEGMENTS; i++)
                {
                    // 각도 0 이 입구 쪽(+Z)이다.
                    float angle = (i / (float)CIRCLE_SEGMENTS) * Mathf.PI * 2.0f;
                    float cos = Mathf.Cos(angle);
                    float sin = Mathf.Sin(angle);

                    // 입구 쪽 반원은 진입 관이 뚫고 들어온 자리부터 시작하고, 반대쪽 반원은 중심(x = 0)에서 시작한다.
                    float startDistance = radius * Mathf.Max(0.0f, cos);
                    float distance = Mathf.Lerp(startDistance, length, ring / (float)rings);

                    var position = new Vector3(direction * distance, radius * sin, -ENTRY_LENGTH + (radius * cos));
                    var normal = new Vector3(0.0f, sin, cos) * normalSign;
                    indices[ring, i] = data.AddVertex(position, normal, new Vector2(i / (float)CIRCLE_SEGMENTS, distance));
                }
            }

            AddGrid(data, indices);
        }

        // normal 방향을 보는 원판(innerRadius 가 0) 또는 고리.
        private static void AddDisc(MeshData data, Vector3 center, Vector3 normal, Vector3 tangent, float innerRadius, float outerRadius)
        {
            Vector3 bitangent = Vector3.Cross(normal, tangent);
            var indices = new int[2, CIRCLE_SEGMENTS + 1];

            for (int i = 0; i <= CIRCLE_SEGMENTS; i++)
            {
                float angle = (i / (float)CIRCLE_SEGMENTS) * Mathf.PI * 2.0f;
                Vector3 radial = (tangent * Mathf.Cos(angle)) + (bitangent * Mathf.Sin(angle));
                float u = i / (float)CIRCLE_SEGMENTS;

                indices[0, i] = data.AddVertex(center + (radial * innerRadius), normal, new Vector2(u, innerRadius));
                indices[1, i] = data.AddVertex(center + (radial * outerRadius), normal, new Vector2(u, outerRadius));
            }

            AddGrid(data, indices);
        }

        private static void AddGrid(MeshData data, int[,] indices)
        {
            int rowCount = indices.GetLength(0);
            int columnCount = indices.GetLength(1);

            for (int row = 0; row < rowCount - 1; row++)
            {
                for (int column = 0; column < columnCount - 1; column++)
                {
                    data.AddQuad(
                        indices[row, column],
                        indices[row + 1, column],
                        indices[row + 1, column + 1],
                        indices[row, column + 1]);
                }
            }
        }
        #endregion

        #region Private Methods - Helpers
        private static Mesh GetOrCreateMesh(string meshName, System.Func<Mesh> create)
        {
            string path = $"{MESH_DIR}/{meshName}.asset";
            Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (mesh != null) return mesh;

            mesh = create();
            mesh.name = meshName;
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }

        private static T LoadShared<T>(string path) where T : Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                Debug.LogError($"[EntryPipeAssetBuilder] 에셋을 찾지 못했습니다: {path}");
            }

            return asset;
        }

        private static Transform CreateGroup(string groupName, Transform parent)
        {
            Transform group = new GameObject(groupName).transform;
            group.SetParent(parent, false);
            return group;
        }

        private static GameObject CreateMeshObject(string objectName, Transform parent, Mesh mesh, Material material)
        {
            var meshObject = new GameObject(objectName);
            meshObject.transform.SetParent(parent, false);
            meshObject.AddComponent<MeshFilter>().sharedMesh = mesh;

            MeshRenderer renderer = meshObject.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;

            // 발광·오버레이 재질은 그림자를 만들지 않는다.
            if (material != _metalMaterial)
            {
                renderer.shadowCastingMode = ShadowCastingMode.Off;
            }

            return meshObject;
        }

        private static void CreateBox(
            string boxName,
            Transform parent,
            Vector3 localPosition,
            Vector3 localScale,
            Material material,
            bool hasCollider)
        {
            GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = boxName;
            if (!hasCollider)
            {
                Object.DestroyImmediate(box.GetComponent<Collider>());
            }

            box.transform.SetParent(parent, false);
            box.transform.localPosition = localPosition;
            box.transform.localScale = localScale;
            box.GetComponent<MeshRenderer>().sharedMaterial = material;
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

        #region Nested Types
        private sealed class MeshData
        {
            private readonly List<Vector3> _vertices  = new List<Vector3>();
            private readonly List<Vector3> _normals   = new List<Vector3>();
            private readonly List<Vector2> _uvs       = new List<Vector2>();
            private readonly List<int>     _triangles = new List<int>();

            public int AddVertex(Vector3 position, Vector3 normal, Vector2 uv)
            {
                _vertices.Add(position);
                _normals.Add(normal);
                _uvs.Add(uv);
                return _vertices.Count - 1;
            }

            public void AddQuad(int a, int b, int c, int d)
            {
                AddTriangle(a, b, c);
                AddTriangle(a, c, d);
            }

            public Mesh ToMesh()
            {
                var mesh = new Mesh();
                mesh.SetVertices(_vertices);
                mesh.SetNormals(_normals);
                mesh.SetUVs(0, _uvs);
                mesh.SetTriangles(_triangles, 0);
                mesh.RecalculateBounds();
                return mesh;
            }

            // 면이 정점 법선과 같은 쪽을 보도록 감는 순서를 맞춘다.
            private void AddTriangle(int a, int b, int c)
            {
                Vector3 faceNormal = Vector3.Cross(_vertices[b] - _vertices[a], _vertices[c] - _vertices[a]);
                Vector3 vertexNormal = _normals[a] + _normals[b] + _normals[c];
                bool isFlipped = Vector3.Dot(faceNormal, vertexNormal) < 0.0f;

                _triangles.Add(a);
                _triangles.Add(isFlipped ? c : b);
                _triangles.Add(isFlipped ? b : c);
            }
        }
        #endregion
    }
}
