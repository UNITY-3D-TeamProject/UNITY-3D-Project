using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Core.Scanning
{
    /// <summary>
    /// 스캔 중에만 대상에 실루엣(외곽선·셸)을 씌우는 반응. 아이템·상호작용 오브젝트처럼 평소에도 보이는 오브젝트에 쓴다.
    /// 대상의 원래 렌더러는 건드리지 않고, 같은 메시에 실루엣 머티리얼을 지정한 별도 렌더러를 켜고 끈다.
    /// 벽 뒤 아이템은 실루엣 머티리얼(M_ScanSilhouette)로 가려진 부분만 홀로그램으로 보이고,
    /// 상호작용 오브젝트는 외곽선 머티리얼(M_ScanOutline)로 벽에 가려지지 않았을 때만 바깥 테두리가 보인다.
    /// MeshFilter 실루엣 렌더러는 각진 모서리에서 외곽선이 끊기지 않도록 Awake 에서 매끄러운 법선 메시로 바꾼다.
    /// 사라질 때는 실루엣 알파가 서서히 줄어든다.
    /// 적처럼 SkinnedMeshRenderer 가 여러 개인 대상은 _autoShellMaterial 을 지정하면 셸 렌더러를 Awake 에서 자동으로 만든다.
    /// </summary>
    public class SilhouetteReaction : ScanReactionBase
    {
        #region Static Fields
        // 셰이더 프로퍼티 이름 "_Color"(실루엣·외곽선·셸 셰이더 공통 색)를 정수 ID 로 바꿔 둔 값.
        // 문자열로 넘기면 호출할 때마다 이름을 ID 로 변환하므로, 매 프레임 쓰는 값은 한 번만 변환해 재사용한다.
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        // 원본 메시 -> 매끄러운 법선 복제본. 같은 아이템이 여러 개여도 메시는 하나만 복제해 함께 쓴다.
        private static readonly Dictionary<Mesh, Mesh> SmoothNormalMeshes = new Dictionary<Mesh, Mesh>();
        #endregion

        #region Serialized Fields
        [Header("약점은 안 해도 됨, 그 외는 필수 (Renderer ← 실루엣 자식)")]
        [Tooltip("스캔 중에만 켜질 실루엣 렌더러. 대상과 같은 메시에 M_ScanSilhouette(벽 뒤 아이템) 또는 M_ScanOutline(상호작용)을 지정한 자식 오브젝트를 연결한다. 평소에는 꺼진다.")]
        // 아이템이 여러 메시 조각(예: 상자 몸통 + 뚜껑)으로 이루어지면 조각마다 실루엣 렌더러가 하나씩 필요하므로 배열로 받는다.
        // 메시가 하나뿐인 아이템은 1칸만 채우면 된다.
        [SerializeField] private Renderer[] _silhouetteRenderers;
        [Header("약점은 필수 (Material ← M_ScanHologram), 그 외는 안 해도 됨")]
        [Tooltip("지정하면 실루엣 렌더러를 직접 연결하지 않고, Awake 에서 자식의 모든 SkinnedMeshRenderer 마다 이 머티리얼을 쓰는 복제 렌더러를 만든다. 적의 홀로그램 셸(M_ScanHologram)용")]
        // 적 프리팹은 메시 조각이 많고(Melee 9개, Ranged 20개) 모델이 바뀔 수 있어, 셸을 손으로 만들지 않고 런타임에 만든다.
        [SerializeField] private Material _autoShellMaterial;
        [Header("약점은 필수 (Transform ← 몬스터 루트), 그 외는 안 해도 됨")]
        [Tooltip("셸을 자동 생성할 때 SkinnedMeshRenderer 를 찾을 기준 오브젝트. 비어 있으면 이 오브젝트. 약점 오브젝트에 붙일 때는 몬스터 루트를 연결한다.")]
        // 몬스터는 루트에 스캔 스크립트를 두지 않고 약점 오브젝트에 이 반응을 붙이므로, 몸 전체 렌더러를 찾을 기준을 따로 받는다.
        [SerializeField] private Transform _shellSourceRoot;
        #endregion

        #region Private Fields
        // 머티리얼을 복제하지 않고 렌더러별로 셰이더 값만 덮어쓰기 위한 블록이다.
        // renderer.material 로 색을 바꾸면 머티리얼 인스턴스가 새로 생기므로 이 방식을 쓴다.
        // 셰이더 프로퍼티 값들을 담아두는 컨테이너Z
        private MaterialPropertyBlock _propertyBlock;
        // 렌더러별 머티리얼 원래 색. _silhouetteRenderers 와 같은 순서로 저장하고, 페이드 때 알파만 곱해 쓴다.
        private Color[] _baseColors;
        // 마지막으로 적용한 세기. 같은 값이면 다시 쓰지 않기 위해 쓴다. 음수면 아직 적용하지 않은 상태다.
        private float _appliedFade = -1.0f;
        #endregion

        #region Properties
        /// <summary>스캔 중에 켜지는 실루엣(셸) 렌더러 목록. 자동 생성한 셸도 포함된다.</summary>
        public Renderer[] SilhouetteRenderers => _silhouetteRenderers;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            // 셸 자동 생성을 쓰면 아래 처리 전에 렌더러 목록부터 채운다.
            if (_autoShellMaterial) _silhouetteRenderers = BuildShellRenderers();

            // 대상의 원래 렌더러까지 잡히지 않도록 자동 탐색하지 않고 직접 연결받는다.
            Debug.Assert(
                (_silhouetteRenderers != null) && (_silhouetteRenderers.Length > 0),
                $"[{name}] 실루엣 렌더러가 연결되지 않았습니다.");

            // 연결이 비어 있어도 아래 반복문들이 null 예외 없이 아무것도 안 하도록 빈 배열로 둔다.
            if (_silhouetteRenderers == null) _silhouetteRenderers = new Renderer[0];

            _propertyBlock = new MaterialPropertyBlock();

            // 외곽선은 법선 방향으로 부풀려 그리므로, 각진 메시는 매끄러운 법선으로 바꿔야 모서리가 이어진다.
            foreach (Renderer targetRenderer in _silhouetteRenderers)
            {
                if (targetRenderer && targetRenderer.TryGetComponent(out MeshFilter meshFilter))
                {
                    meshFilter.sharedMesh = GetSmoothNormalMesh(meshFilter.sharedMesh);
                }
            }

            // 페이드 때마다 머티리얼을 다시 읽지 않도록 원래 색을 미리 저장해 둔다.
            _baseColors = new Color[_silhouetteRenderers.Length];
            for (int i = 0; i < _silhouetteRenderers.Length; i++)
            {
                Renderer targetRenderer = _silhouetteRenderers[i];
                Material material = targetRenderer ? targetRenderer.sharedMaterial : null;

                // 실루엣 셰이더가 아니라 _Color 가 없는 머티리얼이면 흰색을 기준으로 삼는다.
                _baseColors[i] = (material && material.HasColor(ColorId)) ? material.GetColor(ColorId) : Color.white;
            }

            // 스캔에 닿기 전에는 실루엣이 보이지 않는다.
            SetRenderersEnabled(false);
        }
        #endregion

        #region Public Methods
        /// <inheritdoc />
        public override void OnScanBegin(in SScanHit hit)
        {
            // 세기 0 에서 시작하므로 첫 프레임에 튀지 않게 투명한 상태로 켠다.
            // 이후 ScanTarget 이 매 프레임 OnFade 로 세기를 올려 주면 서서히 진해진다.
            ApplyAlpha(0.0f);
            SetRenderersEnabled(true);
        }

        /// <inheritdoc />
        public override void OnFade(float fade, bool isFadingOut)
        {
            // 나타날 때(0 -> 1)와 사라질 때(1 -> 0) 모두 세기를 그대로 알파에 반영하므로 isFadingOut 은 구분하지 않는다.
            ApplyAlpha(fade);
        }

        /// <inheritdoc />
        public override void OnScanEnd()
        {
            // 완전히 사라졌거나 중단되면 렌더러를 꺼서 다음 스캔 전까지 그리기 비용도 쓰지 않는다.
            SetRenderersEnabled(false);
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// 기준 오브젝트(_shellSourceRoot, 비어 있으면 자신) 아래 모든 SkinnedMeshRenderer 마다 같은 메시·본을 쓰고 _autoShellMaterial 을 지정한 셸 렌더러를 만든다.
        /// 셸은 원본 렌더러 오브젝트의 자식으로 두므로 원본과 함께 켜지고 꺼지며, 같은 본을 따라 애니메이션된다.
        /// </summary>
        /// <returns>만든 셸 렌더러 목록</returns>
        private Renderer[] BuildShellRenderers()
        {
            Transform sourceRoot = _shellSourceRoot ? _shellSourceRoot : transform;

            // 새로 만든 셸이 다시 잡히지 않도록 원본 목록을 먼저 모아 둔다.
            SkinnedMeshRenderer[] sources = sourceRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            Renderer[] shells = new Renderer[sources.Length];

            for (int i = 0; i < sources.Length; i++)
            {
                SkinnedMeshRenderer source = sources[i];

                GameObject shellObject = new GameObject("ScanHologram");
                shellObject.transform.SetParent(source.transform, false);

                SkinnedMeshRenderer shell = shellObject.AddComponent<SkinnedMeshRenderer>();
                shell.sharedMesh = source.sharedMesh;
                shell.bones = source.bones;
                shell.rootBone = source.rootBone;
                shell.localBounds = source.localBounds;
                shell.shadowCastingMode = ShadowCastingMode.Off;
                shell.receiveShadows = false;

                // 서브메시가 여러 개여도 몸 전체가 셸로 덮이도록 서브메시 수만큼 같은 머티리얼을 채운다.
                int subMeshCount = source.sharedMesh ? Mathf.Max(1, source.sharedMesh.subMeshCount) : 1;
                Material[] materials = new Material[subMeshCount];
                for (int j = 0; j < subMeshCount; j++)
                {
                    materials[j] = _autoShellMaterial;
                }
                shell.sharedMaterials = materials;

                shells[i] = shell;
            }

            return shells;
        }

        /// <summary>
        /// 같은 위치의 정점들이 법선을 평균해 나눠 쓰는 복제 메시를 돌려준다.
        /// 상자처럼 모서리 정점이 면마다 다른 법선을 가지면 외곽선을 부풀릴 때 모서리가 벌어지므로, 위치가 같으면 같은 방향으로 밀리게 한다.
        /// 실루엣 렌더러 전용이므로 원래 렌더러의 메시는 바뀌지 않는다.
        /// </summary>
        /// <param name="source">원본 메시</param>
        /// <returns>매끄러운 법선 메시. 정점을 읽을 수 없는 메시는 원본을 그대로 돌려준다.</returns>
        private Mesh GetSmoothNormalMesh(Mesh source)
        {
            if (!source) return source;
            if (SmoothNormalMeshes.TryGetValue(source, out Mesh cached)) return cached;

            // 빌드에서는 Read/Write 가 꺼진 메시의 정점을 읽을 수 없으므로 원본을 쓴다(모서리가 끊길 수 있다).
            if (!source.isReadable)
            {
                Debug.LogWarning($"[{name}] 메시 '{source.name}' 의 Read/Write 가 꺼져 있어 외곽선 법선을 매끄럽게 하지 못했습니다.");
                SmoothNormalMeshes[source] = source;
                return source;
            }

            Vector3[] vertices = source.vertices;
            Vector3[] normals = source.normals;

            // 위치별로 법선을 모두 더한 뒤, 각 정점은 자기 위치의 합을 정규화해 쓴다.
            Dictionary<Vector3, Vector3> normalSums = new Dictionary<Vector3, Vector3>();
            for (int i = 0; i < normals.Length; i++)
            {
                normalSums.TryGetValue(vertices[i], out Vector3 sum);
                normalSums[vertices[i]] = sum + normals[i];
            }

            Vector3[] smoothNormals = new Vector3[normals.Length];
            for (int i = 0; i < normals.Length; i++)
            {
                smoothNormals[i] = normalSums[vertices[i]].normalized;
            }

            Mesh smoothMesh = Instantiate(source);
            smoothMesh.name = $"{source.name}_SmoothNormals";
            smoothMesh.normals = smoothNormals;

            SmoothNormalMeshes[source] = smoothMesh;
            return smoothMesh;
        }

        /// <summary>
        /// 모든 실루엣 렌더러의 색을 원래 색에서 알파만 fade 비율로 줄여 적용한다.
        /// </summary>
        /// <param name="fade">반응의 세기(0~1). 0 이면 완전히 투명하다.</param>
        private void ApplyAlpha(float fade)
        {
            // 다 나타난 뒤 유지 시간 동안은 매 프레임 같은 세기가 들어오므로 다시 쓰지 않는다.
            if (fade == _appliedFade) return;
            _appliedFade = fade;

            for (int i = 0; i < _silhouetteRenderers.Length; i++)
            {
                Renderer targetRenderer = _silhouetteRenderers[i];

                // 파괴된 렌더러는 건너뛴다.
                if (!targetRenderer) continue;

                // 원래 색은 유지하고 알파만 fade(0~1)에 비례해 줄인다.
                Color color = _baseColors[i];
                color.a *= fade;

                // 렌더러에 이미 있던 블록 값을 유지한 채 색만 덮어쓴다.
                targetRenderer.GetPropertyBlock(_propertyBlock);
                _propertyBlock.SetColor(ColorId, color);
                targetRenderer.SetPropertyBlock(_propertyBlock);
            }
        }

        /// <summary>
        /// 모든 실루엣 렌더러를 켜거나 끈다. 오브젝트가 아니라 렌더러만 끄므로 자식에 붙은 다른 컴포넌트는 그대로 동작한다.
        /// </summary>
        /// <param name="isEnabled">켤지 여부</param>
        private void SetRenderersEnabled(bool isEnabled)
        {
            foreach (Renderer targetRenderer in _silhouetteRenderers)
            {
                if (targetRenderer) targetRenderer.enabled = isEnabled;
            }
        }
        #endregion
    }
}
