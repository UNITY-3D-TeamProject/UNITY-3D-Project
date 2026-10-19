using System.Collections;
using System.Collections.Generic;
using INab.Common;
using INab.CommonVFX;
using UnityEngine;

namespace Visual
{
    /// <summary>
    /// Player 가 아래에서 위로 생성되는 연출을 한 번 재생한다.
    /// 생성 중에만 원본 재질의 plane mask 사본(원본 외형 유지)을 쓰고, 끝나면 원본 재질·원본 mesh 로 복원한다. 원본 재질 에셋은 수정하지 않는다.
    /// </summary>
    public class SpawnEffectDriver : MonoBehaviour
    {
        #region Static Fields
        private static readonly int PositionVectorId = Shader.PropertyToID("_PositionVector");
        private static readonly int UpVectorId = Shader.PropertyToID("_UpVector");
        private static readonly int GuideTextureId = Shader.PropertyToID("_GuideTexture");
        #endregion

        #region Serialized Fields
        [Header("References")]
        [Tooltip("비활성 상태로 저장된 INab 효과. 런타임에서만 활성화한다.")]
        [SerializeField] private InteractiveEffect _effect;
        [Tooltip("VFX 에 mask·텍스처 속성을 공급할 재질 템플릿. 렌더러에는 할당하지 않고 런타임 사본 1개만 만든다.")]
        [SerializeField] private Material _burnTemplate;
        [Tooltip("VFX 가 샘플링하는 몸체 렌더러")]
        [SerializeField] private SkinnedMeshRenderer _bodyRenderer;
        [Tooltip("몸체 mesh 의 Read/Write 사본. 생성 중에만 몸체 렌더러에 연결한다.")]
        [SerializeField] private Mesh _readableBodyMesh;
        [Tooltip("같은 mask 로 함께 사라지고 나타날 렌더러 (몸체, 렌즈, 무기, PowerPod)")]
        [SerializeField] private Renderer[] _targetRenderers;

        [Header("Shaders")]
        [Tooltip("원본 Opaque 셰이더 (M_Material_Parent)")]
        [SerializeField] private Shader _opaqueSourceShader;
        [Tooltip("Opaque 원본에 plane mask 를 추가한 사본 셰이더")]
        [SerializeField] private Shader _opaqueMaskShader;
        [Tooltip("원본 Transparent 셰이더 (M_TransparentMaterial_Parent)")]
        [SerializeField] private Shader _transparentSourceShader;
        [Tooltip("Transparent 원본에 plane mask 를 추가한 사본 셰이더")]
        [SerializeField] private Shader _transparentMaskShader;

        [Header("Mask Range")]
        [Tooltip("시작 plane 이 대상 bounds 바닥보다 얼마나 아래인지 (bounds 높이 대비).")]
        [SerializeField] private float _startPaddingRatio;
        [Tooltip("끝 plane 이 대상 bounds 꼭대기보다 얼마나 위인지 (bounds 높이 대비).")]
        [SerializeField] private float _endPaddingRatio;

        [Header("Playback")]
        [Tooltip("생성 중 VFX 재생 속도. 입자 수명 꼬리를 줄이려고 쓰며, 끝나면 원래 값으로 되돌린다.")]
        [SerializeField] private float _vfxPlayRate = 1.0f;
        #endregion

        #region Private Fields
        private Material _vfxSourceMaterial;
        private readonly Dictionary<Material, Material> _maskMaterials = new Dictionary<Material, Material>();
        private Material[][] _originalMaterials;
        private Material[][] _maskedMaterials;
        private Mesh _originalBodyMesh;
        private Transform[] _originalBones;
        private Transform _originalRootBone;
        private Bounds _originalLocalBounds;
        private Transform _originalMeshTransform;
        private VFXLossyTransformBinder _meshBinder;
        private Transform _originalBinderTarget;
        private float _originalPlayRate;
        private bool _isPlayRateSwapped;
        private bool _isPrepared;
        private bool _isMeshTransformSwapped;
        private bool _areMaterialsSwapped;
        private bool _isMeshSwapped;
        #endregion

