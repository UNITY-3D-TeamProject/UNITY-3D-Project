// 홀로그램 사인용 셰이더: 스캔라인 + 깜빡임 + 가끔 가로 띠가 밀리는 글리치. (가산 블렌드)
Shader "Project/HologramSign"
{
    Properties
    {
        _MainTex ("Sign Texture", 2D) = "white" {}
        [HDR] _Color ("Tint", Color) = (1.6, 0.56, 0.16, 1)
        _ScanlineDensity ("Scanline Density", Float) = 120
        _ScanlineSpeed ("Scanline Speed", Float) = 2
        _FlickerSpeed ("Flicker Speed", Float) = 18
        _GlitchAmount ("Glitch Amount", Range(0, 2)) = 1
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

        Blend SrcAlpha One
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

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4 _Color;
                float _ScanlineDensity;
                float _ScanlineSpeed;
                float _FlickerSpeed;
                float _GlitchAmount;
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

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 uv = input.uv;

                // 글리치: 가끔 특정 가로 띠가 좌우로 밀린다.
                float band = floor(uv.y * 16.0);
                float tick = floor(_Time.y * 8.0);
                float trigger = step(0.93, Hash11(band + (tick * 17.0)));
                uv.x += trigger * (Hash11((band * 3.1) + tick) - 0.5) * 0.1 * _GlitchAmount;

                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);

                float scan = 0.7 + (0.3 * sin((uv.y * _ScanlineDensity) - (_Time.y * _ScanlineSpeed)));
                float flicker = 0.85 + (0.15 * sin(_Time.y * _FlickerSpeed) * sin(_Time.y * 7.3));

                half3 rgb = tex.rgb * _Color.rgb * scan * flicker;
                return half4(rgb, tex.a * _Color.a);
            }
            ENDHLSL
        }
    }
}
