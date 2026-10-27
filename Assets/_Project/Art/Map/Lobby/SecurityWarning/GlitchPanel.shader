// 로비 보안 경고 앞 패널 (URP, 조명 없음, 반투명, 양면).
// 평소엔 글자 그대로 빛나다가 _GlitchInterval 초마다 짧게(_GlitchDuration) 찢어진다:
// 가로 띠 몇 줄이 좌우로 밀리고 R·B 채널이 어긋나며 깜빡인다.
Shader "Project/GlitchPanel"
{
    Properties
    {
        _BaseMap ("Texture", 2D) = "white" {}
        [HDR] _Color ("Tint", Color) = (1, 1, 1, 1)
        _GlitchInterval ("Glitch Interval (s)", Float) = 2.6
        _GlitchDuration ("Glitch Duration (s)", Range(0.05, 1)) = 0.3
        _GlitchShift ("Glitch Shift", Range(0, 0.2)) = 0.04
        _RgbSplit ("RGB Split", Range(0, 0.05)) = 0.012
        _BandCount ("Glitch Band Count", Float) = 14
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
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _Color;
                float _GlitchInterval;
                float _GlitchDuration;
                float _GlitchShift;
                float _RgbSplit;
                float _BandCount;
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

            float Hash(float n)
            {
                return frac(sin(n) * 43758.5453);
            }

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.fogFactor = ComputeFogFactor(output.positionCS.z);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float t = _Time.y;
                float cycle = floor(t / _GlitchInterval);
                half isGlitch = step(fmod(t, _GlitchInterval), _GlitchDuration);

                // 글리치 중: 빠르게 바뀌는 가로 띠 중 일부만 좌우로 밀림
                float step20 = floor(t * 20.0);
                float band = floor(input.uv.y * _BandCount);
                float bandRand = Hash(band * 7.13 + step20 + cycle * 3.1);
                float shift = (bandRand - 0.5) * 2.0 * _GlitchShift * step(0.6, bandRand) * isGlitch;
                float split = _RgbSplit * isGlitch;

                float2 uv = input.uv + float2(shift, 0.0);
                half4 center = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv);
                half r = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv - float2(split, 0.0)).r;
                half b = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv + float2(split, 0.0)).b;
                half4 color = half4(r, center.g, b, center.a) * _Color;

                // 글리치 중 깜빡임
                half flicker = lerp(1.0, 0.35 + 0.65 * step(0.3, Hash(step20 + cycle)), isGlitch);
                return half4(MixFog(color.rgb * flicker, input.fogFactor), color.a * saturate(flicker + 0.3));
            }
            ENDHLSL
        }
    }
}
