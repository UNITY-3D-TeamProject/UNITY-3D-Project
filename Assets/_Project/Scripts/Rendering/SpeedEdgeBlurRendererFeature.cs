using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

namespace Rendering
{
    /// <summary>
    /// 가장자리 블러 설정. SetTarget 에 함께 넘기면 머티리얼 기본값 대신 이 값을 쓴다.
    /// </summary>
    public struct SSpeedEdgeBlurSettings
    {
        /// <summary>블러가 번지는 정도.</summary>
        public float Strength;
        /// <summary>블러가 없는 중앙 영역 반경 (세로 기준, 0.5 = 화면 가장자리).</summary>
        public float CenterRadius;
        /// <summary>중앙에서 블러가 최대가 될 때까지의 부드러운 경계 폭.</summary>
        public float Feather;
        /// <summary>블러 샘플 수.</summary>
        public int SampleCount;
        /// <summary>블러 중심 (화면 UV).</summary>
        public Vector2 Center;
    }

    /// <summary>
    /// 화면 가장자리를 방사형으로 흐리게 하는 속도감 효과 렌더러 기능.
    /// SetTarget 으로 지정된 카메라에만, 강도가 0 보다 클 때만 패스를 등록한다.
    /// 강도는 전역 셰이더 값이 아닌 MaterialPropertyBlock 으로 전달해 머티리얼 에셋을 바꾸지 않는다.
    /// </summary>
    public class SpeedEdgeBlurRendererFeature : ScriptableRendererFeature
    {
        #region Constants
        private const string PASS_NAME = "SpeedEdgeBlur";
        private const string SOURCE_TEXTURE_NAME = "_SpeedEdgeBlurSource";
        #endregion

        #region Static Fields
        private static Camera s_TargetCamera;
        private static float s_Weight;
        private static bool s_HasSettings;
        private static SSpeedEdgeBlurSettings s_Settings;
        #endregion

        #region Serialized Fields
        [Tooltip("가장자리 블러 머티리얼 (Hidden/Project/SpeedEdgeBlur)")]
        [SerializeField] private Material _material;
        [SerializeField] private RenderPassEvent _renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
        #endregion

        #region Private Fields
        private SpeedEdgeBlurPass _pass;
        #endregion

        #region Public Methods
        /// <summary>
        /// 블러를 적용할 카메라와 강도를 지정한다. 강도가 0 이하면 해당 카메라는 대상에서 빠진다.
        /// </summary>
        /// <param name="camera">블러를 적용할 게임 카메라</param>
        /// <param name="weight">블러 강도 (0~1)</param>
        public static void SetTarget(Camera camera, float weight)
        {
            if (!camera) return;

            s_TargetCamera = camera;
            s_Weight = Mathf.Clamp01(weight);
            s_HasSettings = false;
        }

        /// <summary>
        /// 블러를 적용할 카메라와 강도, 블러 설정을 지정한다. 설정은 머티리얼 기본값 대신 쓰인다.
        /// </summary>
        /// <param name="camera">블러를 적용할 게임 카메라</param>
        /// <param name="weight">블러 강도 (0~1)</param>
        /// <param name="settings">블러 설정</param>
        public static void SetTarget(Camera camera, float weight, SSpeedEdgeBlurSettings settings)
        {
            if (!camera) return;

            s_TargetCamera = camera;
            s_Weight = Mathf.Clamp01(weight);
            s_HasSettings = true;
            s_Settings = settings;
        }

        /// <summary>
        /// 지정된 카메라가 현재 대상일 때만 블러 대상을 해제한다. 다른 카메라가 대상이면 건드리지 않는다.
        /// </summary>
        /// <param name="camera">해제하려는 카메라</param>
        public static void ClearTarget(Camera camera)
        {
            if (s_TargetCamera != camera) return;

            s_TargetCamera = null;
            s_Weight = 0.0f;
            s_HasSettings = false;
        }
        #endregion

        #region ScriptableRendererFeature
        public override void Create()
        {
            _pass = new SpeedEdgeBlurPass(_material)
            {
                renderPassEvent = _renderPassEvent,
            };
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (_pass == null || !_material) return;

            CameraData cameraData = renderingData.cameraData;
            bool isTarget = s_TargetCamera && (s_Weight > 0.0f) &&
                            (cameraData.camera == s_TargetCamera) &&
                            (cameraData.cameraType == CameraType.Game);
            if (!isTarget) return;

            _pass.Setup(s_Weight, s_HasSettings, s_Settings);
            _pass.requiresIntermediateTexture = true;
            renderer.EnqueuePass(_pass);
        }

        protected override void Dispose(bool isDisposing)
        {
            _pass = null;
        }
        #endregion

        #region Static Initialization
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            s_TargetCamera = null;
            s_Weight = 0.0f;
            s_HasSettings = false;
        }
        #endregion

        private class SpeedEdgeBlurPass : ScriptableRenderPass
        {
            private static readonly int WeightId = Shader.PropertyToID("_SpeedEdgeBlurWeight");
            private static readonly int StrengthId = Shader.PropertyToID("_BlurStrength");
            private static readonly int CenterRadiusId = Shader.PropertyToID("_CenterRadius");
            private static readonly int FeatherId = Shader.PropertyToID("_Feather");
            private static readonly int SampleCountId = Shader.PropertyToID("_SampleCount");
            private static readonly int CenterId = Shader.PropertyToID("_Center");

            private readonly Material _material;
            private readonly MaterialPropertyBlock _propertyBlock = new MaterialPropertyBlock();

            public SpeedEdgeBlurPass(Material material)
            {
                _material = material;
            }

            public void Setup(float weight, bool hasSettings, SSpeedEdgeBlurSettings settings)
            {
                _propertyBlock.Clear();
                _propertyBlock.SetFloat(WeightId, weight);
                if (!hasSettings) return;

                _propertyBlock.SetFloat(StrengthId, settings.Strength);
                _propertyBlock.SetFloat(CenterRadiusId, settings.CenterRadius);
                _propertyBlock.SetFloat(FeatherId, settings.Feather);
                _propertyBlock.SetFloat(SampleCountId, settings.SampleCount);
                _propertyBlock.SetVector(CenterId, settings.Center);
            }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                if (!_material) return;

                UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
                if (resourceData.isActiveTargetBackBuffer) return;

                TextureHandle activeColor = resourceData.activeColorTexture;
                if (!activeColor.IsValid()) return;

                // 같은 텍스처를 읽고 쓸 수 없으므로 현재 화면을 임시 텍스처로 복사한 뒤 그것을 읽어 다시 그린다
                TextureDesc desc = renderGraph.GetTextureDesc(activeColor);
                desc.name = SOURCE_TEXTURE_NAME;
                desc.clearBuffer = false;
                desc.msaaSamples = MSAASamples.None;
                TextureHandle sourceCopy = renderGraph.CreateTexture(desc);

                renderGraph.AddBlitPass(activeColor, sourceCopy, Vector2.one, Vector2.zero, passName: PASS_NAME + "_Copy");

                RenderGraphUtils.BlitMaterialParameters blitParameters = new RenderGraphUtils.BlitMaterialParameters(
                    sourceCopy,
                    activeColor,
                    _material,
                    0,
                    _propertyBlock);
                renderGraph.AddBlitPass(blitParameters, PASS_NAME);
            }
        }
    }
}
