// 로비 앱 플랫폼 네온 로고·꺾쇠 (URP, 조명 없음, 반투명).
// - 들어갈 수 있는 앱(_Locked 0): Intensity 배로 밝게 + 숨 쉬듯 천천히 밝기가 오르내림
// - 들어갈 수 없는 앱(_Locked 1): LockedIntensity 로 어둡고 색이 빠진 채, 고장 난 네온처럼 불규칙하게 깜빡임
// 머티리얼 _Phase 로 위상을 달리해 플랫폼끼리 같이 깜빡이지 않는다(같은 앱의 면·로고는 함께 깜빡임).
Shader "Project/NeonLogo"
{
    Properties
    {
        _BaseMap ("Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1, 1, 1, 1)
        _Intensity ("Intensity", Range(0, 4)) = 1.6
        _BreatheStrength ("Breathe Strength", Range(0, 1)) = 0.15
        [Toggle] _Locked ("Locked (어둡게 깜빡임)", Float) = 0
        _LockedIntensity ("Locked Intensity", Range(0, 1)) = 0.45
        _LockedDesaturate ("Locked Desaturate", Range(0, 1)) = 0.4
        _Phase ("Phase (앱마다 다르게)", Range(0, 1)) = 0
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 2
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
            Cull [_Cull]

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
                half _Intensity;
                half _BreatheStrength;
                half _Locked;
                half _LockedIntensity;
                half _LockedDesaturate;
                float _Phase;
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
                float phase : TEXCOORD1;
                half fogFactor : TEXCOORD2;
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
                output.phase = _Phase;
                output.fogFactor = ComputeFogFactor(output.positionCS.z);
                return output;
            }

            // 고장 난 네온: 몇 초마다 깜빡임 구간이 오고, 그 안에서 빠르게 꺼졌다 켜진다.
            half BrokenFlicker(float phase)
            {
                float t = _Time.y + phase * 37.0;
                float burstSeed = Hash(floor(t / 2.5) + phase * 101.0);
                half isBurst = step(0.45, burstSeed);
                half fast = step(0.5, Hash(floor(t * 14.0) + phase * 13.0));
                half dip = lerp(1.0, 0.12 + 0.88 * fast, isBurst);
                // 평소에도 미세하게 떨림
                half hum = 0.9 + 0.1 * sin(t * 47.0);
                return dip * hum;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half4 color = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _Color;

                half breathe = 1.0 + _BreatheStrength * sin(_Time.y * 2.2 + input.phase * 6.2832);
                half openBrightness = _Intensity * breathe;
                half lockedBrightness = _LockedIntensity * BrokenFlicker(input.phase);

                half grey = dot(color.rgb, half3(0.299, 0.587, 0.114));
                half3 lockedRgb = lerp(color.rgb, grey.xxx, _LockedDesaturate);

                half3 rgb = lerp(color.rgb * openBrightness, lockedRgb * lockedBrightness, _Locked);
                half alpha = color.a * lerp(1.0, saturate(0.5 + lockedBrightness), _Locked);

                return half4(MixFog(rgb, input.fogFactor), alpha);
            }
            ENDHLSL
        }
    }
}