        #region Properties
        public bool IsPrepared => _isPrepared;
        public bool IsPlaying { get; private set; }
        public bool IsCompleted { get; private set; }
        public bool AreMaterialsSwapped => _areMaterialsSwapped;
        public bool IsMeshSwapped => _isMeshSwapped;
        public Material RuntimeMaterial => _vfxSourceMaterial;
        public float StartPlaneWorldY { get; private set; }
        public float EndPlaneWorldY { get; private set; }
        public Bounds TargetBounds { get; private set; }
        public bool IsVfxAwakeBeforePlay { get; private set; }
        public int ParticleCountAtRestore { get; private set; } = -1;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            // 첫 프레임에 원본이 그대로 보이지 않도록, 렌더 전에 재질·mask 를 모두 준비한다
            if (!TryPrepare()) enabled = false;
        }

        private void Start()
        {
            // Start 전에 비활성화되어 이미 복원된 경우에는 재생하지 않는다
            if (_isPrepared && _areMaterialsSwapped) StartCoroutine(CoSpawnDissolve());
        }

        private void LateUpdate()
        {
            // 표시 재질도 VFX 와 같은 mask Transform 에서 매 프레임 값을 받는다 (InteractiveEffect.LateUpdate 와 같은 기준)
            if (_areMaterialsSwapped) UpdateMaskVectors();
        }

        private void OnDisable()
        {
            StopAllCoroutines();
            Restore();
        }

        private void OnDestroy()
        {
            Restore();
        }
        #endregion

        #region Private Methods
        private bool TryPrepare()
        {
            if (!HasValidReferences()) return false;

            Bounds bounds = GetTargetBounds();
            float height = bounds.size.y;
            if (!(height > 0.0f) || float.IsInfinity(height))
            {
                Debug.LogError($"[{name}] 대상 bounds 높이가 유효하지 않아 생성 연출을 건너뜁니다.", this);
                return false;
            }

            // mask 는 world 의 위쪽(+Y)을 향해야 plane 이 아래에서 위로 올라간다
            Transform maskTransform = _effect.mask.transform;
            if (Vector3.Dot(maskTransform.up, Vector3.up) < 0.99f)
            {
                Debug.LogError($"[{name}] Mask 의 up 이 world up 과 맞지 않아 생성 연출을 건너뜁니다.", this);
                return false;
            }

            // 셰이더 매핑에 실패하면 아무것도 바꾸지 않고 원본을 그대로 보여 준다
            if (!TryCreateMaskMaterials())
            {
                DestroyRuntimeMaterials();
                return false;
            }

            TargetBounds = bounds;
            StartPlaneWorldY = bounds.min.y - _startPaddingRatio * height;
            EndPlaneWorldY = bounds.max.y + _endPaddingRatio * height;

            Transform maskParent = maskTransform.parent;
            _effect.initialPosition = new Vector3(0.0f, maskParent.InverseTransformPoint(new Vector3(maskParent.position.x, StartPlaneWorldY, maskParent.position.z)).y, 0.0f);
            _effect.finalPosition = new Vector3(0.0f, maskParent.InverseTransformPoint(new Vector3(maskParent.position.x, EndPlaneWorldY, maskParent.position.z)).y, 0.0f);
            maskTransform.localPosition = _effect.initialPosition;

            SwapBodyMesh();
            SwapMaterials();
            SwapMeshTransform();

            _effect.isFinished = false;
            _effect.gameObject.SetActive(true);
            SwapPlayRate();
            _isPrepared = true;
            return true;
        }

        private bool HasValidReferences()
        {
            bool hasEffect = _effect != null && _effect.mask != null && _effect.visualEffect != null;
            bool hasAssets = _burnTemplate != null && _readableBodyMesh != null;
            bool hasRenderers = _bodyRenderer != null && _targetRenderers != null && _targetRenderers.Length > 0;
            bool hasShaders = _opaqueSourceShader != null && _opaqueMaskShader != null &&
                              _transparentSourceShader != null && _transparentMaskShader != null;
            bool hasRange = _startPaddingRatio > 0.0f && _endPaddingRatio > 0.0f;
            bool hasRootBone = _bodyRenderer != null && _bodyRenderer.rootBone != null;

            if (hasEffect && hasAssets && hasRenderers && hasShaders && hasRange && hasRootBone) return true;

            Debug.LogError($"[{name}] 필요한 참조 또는 mask 범위 비율이 없어 생성 연출을 건너뜁니다.", this);
            return false;
        }

