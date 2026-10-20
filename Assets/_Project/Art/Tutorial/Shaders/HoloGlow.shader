// 튜토리얼 홀로그램의 빛 선·소용돌이·입자용 Unlit 셰이더 (HDR 색 × 텍스처 × 버텍스 색).
// 블렌드는 머티리얼 값으로 정한다: 불투명 빛 선 = One / Zero / ZWrite 1, 투명 = SrcAlpha / OneMinusSrcAlpha, 가산 입자 = SrcAlpha / One.
// 전역 어둠(_HoloDarkness)이면 칸 단위로 조각조각 사라지고, 경보(_HoloAlarm)면 붉어진다. 안개를 따른다.
Shader "Project/HoloGlow"
{
    Properties
    {
        _BaseMap ("Texture", 2D) = "white" {}
        [HDR] _BaseColor ("Color", Color) = (1, 1, 1, 1)
        [Toggle] _DarkImmune ("Dark Immune (어둠에서도 보임)", Float) = 0
        _DissolveCell ("Dissolve Cell (m)", Float) = 0.6

        [Header(Blend)]
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Src Blend", Float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Dst Blend", Float) = 0
        [Enum(Off, 0, On, 1)] _ZWrite ("ZWrite", Float) = 1
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 2
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        Blend [_SrcBlend] [_DstBlend]
        ZWrite [_ZWrite]
        Cull [_Cull]

        Pass
        {
            Name "Unlit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "HoloCommon.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                float _DarkImmune;
                float _DissolveCell;
                float _SrcBlend;
                float _DstBlend;
                float _ZWrite;
                float _Cull;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
                float3 positionWS : TEXCOORD1;
                float fogFactor : TEXCOORD2;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.color = input.color;
                output.fogFactor = ComputeFogFactor(output.positionCS.z);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 color = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor * input.color;

                // 어둠: 칸 단위로 조각조각 사라진다 (드러날 때도 조각조각 켜짐)
                float visibility = HoloVisibility(_DarkImmune);
                clip(HoloCellNoise(input.positionWS, _DissolveCell) - (1.0 - visibility) - 0.0001);

                color.rgb = HoloAlarmTint(color.rgb);

                // 불투명이면 안개 색과 섞고, 투명이면 안개만큼 옅어진다
                float fogIntensity = HoloFogVisibility(input.fogFactor);
                if (_ZWrite > 0.5)
                {
                    color.rgb = MixFog(color.rgb, input.fogFactor);
                }
                else
                {
                    color.a *= fogIntensity;
                    if (_DstBlend == 1.0) color.rgb *= fogIntensity;   // 가산
                }

                return color;
            }
            ENDHLSL
        }
    }
}
