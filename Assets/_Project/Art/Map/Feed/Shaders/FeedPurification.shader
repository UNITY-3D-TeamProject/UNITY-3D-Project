Shader "Project/FeedPurification"
{
    Properties
    {
        _BaseMap ("Texture", 2D) = "white" {}
        _BaseColor ("Color", Color) = (1, 1, 1, 1)
        _Progress ("Purification", Range(0, 1)) = 0
        _Reveal ("Reveal Normal Feed", Float) = 1
        _CardHeight ("Card Height", Float) = 4.6
        [HideInInspector] _SparkMode ("Spark Mode", Float) = 0
        [HideInInspector] _SrcBlend ("Source Blend", Float) = 1
        [HideInInspector] _DstBlend ("Destination Blend", Float) = 0
        [HideInInspector] _ZWrite ("Depth Write", Float) = 1
    }
    SubShader
    {
        Tags { "RenderType" = "TransparentCutout" "Queue" = "AlphaTest" "RenderPipeline" = "UniversalPipeline" }
        Cull Off
        Blend [_SrcBlend] [_DstBlend]
        ZWrite [_ZWrite]

        Pass
        {
            Name "Purification"
            Tags { "LightMode" = "UniversalForward" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                float _Progress;
                float _Reveal;
                float _CardHeight;
                float _SparkMode;
                float4x4 _WipeWorldToCard;
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
                float3 positionWS : TEXCOORD1;
                half4 color : COLOR;
            };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.color = input.color;
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                if (_SparkMode > 0.5)
                {
                    float radius = length(input.uv * 2.0 - 1.0);
                    float softGlow = pow(saturate(1.0 - radius), 1.6);
                    clip(softGlow - 0.01);
                    return half4(input.color.rgb * 2.5, input.color.a * softGlow);
                }
                half4 color = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;
                clip(color.a - 0.01);
                float height = mul(_WipeWorldToCard, float4(input.positionWS, 1)).y / _CardHeight + 0.5;
                float edge = lerp(1.08, -0.08, _Progress);
                float upper = smoothstep(edge - 0.035, edge + 0.035, height);
                float coverage = lerp(1.0 - upper, upper, _Reveal);
                clip(coverage - 0.5);
                // 하얀 청록색 핵과 넓은 빛 번짐이 위에서 아래로 정화 경계를 따라간다.
                float distanceToEdge = abs(height - edge);
                float glow = 1.0 - smoothstep(0.0, 0.11, distanceToEdge);
                float core = 1.0 - smoothstep(0.0, 0.016, distanceToEdge);
                float pulse = 0.9 + 0.1 * sin(_Time.y * 7.0);
                color.rgb = lerp(color.rgb, half3(0.45, 1.0, 0.86), glow * 0.6);
                color.rgb += half3(0.3, 0.9, 0.75) * glow * 1.2 * pulse;
                color.rgb += half3(0.8, 1.0, 0.94) * core * 1.7;
                return half4(color.rgb, 1);
            }
            ENDHLSL
        }
    }
}
