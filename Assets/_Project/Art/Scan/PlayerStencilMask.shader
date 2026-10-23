// 플레이어가 보이는 픽셀에 스텐실 비트(128)만 찍는 셰이더. 색과 깊이는 쓰지 않는다.
// PlayerStencilRendererFeature 가 플레이어 불투명 메시를 이 셰이더로 한 번 더 그리고,
// ScanSilhouetteHologram 은 이 비트가 있는 픽셀을 건너뛰어 플레이어 몸 위에 실루엣이 그려지지 않게 한다.
// 비트 128 은 ScanOutline 이 쓰는 64 와 겹치지 않게 골랐다.
Shader "Hidden/Project/PlayerStencilMask"
{
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "PlayerStencilMask"

            ColorMask 0
            ZWrite Off
            ZTest LEqual
            Cull Back

            Stencil
            {
                Ref 128
                WriteMask 128
                Comp Always
                Pass Replace
            }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }
    }
}
