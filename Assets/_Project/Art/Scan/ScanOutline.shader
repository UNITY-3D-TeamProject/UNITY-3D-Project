// 스캔된 상호작용 오브젝트에 씌우는 외곽선 셰이더. 대상의 바깥 테두리만 _Color 로 그리고, 그 바깥에 옅은 빛 번짐을 더한다.
// 대상과 같은 메시를 쓰는 자식 렌더러에 지정하고, SilhouetteReaction 으로 스캔 중에만 켜고 _Color 알파로 페이드한다.
// 깊이 테스트를 하므로 벽에 가려진 부분은 그리지 않는다(가려지지 않았을 때만 외곽선이 보인다).
// 1) Mask 패스가 대상이 보이는 픽셀에 스텐실을 찍고, 2) Outline 패스가 부풀린 메시를 스텐실이 없는 곳(테두리)에만 그리고,
// 3) Glow 패스가 더 넓게 부풀린 메시를 반투명 가산으로 덧그려 빛이 번지는 것처럼 보이게 한다.
// _Color 는 HDR 이므로 화면에 블룸이 켜진 씬에서는 더 밝게 번진다.
// 각진 메시는 법선이 모서리에서 갈라져 외곽선이 끊기므로, SilhouetteReaction 이 매끄러운 법선 메시로 바꿔 준다.
// 외곽선과 빛 번짐에는 홀로그램 연출(화면 가로줄 + 위로 지나가는 밝은 띠 + 가끔 깜빡임)을 입힌다. 함수는 ScanHologramEffects.hlsl.
Shader "Project/ScanOutline"
{
    Properties
    {
        [HDR] _Color ("Color", Color) = (1.4, 0.75, 0.12, 1.0)
        _OutlineWidth ("Outline Width (px)", Float) = 3
        _GlowWidth ("Glow Width (px)", Float) = 9
        _GlowStrength ("Glow Strength", Range(0, 1)) = 0.35
        _DepthPush ("Depth Push (m)", Float) = 0.1
        _StencilRef ("Stencil Ref", Int) = 64

        [Header(Hologram)]
        _LinePeriod ("Line Period (px)", Float) = 3
        _LineDim ("Line Dim", Range(0, 1)) = 0.15
        _SweepSpacing ("Sweep Spacing (m)", Float) = 3
        _SweepWidth ("Sweep Width (m)", Float) = 0.15
        _SweepSpeed ("Sweep Speed (m/s)", Float) = 1.2
        _SweepStrength ("Sweep Strength", Range(0, 2)) = 1
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

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "ScanHologramEffects.hlsl"

        CBUFFER_START(UnityPerMaterial)
            half4 _Color;
            float _OutlineWidth;
            float _GlowWidth;
            float _GlowStrength;
            float _DepthPush;
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
        };

        // 메시를 화면에서 법선 방향으로 widthPixels 만큼 부풀린 클립 좌표를 돌려준다
        float4 ExtrudedPositionCS(Attributes input, float widthPixels)
        {
            // 바닥처럼 대상이 닿아 있는 면에 테두리가 묻히지 않도록 카메라 쪽으로 조금 당긴다
            float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
            positionWS += normalize(GetWorldSpaceViewDir(positionWS)) * _DepthPush;
            float4 positionCS = TransformWorldToHClip(positionWS);

            // 화면 공간에서 법선 방향으로 밀어야 거리와 상관없이 두께가 픽셀 단위로 일정하다
            float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
            float2 normalCS = TransformWorldToHClipDir(normalWS).xy;
            float normalLength = max(length(normalCS), 1e-5);

            // 픽셀 두께를 NDC(-1~1) 길이로 바꾸고, 원근 나눗셈 전 좌표이므로 w 를 곱한다
            float2 offset = (normalCS / normalLength) * (widthPixels * 2.0 / _ScreenParams.xy) * positionCS.w;
            positionCS.xy += offset;
            return positionCS;
        }

        // 부풀린 메시를 그리는 Outline·Glow 패스 공용 정점 함수
        Varyings ExtrudedVert(Attributes input, float widthPixels)
        {
            Varyings output;
            output.positionCS = ExtrudedPositionCS(input, widthPixels);
            output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
            return output;
        }

        // 홀로그램 연출을 입힌 색. 지나가는 띠에서는 더 밝고 진해지며, 가로줄과 깜빡임만큼 옅어진다
        half4 HologramColor(Varyings input, half alpha)
        {
            float sweep = ScanSweep(input.positionWS.y, _SweepSpacing, _SweepWidth, _SweepSpeed) * _SweepStrength;
            float lines = ScanLines(input.positionCS.y, _LinePeriod, _LineDim);
            float flicker = ScanFlicker(_FlickerRate, _FlickerStrength);
            return half4(_Color.rgb * (1.0 + sweep), saturate(alpha * (1.0 + sweep)) * lines * flicker);
        }
        ENDHLSL

        // 대상이 보이는 픽셀(벽에 가려지지 않은 부분)에만 스텐실을 찍는다. 색은 쓰지 않는다
        Pass
        {
            Name "Mask"
            Tags { "LightMode" = "SRPDefaultUnlit" }

            ColorMask 0
            ZWrite Off
            ZTest LEqual
            Cull Back

            Stencil
            {
                Ref [_StencilRef]
                WriteMask [_StencilRef]
                Comp Always
                Pass Replace
            }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.positionWS = 0;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }

        // 법선 방향으로 부풀린 메시를 스텐실이 없는 곳에만 그려 바깥 테두리만 남긴다
        Pass
        {
            Name "Outline"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual
            Cull Back

            Stencil
            {
                Ref [_StencilRef]
                ReadMask [_StencilRef]
                Comp NotEqual
            }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            Varyings vert(Attributes input)
            {
                return ExtrudedVert(input, _OutlineWidth);
            }

            half4 frag(Varyings input) : SV_Target
            {
                return HologramColor(input, _Color.a);
            }
            ENDHLSL
        }

        // 외곽선보다 넓게 부풀린 메시를 옅게 더해, 테두리 바깥으로 빛이 번지는 것처럼 보이게 한다.
        // 외곽선 위에도 겹쳐 더해지므로 테두리 쪽이 가장 밝고 바깥 띠는 옅다.
        Pass
        {
            Name "Glow"
            Tags { "LightMode" = "UniversalForwardOnly" }

            Blend SrcAlpha One
            ZWrite Off
            ZTest LEqual
            Cull Back

            Stencil
            {
                Ref [_StencilRef]
                ReadMask [_StencilRef]
                Comp NotEqual
            }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            Varyings vert(Attributes input)
            {
                return ExtrudedVert(input, _GlowWidth);
            }

            half4 frag(Varyings input) : SV_Target
            {
                return HologramColor(input, _Color.a * _GlowStrength);
            }
            ENDHLSL
        }
    }
}
