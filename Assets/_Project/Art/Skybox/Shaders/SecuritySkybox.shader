// "보안 앱 최종 전투 - 감염" 절차적 스카이박스 (URP).
// 한 셰이더 안에 여러 레이어가 겹쳐 있고, 각 레이어의 세기(Strength)를 0으로 내리면 그 레이어만 꺼진다.
//   기본 그라데이션 + 지평선 띠 / 붉은 구름 / 구역이 뭉개지며 치지직거리는 글리치 / 검은 구멍(붉은 테두리)
//   검은 구멍으로 빨려 들어가는 오류 코드 글자(글자가 계속 깨지고 바뀐다) / 떠다니는 티끌 / 발판 아래 그리드
// 팔레트: 검붉은 색 + 빨강(감염) + 소량의 강조색(깨진 데이터).
// 하늘 방향 규약: 월드 방향 기준이며 _FocusYaw = 0 이 +Z 방향이다. (검은 구멍을 보스 맵 쪽으로 맞춘다)
Shader "Project/SecuritySkybox"
{
    Properties
    {
        [Header(Colors)]
        _ZenithColor ("Zenith (top)", Color) = (0.02, 0.004, 0.01, 1)
        _HorizonColor ("Horizon", Color) = (0.3, 0.03, 0.04, 1)
        _GroundColor ("Ground (below horizon)", Color) = (0.015, 0.002, 0.004, 1)
        [HDR] _CoreColor ("Core (infection)", Color) = (1, 0.14, 0.08, 1)
        [HDR] _AccentColor ("Accent (broken data)", Color) = (0.55, 0.9, 1, 1)

        [Header(Black Hole)]
        _FocusYaw ("Focus Yaw (deg, 0 = +Z)", Range(0, 360)) = 90
        _FocusHeight ("Focus Height", Range(0, 1.5)) = 0.3
        _HoleRadius ("Hole Radius", Range(0.05, 0.4)) = 0.17
        _DiskStrength ("Spinning Disk (0 = still hole)", Range(0, 2)) = 0
        _HoleSpin ("Disk Spin Speed", Float) = 1

        [Header(Layer Strength)]
        _CloudStrength ("Red Clouds", Range(0, 2)) = 1
        _GlitchStrength ("Glitch Smear", Range(0, 2)) = 1
        _HoleStrength ("Black Hole Rim", Range(0, 2)) = 1
        _ParticleStrength ("Error Code Text", Range(0, 2)) = 1
        _MoteStrength ("Drifting Motes", Range(0, 2)) = 1
        _GridStrength ("Abyss Grid", Range(0, 2)) = 1

        [Header(Details)]
        [IntRange] _TextStyle ("Text Style (0 Codes, 1 Hex, 2 Binary, 3 Mixed)", Range(0, 3)) = 0
        _TextLanes ("Text Density (lanes around the hole)", Range(24, 200)) = 124
        _ParticleSpeed ("Text Speed", Float) = 1
        _ParticleSwirl ("Text Swirl (far)", Range(-1, 1)) = 0.2
        _Vortex ("Vortex Twist (near the hole)", Range(0, 6)) = 2.5
        _CodeGlitch ("Text Glitch", Range(0, 2)) = 1
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
                half4 _CoreColor;
                half4 _AccentColor;
                float _FocusYaw;
                float _FocusHeight;
                float _HoleRadius;
                float _CloudStrength;
                float _GlitchStrength;
                float _HoleStrength;
                float _ParticleStrength;
                float _MoteStrength;
                float _GridStrength;
                float _ParticleSpeed;
                float _ParticleSwirl;
                float _CodeGlitch;
                float _TextStyle;
                float _TextLanes;
                float _Vortex;
                float _DiskStrength;
                float _HoleSpin;
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

            float Noise3(float3 p)
            {
                float3 i = floor(p);
                float3 f = frac(p);
                f = f * f * (3.0 - (2.0 * f));

                float a = lerp(Hash31(i), Hash31(i + float3(1, 0, 0)), f.x);
                float b = lerp(Hash31(i + float3(0, 1, 0)), Hash31(i + float3(1, 1, 0)), f.x);
                float c = lerp(Hash31(i + float3(0, 0, 1)), Hash31(i + float3(1, 0, 1)), f.x);
                float d = lerp(Hash31(i + float3(0, 1, 1)), Hash31(i + float3(1, 1, 1)), f.x);
                return lerp(lerp(a, b, f.y), lerp(c, d, f.y), f.z);
            }

            float Fbm(float3 p)
            {
                float sum = 0.0;
                float amplitude = 0.5;
                for (int i = 0; i < 5; i++)
                {
                    sum += Noise3(p) * amplitude;
                    p = (p * 2.02) + 13.1;
                    amplitude *= 0.5;
                }
                return sum;
            }

            // ---------- 레이어 ----------

            // 글리치: 하늘의 일부 구역이 잠깐씩 뭉개진다. 반환값 = 그 픽셀이 뭉개지는 정도(0~1)
            // 구역은 (방위각, 고도) 격자 칸이고, 짧은 틱마다 일부 칸만 켜진다. 노이즈로 가장자리를 불규칙하게 깎는다.
            float GlitchAmount(float3 dir, float u, float elev, float t)
            {
                // 시간이 커져도 해시 정밀도가 유지되도록 틱을 작은 난수로 바꿔 쓴다.
                float tick = Hash11(fmod(floor(t * 5.0), 997.0)) * 41.0;
                float2 patchCoord = float2(u * 10.0, (elev + 1.6) * 3.0);
                float2 patchId = floor(patchCoord);
                float2 local = frac(patchCoord);

                float isOn = step(0.9, Hash21(patchId + tick));
                float edge = smoothstep(0.0, 0.2, local.x) * smoothstep(1.0, 0.8, local.x) * smoothstep(0.0, 0.2, local.y) * smoothstep(1.0, 0.8, local.y);
                float ragged = smoothstep(0.35, 0.55, Noise3((dir * 14.0) + tick));
                return saturate(isOn * edge * ragged * _GlitchStrength);
            }

            // 뭉개기: 방향을 굵은 격자에 맞춰 모자이크처럼 뭉개고, 틱마다 조금씩 어긋나게 한다.
            float3 GlitchSmear(float3 dir, float amount, float t)
            {
                const float BLOCKS = 26.0;
                float tick = fmod(floor(t * 12.0), 997.0);
                float3 jitter = (float3(Hash11(tick), Hash11(tick + 3.1), Hash11(tick + 7.7)) - 0.5) * 0.6;
                float3 blocky = (floor((dir * BLOCKS) + jitter) + 0.5 - jitter) / BLOCKS;
                return normalize(lerp(dir, blocky, amount));
            }

            // 치지직거리는 노이즈 알갱이 (프레임마다 바뀐다)
            float GlitchStatic(float u, float elev, float t)
            {
                float2 grain = floor(float2(u * 1100.0, elev * 360.0));
                return Hash21(grain + (Hash11(fmod(floor(t * 30.0), 997.0)) * 53.0));
            }

            // 떠다니는 티끌: 방향 격자 셀마다 하나
            float Motes(float3 dir, float t)
            {
                float3 scaled = normalize(dir - float3(0.0, t * 0.008, 0.0)) * 70.0;
                float3 cell = floor(scaled);
                float3 local = frac(scaled);

                float h = Hash31(cell);
                float3 motePosition = 0.3 + (0.4 * float3(Hash31(cell + 1.3), Hash31(cell + 2.7), Hash31(cell + 4.1)));
                float mote = step(0.975, h) * (1.0 - smoothstep(0.0, 0.16, length(local - motePosition)));
                return mote * (0.5 + (0.5 * sin((t * (2.0 + (h * 5.0))) + (h * 100.0))));
            }

            // 3x5 픽셀 글꼴. 글자마다 15비트(위에서부터 한 줄에 3비트)로 저장한다.
            // 순서: 0-9, A-Z, 공백, ':', '_', '!', '#', '-', 'x', '?', '/'
            #define GLYPH_COUNT 45
            #define ALNUM_COUNT 36
            static const int GLYPHS[GLYPH_COUNT] =
            {
                31599, 11415, 29671, 29647, 23497, 31183, 31215, 29257, 31727,
                31695, 11245, 27566, 31015, 27502, 31207, 31204, 31087, 23533,
                29847, 4719, 23469, 18727, 24557, 31597, 31599, 31716, 31609,
                27565, 14478, 29842, 23407, 23402, 23549, 23213, 23186, 29351,
                0, 1040, 7, 9346, 24445, 448, 2728, 29314, 4772
            };

            // 오류 코드 단어 목록. 단어마다 8칸이고 남는 칸은 공백(36)이다.
            // ERR:404, 0xDEAD, NULL, FATAL, E_FAIL, SEGFAULT, 0xC0DE, DENIED, TIMEOUT, ERR:500, CORRUPT, 0xBAD, NAN, PANIC!, ABORT, #VIRUS, HACKED, 0x0000, KERNEL, EOF
            #define WORD_COUNT 20
            #define WORD_LENGTH 8
            static const int WORDS[WORD_COUNT * WORD_LENGTH] =
            {
                14, 27, 27, 37, 4, 0, 4, 36,
                0, 42, 13, 14, 10, 13, 36, 36,
                23, 30, 21, 21, 36, 36, 36, 36,
                15, 10, 29, 10, 21, 36, 36, 36,
                14, 38, 15, 10, 18, 21, 36, 36,
                28, 14, 16, 15, 10, 30, 21, 29,
                0, 42, 12, 0, 13, 14, 36, 36,
                13, 14, 23, 18, 14, 13, 36, 36,
                29, 18, 22, 14, 24, 30, 29, 36,
                14, 27, 27, 37, 5, 0, 0, 36,
                12, 24, 27, 27, 30, 25, 29, 36,
                0, 42, 11, 10, 13, 36, 36, 36,
                23, 10, 23, 36, 36, 36, 36, 36,
                25, 10, 23, 18, 12, 39, 36, 36,
                10, 11, 24, 27, 29, 36, 36, 36,
                40, 31, 18, 27, 30, 28, 36, 36,
                17, 10, 12, 20, 14, 13, 36, 36,
                0, 42, 0, 0, 0, 0, 36, 36,
                20, 14, 27, 23, 14, 21, 36, 36,
                14, 24, 15, 36, 36, 36, 36, 36
            };

            // 글자 한 칸(가로 4 = 글자 3 + 간격 1, 세로 5) 안에서 해당 픽셀이 켜져 있는지
            float GlyphPixel(int glyph, float2 cellPosition)
            {
                int column = (int)floor(cellPosition.x);
                int row = (int)floor(cellPosition.y);
                if (column < 0 || column > 2 || row < 0 || row > 4)
                {
                    return 0.0;
                }

                int bit = ((4 - row) * 3) + (2 - column);
                return (float)((GLYPHS[glyph] >> bit) & 1);
            }

            // 단어 한 개. along = 글이 진행하는 방향(레인 단위), across = 글자 위쪽 방향(레인 단위, 중심 0)
            // 글자마다 가끔 다른 글자로 바뀌고, 가끔 한 줄이 옆으로 밀린다.
            // style: 0 = 오류 코드 단어, 1 = 16진수 덤프(두 자리씩 띄어 쓴다), 2 = 이진수
            float WordPixel(float along, float across, int wordIndex, int style, float cellSeed, float t)
            {
                const float CHAR_WIDTH = 0.42;
                const float TEXT_HEIGHT = 0.6;

                float row = ((TEXT_HEIGHT * 0.5) - across) / (TEXT_HEIGHT / 5.0);
                float glitchTick = fmod(floor(t * 8.0), 997.0);
                float rowSlip = step(1.0 - (0.1 * _CodeGlitch), Hash21(float2(floor(row) + cellSeed, glitchTick)));
                along += rowSlip * (Hash11(glitchTick + cellSeed) - 0.5) * 0.3;

                float charPosition = along / CHAR_WIDTH;
                int charIndex = (int)floor(charPosition);
                if (charPosition < 0.0 || charIndex >= WORD_LENGTH)
                {
                    return 0.0;
                }

                int glyph = WORDS[(wordIndex * WORD_LENGTH) + charIndex];
                float digit = Hash21(float2(charIndex + cellSeed, wordIndex + 0.5));
                if (style == 1)
                {
                    glyph = (charIndex % 3) == 2 ? 36 : (int)floor(digit * 15.99);
                }
                else if (style == 2)
                {
                    glyph = (int)step(0.5, digit);
                }

                float swap = Hash21(float2(charIndex + cellSeed, glitchTick + 3.0));
                if (swap > 1.0 - (0.14 * _CodeGlitch))
                {
                    glyph = (int)floor(Hash21(float2(glitchTick, charIndex + cellSeed)) * (ALNUM_COUNT - 0.01));
                }

                return GlyphPixel(glyph, float2(frac(charPosition) * 4.0, row));
            }

            // 오류 코드 글자 한 층. 검은 구멍 중심의 로그 극좌표에서 칸을 나누므로, 구멍에 가까울수록 글자가 작아진다.
            // 단어는 나선 레인을 따라 안쪽으로 흐르고, 일정 시간마다 다른 오류 코드로 통째로 바뀐다.
            // x = 글자 밝기, y = 색이 어긋난 잔상 밝기, z = 색 선택용 난수
            float3 CodeLayer(float angle, float phi, float t, float lanes, float seed)
            {
                const float CELL_LENGTH = 5.0;
                const float TEXT_START = 0.4;
                const float TEXT_LENGTH = 0.42 * WORD_LENGTH;

                float radial = log(max(angle, 0.0001)) * (lanes / TAU);
                // 구멍에 가까워질수록 레인이 더 세게 감겨, 글자가 회오리치며 빨려 든다.
                float vortex = _Vortex * pow(_HoleRadius / max(angle, _HoleRadius * 0.5), 1.5);
                float laneCoord = (((phi + vortex) / TAU) * lanes) + (radial * _ParticleSwirl);
                float lane = floor(laneCoord);
                float laneId = lane - (lanes * floor(lane / lanes));
                float across = frac(laneCoord) - 0.5;

                float speed = lerp(0.6, 1.6, Hash11(laneId + seed)) * _ParticleSpeed;
                float s = (radial + (t * speed) + (Hash11((laneId * 1.7) + seed) * 40.0)) / CELL_LENGTH;
                float cell = fmod(floor(s), 512.0);
                float along = (frac(s) * CELL_LENGTH) - TEXT_START;

                float cellSeed = fmod((laneId * 7.0) + (cell * 13.0) + seed, 251.0);
                float h = Hash21(float2(laneId, cell) + seed);
                float exists = step(0.45, h);

                int style = (int)_TextStyle;
                if (style == 3)
                {
                    style = (int)floor(Hash11(cellSeed + 0.5) * 2.99);
                }

                // 검은 구멍 왼쪽에 있는 레인은 글이 뒤집혀 보이지 않게 180도 돌린다.
                float isLeft = step(cos(((laneId + 0.5) / lanes) * TAU), 0.0);
                float textAlong = lerp(along, TEXT_LENGTH - along, isLeft);
                float textAcross = lerp(across, -across, isLeft);

                // 단어 자체도 가끔 다른 오류 코드로 바뀐다.
                float wordTick = fmod(floor((t * 0.6) + (h * 10.0)), 997.0);
                int wordIndex = (int)floor(Hash21(float2(cellSeed, wordTick)) * (WORD_COUNT - 0.01));

                float glyph = WordPixel(textAlong, textAcross, wordIndex, style, cellSeed, t);
                float burst = step(1.0 - (0.25 * _CodeGlitch), Hash21(float2(cellSeed, fmod(floor(t * 6.0), 997.0))));
                float ghost = WordPixel(textAlong + (0.09 * burst), textAcross + (0.05 * burst), wordIndex, style, cellSeed, t) * burst;

                float tail = (1.0 - smoothstep(0.02, 0.07, abs(across))) * step(TEXT_LENGTH + 0.1, along) * exp(-(along - TEXT_LENGTH) * 1.6) * 0.35;
                float flicker = 0.75 + (0.25 * sin((t * 11.0) + (h * 60.0)));

                // 글꼴 픽셀이 화면 픽셀보다 작아지면 깜빡거리므로 서서히 지운다.
                float detail = saturate(1.6 - (fwidth(radial) / 0.105));
                return float3(exists * max(glyph, tail) * flicker * detail, exists * ghost * detail, Hash21(float2(cell, laneId) + seed + 9.7));
            }

            float3 Particles(float angle, float phi, float t)
            {
                float3 nearLayer = CodeLayer(angle, phi, t, floor(_TextLanes), 3.0);
                float3 farLayer = CodeLayer(angle, phi, t * 0.7, floor(_TextLanes * 1.7), 17.0);

                float3 nearTone = lerp(_CoreColor.rgb, float3(1.0, 0.82, 0.7), step(nearLayer.z, 0.25));
                float3 farTone = _CoreColor.rgb;

                float range = smoothstep(_HoleRadius, _HoleRadius * 1.5, angle) * (1.0 - smoothstep(0.7, 1.15, angle));
                float heat = 1.0 + (1.5 * exp(-max(angle - _HoleRadius, 0.0) * 5.0));
                float3 color = (nearTone * nearLayer.x) + (_AccentColor.rgb * nearLayer.y * (1.0 - nearLayer.x) * 0.8);
                color += ((farTone * farLayer.x) + (_AccentColor.rgb * farLayer.y * (1.0 - farLayer.x) * 0.8)) * 0.5;
                return color * range * heat;
            }

            // 지평선 아래 심연: 바닥 평면에 투영한 발광 그리드
            float3 AbyssGrid(float3 dir, float t)
            {
                float down = max(-dir.y, 0.0001);
                float2 p = (dir.xz / down) * _GridScale;
                float2 width = max(fwidth(p), 0.0001);
                float2 g = abs(frac(p - 0.5) - 0.5) / width;
                float gridLine = 1.0 - saturate(min(g.x, g.y));
                float pulse = 0.6 + (0.4 * smoothstep(0.8, 1.0, frac((length(p) / 12.0) - (t * 0.1))));
                return _CoreColor.rgb * gridLine * pulse * smoothstep(0.0, 0.25, down) * step(0.001, -dir.y) * 0.35;
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
                float elev = asin(clamp(dir.y, -1.0, 1.0));

                // 글리치로 뭉개진 방향. 구름, 검은 구멍, 입자가 이 방향을 써서 함께 뭉개진다.
                float u = atan2(dir.x, dir.z) / TAU + 0.5;
                float glitch = GlitchAmount(dir, u, elev, t);
                float3 warped = GlitchSmear(dir, glitch, t);

                // 기본 그라데이션 + 붉은 구름
                float3 color = lerp(_HorizonColor.rgb, _ZenithColor.rgb, pow(saturate(dir.y), 0.5));
                color = lerp(color, _GroundColor.rgb, saturate(-dir.y * 2.5));

                float cloud = Fbm((warped * 3.0) + float3(0.0, t * 0.015, 0.0));
                color += _CoreColor.rgb * smoothstep(0.45, 0.8, cloud) * 0.35 * smoothstep(-0.05, 0.25, dir.y) * _CloudStrength;

                // 검은 구멍 기준 좌표 (angle = 중심에서 벌어진 각, phi = 중심 둘레 각)
                float yaw = radians(_FocusYaw);
                float3 focus = normalize(float3(sin(yaw), _FocusHeight, cos(yaw)));
                float3 right = normalize(cross(float3(0.0, 1.0, 0.0), focus));
                float3 up = cross(focus, right);
                float angle = acos(clamp(dot(warped, focus), -1.0, 1.0));
                float phi = atan2(dot(warped, up), dot(warped, right));

                color += Motes(dir, t) * _CoreColor.rgb * (1.0 - smoothstep(0.2, 1.1, abs(elev))) * _MoteStrength;
                color += Particles(angle, phi, t) * _ParticleStrength;

                // 검은 구멍: 안쪽은 완전히 어둡게, 테두리는 밝은 고리 + 바깥으로 번지는 기운
                float rim = exp(-pow((angle - _HoleRadius) / 0.014, 2.0));
                float corona = exp(-max(angle - _HoleRadius, 0.0) * 7.0) * step(_HoleRadius, angle);
                color *= lerp(1.0, smoothstep(_HoleRadius - 0.01, _HoleRadius, angle), saturate(_HoleStrength));
                // 도는 원반: 구멍 둘레를 감아 도는 나선 팔 + 테두리를 따라 도는 밝은 점. _DiskStrength = 0 이면 멈춘 구멍이다.
                float ratio = angle / _HoleRadius;
                float turn = log(max(ratio, 0.001));
                float arms = pow(0.5 + (0.5 * sin((3.0 * phi) + (9.0 * turn) - (t * _HoleSpin * 3.0))), 3.0);
                float streaks = 0.5 + (0.5 * sin((7.0 * phi) + (15.0 * turn) - (t * _HoleSpin * 5.0)));
                float disk = exp(-max(ratio - 1.0, 0.0) * 1.6) * step(1.0, ratio) * (0.2 + (arms * (0.5 + (0.5 * streaks))));
                float3 diskTone = lerp(_CoreColor.rgb, float3(1.0, 0.85, 0.75), exp(-max(ratio - 1.0, 0.0) * 3.0));
                color += diskTone * disk * 1.4 * _DiskStrength * _HoleStrength;
                rim *= 1.0 + (saturate(_DiskStrength) * 0.6 * sin(phi - (t * _HoleSpin * 2.0)));

                color += ((lerp(_CoreColor.rgb, float3(1.0, 0.85, 0.75), 0.35) * rim * 2.2) + (_CoreColor.rgb * corona * 0.4)) * _HoleStrength;

                // 뭉개진 구역에 치지직거리는 노이즈를 얹는다.
                float grain = GlitchStatic(u, elev, t);
                color = lerp(color, (color * (0.35 + (1.3 * grain))) + (_CoreColor.rgb * grain * 0.12), glitch);
                color += _CoreColor.rgb * exp(-abs(elev) * 22.0) * 0.6;
                color += AbyssGrid(dir, t) * _GridStrength;

                return half4(color * _Exposure, 1.0);
            }
            ENDHLSL
        }
    }
}
