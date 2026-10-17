// 튜토리얼 "뒤엉킨 크래시 도시 · 교차형"(AE-3N) 절차적 스카이박스 (URP).
// 시안: docs/design/tutorial-redesign/concepts.html (AE-3N), intent-046
//   파란 블루스크린 하늘 + 양옆·앞뒤·대각으로 엉킨 사각 단면 데이터 빔 + 글리치로 깜빡이는 시스템 창.
//   진행도(0~1)가 오르면: 덤프 진행률 0→87% → 87%에서 멈춤 → WARNING → 빨간 경고·모자이크가 늘며 하늘이 붉어짐.
// 진행도 = max(_PreviewProgress, 전역 _TutorialCrashProgress). 게임에서는 Map.Tutorial.TutorialCrashSky 가 전역 값을 올린다.
// 색은 시안(sRGB) 값을 그대로 쓰고 마지막에 Linear 로 바꾼다.
// 하늘 방향 규약: _FocusYaw = 0 이 +Z. 빔이 모이는 소실점과 시스템 창이 이 방향에 있다.
Shader "Project/TutorialCrashSkybox"
{
    Properties
    {
        [Header(Progress)]
        _PreviewProgress ("Preview Progress (keep 0 in game)", Range(0, 1)) = 0
        _StallAt ("Stall At (87%)", Range(0, 1)) = 0.5
        _WarnAt ("Warning At", Range(0, 1)) = 0.53

        [Header(Layout)]
        _FocusYaw ("Focus Yaw (deg, 0 = +Z)", Range(0, 360)) = 0
        _WindowPitch ("Window Pitch (deg)", Range(-10, 60)) = 20
        _WindowSize ("Window Size", Range(0.1, 1)) = 0.42
        [IntRange] _BeamCount ("Beam Count", Range(8, 64)) = 56

        [Header(Strength)]
        [IntRange] _BeamStyle ("Beam Style (0 Hologram, 1 Neon, 2 Solid, 3 Wire, 4 Rack)", Range(0, 4)) = 0
        _LogStrength ("Background Log Text", Range(0, 2)) = 1
        _StreamStrength ("Data Streams", Range(0, 2)) = 1
        _WarnDensity ("Warning Density", Range(0, 1)) = 0.45
        _GlitchStrength ("Glitch", Range(0, 2)) = 1
        _ScanlineStrength ("Scanlines", Range(0, 1)) = 1
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
            #define MAX_BEAMS 64

            CBUFFER_START(UnityPerMaterial)
                float _PreviewProgress;
                float _StallAt;
                float _WarnAt;
                float _FocusYaw;
                float _WindowPitch;
                float _WindowSize;
                float _BeamCount;
                float _BeamStyle;
                float _LogStrength;
                float _StreamStrength;
                float _WarnDensity;
                float _GlitchStrength;
                float _ScanlineStrength;
                float _TimeScale;
                float _Exposure;
            CBUFFER_END

            // 게임 진행도. TutorialCrashSky 가 Shader.SetGlobalFloat 으로 올린다.
            float _TutorialCrashProgress;

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

            float3 ToLinear(float3 c)
            {
                return pow(max(c, 0.0), 2.2);
            }

            float3 ToSrgb(float3 c)
            {
                return pow(max(c, 0.0), 1.0 / 2.2);
            }

            float3 RotateY(float3 v, float angle)
            {
                float c = cos(angle);
                float s = sin(angle);
                return float3((v.x * c) - (v.z * s), v.y, (v.x * s) + (v.z * c));
            }

            // ---------- 3x5 픽셀 글꼴 (SecuritySkybox 와 같은 인코딩 + 6자 추가) ----------
            // 순서: 0-9, A-Z, 공백(36), ':', '_', '!', '#', '-', 'x', '?', '/', '.', '%', '(', ')', ',', '='
            #define GLYPH_COUNT 51
            #define GLYPH_SPACE 36
            static const int GLYPHS[GLYPH_COUNT] =
            {
                31599, 11415, 29671, 29647, 23497, 31183, 31215, 29257, 31727,
                31695, 11245, 27566, 31015, 27502, 31207, 31204, 31087, 23533,
                29847, 4719, 23469, 18727, 24557, 31597, 31599, 31716, 31609,
                27565, 14478, 29842, 23407, 23402, 23549, 23213, 23186, 29351,
                0, 1040, 7, 9346, 24445, 448, 2728, 29314, 4772,
                2, 21157, 10530, 8778, 20, 3640
            };

            // 깨진 글자로 바꿀 때 쓰는 기호들
            static const int BROKEN_GLYPHS[8] = { 40, 43, 44, 46, 50, 39, 41, 37 };

            // 시스템 창 문구 (줄마다 46칸). 6번 줄 35~36칸이 진행률 숫자 자리, 7번 줄은 경고 후에만 보인다.
            #define WIN_COLS 46
            #define WIN_LINES 8
            #define PERCENT_LINE 6
            #define PERCENT_COL 35
            #define FAIL_LINE 7
            static const int WIN_TEXT[368] =
            {
                10, 36, 25, 27, 24, 11, 21, 14, 22, 36, 17, 10, 28, 36, 11, 14, 14, 23, 36, 13, 14, 29, 14, 12, 29, 14, 13, 36, 10, 23, 13, 36, 29, 17, 14, 36, 13, 14, 31, 18, 12, 14, 36, 36, 36, 36,
                17, 10, 28, 36, 11, 14, 14, 23, 36, 28, 17, 30, 29, 36, 13, 24, 32, 23, 36, 29, 24, 36, 25, 27, 14, 31, 14, 23, 29, 36, 13, 10, 22, 10, 16, 14, 45, 36, 36, 36, 36, 36, 36, 36, 36, 36,
                36, 36, 36, 36, 36, 36, 36, 36, 36, 36, 36, 36, 36, 36, 36, 36, 36, 36, 36, 36, 36, 36, 36, 36, 36, 36, 36, 36, 36, 36, 36, 36, 36, 36, 36, 36, 36, 36, 36, 36, 36, 36, 36, 36, 36, 36,
                25, 17, 24, 23, 14, 38, 13, 27, 24, 25, 38, 18, 22, 25, 10, 12, 29, 36, 36, 36, 36, 36, 36, 36, 36, 36, 36, 36, 36, 36, 36, 36, 36, 36, 36, 36, 36, 36, 36, 36, 36, 36, 36, 36, 36, 36,
                28, 29, 24, 25, 37, 36, 0, 42, 0, 0, 0, 0, 0, 0, 7, 14, 36, 47, 0, 42, 12, 0, 0, 0, 0, 0, 0, 5, 49, 36, 0, 42, 8, 10, 3, 15, 0, 4, 5, 1, 48, 36, 36, 36, 36, 36,
                36, 36, 36, 36, 36, 36, 36, 36, 36, 36, 36, 36, 36, 36, 36, 36, 36, 36, 36, 36, 36, 36, 36, 36, 36, 36, 36, 36, 36, 36, 36, 36, 36, 36, 36, 36, 36, 36, 36, 36, 36, 36, 36, 36, 36, 36,
                12, 24, 21, 21, 14, 12, 29, 18, 23, 16, 36, 13, 10, 29, 10, 36, 15, 24, 27, 36, 12, 27, 10, 28, 17, 36, 13, 30, 22, 25, 36, 45, 45, 45, 36, 40, 40, 46, 36, 36, 36, 36, 36, 36, 36, 36,
                39, 39, 36, 13, 30, 22, 25, 36, 15, 10, 18, 21, 14, 13, 36, 10, 29, 36, 8, 7, 46, 36, 41, 36, 13, 10, 29, 10, 36, 12, 24, 27, 27, 30, 25, 29, 14, 13, 36, 36, 36, 36, 36, 36, 36, 36
            };

            // 하늘에 번지는 빨간 경고 문구 (문구마다 22칸)
            // WARNING, FATAL ERROR, DATA CORRUPTED, MEMORY_MANAGEMENT, ACCESS VIOLATION, DUMP FAILED,
            // SYSTEM HALTED, 0xDEADDEAD, PAGE_FAULT, UNRECOVERABLE, DATA LOSS DETECTED, CRITICAL_PROCESS_DIED
            #define WARN_COLS 22
            #define WARN_COUNT 12
            static const int WARN_TEXT[264] =
            {
                32, 10, 27, 23, 18, 23, 16, 36, 36, 36, 36, 36, 36, 36, 36, 36, 36, 36, 36, 36, 36, 36,
                15, 10, 29, 10, 21, 36, 14, 27, 27, 24, 27, 36, 36, 36, 36, 36, 36, 36, 36, 36, 36, 36,
                13, 10, 29, 10, 36, 12, 24, 27, 27, 30, 25, 29, 14, 13, 36, 36, 36, 36, 36, 36, 36, 36,
                22, 14, 22, 24, 27, 34, 38, 22, 10, 23, 10, 16, 14, 22, 14, 23, 29, 36, 36, 36, 36, 36,
                10, 12, 12, 14, 28, 28, 36, 31, 18, 24, 21, 10, 29, 18, 24, 23, 36, 36, 36, 36, 36, 36,
                13, 30, 22, 25, 36, 15, 10, 18, 21, 14, 13, 36, 36, 36, 36, 36, 36, 36, 36, 36, 36, 36,
                28, 34, 28, 29, 14, 22, 36, 17, 10, 21, 29, 14, 13, 36, 36, 36, 36, 36, 36, 36, 36, 36,
                0, 42, 13, 14, 10, 13, 13, 14, 10, 13, 36, 36, 36, 36, 36, 36, 36, 36, 36, 36, 36, 36,
                25, 10, 16, 14, 38, 15, 10, 30, 21, 29, 36, 36, 36, 36, 36, 36, 36, 36, 36, 36, 36, 36,
                30, 23, 27, 14, 12, 24, 31, 14, 27, 10, 11, 21, 14, 36, 36, 36, 36, 36, 36, 36, 36, 36,
                13, 10, 29, 10, 36, 21, 24, 28, 28, 36, 13, 14, 29, 14, 12, 29, 14, 13, 36, 36, 36, 36,
                12, 27, 18, 29, 18, 12, 10, 21, 38, 25, 27, 24, 12, 14, 28, 28, 38, 13, 18, 14, 13, 36
            };
            static const int WARN_LENGTH[WARN_COUNT] = { 7, 11, 14, 17, 16, 11, 13, 10, 10, 13, 18, 21 };

            // 창 제목: "SYSTEM" / "!! WARNING"
            static const int SYSTEM_TITLE[6] = { 28, 34, 28, 29, 14, 22 };
            static const int WARN_TITLE[10] = { 39, 39, 36, 32, 10, 27, 23, 18, 23, 16 };

            // 글자 한 칸(가로 4 = 글자 3 + 간격 1, 세로 6 = 글자 5 + 간격 1) 안에서 픽셀이 켜져 있는지. cell.y 는 위에서부터.
            float GlyphPixel(int glyph, float2 cell)
            {
                int column = (int)floor(cell.x);
                int row = (int)floor(cell.y);
                if (column < 0 || column > 2 || row < 0 || row > 4)
                {
                    return 0.0;
                }

                int bit = ((4 - row) * 3) + (2 - column);
                return (float)((GLYPHS[glyph] >> bit) & 1);
            }

            // 글자를 확률적으로 깨진 기호로 바꾼다.
            int CorruptGlyph(int glyph, float glitch, float2 seed)
            {
                if (glyph == GLYPH_SPACE)
                {
                    return glyph;
                }

                if (Hash21(seed) < glitch)
                {
                    return BROKEN_GLYPHS[(int)floor(Hash21(seed + 7.3) * 7.99)];
                }

                return glyph;
            }

            // ---------- 진행도 ----------

            struct CrashState
            {
                float percent;  // 덤프 진행률 0~87
                float stall;    // 87%에서 멈춤 (0/1)
                float warn;     // 경고 시작 (0/1)
                float red;      // 붉어짐 0~1
                float glitch;   // 글자 깨짐·글리치 세기
            };

            CrashState GetCrashState()
            {
                float progress = saturate(max(_PreviewProgress, _TutorialCrashProgress));
                CrashState state;
                state.stall = step(_StallAt, progress);
                state.warn = step(_WarnAt, progress);
                state.percent = lerp(floor(87.0 * (1.0 - pow(1.0 - saturate(progress / max(_StallAt, 0.001)), 1.6))), 87.0, state.stall);
                state.red = smoothstep(0.0, 1.0, saturate((progress - _WarnAt) / max(1.0 - _WarnAt, 0.001)));
                state.glitch = 0.04 + (0.3 * state.red) + (state.stall * (1.0 - state.warn) * 0.08);
                return state;
            }

            // ---------- 하늘 레이어 ----------

            float3 Background(float3 dir)
            {
                const float3 TOP = float3(0.039, 0.122, 0.478);
                const float3 MID = float3(0.063, 0.212, 0.784);
                const float3 BOTTOM = float3(0.024, 0.078, 0.337);
                float3 color = lerp(MID, TOP, pow(saturate(dir.y), 0.6));
                color = lerp(color, BOTTOM, pow(saturate(-dir.y), 0.6));

                // 소실점 쪽이 살짝 밝다.
                float focus = saturate(dot(dir, float3(0.0, 0.0, 1.0)));
                return color + (MID * pow(focus, 6.0) * 0.35);
            }

            // 뒤에서 흐르는 흐린 로그 글자 (방위각·고도 격자)
            float LogText(float3 dir, float t)
            {
                float azimuth = atan2(dir.x, dir.z);
                float2 grid = float2((azimuth / TAU) * 480.0, (dir.y * 150.0) - (t * 3.0));
                float2 cell = floor(grid);

                // 글이 있는 세로 띠만 쓴다.
                float column = floor(cell.x / 30.0);
                float hasColumn = step(0.62, Hash11(column + 0.5));
                float hasChar = step(0.4, Hash21(cell));
                int glyph = (int)floor(Hash21(cell + floor(t * 0.5)) * 35.99);
                float2 local = frac(grid) * float2(4.0, 6.0);
                local.y = 6.0 - local.y;
                return GlyphPixel(glyph, local) * hasColumn * hasChar * (1.0 - smoothstep(0.5, 0.75, abs(dir.y)));
            }

            // 빔 한 개의 모양을 시드로 만든다. (시안 TANGLE mode n 과 같은 분포)
            void BeamShape(int index, out float3 center, out float3 axis, out float halfLength, out float halfSize)
            {
                float base = (float)index * 7.13;
                float h1 = Hash11(base + 0.11);
                float h2 = Hash11(base + 1.37);
                float h3 = Hash11(base + 2.53);
                float h4 = Hash11(base + 3.71);
                float h5 = Hash11(base + 4.97);
                float h6 = Hash11(base + 6.19);
                float h7 = Hash11(base + 7.43);
                halfSize = 2.4 + (h7 * 5.5);

                if (h1 < 0.32)
                {
                    // 가로로 가로지르는 빔 (±23도 비틀림)
                    float a = (h2 - 0.5) * 0.8;
                    axis = normalize(float3(cos(a), (h3 - 0.5) * 0.25, sin(a)));
                    center = float3((h4 - 0.5) * 80.0, (h5 - 0.5) * 110.0, 50.0 + (pow(h6, 0.9) * 150.0));
                    halfLength = 450.0;
                }
                else if (h1 < 0.72)
                {
                    // 앞에서 소실점 쪽으로 뻗는 빔 (가운데 통로는 비운다)
                    float x = (h2 - 0.5) * 200.0;
                    x += (abs(x) < 14.0) ? sign(x + 0.001) * 18.0 : 0.0;
                    float y = (h3 - 0.5) * 80.0;
                    float3 start = float3(x, y, 26.0 + (h4 * 40.0));
                    float3 finish = float3(x + ((h5 - 0.5) * 40.0), y + ((h6 - 0.5) * 20.0), 320.0);
                    center = (start + finish) * 0.5;
                    axis = normalize(finish - start);
                    halfLength = length(finish - start) * 0.5;
                }
                else
                {
                    // 비스듬한 빔
                    float a = h2 * 3.14159265;
                    axis = normalize(float3(cos(a), (h3 - 0.5) * 0.35, sin(a)));
                    center = float3((h4 - 0.5) * 120.0, (h5 - 0.5) * 80.0, 50.0 + (h6 * 150.0));
                    halfLength = 350.0;
                }

                // 일부는 뒤쪽으로 뒤집어 앞뒤 사방에서 엉키게 한다. (소실점 쪽이 조금 더 빽빽하다)
                if (Hash11(base + 8.61) < 0.4)
                {
                    center.z = -center.z;
                    axis.z = -axis.z;
                }
            }

            // 빔 면 디자인(스타일 5종, BeamFace)은 프리팹 셰이더와 함께 쓰려고 공용 파일로 뺐다.
            #include "CrashPanelCommon.hlsl"

            struct BeamHit
            {
                float tNear;
                float tFar;
                float3 localNear;
                float3 localFar;
                int axisNear;
                int axisFar;
                float3 normalNear;
                float halfSize;
                float halfLength;
            };

            // 빔 i 와 광선의 교차. 맞지 않으면 tNear 가 음수다.
            BeamHit TraceBeam(int index, float3 ray)
            {
                float3 center;
                float3 axis;
                float halfLength;
                float halfSize;
                BeamShape(index, center, axis, halfLength, halfSize);

                float3 helper = (abs(axis.y) > 0.9) ? float3(1.0, 0.0, 0.0) : float3(0.0, 1.0, 0.0);
                float3 u = normalize(cross(axis, helper));
                float3 v = cross(u, axis);

                // 빔 기준 좌표로 옮긴 광선 (광선 시작점 = 원점)
                float3 origin = -float3(dot(center, axis), dot(center, u), dot(center, v));
                float3 direction = float3(dot(ray, axis), dot(ray, u), dot(ray, v));
                direction += (abs(direction) < 1e-5) * 1e-5;
                float3 inverse = 1.0 / direction;
                float3 extent = float3(halfLength, halfSize, halfSize);
                float3 t1 = (-extent - origin) * inverse;
                float3 t2 = (extent - origin) * inverse;
                float3 tMin = min(t1, t2);
                float3 tMax = max(t1, t2);

                BeamHit hit;
                hit.tNear = max(max(tMin.x, tMin.y), tMin.z);
                hit.tFar = min(min(tMax.x, tMax.y), tMax.z);
                if ((hit.tNear >= hit.tFar) || (hit.tNear <= 0.0))
                {
                    hit.tNear = -1.0;
                }

                hit.localNear = origin + (direction * hit.tNear);
                hit.localFar = origin + (direction * hit.tFar);
                hit.axisNear = (hit.tNear == tMin.y) ? 1 : ((hit.tNear == tMin.z) ? 2 : 0);
                hit.axisFar = (hit.tFar == tMax.y) ? 1 : ((hit.tFar == tMax.z) ? 2 : 0);
                float3 axes[3] = { axis, u, v };
                hit.normalNear = axes[hit.axisNear] * -sign(direction[hit.axisNear]);
                hit.halfSize = halfSize;
                hit.halfLength = halfLength;
                return hit;
            }

            // 맞은 면 위의 (길이 방향, 면 가로) 좌표와 면 구분값
            void FaceCoords(float3 local, int axis, float halfSize, float halfLength, out float along, out float across, out float faceSeed)
            {
                if (axis == 1)
                {
                    along = local.x + halfLength;
                    across = local.z + halfSize;
                    faceSeed = (local.y > 0.0) ? 2.0 : 3.0;
                }
                else if (axis == 2)
                {
                    along = local.x + halfLength;
                    across = local.y + halfSize;
                    faceSeed = (local.z > 0.0) ? 4.0 : 5.0;
                }
                else
                {
                    along = local.z + halfSize;
                    across = local.y + halfSize;
                    faceSeed = (local.x > 0.0) ? 0.0 : 1.0;
                }
            }

            // 가까운 빔 4개까지 앞에서부터 겹쳐 그린다. sky = 그 방향의 하늘 색(Linear)
            float3 Beams(float3 ray, float3 sky, float pixelAngle, float t)
            {
                BeamLook look = GetBeamLook((int)_BeamStyle);

                float4 nearT = float4(1e9, 1e9, 1e9, 1e9);
                float4 nearId = float4(-1.0, -1.0, -1.0, -1.0);
                int count = (int)_BeamCount;
                for (int i = 0; i < MAX_BEAMS; i++)
                {
                    if (i >= count)
                    {
                        break;
                    }

                    BeamHit hit = TraceBeam(i, ray);
                    float d = hit.tNear;
                    if (d <= 0.0 || d >= nearT.w)
                    {
                        continue;
                    }

                    // 가까운 순서로 끼워 넣는다.
                    if (d < nearT.x)
                    {
                        nearT = float4(d, nearT.xyz);
                        nearId = float4(i, nearId.xyz);
                    }
                    else if (d < nearT.y)
                    {
                        nearT = float4(nearT.x, d, nearT.yz);
                        nearId = float4(nearId.x, i, nearId.yz);
                    }
                    else if (d < nearT.z)
                    {
                        nearT = float4(nearT.xy, d, nearT.z);
                        nearId = float4(nearId.xy, i, nearId.z);
                    }
                    else
                    {
                        nearT.w = d;
                        nearId.w = i;
                    }
                }

                float3 light = normalize(float3(-0.4, 0.75, -0.5));
                float3 color = 0.0;
                float transmittance = 1.0;
                for (int k = 0; k < 4; k++)
                {
                    float id = (k == 0) ? nearId.x : ((k == 1) ? nearId.y : ((k == 2) ? nearId.z : nearId.w));
                    if (id < 0.0)
                    {
                        break;
                    }

                    BeamHit hit = TraceBeam((int)id, ray);
                    float faceW = hit.halfSize * 2.0;

                    float along;
                    float across;
                    float faceSeed;
                    FaceCoords(hit.localNear, hit.axisNear, hit.halfSize, hit.halfLength, along, across, faceSeed);
                    float fog = exp(-hit.tNear / BEAM_FOG_DISTANCE);
                    float3 front = BeamFace(along, across, faceW, hit.tNear * pixelAngle, id, faceSeed, t, false, look, 1.0);

                    float alongBack;
                    float acrossBack;
                    float faceSeedBack;
                    FaceCoords(hit.localFar, hit.axisFar, hit.halfSize, hit.halfLength, alongBack, acrossBack, faceSeedBack);
                    float3 back = BeamFace(alongBack, acrossBack, faceW, hit.tFar * pixelAngle, id, faceSeedBack, t, true, look, 1.0);

                    float3 body = look.body;
                    if (look.isShaded > 0.5)
                    {
                        float shade = 0.5 + (0.5 * dot(hit.normalNear, light));
                        body = lerp(float3(0.006, 0.03, 0.2), look.body, shade);
                    }
                    body = lerp(sky, body, fog);

                    color += transmittance * ((front * fog) + (body * look.alpha) + (back * fog * (1.0 - look.alpha)));
                    transmittance *= 1.0 - look.alpha;
                    if (transmittance < 0.03)
                    {
                        break;
                    }
                }

                return color + (transmittance * sky);
            }

            // 하늘을 가로지르는 데이터 줄기 (머리 + 꼬리)
            float3 Streams(float3 dir, float red, float t)
            {
                float azimuth = atan2(dir.x, dir.z);
                float3 sum = 0.0;
                for (int i = 0; i < 8; i++)
                {
                    float seed = (float)i * 3.17;
                    float height = (Hash11(seed) - 0.5) * 1.1;
                    float tilt = (Hash11(seed + 1.0) - 0.5) * 0.4;
                    float lineY = height + (tilt * sin(azimuth + (Hash11(seed + 2.0) * TAU)));
                    float onLine = 1.0 - smoothstep(0.0015, 0.004, abs(dir.y - lineY));

                    float direction = (Hash11(seed + 3.0) < 0.5) ? 1.0 : -1.0;
                    float head = (frac((t * (0.03 + (Hash11(seed + 4.0) * 0.05))) + Hash11(seed + 5.0)) * TAU) - 3.14159265;
                    float behind = (head - azimuth) * direction;
                    behind -= TAU * floor(behind / TAU);
                    float tail = (1.0 - smoothstep(0.0, 0.9, behind)) * step(0.0, behind);
                    float isYellow = step(0.7, Hash11(seed + 6.0));
                    float3 tone = lerp(float3(0.95, 0.96, 1.0), float3(1.0, 0.88, 0.3), isYellow);
                    tone = lerp(tone, float3(1.0, 0.25, 0.3), red);
                    sum += tone * onLine * tail * 0.85;
                }
                return sum;
            }

            // 진행도에 따라 하늘 전체를 붉게: 밝기는 두고 색만 빨강 쪽으로 돌린다.
            float3 RedShift(float3 color, float3 dir, float red)
            {
                // 파랑 하늘은 밝기 차이가 작아서, 밝기를 키워 빨강으로 옮겨야 빔 명암이 살아 있다.
                float luma = dot(color, float3(0.3, 0.59, 0.11));
                float3 shifted = float3(saturate((luma * 1.9) + 0.05), luma * 0.28, luma * 0.35);
                color = lerp(color, shifted, red * 0.9);
                color += float3(110.0, 0.0, 18.0) / 255.0 * red * 0.22;
                return color * (1.0 - (red * 0.35 * smoothstep(0.3, 1.0, abs(dir.y))));
            }

            // 빨간 경고 문구 + 모자이크 덩어리. 하늘을 칸으로 나누고 칸마다 켜지는 임계값이 다르다.
            float4 Warnings(float3 dir, CrashState state, float t)
            {
                if (state.red <= 0.0)
                {
                    return 0.0;
                }

                float azimuth = atan2(dir.x, dir.z);
                float2 grid = float2((azimuth / TAU) * 22.0, (dir.y + 0.9) * 5.0);
                float2 cell = floor(grid);
                float2 local = frac(grid);
                if (abs(dir.y) > 0.85)
                {
                    return 0.0;
                }

                float h1 = Hash21(cell + 0.3);
                float threshold = pow(Hash21(cell + 1.7), 0.9) * 0.95;
                float exists = step(h1, _WarnDensity);
                float tick7 = fmod(floor(t * 7.0), 997.0);
                float alpha = saturate((state.red - threshold) * 5.0) * exists;
                if (alpha <= 0.0)
                {
                    return 0.0;
                }

                alpha *= (Hash21(cell + tick7) < 0.14) ? 0.25 : 1.0;
                local.x += (Hash21(cell + tick7 + 3.0) < 0.2) ? (Hash21(cell + tick7 + 5.0) - 0.5) * 0.1 : 0.0;

                // 칸 안의 사각형
                float2 rectMin = float2(Hash21(cell + 2.1) * 0.3, Hash21(cell + 2.9) * 0.4);
                float2 rectSize = float2(0.45 + (Hash21(cell + 3.3) * 0.55), 0.25 + (Hash21(cell + 4.1) * 0.5)) * (1.0 - rectMin);
                float2 rectLocal = (local - rectMin) / rectSize;
                if (any(rectLocal < 0.0) || any(rectLocal > 1.0))
                {
                    return 0.0;
                }

                int message = (int)floor(Hash21(cell + 5.3) * (WARN_COUNT - 0.01));
                int messageLength = WARN_LENGTH[message];
                float isBox = step(Hash21(cell + 6.7), 0.6);
                float3 color = 0.0;
                float coverage = 0.0;

                if (isBox > 0.5)
                {
                    // 모자이크: 작은 칸마다 빨강·검정·파랑·흰색이 바뀐다.
                    float2 mosaicCell = floor(rectLocal * float2(18.0, 7.0));
                    float tick5 = fmod(floor(t * 5.0), 997.0);
                    float pick = Hash21(mosaicCell + cell * 13.0 + tick5);
                    float visible = step(0.42, Hash21(mosaicCell + cell * 7.0 + tick5 + 1.0));
                    float3 tone = float3(1.0, 0.125, 0.25);
                    tone = (pick > 0.2) ? float3(0.54, 0.0, 0.09) : tone;
                    tone = (pick > 0.4) ? float3(0.1, 0.0, 0.02) : tone;
                    tone = (pick > 0.55) ? float3(1.0, 0.5, 0.56) : tone;
                    tone = (pick > 0.7) ? float3(0.063, 0.212, 0.784) : tone;
                    tone = (pick > 0.82) ? float3(1.0, 1.0, 1.0) : tone;
                    tone = (pick > 0.9) ? float3(0.25, 0.0, 0.06) : tone;
                    color = tone;
                    coverage = visible * 0.85;
                }

                // 문구 한 줄 (사각형 세로 가운데)
                float textHeight = 0.34;
                float textRow = (rectLocal.y - (0.5 - (textHeight * 0.5))) / textHeight;
                // 칸 한 개 = 가로 TAU/22 rad, 세로 0.2. 글자 칸 비율(4:6)을 지키도록 사각형 기준 글자 폭을 구한다.
                const float CELL_WIDTH = TAU / 22.0;
                const float CELL_HEIGHT = 0.2;
                float charWidth = (textHeight * rectSize.y * CELL_HEIGHT * (4.0 / 6.0)) / (rectSize.x * CELL_WIDTH);
                float charPosition = (rectLocal.x - 0.03) / max(charWidth, 0.001);
                int charIndex = (int)floor(charPosition);
                if ((textRow >= 0.0) && (textRow < 1.0) && (charPosition >= 0.0) && (charIndex < messageLength))
                {
                    int glyph = WARN_TEXT[(message * WARN_COLS) + charIndex];
                    glyph = CorruptGlyph(glyph, state.glitch, float2(charIndex + (cell.x * 3.0), cell.y + tick7));
                    float2 glyphCell = float2(frac(charPosition) * 4.0, (1.0 - textRow) * 6.0);
                    float on = GlyphPixel(glyph, glyphCell);
                    // 글자 뒤 검은 띠
                    color = lerp(color, float3(0.0, 0.0, 0.0), 1.0 - (1.0 - isBox) * 0.6);
                    coverage = max(coverage, 0.7);
                    color = lerp(color, float3(1.0, 0.19, 0.28), on);
                    coverage = max(coverage, on);
                }

                return float4(color, coverage * alpha);
            }

            // 시스템 창 본문 글자 한 픽셀. x = 창 가로 좌표(-1~1), 진행률 줄의 숫자 자리는 현재 진행률로 바꾼다.
            float WindowGlyph(float x, int lineIndex, float rowInLine, float charWidth, CrashState state, float glitch, float tick)
            {
                float columnCoord = (x + 1.0 - (charWidth * 0.8)) / charWidth;
                int column = (int)floor(columnCoord);
                if (column < 0 || column >= WIN_COLS)
                {
                    return 0.0;
                }

                int glyph = WIN_TEXT[(lineIndex * WIN_COLS) + column];
                if (lineIndex == PERCENT_LINE && (column == PERCENT_COL || column == PERCENT_COL + 1))
                {
                    int tens = (int)floor(state.percent / 10.0);
                    int ones = (int)state.percent - (tens * 10);
                    glyph = (column == PERCENT_COL) ? ((tens == 0) ? GLYPH_SPACE : tens) : ones;
                }
                glyph = CorruptGlyph(glyph, glitch, float2(column + (lineIndex * 61.0), (tick * 7.0) + lineIndex));
                return GlyphPixel(glyph, float2(frac(columnCoord) * 4.0, rowInLine));
            }

            // 가운데 시스템 창. 반환 = (색, 덮는 정도)
            float4 SystemWindow(float3 dir, CrashState state, float t)
            {
                float tick = fmod(floor(t * 12.0), 997.0);

                // 창 전체가 순간적으로 꺼진다 (붉어질수록 자주).
                if (Hash11(tick + 0.7) < ((0.05 + (0.25 * state.red)) * _GlitchStrength))
                {
                    return 0.0;
                }

                float pitch = radians(_WindowPitch);
                float3 forward = float3(0.0, sin(pitch), cos(pitch));
                float3 right = float3(1.0, 0.0, 0.0);
                float3 up = normalize(cross(forward, right));
                float facing = dot(dir, forward);
                if (facing <= 0.0)
                {
                    return 0.0;
                }

                float3 onPlane = dir / facing;
                float2 p = float2(dot(onPlane, right), dot(onPlane, up)) / _WindowSize;

                // 창이 옆으로 튀고, 가로 띠가 밀린다.
                p.x += (Hash11(tick + 1.3) < 0.15) ? (Hash11(tick + 2.1) - 0.5) * 0.12 : 0.0;
                float bandId = floor(p.y / 0.05);
                float bandOn = step(1.0 - ((0.2 + (0.4 * state.red)) * _GlitchStrength), Hash21(float2(bandId, tick)));
                p.x += bandOn * (Hash21(float2(bandId + 4.0, tick)) - 0.5) * (0.04 + (0.3 * state.glitch));

                // 창 크기: 가로 -1~1, 세로 -0.5~0.5
                if ((abs(p.x) > 1.0) || (abs(p.y) > 0.5))
                {
                    return 0.0;
                }

                float3 body = lerp(float3(0.063, 0.212, 0.784), float3(0.3, 0.055, 0.2), state.warn * state.red);
                float blink = step(0.5, frac(t * 2.0));
                float isRed = state.warn * saturate(blink + step(0.5, state.red));
                float3 frameColor = lerp(float3(1.0, 1.0, 1.0), float3(1.0, 0.165, 0.267), isRed);

                const float BORDER = 0.008;
                if ((abs(p.x) > 1.0 - BORDER) || (abs(p.y) > 0.5 - (BORDER * 2.0)))
                {
                    return float4(frameColor, 1.0);
                }

                // 글자 격자: 가로 WIN_COLS + 여백, 세로 = 제목 1줄 + 본문 줄
                float charWidth = 1.84 / (WIN_COLS + 2.0);
                float lineHeight = charWidth * 1.5 * 1.15;
                float titleHeight = lineHeight * 1.3;
                float top = 0.5 - (BORDER * 2.0);
                float3 color = body;

                // 제목 띠
                if (p.y > top - titleHeight)
                {
                    color = frameColor;
                    float row = (top - p.y) / titleHeight * 6.0 - 0.6;
                    float column = (p.x + 1.0 - (charWidth * 0.5)) / charWidth;
                    int index = (int)floor(column);
                    // " SYSTEM" / " !! WARNING"
                    int glyph = GLYPH_SPACE;
                    if (state.warn > 0.5)
                    {
                        glyph = (index >= 0 && index < 10) ? WARN_TITLE[index] : GLYPH_SPACE;
                    }
                    else
                    {
                        glyph = (index >= 0 && index < 6) ? SYSTEM_TITLE[index] : GLYPH_SPACE;
                    }
                    float on = GlyphPixel(glyph, float2(frac(column) * 4.0, row));
                    float3 titleText = (isRed > 0.5) ? float3(1.0, 1.0, 1.0) : float3(0.039, 0.122, 0.478);
                    return float4(lerp(color, titleText, on), 0.96);
                }

                // 본문
                float textTop = top - titleHeight - (lineHeight * 0.6);
                float lineCoord = (textTop - p.y) / lineHeight;
                int lineIndex = (int)floor(lineCoord);
                float rowInLine = frac(lineCoord) * 6.0;
                float columnCoord = (p.x + 1.0 - (charWidth * 0.8)) / charWidth;
                int column = (int)floor(columnCoord);

                bool isVisibleLine = (lineIndex >= 0) && (lineIndex < WIN_LINES) && (column >= 0) && (column < WIN_COLS);
                if (lineIndex == FAIL_LINE)
                {
                    isVisibleLine = isVisibleLine && (state.warn > 0.5);
                }
                if (lineIndex == PERCENT_LINE)
                {
                    // 87%에서 멈춘 동안(경고 전) 진행률 줄이 깜빡인다.
                    bool isHidden = (state.stall > 0.5) && (state.warn < 0.5) && (frac(t * 1.5) > 0.5);
                    isVisibleLine = isVisibleLine && !isHidden;
                }

                if (isVisibleLine)
                {
                    float3 textColor = (lineIndex == FAIL_LINE) ? float3(1.0, 0.353, 0.431) : float3(0.95, 0.96, 1.0);
                    float glitch = (lineIndex == FAIL_LINE) ? state.glitch : state.glitch * 0.8;
                    float shift = charWidth * 0.12;

                    // RGB 어긋남: 빨강은 왼쪽, 청록은 오른쪽에서 한 번 더 그린다.
                    float redGhost = WindowGlyph(p.x - shift, lineIndex, rowInLine, charWidth, state, glitch, tick);
                    float cyanGhost = WindowGlyph(p.x + shift, lineIndex, rowInLine, charWidth, state, glitch, tick);
                    float main = WindowGlyph(p.x, lineIndex, rowInLine, charWidth, state, glitch, tick);

                    color = lerp(color, float3(1.0, 0.157, 0.314), redGhost * 0.55);
                    color = lerp(color, float3(0.235, 0.94, 1.0), cyanGhost * 0.45);
                    color = lerp(color, textColor, main);
                }

                return float4(color, 0.94);
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
                float t = _Time.y * _TimeScale;
                CrashState state = GetCrashState();
                float2 screen = input.positionCS.xy;

                // 화면 가로 띠 글리치: 해당 띠의 시선 방향을 옆으로 돌려 하늘이 밀려 보이게 한다.
                float tick = fmod(floor(t * 12.0), 997.0);
                float band = floor(screen.y / 18.0);
                float bandOn = step(1.0 - ((0.03 + (0.12 * state.red)) * _GlitchStrength), Hash21(float2(band, tick)));
                float bandYaw = bandOn * (Hash21(float2(band + 3.1, tick)) - 0.5) * (0.05 + (0.15 * state.red));

                // 월드 방향 → 하늘 기준 방향 (_FocusYaw 방향이 +Z)
                float3 dir = RotateY(normalize(input.direction), radians(_FocusYaw) + bandYaw);

                // 하늘·창·경고는 시안 색(sRGB)으로 계산해 Linear 로 바꾸고, 빔은 Linear(HDR 모서리)로 겹친다.
                float pixelAngle = 2.0 / (abs(UNITY_MATRIX_P[1][1]) * _ScreenParams.y);
                float3 sky = Background(dir);
                sky = lerp(sky, float3(0.95, 0.96, 1.0), LogText(dir, t) * 0.22 * _LogStrength);
                float3 color = Beams(dir, ToLinear(sky), pixelAngle, t);
                color += ToLinear(Streams(dir, state.red, t)) * _StreamStrength;
                color = ToLinear(RedShift(ToSrgb(color), dir, state.red));

                float4 warning = Warnings(dir, state, t);
                color = lerp(color, ToLinear(warning.rgb), warning.a);

                float4 window = SystemWindow(dir, state, t);
                color = lerp(color, ToLinear(window.rgb), window.a);

                // 주사선
                float scan = step(fmod(screen.y, 3.0), 1.0);
                color *= 1.0 - (scan * 0.12 * _ScanlineStrength);
                return half4(color * _Exposure, 1.0);
            }
            ENDHLSL
        }
    }
}
