// "대형 전광판" 절차적 스카이박스 (URP). 피드 맵(인스타그램 레퍼런스) 전용.
// 파스텔 하늘에 큰 게시물 카드 기둥이 한 칸 건너 하나씩 서서 위로 흘러가고, 하트·알림 같은 UI 조각이 풍등처럼 떠오른다.
//   기본 그라데이션 + 지평선 빛 / 반짝이(별) / 먼 UI 조각 / 카드 기둥(피드·릴스·스토리) / 가까운 UI 조각
// 카드는 종류별 아틀라스(4x4 = 16장)에서, UI 조각은 _UIAtlas(4x2)에서 한 칸을 골라 그린다.
// 지평선 아래는 하늘을 뒤집어 비춘 뒤 바닥 색으로 가라앉힌다.
Shader "Project/FeedSkybox"
{
    Properties
    {
        [Header(Colors)]
        _ZenithColor ("Zenith (top)", Color) = (0.435, 0.635, 1, 1)
        _MidColor ("Mid Sky", Color) = (0.725, 0.69, 1, 1)
        _LowColor ("Low Sky", Color) = (1, 0.769, 0.886, 1)
        _HorizonColor ("Horizon", Color) = (1, 0.85, 0.9, 1)
        _GroundColor ("Ground (below horizon)", Color) = (0.722, 0.639, 0.902, 1)

        [Header(Horizon Glow)]
        [HDR] _GlowColor ("Glow (wide)", Color) = (1, 0.85, 0.9, 1)
        [HDR] _GlowWarmColor ("Glow (thin, warm)", Color) = (1, 0.93, 0.8, 1)
        _GlowStrength ("Glow Strength", Range(0, 2)) = 0.05

        [Header(Post Cards)]
        _FeedAtlas ("Feed Card Atlas (4x4)", 2D) = "white" {}
        _ReelAtlas ("Reel Card Atlas (4x4)", 2D) = "white" {}
        _StoryAtlas ("Story Card Atlas (4x4)", 2D) = "white" {}
        _CardStrength ("Card Opacity", Range(0, 1)) = 0.95
        _CardColumns ("Card Columns (even, every other one is used)", Float) = 14
        _ScrollSpeed ("Card Scroll Speed (cards per second)", Float) = 0.04

        [Header(Floating UI)]
        _UIAtlas ("Floating UI Atlas (4x2)", 2D) = "black" {}
        _UIStrength ("Floating UI Opacity", Range(0, 1)) = 1
        _UIScrollSpeed ("Floating UI Rise Speed", Float) = 0.12
        [HDR] _UIGlowColor ("Floating UI Glow", Color) = (1, 0.5, 0.72, 1)

        [Header(Details)]
        _StarStrength ("Sparkles", Range(0, 2)) = 0.7
        _ReflectionStrength ("Ground Reflection", Range(0, 1)) = 0.3
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
            // 카드 아틀라스: 한 변에 놓인 카드 수와 카드 한 장의 픽셀 크기 (밉 단계 계산용)
            #define CARD_GRID 4.0
            #define CARD_TILE_PIXELS 512.0
            // 카드의 가로/세로 비율 (피드 640x1046, 릴스·스토리 9:16)
            #define FEED_ASPECT 0.6119
            #define TALL_ASPECT 0.5625
            // UI 조각 아틀라스: 4x2 칸 중 앞의 7칸을 쓴다
            #define UI_GRID float2(4.0, 2.0)
            #define UI_COUNT 7.0
            #define UI_TILE_PIXELS 256.0

            TEXTURE2D(_FeedAtlas);
            SAMPLER(sampler_FeedAtlas);
            TEXTURE2D(_ReelAtlas);
            SAMPLER(sampler_ReelAtlas);
            TEXTURE2D(_StoryAtlas);
            SAMPLER(sampler_StoryAtlas);
            TEXTURE2D(_UIAtlas);
            SAMPLER(sampler_UIAtlas);

            CBUFFER_START(UnityPerMaterial)
                half4 _ZenithColor;
                half4 _MidColor;
                half4 _LowColor;
                half4 _HorizonColor;
                half4 _GroundColor;
                half4 _GlowColor;
                half4 _GlowWarmColor;
                float _GlowStrength;
                float4 _FeedAtlas_ST;
                float4 _ReelAtlas_ST;
                float4 _StoryAtlas_ST;
                float _CardStrength;
                float _CardColumns;
                float _ScrollSpeed;
                float4 _UIAtlas_ST;
                float _UIStrength;
                float _UIScrollSpeed;
                half4 _UIGlowColor;
                float _StarStrength;
                float _ReflectionStrength;
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

            float SdRoundBox(float2 p, float2 halfSize, float radius)
            {
                float2 q = abs(p) - halfSize + radius;
                return length(max(q, 0.0)) + min(max(q.x, q.y), 0.0) - radius;
            }

            float Fill(float signedDistance, float aa)
            {
                return 1.0 - smoothstep(0.0, aa, signedDistance);
            }

            // ---------- 레이어 ----------

            // 반짝이(별): 방향 격자 셀마다 하나
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

                return float3(1.0, 1.0, 1.0) * star * twinkle * fade;
            }

            // 카드 기둥: 방위각 열을 한 칸 건너 하나씩 켜고, 켜진 열마다 큰 카드가 줄지어 위로 흘러간다.
            // 셀마다 종류(피드 절반, 릴스·스토리 반의반씩)와 아틀라스의 카드 한 장을 고른다. (rgb = 색, a = 커버리지)
            float4 CardLayer(float u, float elev, float t)
            {
                const float MIN_ELEV = 0.08;
                const float MAX_ELEV = 1.3;
                const float SEED = 3.0;

                float columns = _CardColumns;
                float rows = max(1.0, round((columns * (MAX_ELEV - MIN_ELEV)) / 9.5));

                float column = floor(u * columns);
                float vRaw = (elev - MIN_ELEV) / (MAX_ELEV - MIN_ELEV);
                float v = (vRaw * rows) + Hash11(column + SEED) - (t * _ScrollSpeed);
                float row = floor(v);

                // 셀 안의 좌표 (라디안)
                float cellWidth = (TAU / columns) * cos(elev);
                float cellHeight = (MAX_ELEV - MIN_ELEV) / rows;
                float2 p = float2((frac(u * columns) - 0.5) * cellWidth, (frac(v) - 0.5) * cellHeight);
                float pixel = fwidth(elev);

                float isFeed = step(Hash21(float2(column + 9.1, row + SEED)), 0.5);
                float isReel = step(Hash21(float2(column + 2.3, row + SEED)), 0.5);
                float aspect = lerp(TALL_ASPECT, FEED_ASPECT, isFeed);

                // 셀 높이에 맞춘 뒤, 열 폭을 넘으면 줄인다. (위로 갈수록 열이 좁아진다)
                float halfHeight = 0.5 * cellHeight * 0.9;
                float halfWidth = halfHeight * aspect;
                float fit = min(1.0, (0.46 * cellWidth) / max(halfWidth, 0.0001));
                float2 halfSize = float2(halfWidth, halfHeight) * fit;

                float bodyMask = Fill(SdRoundBox(p, halfSize, halfSize.x * 0.07), max(pixel * 1.5, 0.0001));

                // 셀 경계에서 UV가 끊기므로 밉 단계는 직접 정한다.
                float cardIndex = min(floor(Hash21(float2(column + 11.0, row + (SEED * 2.0))) * CARD_GRID * CARD_GRID), (CARD_GRID * CARD_GRID) - 1.0);
                float2 cardCell = float2(fmod(cardIndex, CARD_GRID), floor(cardIndex / CARD_GRID));
                float2 cardLocal = clamp((p / (halfSize * 2.0)) + 0.5, 0.005, 0.995);
                float2 cardUV = float2(cardCell.x + cardLocal.x, CARD_GRID - cardCell.y - 1.0 + cardLocal.y) / CARD_GRID;
                float lod = clamp(log2(max((pixel * CARD_TILE_PIXELS) / max(halfSize.x * 2.0, 0.0001), 1.0)), 0.0, 6.0);

                float3 feed = SAMPLE_TEXTURE2D_LOD(_FeedAtlas, sampler_FeedAtlas, cardUV, lod).rgb;
                float3 reel = SAMPLE_TEXTURE2D_LOD(_ReelAtlas, sampler_ReelAtlas, cardUV, lod).rgb;
                float3 story = SAMPLE_TEXTURE2D_LOD(_StoryAtlas, sampler_StoryAtlas, cardUV, lod).rgb;
                float3 color = lerp(lerp(story, reel, isReel), feed, isFeed);

                float isActive = step(fmod(column, 2.0), 0.5);
                float range = smoothstep(0.0, 0.1, vRaw) * (1.0 - smoothstep(0.9, 1.0, vRaw));
                return float4(color, bodyMask * isActive * range);
            }

            // 떠다니는 UI 조각: 하트·좋아요 알림·댓글·공유·팔로워 +1 이 풍등처럼 제각각 떠오른다.
            // (rgb = 색, a = 커버리지), glow = 조각 둘레의 빛무리 세기
            float4 FloatingUILayer(float u, float elev, float t, float columns, float rows, float seed, float size, float density, out float glow)
            {
                const float MIN_ELEV = 0.05;
                const float MAX_ELEV = 1.35;

                float column = floor(u * columns);
                float vRaw = (elev - MIN_ELEV) / (MAX_ELEV - MIN_ELEV);
                float speed = 0.7 + (0.6 * Hash11(column + seed + 2.0));
                float v = (vRaw * rows) + Hash11(column + seed) - (t * _UIScrollSpeed * speed);
                float row = floor(v);

                float cellWidth = (TAU / columns) * cos(elev);
                float cellHeight = (MAX_ELEV - MIN_ELEV) / rows;
                float unit = max(min(cellWidth, cellHeight), 0.0001);
                float2 cellId = float2(column, row);

                // 셀 안에서 자리·크기·기울기를 무작위로 준다.
                float2 offset = (float2(Hash21(cellId + float2(3.3, seed)), Hash21(cellId + float2(seed, 6.1))) - 0.5) * 0.5;
                float2 p = float2((frac(u * columns) - 0.5 - offset.x) * cellWidth, (frac(v) - 0.5 - offset.y) * cellHeight);
                float halfSize = unit * size * (0.5 + (0.5 * Hash21(cellId + float2(8.8, seed))));
                float angle = (Hash21(cellId + float2(1.9, seed * 3.0)) - 0.5) * 0.7;
                float2 rotated = float2((p.x * cos(angle)) - (p.y * sin(angle)), (p.x * sin(angle)) + (p.y * cos(angle)));

                float exists = step(Hash21(cellId + float2(seed, seed * 1.3)), density)
                    * smoothstep(0.0, 0.12, vRaw) * (1.0 - smoothstep(0.88, 1.0, vRaw));
                glow = exp(-dot(p, p) / (halfSize * halfSize * 0.9)) * exists;

                float2 local = (rotated / (halfSize * 2.0)) + 0.5;
                float inside = step(0.0, local.x) * step(local.x, 1.0) * step(0.0, local.y) * step(local.y, 1.0);
                float index = min(floor(pow(Hash21(cellId + float2(13.0, seed)), 1.6) * UI_COUNT), UI_COUNT - 1.0);
                float2 tile = float2(fmod(index, UI_GRID.x), floor(index / UI_GRID.x));
                float2 uv = float2(tile.x + saturate(local.x), UI_GRID.y - tile.y - 1.0 + saturate(local.y)) / UI_GRID;
                float lod = clamp(log2(max((fwidth(elev) * UI_TILE_PIXELS) / (halfSize * 2.0), 1.0)), 0.0, 5.0);
                float4 sprite = SAMPLE_TEXTURE2D_LOD(_UIAtlas, sampler_UIAtlas, uv, lod);

                return float4(sprite.rgb, sprite.a * inside * exists);
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
                float isBelow = step(dir.y, 0.0);

                // 지평선 아래는 위쪽 하늘을 뒤집어 계산한다.
                dir.y = abs(dir.y);
                float t = _Time.y;
                float u = (atan2(dir.x, dir.z) / TAU) + 0.5;
                float elev = asin(clamp(dir.y, 0.0, 1.0));

                // 기본 그라데이션 + 지평선 빛
                float height = elev / (TAU * 0.25);
                float3 base = lerp(_HorizonColor.rgb, _LowColor.rgb, saturate(height / 0.07));
                base = lerp(base, _MidColor.rgb, saturate((height - 0.07) / 0.18));
                base = lerp(base, _ZenithColor.rgb, saturate((height - 0.25) / 0.75));
                base += ((_GlowColor.rgb * exp(-elev * 14.0)) + (_GlowWarmColor.rgb * exp(-elev * 30.0))) * 0.5 * _GlowStrength;

                // 먼 UI 조각 -> 카드 -> 가까운 UI 조각 순서로 덮는다.
                float3 sky = base + (Stars(dir, t) * _StarStrength);

                float farGlow;
                float4 farUI = FloatingUILayer(u, elev, t, 52.0, 10.0, 3.0, 0.24, 0.4, farGlow);
                sky += _UIGlowColor.rgb * farGlow * 0.1 * _UIStrength;
                sky = lerp(sky, farUI.rgb, farUI.a * 0.75 * _UIStrength);

                float4 cards = CardLayer(u, elev, t);
                sky = lerp(sky, cards.rgb, cards.a * _CardStrength);

                float nearGlow;
                float4 nearUI = FloatingUILayer(u, elev, t, 28.0, 7.0, 11.0, 0.22, 0.35, nearGlow);
                sky += _UIGlowColor.rgb * nearGlow * 0.14 * _UIStrength;
                sky = lerp(sky, nearUI.rgb, nearUI.a * _UIStrength);

                float sink = (smoothstep(0.0, 0.8, elev) * 0.75) + 0.18;
                float3 ground = lerp(lerp(base, sky, _ReflectionStrength), _GroundColor.rgb, sink);
                float3 color = lerp(sky, ground, isBelow);

                return half4(color * _Exposure, 1.0);
            }
            ENDHLSL
        }
    }
}
