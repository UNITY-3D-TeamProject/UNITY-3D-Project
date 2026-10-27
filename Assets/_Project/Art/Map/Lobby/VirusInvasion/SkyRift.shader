// 로비 바이러스 침공: 보안 발판 위 하늘의 찢어진 틈 (URP, 조명 없음, 반투명).
// _MaskMap: R = 틈 안쪽, G = 테두리 빛 (불투명 PNG, sRGB 끔). 안쪽은 어두운 빨강에 _CodeMap 글자가 위로 흐르고,
// 테두리는 숨 쉬듯 밝아졌다 어두워진다.
Shader "Project/SkyRift"
{
    Properties
    {
        _MaskMap ("Mask (R inside, G edge glow)", 2D) = "black" {}
        _CodeMap ("Code (R glyph, tiles)", 2D) = "black" {}
        _CodeTiling ("Code Tiling", Vector) = (2, 3, 0, 0)
        _CodeSpeed ("Code Scroll Speed", Float) = 0.12
        [HDR] _EdgeColor ("Edge Glow", Color) = (2.4, 0.4, 0.6, 1)
        _InsideColor ("Inside", Color) = (0.12, 0.0, 0.03, 1)
        [HDR] _CodeColor ("Code", Color) = (1.6, 0.25, 0.4, 1)
        _PulseSpeed ("Edge Pulse Speed", Float) = 2
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "Unlit"
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MaskMap);
            SAMPLER(sampler_MaskMap);
            TEXTURE2D(_CodeMap);
            SAMPLER(sampler_CodeMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _MaskMap_ST;
                float4 _CodeMap_ST;
                float4 _CodeTiling;
                float _CodeSpeed;
                half4 _EdgeColor;
                half4 _InsideColor;
                half4 _CodeColor;
                float _PulseSpeed;
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
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half2 mask = SAMPLE_TEXTURE2D(_MaskMap, sampler_MaskMap, input.uv).rg;
                float2 codeUv = input.uv * _CodeTiling.xy + float2(0.0, _Time.y * _CodeSpeed);
                half glyph = SAMPLE_TEXTURE2D(_CodeMap, sampler_CodeMap, codeUv).r;

                half pulse = 0.75 + 0.25 * sin(_Time.y * _PulseSpeed) + 0.1 * sin(_Time.y * _PulseSpeed * 3.7);
                half3 inside = _InsideColor.rgb + _CodeColor.rgb * glyph;
                half3 rgb = inside * mask.r + _EdgeColor.rgb * mask.g * pulse;
                half alpha = saturate(max(mask.r, mask.g * pulse));
                return half4(rgb, alpha);
            }
            ENDHLSL
        }
    }
}
