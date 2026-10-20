using UnityEngine;
using UnityEngine.Rendering;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Map.Tutorial
{
    /// <summary>
    /// 시스템 심연 3D 하늘: 거대한 고리 모듈 + 빛 기둥을 카메라를 따라 움직이고 고리마다 천천히 돌린다.
    /// 카메라 이동의 일부만 따라가서(_followRatio) 약한 시차가 생긴다. 판정 없음.
    /// 메시는 컴포넌트 메뉴 "Build Abyss Sky" 로 굽는다(자식 Ring_i 를 지우고 다시 만들고, 메시는 Art/Skybox/AbyssSky/Meshes 에 저장).
    /// 바탕 하늘은 Project/AbyssAtmosphereSkybox, 어둠·경보는 전역 _HoloDarkness·_HoloAlarm 을 셰이더가 직접 읽는다.
    /// </summary>
    [DefaultExecutionOrder(100)]
    public class AbyssSkyRig : MonoBehaviour
    {
        #region Constants
        private const string RING_PREFIX = "Ring_";
        private const string MESH_PARENT_FOLDER = "Assets/_Project/Art/Skybox/AbyssSky";
        private const string MESH_FOLDER = MESH_PARENT_FOLDER + "/Meshes";
        #endregion

        #region Serialized Fields
        [Header("Follow")]
        [Tooltip("카메라 이동을 따라가는 비율. 1 = 완전히 붙음(시차 없음), 낮을수록 고리가 더 움직여 보인다.")]
        [SerializeField, Range(0.0f, 1.0f)] private float _followRatio = 0.95f;

        [Header("Rings")]
        [SerializeField] private SAbyssRing[] _rings;

        [Tooltip("모양 난수 시드. 바꾸고 Build Abyss Sky 를 다시 누르면 다른 배치가 나온다.")]
        [SerializeField] private int _seed = 20261020;

        [Header("Materials")]
        [SerializeField] private Material _moduleMaterial;
        [SerializeField] private Material _edgeMaterial;
        [SerializeField] private Material _pillarMaterial;
        #endregion

        #region Private Fields
        private Transform _cameraTransform;
        private Vector3 _anchor;
        private Transform[] _ringTransforms;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            _anchor = transform.position;
            _ringTransforms = new Transform[_rings.Length];
            for (int i = 0; i < _rings.Length; i++)
            {
                _ringTransforms[i] = transform.Find(RING_PREFIX + i);
            }
        }

        private void LateUpdate()
        {
            if (_cameraTransform == null)
            {
                Camera mainCamera = Camera.main;
                if (mainCamera == null)
                {
                    return;
                }

                _cameraTransform = mainCamera.transform;
            }

            transform.position = _anchor + (_cameraTransform.position * _followRatio);

            for (int i = 0; i < _ringTransforms.Length; i++)
            {
                if (_ringTransforms[i] != null)
                {
                    _ringTransforms[i].Rotate(0.0f, _rings[i].DegreesPerSecond * Time.deltaTime, 0.0f, Space.Self);
                }
            }
        }
        #endregion

        #region Editor
#if UNITY_EDITOR
        [ContextMenu("Build Abyss Sky")]
        private void BuildSky()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Undo.DestroyObjectImmediate(transform.GetChild(i).gameObject);
            }

            if (!AssetDatabase.IsValidFolder(MESH_PARENT_FOLDER))
            {
                AssetDatabase.CreateFolder("Assets/_Project/Art/Skybox", "AbyssSky");
            }

            if (!AssetDatabase.IsValidFolder(MESH_FOLDER))
            {
                AssetDatabase.CreateFolder(MESH_PARENT_FOLDER, "Meshes");
            }

            var random = new System.Random(_seed);
            for (int i = 0; i < _rings.Length; i++)
            {
                SAbyssRing ring = _rings[i];
                var ringObject = new GameObject(RING_PREFIX + i);
                Undo.RegisterCreatedObjectUndo(ringObject, "Build Abyss Sky");
                ringObject.transform.SetParent(transform, false);
                ringObject.transform.localPosition = Vector3.up * ring.Height;
                ringObject.transform.localRotation = Quaternion.Euler(ring.Tilt.x, 0.0f, ring.Tilt.y);

                CreatePart(ringObject.transform, i, "Modules", AbyssSkyMeshBuilder.BuildModules(ring, random), _moduleMaterial);
                CreatePart(ringObject.transform, i, "Edges", AbyssSkyMeshBuilder.BuildEdges(ring, random), _edgeMaterial);
                if (ring.PillarCount > 0)
                {
                    CreatePart(ringObject.transform, i, "Pillars", AbyssSkyMeshBuilder.BuildPillars(ring, random), _pillarMaterial);
                }
            }

            AssetDatabase.SaveAssets();
        }

        private void CreatePart(Transform parent, int ringIndex, string partName, Mesh mesh, Material material)
        {
            // 덮어쓰기(CopySerialized)는 렌더러에 반영되지 않아 지우고 새로 만든다
            string path = $"{MESH_FOLDER}/{gameObject.name}_{RING_PREFIX}{ringIndex}_{partName}.asset";
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(mesh, path);

            var part = new GameObject(partName);
            part.transform.SetParent(parent, false);
            part.AddComponent<MeshFilter>().sharedMesh = mesh;

            var meshRenderer = part.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = material;
            meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;
            meshRenderer.lightProbeUsage = LightProbeUsage.Off;
            meshRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        }
#endif
        #endregion
    }
}