        private Bounds GetTargetBounds()
        {
            Bounds bounds = _targetRenderers[0].bounds;
            for (int i = 1; i < _targetRenderers.Length; i++)
            {
                bounds.Encapsulate(_targetRenderers[i].bounds);
            }

            return bounds;
        }

        private void SwapBodyMesh()
        {
            _originalBodyMesh = _bodyRenderer.sharedMesh;
            _originalBones = _bodyRenderer.bones;
            _originalRootBone = _bodyRenderer.rootBone;
            _originalLocalBounds = _bodyRenderer.localBounds;

            _bodyRenderer.sharedMesh = _readableBodyMesh;
            ApplyBodyRig(_originalBones, _originalRootBone, _originalLocalBounds);
            _isMeshSwapped = true;
        }

        /// <summary>
        /// VFX 의 샘플링 좌표는 root 뼈 기준이므로, 변환 기준 Transform 을 SMR 이 아닌 rootBone 으로 바꾼다.
        /// InteractiveEffect.Start 가 이 값을 VFXLossyTransformBinder.Target 으로 옮긴다.
        /// </summary>
        private void SwapMeshTransform()
        {
            _originalMeshTransform = _effect.meshTransform;
            _meshBinder = _effect.visualEffect.GetComponent<VFXLossyTransformBinder>();
            if (_meshBinder) _originalBinderTarget = _meshBinder.Target;

            _effect.meshTransform = _bodyRenderer.rootBone;
            _isMeshTransformSwapped = true;
        }

        /// <summary>
        /// 원본 재질마다 plane mask 셰이더 사본 재질을 하나씩 만든다. 같은 재질은 사본을 공유한다.
        /// </summary>
        private bool TryCreateMaskMaterials()
        {
            _originalMaterials = new Material[_targetRenderers.Length][];
            _maskedMaterials = new Material[_targetRenderers.Length][];

            for (int i = 0; i < _targetRenderers.Length; i++)
            {
                Material[] original = _targetRenderers[i].sharedMaterials;
                Material[] masked = new Material[original.Length];

                for (int j = 0; j < original.Length; j++)
                {
                    Material source = original[j];
                    if (source == null) continue;

                    if (!_maskMaterials.TryGetValue(source, out Material mask))
                    {
                        mask = CreateMaskMaterial(source);
                        if (mask == null) return false;

                        _maskMaterials.Add(source, mask);
                    }

                    masked[j] = mask;
                }

                _originalMaterials[i] = original;
                _maskedMaterials[i] = masked;
            }

            return true;
        }

        private Material CreateMaskMaterial(Material source)
        {
            Shader maskShader;
            if (source.shader == _opaqueSourceShader)
            {
                maskShader = _opaqueMaskShader;
            }
            else if (source.shader == _transparentSourceShader)
            {
                maskShader = _transparentMaskShader;
            }
            else
            {
                Debug.LogError($"[{name}] '{source.name}' 의 셰이더 '{source.shader.name}' 에 대응하는 mask 셰이더가 없어 생성 연출을 건너뜁니다.", this);
                return null;
            }

            // 셰이더만 바꾸면 같은 이름의 속성값·keyword 는 그대로 유지된다. queue 는 원본 재질 값으로 되돌린다
            Material mask = new Material(source) { name = source.name + " (Runtime)" };
            mask.shader = maskShader;
            mask.renderQueue = source.renderQueue;
            return mask;
        }

        private void SwapMaterials()
        {
            // VFX 속성 공급원은 렌더러에 할당하지 않는다. Guide 텍스처를 비워 VFX 가 순수 plane 경계를 쓰게 한다
            _vfxSourceMaterial = new Material(_burnTemplate) { name = _burnTemplate.name + " (Runtime)" };
            _vfxSourceMaterial.SetTexture(GuideTextureId, null);

            _effect.materials.Clear();
            _effect.materials.Add(_vfxSourceMaterial);

            // 첫 프레임부터 mask 가 가장 아래에 있도록 표시 재질 값도 미리 맞춘다
            UpdateMaskVectors();

            for (int i = 0; i < _targetRenderers.Length; i++)
            {
                _targetRenderers[i].sharedMaterials = _maskedMaterials[i];
            }

            _areMaterialsSwapped = true;
        }

