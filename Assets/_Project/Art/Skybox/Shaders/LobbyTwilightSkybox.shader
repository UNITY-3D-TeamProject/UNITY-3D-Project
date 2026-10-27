// "잠금화면 트와일라잇" 절차적 스카이박스 (URP). 로비 전용 (시안 A, docs/design/lobby-skybox/).
// 보라 -> 복숭아 노을 하늘에 모자이크 타일 구름이 흐르고, 지평선 아래는 데이터가 흐르는 타일 바닥이 깔린다.
//   하늘 그라데이션 / 해 빛무리 / 모자이크 타일 구름 / 데이터 흐름 타일 바닥 / 별(천정 쪽)
// 구름은 구름 좌표(방위각 + 회전, 고도 / 단위 도)를 타일로 나누고 타일 중심에서 노이즈를 한 번 샘플한다.
// 구름 가장자리 타일은 2x2 로 쪼개 윤곽을 살리고, 일부는 깜빡이며 불러와지는 것처럼 보인다.
// 색 섞기는 시안과 같게 sRGB 공간에서 하고 마지막에 Linear 로 바꾼다.
// "Project/LobbySkybox"(프로토타입, 미사용)와는 별개 셰이더다.
Shader "Project/LobbyTwilightSkybox"
{
    Properties
    {
        [Header(Sky Gradient)]
        _ZenithColor ("Zenith (90)", Color) = (0.082, 0.071, 0.247, 1)
        _HighColor ("High (65)", Color) = (0.165, 0.141, 0.439, 1)
        _MidColor ("Mid (38)", Color) = (0.416, 0.31, 0.847, 1)
        _LowColor ("Low (18)", Color) = (0.773, 0.482, 0.863, 1)
        _PinkColor ("Pink (6)", Color) = (1, 0.612, 0.722, 1)
        _HorizonColor ("Horizon (0)", Color) = (1, 0.765, 0.627, 1)

        [Header(Sun)]
        _SunAzimuth ("Sun Azimuth (deg)", Float) = 230
        _SunElevation ("Sun Elevation (deg)", Float) = 4
        _SunGlowColor ("Sun Glow", Color) = (1, 0.824, 0.659, 1)
        _SunDiskColor ("Sun Disk", Color) = (1, 0.945, 0.863, 1)
        _SunGlowStrength ("Sun Glow Strength", Range(0, 2)) = 0.55

        [Header(Cloud Shape)]
        _CloudScale ("Cloud Scale", Float) = 3.2
        _CloudCoverage ("Cloud Coverage (lower = more)", Range(0.3, 0.7)) = 0.5
        _CloudOpacity ("Cloud Opacity", Range(0, 1)) = 0.95
        _CloudSpeed ("Cloud Drift (deg per second)", Float) = 1.5

        [Header(Cloud Tiles)]
        _CloudShadowColor ("Tile Shadow", Color) = (0.604, 0.498, 0.839, 1)
        _CloudLightColor ("Tile Light", Color) = (1, 0.816, 0.769, 1)
        [HDR] _NeonA ("Accent A (cyan)", Color) = (0.373, 0.953, 1, 1)
        [HDR] _NeonB ("Accent B (pink)", Color) = (1, 0.478, 0.851, 1)
        _TileSize ("Tile Size (deg)", Float) = 1.3
        _TileGap ("Tile Gap (0 ~ 0.3)", Range(0, 0.3)) = 0.09
        _TileRound ("Tile Corner Round", Range(0, 0.4)) = 0.12
        _SplitBand ("Edge Split Range", Range(0, 0.2)) = 0.07
        _AccentChance ("Accent Tile Chance", Range(0, 0.3)) = 0.08
        _AccentGlow ("Accent Glow", Range(1, 3)) = 1.5
        _BlinkSpeed ("Edge Tile Blink Speed", Float) = 2
        _SweepStrength ("Refresh Sweep", Range(0, 1)) = 0.35
        _SweepSpeed ("Refresh Sweep Speed", Float) = 0.06

        [Header(Floor Tiles)]
        _SeaDeepColor ("Tile Shadow", Color) = (0.294, 0.227, 0.561, 1)
        _SeaLightColor ("Tile Light", Color) = (0.894, 0.706, 0.839, 1)
        _GroundColor ("Below (deep)", Color) = (0.169, 0.129, 0.376, 1)
        _SeaScale ("Floor Pattern Scale", Float) = 0.8
        _SeaSpeed ("Floor Drift Speed", Float) = 0.01
        _FloorTile ("Floor Tile Size", Float) = 0.06
        _StreamLanes ("Data Stream Lane Ratio", Range(0, 1)) = 0.4
        _StreamSpeed ("Data Stream Speed", Float) = 1

        [Header(Stars)]
        _StarStrength ("Stars", Range(0, 2)) = 0.9
        _TwinkleSpeed ("Twinkle Speed", Float) = 1.5

        [Header(Virus Infection)]
        [HDR] _InfectColor ("Infected Tile (sRGB)", Color) = (1, 0.12, 0.22, 1)
        _InfectChance ("Infected Tile Chance", Range(0, 1)) = 0.3
        _InfectGlow ("Infected Tile Glow", Range(1, 3)) = 2
        _InfectSkyTint ("Sky Red Tint", Range(0, 1)) = 0.45

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

            #define DEG2RAD 0.01745329252
            #define RAD2DEG 57.2957795131
            // 구름은 가로로 길쭉하게 (y 축을 이만큼 늘려 샘플)
            #define CLOUD_STRETCH float3(1.0, 2.6, 1.0)

            CBUFFER_START(UnityPerMaterial)
                half4 _ZenithColor;
                half4 _HighColor;
                half4 _MidColor;
                half4 _LowColor;
                half4 _PinkColor;
                half4 _HorizonColor;
                float _SunAzimuth;
                float _SunElevation;
                half4 _SunGlowColor;
                half4 _SunDiskColor;
                float _SunGlowStrength;
                float _CloudScale;
                float _CloudCoverage;
                float _CloudOpacity;
                float _CloudSpeed;
                half4 _CloudShadowColor;
                half4 _CloudLightColor;
                half4 _NeonA;
                half4 _NeonB;
                float _TileSize;
                float _TileGap;
                float _TileRound;
                float _SplitBand;
                float _AccentChance;
                float _AccentGlow;
                float _BlinkSpeed;
                float _SweepStrength;
                float _SweepSpeed;
                half4 _SeaDeepColor;
                half4 _SeaLightColor;
                half4 _GroundColor;
                float _SeaScale;
                float _SeaSpeed;
                float _FloorTile;
                float _StreamLanes;
                float _StreamSpeed;
                float _StarStrength;
                float _TwinkleSpeed;
                float _Exposure;
                half4 _InfectColor;
                float _InfectChance;
                float _InfectGlow;
                float _InfectSkyTint;
            CBUFFER_END

            // 보안이 다음 목적지일 때 VirusInvasionDirector 가 1 로 올리는 전역 값 (평소 0)
            float _LobbyInfection;

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 direction : TEXCOORD0;
            };

            struct CloudSample
            {
                float raw;      // 문턱 비교 전 노이즈 값
                float shade;    // 0 ~ 1 해 쪽 밝기
            };

            // --- 공통 도구 ---
            // 머티리얼 색은 Linear 로 들어오므로 시안과 같은 sRGB 로 되돌려 섞는다
            float3 ToGamma(float3 c) { return pow(max(c, 0.0), 1.0 / 2.2); }
            float3 ToLinear(float3 c) { return pow(max(c, 0.0), 2.2); }

            float Hash13(float3 p)
            {
                p = frac(p * 0.1031);
                p += dot(p, p.zyx + 31.32);
                return frac((p.x + p.y) * p.z);
            }

            float ValueNoise(float3 p)
            {
                float3 i = floor(p);
                float3 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                float a = lerp(Hash13(i), Hash13(i + float3(1, 0, 0)), f.x);
                float b = lerp(Hash13(i + float3(0, 1, 0)), Hash13(i + float3(1, 1, 0)), f.x);
                float c = lerp(Hash13(i + float3(0, 0, 1)), Hash13(i + float3(1, 0, 1)), f.x);
                float d = lerp(Hash13(i + float3(0, 1, 1)), Hash13(i + float3(1, 1, 1)), f.x);
                return lerp(lerp(a, b, f.y), lerp(c, d, f.y), f.z);
            }

            float Fbm(float3 p, int octaves)
            {
                float sum = 0.0;
                float amp = 0.5;
                float total = 0.0;
                for (int i = 0; i < octaves; i++)
                {
                    sum += amp * ValueNoise(p);
                    total += amp;
                    p = p * 2.03 + 3.1;
                    amp *= 0.5;
                }
                return sum / total;
            }

            float3 DirFromAngles(float azimuthDeg, float elevationDeg)
            {
                float az = azimuthDeg * DEG2RAD;
                float el = elevationDeg * DEG2RAD;
                return float3(cos(el) * sin(az), sin(el), cos(el) * cos(az));
            }

            float SdRoundBox(float2 p, float2 halfSize, float radius)
            {
                float2 q = abs(p) - halfSize + radius;
                return length(max(q, 0.0)) + min(max(q.x, q.y), 0.0) - radius;
            }

            // 고도(도)에 따른 하늘 색 (sRGB)
            float3 SkyGradient(float elev)
            {
                float3 col = ToGamma(_HorizonColor.rgb);
                col = lerp(col, ToGamma(_PinkColor.rgb), smoothstep(0.0, 6.0, elev));
                col = lerp(col, ToGamma(_LowColor.rgb), smoothstep(6.0, 18.0, elev));
                col = lerp(col, ToGamma(_MidColor.rgb), smoothstep(18.0, 38.0, elev));
                col = lerp(col, ToGamma(_HighColor.rgb), smoothstep(38.0, 65.0, elev));
                col = lerp(col, ToGamma(_ZenithColor.rgb), smoothstep(65.0, 90.0, elev));
                return col;
            }

            // 천정 쪽 별: 3D 칸마다 하나, 칸 안 무작위 위치
            float Stars(float3 dir, float t)
            {
                float3 p = dir * 80.0;
                float3 cell = floor(p);
                float exists = step(Hash13(cell + 17.0), 0.06);
                float3 jitter = float3(Hash13(cell + 11.0), Hash13(cell + 12.0), Hash13(cell + 13.0));
                float d = length(p - (cell + 0.2 + jitter * 0.6));
                float bright = 0.3 + 0.7 * Hash13(cell + 21.0);
                float twinkle = 0.65 + 0.35 * sin(t * _TwinkleSpeed * (1.0 + bright * 2.0) + bright * 40.0);
                return exists * exp(-(d * d) / (0.07 * 0.07)) * bright * twinkle;
            }

            // --- 구름 ---
            // 구름 좌표 (u = 구름과 함께 도는 방위각, v = 고도, 단위 도) 한 점의 구름 값
            CloudSample SampleCloud(float2 uv, float sunU)
            {
                CloudSample s;
                float band = smoothstep(-2.0, 4.0, uv.y) * (1.0 - smoothstep(26.0, 36.0, uv.y));
                float3 d = DirFromAngles(uv.x, uv.y);
                float3 sunLocal = DirFromAngles(sunU, _SunElevation);
                float3 p = d * CLOUD_STRETCH * _CloudScale;
                float n = Fbm(p, 4);
                s.raw = n * (0.6 + 0.4 * band) * band;
                // 해 쪽으로 조금 옮긴 낮은 옥타브와 비교해 덩어리 단위 음영
                float n0 = Fbm(p, 2);
                float n2 = Fbm((d + sunLocal * 0.05) * CLOUD_STRETCH * _CloudScale, 2);
                s.shade = saturate((n0 - n2) * 9.0 + 0.55 + (n - 0.5) * 1.5);
                return s;
            }

            // 모자이크 타일 구름. uvWidth = 픽셀 하나의 구름 좌표 폭 (가장자리 안티앨리어싱).
            // rgb = sRGB 색, a = 덮임
            float4 MosaicClouds(float2 uv, float2 uvWidth, float sunU, float t)
            {
                float cs = _TileSize;
                float2 cell = floor(uv / cs);
                float2 f = frac(uv / cs);
                CloudSample s = SampleCloud((cell + 0.5) * cs, sunU);

                // 구름 윤곽 근처 타일은 2x2 로 쪼개 다시 샘플
                float threshold = _CloudCoverage + 0.03;
                float tileSize = cs;
                float3 tileId = float3(cell, 0.0);
                if (abs(s.raw - threshold) < _SplitBand)
                {
                    float2 sub = floor(f * 2.0);
                    f = frac(f * 2.0);
                    tileSize = cs * 0.5;
                    tileId = float3(cell * 2.0 + sub, 1.0);
                    s = SampleCloud((tileId.xy + 0.5) * tileSize, sunU);
                }

                float h = Hash13(tileId + 7.0);
                float on = step(threshold, s.raw);

                // 윤곽 바로 바깥 타일은 깜빡이며 불러와진다 (부드럽게 켜졌다 꺼짐)
                float blinkPhase = frac(t * _BlinkSpeed * (0.5 + h) + h * 9.0);
                float blink = smoothstep(0.0, 0.15, blinkPhase) * (1.0 - smoothstep(0.55, 0.75, blinkPhase));
                float edgeTile = step(threshold - 0.045, s.raw) * (1.0 - on) * step(0.45, Hash13(tileId + 3.0));
                float alpha = max(on, edgeTile * blink * 0.7);

                // 둥근 사각 타일 + 틈 (픽셀 폭 기준 안티앨리어싱)
                float2 p = f - 0.5;
                float d = SdRoundBox(p, 0.5 - _TileGap * 0.5, _TileRound);
                float2 px = uvWidth / tileSize;
                float aa = max(max(px.x, px.y), 1e-4);
                float tileMask = saturate(0.5 - d / aa);

                // 색: 4단 음영 + 타일마다 약간 다른 밝기
                float q = saturate(floor(s.shade * 4.0) / 3.0);
                float3 col = lerp(ToGamma(_CloudShadowColor.rgb), ToGamma(_CloudLightColor.rgb), q);
                col *= 0.94 + 0.1 * Hash13(tileId + 1.0);

                // 강조 타일 (청록·분홍), 천천히 숨 쉬듯 밝아짐
                float pulse = 0.85 + 0.15 * sin(t * 1.7 + h * 30.0);
                float accentA = step(h, _AccentChance * 0.5);
                float accentB = step(1.0 - _AccentChance * 0.5, h);
                col = lerp(col, ToGamma(_NeonA.rgb) * _AccentGlow * pulse, accentA * 0.75);
                col = lerp(col, ToGamma(_NeonB.rgb) * _AccentGlow * pulse, accentB * 0.75);

                // 타일 입체감: 위쪽 밝은 테, 아래쪽 어두운 테, 세로 그라데이션
                col *= 0.94 + 0.1 * f.y;
                col += 0.07 * smoothstep(0.28, 0.42, p.y) * step(d, 0.0);
                col *= 1.0 - 0.07 * smoothstep(-0.28, -0.42, p.y);

                // 새로고침 빛줄기: 방위각을 따라 지나가며 타일 단위로 밝힘
                float tileU = (tileId.x + 0.5) * tileSize;
                float sweep = frac(tileU / 120.0 - t * _SweepSpeed);
                col += _SweepStrength * exp(-pow((sweep - 0.5) * 30.0, 2.0)) * on;

                // 바이러스 감염: 일부 타일이 빨간 타일로 바뀌어 불규칙하게 깜빡임
                float infected = step(Hash13(tileId + 41.0), _InfectChance * _LobbyInfection);
                float flickerPhase = frac(t * (0.8 + Hash13(tileId + 43.0) * 1.6) + h * 5.0);
                float flicker = lerp(0.45, 1.0, step(0.35, flickerPhase)) * (0.85 + 0.15 * step(0.8, flickerPhase));
                // 감염 색은 sRGB 값 그대로 쓴다 (Linear 로 들어온 값을 되돌리면 분홍빛으로 바래므로)
                col = lerp(col, _InfectColor.rgb * _InfectGlow * flicker, infected);

                return float4(col, alpha * tileMask);
            }

            // --- 바닥 ---
            // 타일 크기 ts 한 층의 데이터 흐름 타일. plane = 흐르는 바닥 좌표(y = -1 평면), planeWidth = 픽셀 하나의 바닥 좌표 폭.
            // gapCol = 타일 틈으로 보이는 아래 하늘 색. 반환은 sRGB 색.
            float3 DataTiles(float2 plane, float2 planeWidth, float ts, float elev, float t, float3 gapCol)
            {
                float3 deep = ToGamma(_SeaDeepColor.rgb);
                float3 light = ToGamma(_SeaLightColor.rgb);
                float3 ground = ToGamma(_GroundColor.rgb);
                float3 neonA = ToGamma(_NeonA.rgb);
                float3 neonB = ToGamma(_NeonB.rgb);

                float2 cell = floor(plane / ts);
                float2 f = frac(plane / ts);
                float h = Hash13(float3(cell, 5));

                // 타일 중심에서 무늬 노이즈 한 번 -> 덮임과 3단 음영
                float s = Fbm(float3((cell + 0.5) * ts, 7.0).xzy, 5);
                float on = step(0.5, s + smoothstep(-2.0, -20.0, elev) * 0.12);
                float q = saturate(floor(smoothstep(0.35, 0.75, s) * 3.0) / 2.0);
                float3 col = lerp(deep, light, q) * (0.93 + 0.12 * Hash13(float3(cell, 6)));
                col = lerp(col, ground * 1.3, smoothstep(-15.0, -60.0, elev));
                col = lerp(col, neonA, step(h, 0.03) * 0.7);
                col = lerp(col, neonB, step(0.97, h) * 0.7);
                col *= 0.94 + 0.1 * f.y;

                // 데이터 흐름: 일부 줄에서 빛 머리가 꼬리를 남기며 타일을 타고 지나감
                float rowH = Hash13(float3(cell.y, 2, 9));
                float lane = step(1.0 - _StreamLanes, rowH);
                float head = frac(cell.x / 40.0 - t * _StreamSpeed * (0.15 + rowH * 0.25) + rowH * 7.0);
                float trail = saturate(smoothstep(0.9, 1.0, head) + smoothstep(0.85, 1.0, head) * 0.4) * lane;
                float3 streamCol = lerp(neonA, neonB, step(0.85, rowH)) * 1.4;
                col = lerp(col, streamCol, trail);
                on = max(on, trail);

                // 둥근 사각 타일 (픽셀 폭 기준 안티앨리어싱)
                float fw = max(planeWidth.x, planeWidth.y) / ts;
                float d = SdRoundBox(f - 0.5, 0.44, 0.15);
                float tile = saturate(0.5 - d / max(fw, 1e-4));
                float3 gaps = gapCol * 0.8;
                float3 near = lerp(gaps, col, on * tile);

                // 타일이 픽셀보다 작아지면 보이는 타일·틈의 평균 색으로 (지평선 쪽은 gapCol 이 하늘색으로 이어짐)
                float3 average = lerp(gaps, lerp(deep, light, 0.5), 0.45);
                float detail = 1.0 - smoothstep(0.35, 1.0, fw);
                return lerp(average, near, detail);
            }

            // 멀어질수록 타일을 2배씩 키워(최대 8배) 무늬가 지평선 가까이까지 이어지게 한다. 이웃한 두 크기를 섞어 경계를 숨긴다.
            float3 DataFloor(float2 plane, float2 planeWidth, float elev, float t, float3 gapCol)
            {
                float fw = max(planeWidth.x, planeWidth.y) / _FloorTile;
                float lod = clamp(log2(max(fw * 3.0, 1.0)), 0.0, 3.0);
                float level = floor(lod);
                float ts = _FloorTile * exp2(level);
                float3 a = DataTiles(plane, planeWidth, ts, elev, t, gapCol);
                float blend = smoothstep(0.6, 1.0, lod - level) * step(level, 2.5);
                if (blend <= 0.0)
                {
                    return a;
                }
                float3 b = DataTiles(plane, planeWidth, ts * 2.0, elev, t, gapCol);
                return lerp(a, b, blend);
            }

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
                float t = _Time.y;
                float elev = asin(clamp(dir.y, -1.0, 1.0)) * RAD2DEG;

                // 1. 하늘 그라데이션
                float3 col = SkyGradient(max(elev, 0.0));
                // 감염 시 하늘을 붉은 쪽으로 살짝 물들임
                col = lerp(col, col * float3(1.3, 0.55, 0.6), _InfectSkyTint * _LobbyInfection);

                // 2. 해 빛무리 + 원반
                float3 sunDir = DirFromAngles(_SunAzimuth, _SunElevation);
                float sunAngle = acos(clamp(dot(dir, sunDir), -1.0, 1.0)) * RAD2DEG;
                col += ToGamma(_SunGlowColor.rgb) * exp(-pow(sunAngle / 18.0, 2.0)) * _SunGlowStrength;
                col = lerp(col, ToGamma(_SunDiskColor.rgb), smoothstep(2.4, 1.8, sunAngle));

                // 3. 모자이크 타일 구름 (고도 -2 ~ 36도 띠, 구름 좌표가 방위각 방향으로 흐름)
                // 픽셀 폭은 분기 밖에서 구한다. 방위각은 ±180 이음매에서 튀므로 180도 돌린 값과 작은 쪽을 쓴다.
                float rot = t * _CloudSpeed;
                float az = atan2(dir.x, dir.z) * RAD2DEG;
                float azFlip = atan2(-dir.x, -dir.z) * RAD2DEG;
                float2 uvWidth = float2(min(fwidth(az), fwidth(azFlip)), fwidth(elev));
                if (elev > -2.0 && elev < 36.0)
                {
                    float4 cloud = MosaicClouds(float2(az + rot, elev), uvWidth, _SunAzimuth + rot, t);
                    col = lerp(col, cloud.rgb, saturate(cloud.a) * _CloudOpacity);
                }

                // 4. 데이터 흐름 타일 바닥 (지평선 아래를 y = -1 평면으로 펴서 샘플, 픽셀 폭은 분기 밖에서)
                float dy = min(dir.y, -0.001);
                float2 home = dir.xz / -dy * _SeaScale;
                float2 planeWidth = fwidth(home);
                if (elev < 0.5)
                {
                    // 타일 틈으로 보이는 아래 하늘 (지평선 근처 라벤더 -> 깊은 남색)
                    float3 gapCol = lerp(ToGamma(_LowColor.rgb) * 0.75, ToGamma(_GroundColor.rgb), smoothstep(0.0, -25.0, elev));
                    gapCol = lerp(ToGamma(_HorizonColor.rgb), gapCol, smoothstep(0.0, -4.0, elev));
                    float below = smoothstep(0.5, -3.0, elev);
                    col = lerp(col, DataFloor(home + t * _SeaSpeed, planeWidth, elev, t, gapCol), below);
                }

                // 5. 별 (천정 쪽만)
                col += Stars(dir, t) * smoothstep(35.0, 60.0, elev) * _StarStrength;

                return half4(ToLinear(col) * _Exposure, 1.0);
            }
            ENDHLSL
        }
    }
}
