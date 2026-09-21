// 소각로 입구 열기 왜곡 + 글리치 셰이더.
// 화면 뒤의 장면(_CameraOpaqueTexture)을 노이즈로 일렁이게 하고, 가끔 가로 띠를 좌우로 밀며 시안 색조를 더한다.
// 주의: 사용 중인 URP 에셋에서 Opaque Texture가 켜져 있어야 한다. (PC_RPAsset은 켜져 있음, Mobile_RPAsset은 꺼져 있음)
Shader "Project/HeatDistortion"
{
    Properties
    {
        _Strength ("Distortion Strength", Float) = 0.012
        _Speed ("Rise Speed", Float) = 0.8
        _GlitchChance ("Glitch Chance", Range(0, 0.5)) = 0.06
        _GlitchStrength ("Glitch Strength", Float) = 0.03
        [HDR] _GlitchTint ("Glitch Tint", Color) = (0.1, 0.9, 1, 1)
        _Alpha ("Alpha", Range(0, 1)) = 1
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent+10"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            Name "Unlit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _Strength;
                float _Speed;
                float _GlitchChance;
                float _GlitchStrength;
                half4 _GlitchTint;
                float _Alpha;
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

            float Hash11(float x)
            {
                return frac(sin(x * 127.1) * 43758.5453);
            }

            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float Noise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - (2.0 * f));

                float a = Hash21(i);
                float b = Hash21(i + float2(1.0, 0.0));
                float c = Hash21(i + float2(0.0, 1.0));
                float d = Hash21(i + float2(1.0, 1.0));
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // 원형 마스크: 가장자리로 갈수록 효과가 사라진다.
                float radius = length(input.uv - 0.5) * 2.0;
                float mask = 1.0 - smoothstep(0.7, 1.0, radius);

                // 열기 일렁임: 위로 흐르는 노이즈로 화면 샘플 위치를 흔든다.
                float2 flowUV = float2(0.0, -_Time.y * _Speed);
                float noiseX = Noise((input.uv * float2(5.0, 3.0)) + flowUV) - 0.5;
                float noiseY = Noise((input.uv * float2(4.0, 6.0)) + float2(3.7, 0.0) + (flowUV * 1.3)) - 0.5;
                float2 offset = float2(noiseX, noiseY) * _Strength * mask;

                // 글리치: 가끔 가로 띠가 좌우로 밀린다.
                float band = floor(input.uv.y * 24.0);
                float tick = floor(_Time.y * 6.0);
                float trigger = step(1.0 - _GlitchChance, Hash11(band + (tick * 13.0)));
                offset.x += trigger * (Hash11((band * 1.7) + tick) - 0.5) * _GlitchStrength * mask;

                float2 screenUV = GetNormalizedScreenSpaceUV(input.positionCS) + offset;
                half3 scene = SampleSceneColor(screenUV);
                scene += _GlitchTint.rgb * trigger * mask * 0.5;

                return half4(scene, mask * _Alpha);
            }
            ENDHLSL
        }
    }
}
