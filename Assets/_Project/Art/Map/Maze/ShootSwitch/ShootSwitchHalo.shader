Shader "Project/ShootSwitchHalo"
{
    Properties
    {
        _BaseColor ("State Color", Color) = (0.15, 0.9, 1, 1)
        _Opacity ("Glow Opacity", Range(0, 1)) = 0.45
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Tags { "LightMode" = "UniversalForward" }
            Blend SrcAlpha One
            ZWrite Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half _Opacity;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                half fogFactor : TEXCOORD1;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                output.fogFactor = ComputeFogFactor(output.positionCS.z);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float radius = length(input.uv * 2.0 - 1.0);
                half band = exp(-pow((radius - 0.79) / 0.13, 2.0));
                half edgeFade = 1.0h - smoothstep(0.92h, 1.0h, radius);
                half3 color = MixFogColor(_BaseColor.rgb, half3(0, 0, 0), input.fogFactor);
                return half4(color, band * edgeFade * _Opacity * _BaseColor.a);
            }
            ENDHLSL
        }
    }
}
