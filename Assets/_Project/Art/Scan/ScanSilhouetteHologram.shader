// 스캔된 벽 뒤 아이템에 씌우는 홀로그램 실루엣 셰이더. 벽에 가려진 부분(ZTest Greater)만 그린다.
// 옅은 면 + 가장자리 빛 + 화면 가로줄 + 위로 지나가는 밝은 띠 + 가끔 깜빡임을 가산으로 더해 벽 너머 홀로그램처럼 보이게 한다.
// 대상과 같은 메시를 쓰는 자식 렌더러에 지정하고, SilhouetteReaction 으로 스캔 중에만 켜고 _Color 알파로 페이드한다.
Shader "Project/ScanSilhouetteHologram"
{
    Properties
    {
        [HDR] _Color ("Color", Color) = (0.3, 1.2, 0.6, 1.0)
        _FillAlpha ("Fill Alpha", Range(0, 1)) = 0.2
        _RimPower ("Rim Power", Float) = 2
        _RimStrength ("Rim Strength", Range(0, 2)) = 1

        [Header(Scan Lines)]
        _LinePeriod ("Line Period (px)", Float) = 4
        _LineDim ("Line Dim", Range(0, 1)) = 0.4

        [Header(Sweep)]
        _SweepSpacing ("Sweep Spacing (m)", Float) = 3
        _SweepWidth ("Sweep Width (m)", Float) = 0.15
        _SweepSpeed ("Sweep Speed (m/s)", Float) = 1.2
        _SweepStrength ("Sweep Strength", Range(0, 2)) = 0.8

        [Header(Flicker)]
        _FlickerRate ("Flicker Rate (회 / s)", Float) = 15
        _FlickerStrength ("Flicker Strength", Range(0, 1)) = 0.35
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
            Name "Silhouette"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha One
            ZWrite Off
            ZTest Greater
            Cull Back

            // ZTest Greater 는 앞을 가린 것이 플레이어여도 그리므로, PlayerStencilMask 가 찍은 플레이어 픽셀(비트 128)은 건너뛴다
            Stencil
            {
                Ref 128
                ReadMask 128
                Comp NotEqual
            }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "ScanHologramEffects.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                float _FillAlpha;
                float _RimPower;
                float _RimStrength;
                float _LinePeriod;
                float _LineDim;
                float _SweepSpacing;
                float _SweepWidth;
                float _SweepSpeed;
                float _SweepStrength;
                float _FlickerRate;
                float _FlickerStrength;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // 가장자리 빛: 비스듬히 보이는 면일수록 밝아 형태가 또렷하다
                float3 viewDirection = normalize(GetWorldSpaceViewDir(input.positionWS));
                float rim = pow(1.0 - saturate(dot(normalize(input.normalWS), viewDirection)), _RimPower) * _RimStrength;

                float sweep = ScanSweep(input.positionWS.y, _SweepSpacing, _SweepWidth, _SweepSpeed) * _SweepStrength;
                float lines = ScanLines(input.positionCS.y, _LinePeriod, _LineDim);
                float flicker = ScanFlicker(_FlickerRate, _FlickerStrength);

                half alpha = saturate(_FillAlpha + rim + sweep) * lines * flicker * _Color.a;
                return half4(_Color.rgb, alpha);
            }
            ENDHLSL
        }
    }
}
