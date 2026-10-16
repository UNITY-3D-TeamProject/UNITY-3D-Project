// 로비 거리 간판 (URP, 조명 없음). 텍스처를 Intensity 배로 밝혀 Bloom 을 받게 하고, 가벼운 움직임을 GPU 에서 처리한다.
// - 스캔라인: 가는 가로줄 + 위에서 아래로 지나가는 밝은 띠 (UV 기준)
// - 깜빡임: 가끔 어두워지는 네온 깜빡임. 오브젝트 위치로 위상을 달리해 간판끼리 같이 깜빡이지 않는다.
// - 둥실: 오브젝트 공간 Y 로 위아래 흔들림
// - 회전: UV 를 (0.5, 0.5) 중심으로 돌림 (홀로그램 링)
// 블렌드·깊이 쓰기·컬링은 머티리얼에서 정한다 (불투명 간판 / 반투명 네온).
Shader "Project/StreetSign"
{
    Properties
    {
        _BaseMap ("Texture", 2D) = "white" {}
        _Intensity ("Intensity", Range(0, 4)) = 1.5
        _ScanlineStrength ("Scanline Strength", Range(0, 1)) = 0
        _FlickerStrength ("Flicker Strength", Range(0, 1)) = 0
        _BobHeight ("Bob Height (Object Space)", Float) = 0
        _BobSpeed ("Bob Speed", Float) = 1.5
        _RotateSpeed ("UV Rotate Speed (rad/s)", Float) = 0

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
            "Queue" = "Geometry"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "Unlit"
            Blend [_SrcBlend] [_DstBlend]
            ZWrite [_ZWrite]
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
                half _Intensity;
                half _ScanlineStrength;
                half _FlickerStrength;
                float _BobHeight;
                float _BobSpeed;
                float _RotateSpeed;
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

            // 오브젝트 월드 위치로 0~1 위상을 만든다.
            float ObjectPhase()
            {
                float3 origin = float3(UNITY_MATRIX_M._m03, UNITY_MATRIX_M._m13, UNITY_MATRIX_M._m23);
                return frac(sin(dot(origin, float3(12.9898, 78.233, 37.719))) * 43758.5453);
            }

            Varyings Vert(Attributes input)
            {
                Varyings output;
                float phase = ObjectPhase();

                float3 positionOS = input.positionOS.xyz;
                positionOS.y += sin(_Time.y * _BobSpeed + phase * 6.2832) * _BobHeight;

                float2 uv = input.uv - 0.5;
                float angle = _Time.y * _RotateSpeed;
                float s = sin(angle);
                float c = cos(angle);
                uv = float2(uv.x * c - uv.y * s, uv.x * s + uv.y * c) + 0.5;

                output.positionCS = TransformObjectToHClip(positionOS);
                output.uv = TRANSFORM_TEX(uv, _BaseMap);
                output.phase = phase;
                output.fogFactor = ComputeFogFactor(output.positionCS.z);
                return output;
            }

            half Flicker(float phase)
            {
                // 약 5초마다 한 번, 짧게 두 번 끊긴다.
                float t = frac(_Time.y * 0.2 + phase);
                half cut = step(0.62, t) * step(t, 0.635) + step(0.66, t) * step(t, 0.67);
                return 1.0 - cut * 0.75;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half4 color = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                half brightness = _Intensity;

                // 스캔라인: 가는 줄 + 아래로 지나가는 띠
                half lines = 0.5 + 0.5 * sin(input.uv.y * 900.0);
                float bandPos = frac(_Time.y * 0.22 + input.phase);
                half band = saturate(1.0 - abs(frac(input.uv.y) - (1.0 - bandPos)) * 8.0);
                brightness *= 1.0 + _ScanlineStrength * (band * 0.8 - lines * 0.25);

                brightness *= lerp(1.0, Flicker(input.phase), _FlickerStrength);

                color.rgb = MixFog(color.rgb * brightness, input.fogFactor);
                return color;
            }
            ENDHLSL
        }
    }
}