        private void UpdateMaskVectors()
        {
            Transform maskTransform = _effect.mask.transform;
            Vector3 position = maskTransform.position;
            Vector3 up = maskTransform.up;

            foreach (Material mask in _maskMaterials.Values)
            {
                mask.SetVector(PositionVectorId, position);
                mask.SetVector(UpVectorId, up);
            }
        }

        private void ApplyBodyRig(Transform[] bones, Transform rootBone, Bounds localBounds)
        {
            _bodyRenderer.bones = bones;
            _bodyRenderer.rootBone = rootBone;
            _bodyRenderer.localBounds = localBounds;
        }

        private void RestoreMaterials()
        {
            if (!_areMaterialsSwapped) return;

            _areMaterialsSwapped = false;
            for (int i = 0; i < _targetRenderers.Length; i++)
            {
                if (_targetRenderers[i]) _targetRenderers[i].sharedMaterials = _originalMaterials[i];
            }
        }

        private void RestoreBodyMesh()
        {
            if (!_isMeshSwapped) return;

            _isMeshSwapped = false;
            if (!_bodyRenderer) return;

            _bodyRenderer.sharedMesh = _originalBodyMesh;
            ApplyBodyRig(_originalBones, _originalRootBone, _originalLocalBounds);
        }

        private void SwapPlayRate()
        {
            _originalPlayRate = _effect.visualEffect.playRate;
            _effect.visualEffect.playRate = _vfxPlayRate;
            _isPlayRateSwapped = true;
        }

        private void RestorePlayRate()
        {
            if (!_isPlayRateSwapped) return;

            _isPlayRateSwapped = false;
            if (_effect && _effect.visualEffect) _effect.visualEffect.playRate = _originalPlayRate;
        }

        private void RestoreMeshTransform()
        {
            if (!_isMeshTransformSwapped) return;

            _isMeshTransformSwapped = false;
            if (!_effect) return;

            _effect.meshTransform = _originalMeshTransform;

            // Start 이후에는 binder 가 rootBone 을 들고 있으므로 같이 되돌린다
            if (_meshBinder) _meshBinder.Target = _originalBinderTarget;
        }

        private void DestroyRuntimeMaterials()
        {
            foreach (Material mask in _maskMaterials.Values)
            {
                if (mask) Destroy(mask);
            }

            _maskMaterials.Clear();

            if (_vfxSourceMaterial)
            {
                Destroy(_vfxSourceMaterial);
                _vfxSourceMaterial = null;
            }
        }

        /// <summary>
        /// 원본 재질 → effect 비활성화 → 원본 mesh → 임시 재질 파기 순으로 되돌린다. 여러 번 불러도 안전하다.
        /// </summary>
        private void Restore()
        {
            RestoreMaterials();

            if (_effect && _effect.gameObject.activeSelf) _effect.gameObject.SetActive(false);

            RestoreBodyMesh();
            RestoreMeshTransform();
            RestorePlayRate();

            if (_effect) _effect.materials.Clear();

            DestroyRuntimeMaterials();

            IsPlaying = false;
        }
        #endregion

        #region Coroutines
        private IEnumerator CoSpawnDissolve()
        {
            // InteractiveEffect.Start 의 mask 초기화와 mesh bake 가 먼저 끝나도록 한 프레임 기다린다
            yield return null;

            IsVfxAwakeBeforePlay = _effect.visualEffect.HasAnySystemAwake();
            IsPlaying = true;
            _effect.PlayEffect();

            while (!_effect.isFinished) yield return null;

            // 생성이 끝나면 재질부터 원본으로 되돌린다
            RestoreMaterials();

            // effect 는 vfxEventOffset 뒤에 VFX stop 이벤트를 보낸다. 그 뒤 한 프레임 이상 지나고 입자가 모두 없어진 다음에 정리한다
            float elapsed = 0.0f;
            while (elapsed < _effect.vfxEventOffset)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            yield return null;

            // aliveParticleCount 는 아직 집계되지 않았을 때 음수를 돌려주므로, 정확히 0 이 될 때까지 기다린다
            while (_effect.visualEffect.aliveParticleCount != 0) yield return null;

            ParticleCountAtRestore = _effect.visualEffect.aliveParticleCount;
            Restore();
            IsCompleted = true;
        }
        #endregion
    }
}
