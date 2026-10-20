// 튜토리얼 경고 하늘 — 회로 돔 (시안 홀로그램 톤, 시안 K2). 글자 없이 모양으로만 경고한다.
// 아주 어두운 남색 바탕 + 지평선 시안 빛 + 바닥 홀로 격자
// + 하늘 전체를 덮는 회로 기판 돔(가로/세로 선·노드, 선을 따라 지평선 쪽으로 흐르는 빛 펄스)
// + 몇몇 구획이 호박색으로 점멸하는 경고.
// 전역 _HoloDarkness 가 1 이면 완전히 검고(드러나는 동안 서서히 밝아짐), _HoloAlarm 이 1 이면 전체가 붉어진다. (HoloCommon.hlsl)
// 시안 미리보기: docs/design/tutorial-skybox/concepts.html (K2)
Shader "Project/HoloWarningSkybox"
{
    Properties
    {
        [Header(Base)]
        _TopColor ("Top Color", Color) = (0.005, 0.008, 0.02, 1)
        _HorizonColor ("Horizon Color", Color) = (0.02, 0.09, 0.14, 1)
        _BottomColor ("Bottom Color", Color) = (0.004, 0.01, 0.02, 1)
        [HDR] _HorizonGlow ("Horizon Glow", Color) = (0.12, 0.55, 0.75, 1)
        _Exposure ("Exposure", Range(0, 3)) = 1

        [Header(Grid)]
        [HDR] _GridColor ("Grid Color", Color) = (0.15, 0.7, 1.0, 1)
        _GridScale ("Grid Scale", Float) = 6

        [Header(Circuit Dome)]
        [HDR] _CircuitColor ("Circuit Color", Color) = (0.25, 0.85, 1.0, 1)
        _CircuitColumns ("Columns (방위각)", Float) = 64
        _CircuitRows ("Rows (고도)", Float) = 22
        _CircuitStrength ("Line Strength", Range(0, 2)) = 0.4
        _PulseStrength ("Pulse Strength", Range(0, 4)) = 1.8
        _PulseSpeed ("Pulse Speed", Float) = 0.25

        [Header(Warning Sectors)]
        [HDR] _WarningColor ("Warning Color", Color) = (1.6, 0.75, 0.15, 1)
        _WarningChance ("Warning Chance", Range(0, 1)) = 0.14
        _WarningBlink ("Warning Blink Speed", Float) = 2
    }

    SubShader
    {
        Tags { "Queue" = "Background" "RenderType" = "Background" "PreviewType" = "Skybox" "RenderPipeline" = "UniversalPipeline" }
        Cull Off
        ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Assets/_Project/Art/Tutorial/Shaders/HoloCommon.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _TopColor;
                half4 _HorizonColor;
                half4 _BottomColor;
                half4 _HorizonGlow;
                float _Exposure;
                half4 _GridColor;
                float _GridScale;
                half4 _CircuitColor;
                float _CircuitColumns;
                float _CircuitRows;
                float _CircuitStrength;
                float _PulseStrength;
                float _PulseSpeed;
                half4 _WarningColor;
                float _WarningChance;
                float _WarningBlink;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 dir : TEXCOORD0; };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.dir = input.positionOS.xyz;
                return output;
            }

            float Hash21(float2 p) { return frac(sin(dot(p, float2(12.9898, 78.233))) * 43758.5453); }

            half4 frag(Varyings input) : SV_Target
            {
                float3 d = normalize(input.dir);
                float y = d.y;
                float u = (atan2(d.z, d.x) / 6.2831853) + 0.5;   // 방위각 0 ~ 1

                // 바탕
                half3 col = (y >= 0.0) ? lerp(_HorizonColor.rgb, _TopColor.rgb, saturate(pow(y, 0.45))) : lerp(_HorizonColor.rgb, _BottomColor.rgb, saturate(pow(-y, 0.35)));
                col += _HorizonGlow.rgb * exp(-abs(y) * 18.0) * 0.6;

                // 바닥 홀로 격자 (원근)
                if (y < -0.001)
                {
                    float2 g = (d.xz / -y) * _GridScale * 0.1;
                    float2 f = abs(frac(g) - 0.5);
                    float gridLine = 1.0 - smoothstep(0.0, 0.03 + (0.02 / max(-y * 8.0, 0.2)), min(f.x, f.y));
                    float fade = saturate(-y * 5.0) * exp(length(g) * -0.02);
                    col += _GridColor.rgb * gridLine * fade * 0.35;
                }

                // 회로 돔: 칸마다 가로 또는 세로 선, 가끔 노드. 선을 따라 지평선 쪽으로 빛 펄스가 흐른다.
                if (y > 0.02)
                {
                    float2 g = float2(u * _CircuitColumns, pow(y, 0.7) * _CircuitRows);
                    float2 id = floor(g);
                    float2 f = frac(g);
                    float r = Hash21(id);

                    float lineH = (1.0 - smoothstep(0.0, 0.06, abs(f.y - 0.5))) * step(0.45, r);
                    float lineV = (1.0 - smoothstep(0.0, 0.06, abs(f.x - 0.5))) * step(r, 0.55);
                    float node = (1.0 - smoothstep(0.08, 0.14, length(f - 0.5))) * step(0.8, Hash21(id + 7.0));
                    float wire = max(max(lineH, lineV) * 0.6, node);

                    float pulse = pow(frac((-g.y * 0.08) + (_Time.y * _PulseSpeed) + r), 18.0);
                    float fade = saturate(y * 4.0) * (1.0 - (y * 0.6));

                    // 경고: 큰 구획 몇 개가 호박색으로 점멸 (구획은 시간에 따라 바뀜)
                    float2 sector = floor(float2(u * 8.0, y * 4.0));
                    float isWarning = step(1.0 - _WarningChance, Hash21(sector + floor(_Time.y * 0.7)));
                    float blink = (step(0.5, frac(_Time.y * _WarningBlink)) * 0.8) + 0.2;
                    half3 lineColor = lerp(_CircuitColor.rgb, _WarningColor.rgb, isWarning * blink);

                    col += lineColor * wire * fade * (_CircuitStrength + (pulse * _PulseStrength));
                }

                col *= _Exposure * (1.0 - saturate(_HoloDarkness));   // 어둠 동안은 하늘 전체를 끈다
                return half4(HoloAlarmTint(col), 1.0);
            }
            ENDHLSL
        }
    }
}
