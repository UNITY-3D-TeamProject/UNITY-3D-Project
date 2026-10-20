// 시스템 심연 3D 하늘 — 고리 모듈 블록 (어두운 반투명 유리 + 빛 테두리 + 잔 패널 선).
// 메시(AbyssSkyMeshBuilder): uv0 = 면 안 좌표(m), uv1 = 면 크기(m), 색 r = 밝기 난수, b = 고리 둘레 위치(0~1).
// 전역 _HoloDarkness 면 사라지고, _HoloAlarm 이면 붉어지며 켜진 패널은 호박색으로 바뀐다. (HoloCommon.hlsl)
Shader "Project/AbyssModule"
{
    Properties
    {
        _BodyColor ("Body Color (A = 불투명도)", Color) = (0.008, 0.04, 0.055, 0.6)
        [HDR] _EdgeColor ("Edge Color", Color) = (0.3, 1.1, 1.4, 1)
        [HDR] _AccentColor ("Lit Panel Color", Color) = (0.2, 0.75, 1.0, 1)
        [HDR] _AlarmAccentColor ("Alarm Lit Panel Color (호박색)", Color) = (1.6, 0.7, 0.12, 1)
        _EdgeWidth ("Edge Width (px)", Range(0.5, 4)) = 1.4
        _PanelSize ("Panel Size (m)", Float) = 3
        _PanelStrength ("Panel Line Strength", Range(0, 1)) = 0.2
        _LitPanelChance ("Lit Panel Chance", Range(0, 0.5)) = 0.06
        _FresnelStrength ("Fresnel Strength", Range(0, 2)) = 0.35
        _PulseCount ("Pulses Per Ring", Float) = 3
        _PulseSpeed ("Pulse Speed", Float) = 0.02
        _PulseStrength ("Pulse Strength", Range(0, 4)) = 1.5
        _HazeColor ("Haze Color", Color) = (0.03, 0.12, 0.16, 1)
        _HazeDistance ("Haze Distance (m)", Float) = 900
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }
        Blend One OneMinusSrcAlpha
        ZWrite On
        Cull Back

        Pass
        {
            Name "Unlit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Assets/_Project/Art/Tutorial/Shaders/HoloCommon.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BodyColor;
                half4 _EdgeColor;
                half4 _AccentColor;
                half4 _AlarmAccentColor;
                float _EdgeWidth;
                float _PanelSize;
                float _PanelStrength;
                float _LitPanelChance;
                float _FresnelStrength;
                float _PulseCount;
                float _PulseSpeed;
                float _PulseStrength;
                half4 _HazeColor;
                float _HazeDistance;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                float2 size : TEXCOORD1;
                half4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float2 size : TEXCOORD1;
                float3 positionWS : TEXCOORD2;
                float3 normalWS : TEXCOORD3;
                half4 color : COLOR;
            };

            float Hash21(float2 p) { return frac(sin(dot(p, float2(12.9898, 78.233))) * 43758.5453); }

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = input.uv;
                output.size = input.size;
                output.color = input.color;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float3 viewDir = GetWorldSpaceNormalizeViewDir(input.positionWS);
                float fresnel = pow(1.0 - abs(dot(normalize(input.normalWS), viewDir)), 3.0);

                // 면 테두리: 화면에서 일정한 굵기(px)
                float2 p = input.uv;
                float2 pixel = max(fwidth(p), 1e-4);
                float2 edgeDist = min(p, input.size - p) / pixel;
                float edge = 1.0 - smoothstep(0.0, _EdgeWidth, min(edgeDist.x, edgeDist.y));

                // 잔 패널 선 + 몇몇 패널만 켜짐. 멀어서 선이 화소보다 촘촘해지면 흐리게.
                float2 cellPos = p / _PanelSize;
                float2 lineDist = (0.5 - abs(frac(cellPos) - 0.5)) * _PanelSize / pixel;
                float panelFade = saturate(1.0 - (max(pixel.x, pixel.y) / _PanelSize) * 4.0);
                float panel = (1.0 - smoothstep(0.0, 1.0, min(lineDist.x, lineDist.y))) * panelFade;
                float panelLit = step(1.0 - _LitPanelChance, Hash21(floor(cellPos) + (input.color.r * 91.7)));

                // 고리 둘레를 따라 도는 빛 펄스
                float pulse = pow(frac((input.color.b * _PulseCount) - (_Time.y * _PulseSpeed)), 24.0);
                float brightness = 0.55 + (0.45 * input.color.r);

                half3 emissive = _EdgeColor.rgb * ((edge * brightness * (1.0 + (pulse * _PulseStrength)))
                    + (fresnel * _FresnelStrength * 0.5) + (panel * _PanelStrength));
                half3 accent = lerp(_AccentColor.rgb, _AlarmAccentColor.rgb, saturate(_HoloAlarm));
                half3 accentLight = accent * panelLit * 0.25 * panelFade * (1.0 - edge);

                // 멀수록 대기에 묻힌다
                float distance = length(input.positionWS - GetCameraPositionWS());
                float haze = pow(saturate(distance / _HazeDistance), 1.5);
                half3 body = lerp(_BodyColor.rgb, _HazeColor.rgb, haze);
                float alpha = saturate(_BodyColor.a + (fresnel * 0.3));
                emissive *= 1.0 - (haze * 0.75);
                accentLight *= 1.0 - (haze * 0.75);

                float visibility = HoloVisibility(0.0);
                half3 color = (HoloAlarmTint(body) * alpha) + HoloAlarmTint(emissive) + accentLight;
                return half4(color * visibility, alpha * visibility);
            }
            ENDHLSL
        }
    }
}
