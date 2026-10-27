// 바이러스 번짐 — 바닥 평면(Quad)을 글자 칸 격자로 나눠, 가운데에서 들쭉날쭉하게 0·1 글자가 번진 뒤 무작위 순서로 깨지듯 꺼진다.
// 칸마다 0·1 이 계속 바뀌고, 일부 칸은 꽉 찬 블록이며, 가끔 한 줄이 옆으로 튀어 화면이 깨진 듯 보인다.
// 감염된 칸 아래에는 흐르는 스캔라인 바탕을 옅게 깐다.
// _Progress(0~1)는 PixelSpreadEffect 가 MaterialPropertyBlock 으로 올린다. _Seed 로 매번 다른 모양이 된다.
// UV 0~1 이 지름 전체다. 원 밖 칸은 그리지 않는다.
Shader "Project/VirusPixelSpread"
{
    Properties
    {
        [HDR] _Color ("Color", Color) = (1, 0.08, 0.08, 0.95)
        [HDR] _DarkColor ("Dark Color", Color) = (0.45, 0, 0.03, 0.9)
        [HDR] _FlashColor ("Flash Color", Color) = (2, 0.6, 0.6, 1)
        _GridCount ("Grid Count", Float) = 22
        _PixelGap ("Block Gap", Range(0, 0.45)) = 0.1
        _BlockRatio ("Block Ratio", Range(0, 1)) = 0.12
        _GlyphRate ("Glyph Change Rate (/s)", Float) = 12
        _GlitchRate ("Row Glitch Rate (/s)", Float) = 14
        _UnderlayAlpha ("Underlay Alpha", Range(0, 1)) = 0.22
        _EdgeJitter ("Edge Jitter", Range(0, 1)) = 0.35
        _SpreadPortion ("Spread Portion", Range(0.05, 1)) = 0.4
        _BreakStart ("Break Start", Range(0, 1)) = 0.5
        _Progress ("Progress", Range(0, 1)) = 0
        _Seed ("Seed", Float) = 0
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        Offset -1, -1

        Pass
        {
            Name "Unlit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half4 _DarkColor;
                half4 _FlashColor;
                float _GridCount;
                float _PixelGap;
                float _BlockRatio;
                float _GlyphRate;
                float _GlitchRate;
                float _UnderlayAlpha;
                float _EdgeJitter;
                float _SpreadPortion;
                float _BreakStart;
                float _Progress;
                float _Seed;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            float Hash(float2 value)
            {
                return frac(sin(dot(value, float2(12.9898, 78.233))) * 43758.5453);
            }

            float BoxDistance(float2 position, float2 halfSize)
            {
                float2 outside = abs(position) - halfSize;
                return length(max(outside, 0.0)) + min(max(outside.x, outside.y), 0.0);
            }

            float SegmentDistance(float2 position, float2 from, float2 to)
            {
                float2 segment = to - from;
                float along = saturate(dot(position - from, segment) / dot(segment, segment));
                return length(position - from - (segment * along));
            }

            // 글자 칸 안 좌표(-1~1)에서 0 또는 1 글자까지의 거리. 음수면 글자 안이다
            float DigitDistance(float2 glyph, bool isOne)
            {
                if (isOne)
                {
                    float stem = BoxDistance(glyph - float2(0.1, 0.0), float2(0.16, 0.9));
                    float flag = SegmentDistance(glyph, float2(0.05, 0.82), float2(-0.45, 0.45)) - 0.14;
                    float foot = BoxDistance(glyph - float2(0.1, -0.82), float2(0.48, 0.1));
                    return min(stem, min(flag, foot));
                }

                // 가로세로 비율을 다르게 한 고리 + 가운데 사선으로 코드 글꼴의 0 처럼 만든다
                float ring = abs(length(glyph / float2(0.62, 0.92)) - 1.0) * 0.62 - 0.12;
                float slash = SegmentDistance(glyph, float2(-0.22, -0.4), float2(0.22, 0.4)) - 0.09;
                return min(ring, slash);
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 gridPosition = input.uv * _GridCount;

                // 가끔 한 줄이 칸 단위로 옆으로 튀어 화면이 깨진 듯 보이게 한다
                float row = floor(gridPosition.y);
                float glitchTick = floor(_Time.y * _GlitchRate);
                float glitchKey = Hash(float2(row + _Seed, glitchTick));
                float glitchShift = (glitchKey > 0.9) ? floor((Hash(float2(row * 3.1, glitchTick + _Seed)) - 0.5) * 6.0) : 0.0;
                gridPosition.x += glitchShift;

                float2 cell = floor(gridPosition);
                float2 cellCenter = (((cell + 0.5) / _GridCount) * 2.0) - 1.0;
                float centerDistance = length(cellCenter);

                float randomA = Hash(cell + _Seed);
                float randomB = Hash(cell.yx + (_Seed * 1.7) + 13.1);

                // 가장자리를 각도마다 흔들고 칸마다 조금씩 더 깎아 바이러스가 번진 듯 들쭉날쭉하게 만든다
                float angle = atan2(cellCenter.y, cellCenter.x);
                float wave = 0.5 + (0.5 * sin((angle * 5.0) + (_Seed * 6.2832)));
                float edge = 1.0 - (_EdgeJitter * 0.5 * (wave + randomB));

                // 가운데에서 가까운 칸부터 켜지고, 꺼지는 순서는 무작위다
                float infectTime = ((centerDistance / edge) * _SpreadPortion) + (randomA * 0.08);
                float breakTime = _BreakStart + (randomB * (0.95 - _BreakStart));
                bool isInfected = (centerDistance < edge) && (_Progress >= infectTime) && (_Progress < breakTime);

                // 칸 안 좌표. 글자는 칸보다 조금 작게 그린다
                float2 inCell = frac(gridPosition) - 0.5;
                float2 glyph = inCell / float2(0.3, 0.4);

                // 칸마다 다른 속도로 0·1 이 계속 바뀐다
                float glyphTick = floor((_Time.y * _GlyphRate * (0.5 + randomA)) + (randomB * 10.0));
                bool isOne = Hash(cell + float2(glyphTick * 0.37, _Seed)) > 0.5;
                bool isBlock = randomB < _BlockRatio;

                float shapeDistance = isBlock ? BoxDistance(inCell, (0.5 - _PixelGap).xx) : DigitDistance(glyph, isOne) * 0.3;
                float antiAlias = max(fwidth(shapeDistance), 0.0001);
                float shape = 1.0 - smoothstep(-antiAlias, antiAlias, shapeDistance);

                half4 color = (randomA > 0.7) ? _DarkColor : _Color;

                // 막 감염된 칸은 잠깐 밝게 번쩍인다
                float flash = saturate(1.0 - ((_Progress - infectTime) / 0.08));
                color = lerp(color, _FlashColor, flash);

                // 깨지기 직전과 일부 칸은 글리치처럼 깜빡인다
                bool isBreaking = (breakTime - _Progress) < 0.12;
                bool isGlitchCell = randomB > 0.85;
                float blink = step(0.4, frac((_Time.y * 18.0) + (randomA * 7.0)));
                if (isBreaking || isGlitchCell)
                {
                    shape *= blink;
                }

                // 감염된 칸 바탕에 흐르는 스캔라인을 옅게 깐다
                float scanline = 0.55 + (0.45 * step(0.5, frac((input.uv.y * _GridCount * 3.0) - (_Time.y * 4.0))));
                half4 underlay = half4(_DarkColor.rgb, _UnderlayAlpha * scanline);

                half4 result = lerp(underlay, color, shape);
                result.a *= isInfected ? 1.0 : 0.0;
                clip(result.a - 0.001);
                return result;
            }
            ENDHLSL
        }
    }
}
