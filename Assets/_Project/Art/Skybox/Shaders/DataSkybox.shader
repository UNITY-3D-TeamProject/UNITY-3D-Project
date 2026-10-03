// "스마트폰 속 데이터 세계" 절차적 스카이박스 (URP).
// 한 셰이더 안에 여러 레이어가 겹쳐 있고, 각 레이어의 세기(Strength)를 0으로 내리면 그 레이어만 꺼진다.
//   기본 그라데이션 + 소각로 방향 주황 기운 / 별(데이터 입자) / 데이터 오로라 / 디지털 레인
//   거대 홈스크린 UI(앱 아이콘, 알림 카드, 와이파이, 배터리) / 하드웨어 도시 스카이라인 / 발광 그리드 지평선
// 팔레트: 어두운 남색 + 시안(디지털) + 주황(열/소각).
// 하늘 방향 규약: 월드 방향 기준이며 _GlowYaw = 0 이 +Z 방향이다. (소각로가 있는 쪽으로 맞춘다)
Shader "Project/DataSkybox"
{
    Properties
    {
        [Header(Colors)]
        _ZenithColor ("Zenith (top)", Color) = (0.008, 0.016, 0.05, 1)
        _HorizonColor ("Horizon", Color) = (0.02, 0.1, 0.18, 1)
        _GroundColor ("Ground (below horizon)", Color) = (0.005, 0.008, 0.02, 1)
        [HDR] _CyanColor ("Cyan (digital)", Color) = (0.1, 0.9, 1, 1)
        [HDR] _OrangeColor ("Orange (heat)", Color) = (1, 0.4, 0.1, 1)

        [Header(Incinerator Glow)]
        _GlowYaw ("Glow Yaw (deg, 0 = +Z)", Range(0, 360)) = 0
        _GlowStrength ("Glow Strength", Range(0, 2)) = 0.6

        [Header(Layer Strength)]
        _StarStrength ("Data Motes (stars)", Range(0, 2)) = 1
        _AuroraStrength ("Data Aurora", Range(0, 2)) = 1
        _RainStrength ("Digital Rain", Range(0, 2)) = 1
        _UIStrength ("Floating Home Screen UI", Range(0, 2)) = 1
        _CityStrength ("Hardware City", Range(0, 1)) = 1
        _GridStrength ("Grid Horizon", Range(0, 2)) = 1

        [Header(Details)]
        _RainColumns ("Rain Columns", Float) = 220
        _GridScale ("Grid Scale", Float) = 1
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
                half4 _HorizonColor;
                half4 _GroundColor;
                half4 _CyanColor;
                half4 _OrangeColor;
                float _GlowYaw;
                float _GlowStrength;
                float _StarStrength;
                float _AuroraStrength;
                float _RainStrength;
                float _UIStrength;
                float _CityStrength;
                float _GridStrength;
                float _RainColumns;
                float _GridScale;
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

            float Hash31(float3 p)
            {
                p = frac(p * 0.1031);
                p += dot(p, p.zyx + 31.32);
                return frac((p.x + p.y) * p.z);
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

            float SdRoundBox(float2 p, float2 halfSize, float radius)
            {
                float2 q = abs(p) - halfSize + radius;
                return length(max(q, 0.0)) + min(max(q.x, q.y), 0.0) - radius;
            }

            float Stroke(float signedDistance, float width, float aa)
            {
                return 1.0 - smoothstep(width, width + aa, abs(signedDistance));
            }

            float Fill(float signedDistance, float aa)
            {
                return 1.0 - smoothstep(0.0, aa, signedDistance);
            }

            // ---------- 레이어 ----------

            // 데이터 입자(별): 방향 격자 셀마다 별 하나
            float3 Stars(float3 dir, float t)
            {
                float3 scaled = dir * 90.0;
                float3 cell = floor(scaled);
                float3 local = frac(scaled);

                float h = Hash31(cell);
                float exists = step(0.988, h);
                float3 starPosition = 0.3 + (0.4 * float3(Hash31(cell + 1.3), Hash31(cell + 2.7), Hash31(cell + 4.1)));
                float star = exists * (1.0 - smoothstep(0.0, 0.2, length(local - starPosition)));
                float twinkle = 0.6 + (0.4 * sin((t * (1.0 + (h * 4.0))) + (h * 100.0)));
                float fade = smoothstep(0.02, 0.35, dir.y);

                return lerp(_CyanColor.rgb, float3(1.0, 1.0, 1.0), h) * star * twinkle * fade;
            }

            // 하늘을 가로지르는 데이터 오로라 (시안 리본 + 옅은 주황 리본)
            float3 Aurora(float u, float elev, float t)
            {
                float az = u * TAU;

                float bandA = 0.85 + (0.18 * sin((az * 2.0) + (t * 0.15))) + (0.07 * sin((az * 5.0) - (t * 0.3))) + (0.03 * sin((az * 11.0) + (t * 0.7)));
                float dA = (elev - bandA) / 0.12;
                float curtainA = exp(-dA * dA) * (0.4 + (0.6 * PeriodicNoise(float2(u * 24.0, (elev * 3.0) - (t * 0.2)), 24.0)));

                float bandB = 0.55 + (0.12 * sin((az * 3.0) - (t * 0.2))) + (0.05 * sin((az * 7.0) + (t * 0.4)));
                float dB = (elev - bandB) / 0.09;
                float curtainB = exp(-dB * dB) * (0.4 + (0.6 * PeriodicNoise(float2(u * 30.0, (elev * 4.0) + (t * 0.25)), 30.0)));

                float elevMask = smoothstep(0.15, 0.4, elev);
                return ((_CyanColor.rgb * curtainA * 0.5) + (_OrangeColor.rgb * curtainB * 0.18)) * elevMask;
            }

            // 디지털 레인: 방위각 열마다 문자 블록이 떨어지고 머리가 밝다
            float3 DigitalRain(float u, float elev, float t)
            {
                float columns = _RainColumns;
                float column = floor(u * columns);
                float xInColumn = frac(u * columns);

                float isActive = step(0.55, Hash11((column * 1.37) + 5.0));
                float speed = lerp(0.08, 0.22, Hash11(column + 7.1));
                float period = lerp(1.2, 2.4, Hash11(column + 3.3));

                float s = frac((elev / period) + (t * speed) + Hash11(column + 11.7));
                float trail = pow(1.0 - s, 3.0);
                float head = pow(1.0 - s, 24.0);

                const float ROWS = 90.0;
                float row = floor(elev * ROWS);
                float glyph = step(0.35, Hash21(float2(column, row + floor(t * 6.0 * Hash11(column + 2.0)))));

                float xMask = 1.0 - smoothstep(0.12, 0.3, abs(xInColumn - 0.5));
                float yMask = 1.0 - smoothstep(0.3, 0.46, abs(frac(elev * ROWS) - 0.5));
                float elevMask = smoothstep(0.02, 0.12, elev) * (1.0 - smoothstep(0.9, 1.4, elev));

                float intensity = isActive * xMask * yMask * elevMask * ((trail * (0.35 + (0.65 * glyph))) + head);
                return lerp(_CyanColor.rgb, float3(1.0, 1.0, 1.0), saturate(head)) * intensity;
            }

            // 거대한 홈스크린 UI: 앱 아이콘 / 알림 카드 / 와이파이 / 배터리가 하늘에 떠다닌다. (rgb = 색, a = 커버리지)
            float4 FloatingUI(float u, float elev, float t, float columns, float rows, float minElev, float maxElev, float drift, float seed)
            {
                float shiftedU = frac(u + (t * drift));
                float v = (elev - minElev) / (maxElev - minElev);

                float2 cellId = float2(floor(shiftedU * columns), floor(v * rows));
                float2 cellUV = float2(frac(shiftedU * columns), frac(v * rows)) - 0.5;

                float cellWidth = (TAU / columns) * cos(elev);
                float cellHeight = (maxElev - minElev) / rows;
                float unit = max(min(cellWidth, cellHeight), 0.0001);

                float h = Hash21(cellId + seed);
                float2 p = float2(cellUV.x * cellWidth, cellUV.y * cellHeight);
                p.y += sin((t * 0.6) + (h * 40.0)) * cellHeight * 0.04;
                p /= unit;

                float aa = max((fwidth(elev) / unit) * 1.5, 0.004);
                float type = Hash21(cellId + seed + 3.7);
                float tint = Hash11((cellId.x * 13.0) + (cellId.y * 71.0) + seed);
                float3 tone = lerp(_CyanColor.rgb, _OrangeColor.rgb, step(tint, 0.15));

                float coverage = 0.0;
                float badge = 0.0;

                if (type < 0.45)
                {
                    // 앱 아이콘
                    float d = SdRoundBox(p, float2(0.28, 0.28), 0.09);
                    coverage = Stroke(d, 0.012, aa) + (Fill(d, aa) * 0.1);
                    coverage += Stroke(length(p) - 0.09, 0.01, aa) * 0.7;
                    badge = Fill(length(p - float2(0.24, 0.24)) - 0.045, aa) * step(0.5, Hash11((cellId.x * 5.0) + cellId.y + seed));
                }
                else if (type < 0.75)
                {
                    // 알림 카드: 테두리 + 아바타 + 텍스트 줄 2개
                    float d = SdRoundBox(p, float2(0.42, 0.15), 0.06);
                    coverage = Stroke(d, 0.012, aa) + (Fill(d, aa) * 0.08);
                    coverage += Stroke(length(p - float2(-0.3, 0.0)) - 0.07, 0.01, aa);
                    coverage += Fill(SdRoundBox(p - float2(0.06, 0.05), float2(0.24, 0.014), 0.01), aa) * 0.8;
                    coverage += Fill(SdRoundBox(p - float2(0.02, -0.04), float2(0.18, 0.014), 0.01), aa) * 0.5;
                }
                else if (type < 0.88)
                {
                    // 와이파이: 점 + 부채꼴 호 3개
                    float2 wp = p - float2(0.0, -0.12);
                    float radius = length(wp);
                    float cone = step(0.0, wp.y) * step(abs(wp.x), wp.y * 1.1);
                    float arcs = Stroke(radius - 0.1, 0.014, aa) + Stroke(radius - 0.19, 0.014, aa) + Stroke(radius - 0.28, 0.014, aa);
                    coverage = (arcs * cone) + Fill(radius - 0.03, aa);
                }
                else
                {
                    // 배터리: 테두리 + 단자 + 잔량
                    float d = SdRoundBox(p, float2(0.3, 0.14), 0.04);
                    float level = 0.3 + (0.6 * Hash11(cellId.x + (cellId.y * 3.0) + seed));
                    coverage = Stroke(d, 0.012, aa);
                    coverage += Fill(SdRoundBox(p - float2(0.34, 0.0), float2(0.03, 0.06), 0.01), aa);
                    coverage += Fill(SdRoundBox(p - float2(-0.26 + (0.26 * level), 0.0), float2(0.26 * level, 0.09), 0.02), aa) * 0.6;
                    tone = lerp(tone, _OrangeColor.rgb, step(level, 0.4));
                }

                float exists = step(0.5, h);
                float range = step(0.0, v) * step(v, 1.0) * smoothstep(0.0, 0.15, v) * (1.0 - smoothstep(0.85, 1.0, v));

                float3 color = lerp(tone, _OrangeColor.rgb, saturate(badge));
                float alpha = saturate(max(coverage, badge)) * exists * range;
                return float4(color, alpha);
            }

            // 하드웨어 도시 스카이라인: 칩셋 빌딩 실루엣 + 발광 창 + 윤곽선 (rgb = 색, a = 커버리지)
            float4 CityLayer(float u, float elev, float t, float bins, float maxHeight, float seed, float3 bodyColor, float lightAmount)
            {
                float bin = floor(u * bins);
                float bx = frac(u * bins);

                float heightRandom = Hash11((bin * 1.7) + seed);
                float height = lerp(0.02, maxHeight, pow(heightRandom, 1.5));
                height = floor(height * 60.0) / 60.0;

                float gap = step(0.08, bx) * step(bx, 0.92);
                float inside = gap * step(elev, height) * step(0.0, elev);

                float rim = smoothstep(height - 0.006, height, elev) * inside;
                float sideRim = (1.0 - smoothstep(0.0, 0.05, min(bx - 0.08, 0.92 - bx))) * inside;

                float2 windowPosition = float2(bx * 5.0, elev * 70.0);
                float2 windowId = floor(windowPosition);
                float2 windowLocal = frac(windowPosition);
                float isLit = step(1.0 - lightAmount, Hash21(windowId + (bin * 13.1) + seed));
                float windowMask = step(0.2, windowLocal.x) * step(windowLocal.x, 0.8) * step(0.25, windowLocal.y) * step(windowLocal.y, 0.75);
                float isWarm = step(0.8, Hash21(windowId + bin + 91.0));
                float twinkle = 0.7 + (0.3 * sin((t * 2.0) + (Hash21(windowId + bin) * 30.0)));
                float3 windowColor = lerp(_CyanColor.rgb, _OrangeColor.rgb, isWarm);

                float3 color = bodyColor
                    + (windowColor * isLit * windowMask * twinkle * 0.6)
                    + (_CyanColor.rgb * (rim + (sideRim * 0.3)) * 0.8);
                return float4(color, inside);
            }

            // 지평선 아래의 발광 그리드 (카메라 높이 1 기준으로 바닥 평면에 투영)
            float3 GridFloor(float3 dir, float t)
            {
                float down = max(-dir.y, 0.0001);
                float isBelow = step(0.001, -dir.y);
                float2 p = (dir.xz / down) * _GridScale;

                float2 gridWidth = max(fwidth(p), 0.0001);
                float2 g = abs(frac(p - 0.5) - 0.5) / gridWidth;
                float minorLine = 1.0 - saturate(min(g.x, g.y));

                float2 major = p / 8.0;
                float2 majorWidth = max(fwidth(major), 0.0001);
                float2 gm = abs(frac(major - 0.5) - 0.5) / majorWidth;
                float majorLine = 1.0 - saturate(min(gm.x, gm.y));

                float pulse = smoothstep(0.85, 1.0, frac((p.y / 8.0) - (t * 0.15) + (floor(p.x / 8.0) * 0.37)));
                float fade = smoothstep(0.0, 0.3, down);

                return _CyanColor.rgb * ((minorLine * 0.35) + (majorLine * 0.9 * (0.4 + (0.6 * pulse)))) * fade * isBelow;
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
                float elev = asin(clamp(dir.y, -1.0, 1.0));

                // 기본 그라데이션 + 지평선 시안 띠 + 소각로 방향 주황 기운
                float horizonBlend = pow(saturate(dir.y), 0.45);
                float3 color = lerp(_HorizonColor.rgb, _ZenithColor.rgb, horizonBlend);
                color = lerp(color, _GroundColor.rgb, saturate(-dir.y * 3.0));

                float yaw = radians(_GlowYaw);
                float3 glowDirection = normalize(float3(sin(yaw), 0.05, cos(yaw)));
                float glow = saturate(dot(dir, glowDirection));
                color += _OrangeColor.rgb * ((pow(glow, 8.0) * 0.6) + (pow(glow, 2.0) * 0.12)) * _GlowStrength;
                color += _CyanColor.rgb * exp(-abs(elev) * 9.0) * 0.25;

                color += Stars(dir, t) * _StarStrength;
                color += Aurora(u, elev, t) * _AuroraStrength;
                color += DigitalRain(u, elev, t) * _RainStrength;

                // 홈스크린 UI: 먼 층(작고 촘촘, 느림) + 가까운 층(크고 듬성, 빠름)
                float4 farUI = FloatingUI(u, elev, t, 36.0, 5.0, 0.25, 1.2, 0.003, 3.0);
                color += farUI.rgb * farUI.a * 0.5 * _UIStrength;
                float4 nearUI = FloatingUI(u, elev, t, 14.0, 4.0, 0.3, 1.15, -0.006, 17.0);
                color += nearUI.rgb * nearUI.a * 0.9 * _UIStrength;

                // 하드웨어 도시: 먼 층 -> 가까운 층 순서로 덮는다.
                float3 farBody = _HorizonColor.rgb * 0.6;
                float4 farCity = CityLayer(u, elev, t, 90.0, 0.14, 1.0, farBody, 0.25);
                color = lerp(color, farCity.rgb, farCity.a * _CityStrength);
                float4 nearCity = CityLayer(u, elev, t, 56.0, 0.22, 9.0, float3(0.004, 0.008, 0.02), 0.3);
                color = lerp(color, nearCity.rgb, nearCity.a * _CityStrength);

                color += GridFloor(dir, t) * _GridStrength;

                return half4(color * _Exposure, 1.0);
            }
            ENDHLSL
        }
    }
}
