using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace Core.Scanning
{
    /// <summary>
    /// 플레이어가 보이는 픽셀에 스텐실 비트를 찍는 렌더러 기능.
    /// 벽 뒤 아이템 실루엣(ZTest Greater)은 앞을 가린 것이 벽인지 플레이어인지 모르므로,
    /// 플레이어 자리를 스텐실로 표시해 두고 실루엣 셰이더가 그 픽셀을 건너뛰게 한다.
    /// 플레이어 메시는 게임오브젝트 레이어가 Default 라서, 렌더링 레이어(PlayerStencil)로 대상을 고른다.
    /// 렌더링 레이어는 스폰 때 MarkRenderers 로 더한다(Player 프리팹은 건드리지 않는다).
    /// </summary>
    public class PlayerStencilRendererFeature : ScriptableRendererFeature
    {
        #region Constants
        // ProjectSettings > Tags and Layers > Rendering Layers 에 있는 이름과 같아야 한다.
        public const string RENDERING_LAYER_NAME = "PlayerStencil";
        private const string PASS_NAME = "PlayerStencil";
        #endregion

        #region Serialized Fields
        [Tooltip("스텐실만 찍는 셰이더 (Hidden/Project/PlayerStencilMask)")]
        [SerializeField] private Shader _shader;
        [Tooltip("불투명 물체를 다 그린 뒤, 투명 물체(실루엣)보다 먼저 찍어야 한다.")]
        [SerializeField] private RenderPassEvent _renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
        #endregion

        #region Private Fields
        private Material _material;
        private PlayerStencilPass _pass;
        #endregion

        #region Public Methods
        /// <summary>
        /// root 아래 모든 렌더러의 렌더링 레이어에 PlayerStencil 을 더한다. 원래 비트는 그대로 둔다.
        /// 투명 렌더러도 비트를 받지만, 렌더러 기능은 불투명 큐만 그리므로 스텐실은 찍히지 않는다.
        /// </summary>
        /// <param name="root">플레이어 루트 오브젝트</param>
        public static void MarkRenderers(GameObject root)
        {
            if (!root) return;

            uint mask = RenderingLayerMask.GetMask(RENDERING_LAYER_NAME);
            if (mask == 0)
            {
                Debug.LogWarning($"렌더링 레이어 '{RENDERING_LAYER_NAME}' 이 없어 플레이어 위 스캔 실루엣을 막지 못합니다.", root);
                return;
            }

            foreach (Renderer targetRenderer in root.GetComponentsInChildren<Renderer>(true))
            {
                targetRenderer.renderingLayerMask |= mask;
            }
        }
        #endregion

        #region ScriptableRendererFeature
        public override void Create()
        {
            _material = _shader ? CoreUtils.CreateEngineMaterial(_shader) : null;
            _pass = new PlayerStencilPass(_material)
            {
                renderPassEvent = _renderPassEvent,
            };
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (!_material) return;

            renderer.EnqueuePass(_pass);
        }

        protected override void Dispose(bool disposing)
        {
            CoreUtils.Destroy(_material);
        }
        #endregion

        #region Passes
        /// <summary>
        /// PlayerStencil 렌더링 레이어를 가진 불투명 렌더러를 스텐실 셰이더로 다시 그린다.
        /// </summary>
        private class PlayerStencilPass : ScriptableRenderPass
        {
            // 렌더러 기능 RenderObjects 의 기본값과 같은 패스 태그. 원래 셰이더에 이 패스가 있는 렌더러만 그린다.
            private static readonly List<ShaderTagId> ShaderTagIds = new List<ShaderTagId>
            {
                new ShaderTagId("UniversalForward"),
                new ShaderTagId("UniversalForwardOnly"),
                new ShaderTagId("SRPDefaultUnlit"),
            };

            private readonly Material _material;

            public PlayerStencilPass(Material material)
            {
                _material = material;
            }

            private class PassData
            {
                public RendererListHandle RendererList;
            }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                uint mask = RenderingLayerMask.GetMask(RENDERING_LAYER_NAME);
                if (mask == 0) return;

                UniversalRenderingData renderingData = frameData.Get<UniversalRenderingData>();
                UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
                UniversalLightData lightData = frameData.Get<UniversalLightData>();
                UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();

                DrawingSettings drawingSettings = RenderingUtils.CreateDrawingSettings(
                    ShaderTagIds,
                    renderingData,
                    cameraData,
                    lightData,
                    cameraData.defaultOpaqueSortFlags);
                drawingSettings.overrideMaterial = _material;
                drawingSettings.overrideMaterialPassIndex = 0;

                FilteringSettings filteringSettings = new FilteringSettings(RenderQueueRange.opaque, -1, mask);
                RendererListParams listParams = new RendererListParams(renderingData.cullResults, drawingSettings, filteringSettings);

                using (IRasterRenderGraphBuilder builder = renderGraph.AddRasterRenderPass(PASS_NAME, out PassData passData))
                {
                    passData.RendererList = renderGraph.CreateRendererList(listParams);
                    builder.UseRendererList(passData.RendererList);

                    // 색은 쓰지 않지만 실루엣과 같은 대상에 그려야 스텐실이 이어지므로 색·깊이를 함께 묶는다.
                    builder.SetRenderAttachment(resourceData.activeColorTexture, 0, AccessFlags.Write);
                    builder.SetRenderAttachmentDepth(resourceData.activeDepthTexture, AccessFlags.ReadWrite);

                    builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                    {
                        context.cmd.DrawRendererList(data.RendererList);
                    });
                }
            }
        }
        #endregion
    }
}
