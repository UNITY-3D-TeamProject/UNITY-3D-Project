// 튜토리얼 "데이터 소각장" 절차적 스카이박스 (URP). 시안 F1 "네온 오로라"를 움직이게 옮긴 것이다.
// 레이어: 기본 그라데이션 / 픽셀 정렬 오로라 3단(일렁임 + 줄기를 타고 내려가는 밝은 덩어리) / 떠오르는 픽셀 먼지
//         / 지평선 아래 어두운 바닥 + 하늘 반사(물결)
// 떨어지며 분해되는 파일은 스카이박스가 아니라 파티클(VFX_FallingFiles)이 맡는다. 스카이박스는 무한히 멀어 시차가 없기 때문이다.
// 각도 단위: 위도(lat)는 도(deg), 방위(u)는 0~1 (u = 0.5 가 +Z).
Shader "Project/TutorialSkybox"
{
    Properties
    {
        [Header(Sky Colors)]
        _ZenithColor ("Zenith (top)", Color) = (0.008, 0.004, 0.016, 1)
        _SkyColor ("Sky (mid)", Color) = (0.02, 0.016, 0.04, 1)
        _HorizonColor ("Horizon", Color) = (0.043, 0.027, 0.07, 1)
        _FloorHorizonColor ("Floor (near horizon)", Color) = (0.07, 0.04, 0.11, 1)
        _FloorColor ("Floor (bottom)", Color) = (0.012, 0.008, 0.02, 1)
        [HDR] _GlowColor ("Horizon Glow", Color) = (1, 0.184, 0.627, 1)
        _GlowStrength ("Horizon Glow Strength", Range(0, 2)) = 0.25

        [Header(Pixel Sort Aurora)]
        [HDR] _AuroraTopColor ("Aurora Top", Color) = (1, 0.184, 0.627, 1)
        [HDR] _AuroraMidColor ("Aurora Mid", Color) = (0.541, 0.294, 1, 1)
        [HDR] _AuroraBottomColor ("Aurora Bottom", Color) = (0.188, 0.878, 1, 1)
        _AuroraStrength ("Aurora Strength", Range(0, 3)) = 0.85
        _AuroraBins ("Columns (around 360 deg)", Float) = 640
        _AuroraSteps ("Color Steps per Column", Float) = 14
        _AuroraSpeed ("Motion Speed", Range(0, 4)) = 1
        _Curtain1 ("Curtain 1 (center deg, wave deg, thickness deg, gain)", Vector) = (22, 10, 14, 1)
        _Curtain2 ("Curtain 2 (center deg, wave deg, thickness deg, gain)", Vector) = (55, 8, 10, 0.55)
        _Curtain3 ("Curtain 3 (center deg, wave deg, thickness deg, gain)", Vector) = (78, 4, 6, 0.3)

        [Header(Pixel Dust)]
        [HDR] _DustColorA ("Dust Color A", Color) = (0.188, 0.878, 1, 1)
        [HDR] _DustColorB ("Dust Color B", Color) = (1, 0.184, 0.627, 1)
        _DustStrength ("Dust Strength", Range(0, 3)) = 1
        _DustCells ("Dust Cells (around 360 deg)", Float) = 1400
        _DustDensity ("Dust Density", Range(0, 0.1)) = 0.012
        _DustRiseSpeed ("Dust Rise Speed (deg/s)", Float) = 0.35

        [Header(Floor Reflection)]
        _ReflectionStrength ("Sky Reflection", Range(0, 1)) = 0.35
        _RippleAmount ("Reflection Ripple", Range(0, 0.02)) = 0.0008
        _RippleSpeed ("Reflection Ripple Speed", Float) = 1

        [Header(Global)]
        _TimeScale ("Time Scale", Float) = 1
        _Exposure ("Exposure", Float) = 1
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Background"
            "Queue" = "Background"
            "PreviewType" = "Skybox"
            "RenderPipeline" = "UniversalPipeline"
        }

        Cull Off
        ZWrite Off

        Pass
        {
            Name "Skybox"

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            #define TAU 6.28318530718

            CBUFFER_START(UnityPerMaterial)
                half4 _ZenithColor;
                half4 _SkyColor;
                half4 _HorizonColor;
                half4 _FloorHorizonColor;
                half4 _FloorColor;
                half4 _GlowColor;
                float _GlowStrength;
                half4 _AuroraTopColor;
                half4 _AuroraMidColor;
                half4 _AuroraBottomColor;
                float _AuroraStrength;
                float _AuroraBins;
                float _AuroraSteps;
                float _AuroraSpeed;
                float4 _Curtain1;
                float4 _Curtain2;
                float4 _Curtain3;
                half4 _DustColorA;
                half4 _DustColorB;
                float _DustStrength;
                float _DustCells;
                float _DustDensity;
                float _DustRiseSpeed;
                float _ReflectionStrength;
                float _RippleAmount;
                float _RippleSpeed;
                float _TimeScale;
                float _Exposure;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 direction : TEXCOORD0;
            };

            // ---------- 유틸 ----------

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

            // x 방향이 period 칸마다 이어지는 노이즈 (방위각 이음매가 보이지 않게 한다)
            float PeriodicNoise(float2 p, float period)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - (2.0 * f));

                float x0 = fmod(i.x, period);
                float x1 = fmod(i.x + 1.0, period);
                float a = Hash21(float2(x0, i.y));
                float b = Hash21(float2(x1, i.y));
                float c = Hash21(float2(x0, i.y + 1.0));
                float d = Hash21(float2(x1, i.y + 1.0));
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            // 방위(u)를 따라 한 바퀴 이어지는 3옥타브 노이즈. y 는 시간으로 흘려 천천히 모양이 바뀌게 한다.
            float PeriodicFbm(float u, float period, float y, float seed)
            {
                float total = 0.0;
                float amp = 1.0;
                float norm = 0.0;
                for (int o = 0; o < 3; o++)
                {
                    total += amp * PeriodicNoise(float2(u * period, y + seed), period);
                    norm += amp;
                    amp *= 0.5;
                    period *= 2.0;
                    y *= 2.0;
                }
                return total / norm;
            }

            // ---------- 레이어 ----------

            // 픽셀 정렬 오로라 한 단: 방위를 _AuroraBins 칸으로 나누고 칸마다 세로 줄기 하나.
            // 줄기 위아래 끝이 숨 쉬듯 늘었다 줄고, 밝기가 깜빡이며, 가끔 밝은 덩어리가 줄기를 타고 내려간다.
            float3 Curtain(float u, float lat, float t, float4 param, float seed)
            {
                float bin = floor(u * _AuroraBins);
                float binU = (bin + 0.5) / _AuroraBins;

                float centre = param.x + (param.y * ((PeriodicFbm(binU, 12.0, t * 0.03, seed) - 0.5) * 2.0));
                float gap = smoothstep(0.35, 0.6, PeriodicFbm(binU, 18.0, t * 0.05, seed + 7.0));

                float h1 = Hash11((bin * 1.31) + seed);
                float h2 = Hash11((bin * 2.17) + seed + 3.1);
                float h3 = Hash11((bin * 0.73) + seed + 5.7);
                float h4 = Hash11((bin * 1.93) + seed + 9.3);

                float top = centre + (param.z * (0.2 + (0.8 * h1)) * (0.85 + (0.15 * sin((t * (0.6 + h3)) + (h3 * TAU)))));
                float bottom = centre - (param.z * (0.1 + (0.7 * h2)) * (0.85 + (0.15 * sin((t * (0.8 + h1)) + (h2 * TAU)))));
                float inside = step(bottom, lat) * step(lat, top);

                // s: 줄기 위 끝 0 -> 아래 끝 1, 계단으로 끊어 픽셀 정렬 느낌을 낸다.
                float s = saturate((top - lat) / max(top - bottom, 0.001));
                s = floor(s * _AuroraSteps) / _AuroraSteps;
                float3 color = (s < 0.55)
                    ? lerp(_AuroraTopColor.rgb, _AuroraMidColor.rgb, s / 0.55)
                    : lerp(_AuroraMidColor.rgb, _AuroraBottomColor.rgb, (s - 0.55) / 0.45);

                float flicker = 0.75 + (0.25 * sin((t * (1.5 + (2.0 * h4))) + (h4 * TAU)));
                float flow = frac((t * (0.08 + (0.12 * h3))) + h2);
                float pulse = (1.0 - smoothstep(0.0, 0.12, abs(s - flow))) * step(0.5, h4);

                float brightness = (0.3 + (0.7 * h4)) * gap * param.w * flicker;
                return color * inside * brightness * (0.35 + (0.65 * s) + (pulse * 0.8));
            }

            // 떠오르는 픽셀 먼지: 위도/방위 격자 칸마다 드물게 네모 하나, 반짝인다.
            float3 Dust(float u, float lat, float t)
            {
                float cellDeg = 360.0 / _DustCells;
                float2 p = float2(u * _DustCells, (lat - (t * _DustRiseSpeed)) / cellDeg);
                float2 cell = floor(p);
                float2 local = frac(p);

                float h = Hash21(cell);
                float exists = step(1.0 - _DustDensity, h);
                float square = step(0.15, local.x) * step(local.x, 0.85) * step(0.15, local.y) * step(local.y, 0.85);
                float h2 = Hash21(cell + 17.3);
                float twinkle = 0.5 + (0.5 * sin((t * (1.0 + (3.0 * h2))) + (h2 * TAU)));
                float fade = smoothstep(-5.0, 2.0, lat) * (1.0 - smoothstep(45.0, 65.0, lat));

                float3 color = lerp(_DustColorA.rgb, _DustColorB.rgb, step(0.5, h2));
                color = lerp(color, float3(1.0, 1.0, 1.0), step(0.85, Hash21(cell + 4.9)));
                return color * exists * square * twinkle * fade;
            }

            // 지평선 위 하늘 (배경 + 오로라 + 먼지). 바닥 반사에서도 같은 함수를 쓴다.
            float3 Sky(float u, float lat, float t)
            {
                float y = sin(radians(max(lat, 0.0)));
                float3 color = lerp(_HorizonColor.rgb, _SkyColor.rgb, saturate((y - 0.02) / 0.48));
                color = lerp(color, _ZenithColor.rgb, saturate((y - 0.5) / 0.5));

                float auroraTime = t * _AuroraSpeed;
                float3 aurora = Curtain(u, lat, auroraTime, _Curtain1, 1.0)
                    + Curtain(u, lat, auroraTime, _Curtain2, 2.0)
                    + Curtain(u, lat, auroraTime, _Curtain3, 3.0);
                color += aurora * _AuroraStrength;
                color += Dust(u, lat, t) * _DustStrength;
                return color;
            }

            // ---------- 본체 ----------

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.direction = input.positionOS.xyz;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float3 dir = normalize(input.direction);
                float t = _Time.y * _TimeScale;
                float u = atan2(dir.x, dir.z) / TAU + 0.5;
                float lat = degrees(asin(clamp(dir.y, -1.0, 1.0)));

                float3 color;
                if (dir.y >= -0.012)
                {
                    color = Sky(u, lat, t);
                }
                else
                {
                    // 바닥: 아래로 갈수록 어두운 그라데이션 + 물결 진 하늘 반사
                    float3 floorColor = lerp(_FloorHorizonColor.rgb, _FloorColor.rgb, saturate((-dir.y - 0.012) / 0.988));
                    float rippleU = u + (sin((lat * 0.45) + (t * _RippleSpeed)) * _RippleAmount);
                    float3 reflection = Sky(frac(rippleU), -lat, t);
                    color = lerp(floorColor, reflection, _ReflectionStrength * smoothstep(-0.7, -0.012, dir.y));
                }

                color += _GlowColor.rgb * exp(-(dir.y / 0.03) * (dir.y / 0.03)) * _GlowStrength;
                return half4(color * _Exposure, 1.0);
            }
            ENDHLSL
        }
    }
}
