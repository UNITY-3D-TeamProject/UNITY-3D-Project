// 파일 맵 미로 벽·기둥 면 (URP, 불투명). 바탕 텍스처 위에 움직이는 층(Fx1, Fx2)을 시간에 따라 바꿔 겹친다.
// 움직임은 층마다 Mode 로 고르고, Rect(UV 범위: xMin, yMin, xMax, yMax)와 Params 로 조절한다.
// 벽마다 위치로 위상을 달리해 같은 머티리얼을 쓰는 벽끼리 똑같이 움직이지 않는다. 조명은 URP Lit 과 같은 PBR 계산을 쓴다.
//
// Mode 와 Params
//   0 Off
//   1 Pulse      : x 속도(Hz), z 가장 어두울 때 불투명도, w 0 = 부드럽게, 1 = 깜빡임
//   2 Scan       : Rect 안을 빛띠가 지나갈 때만 보인다. x 속도(회/s), y 띠 폭(Rect 높이 비율), z 띠 밖 불투명도, w 1 = 아래로, -1 = 위로
//   3 Scroll     : Rect 안에서 감아 돌며 흐른다. x 가로 속도, y 세로 속도 (Rect 크기/s)
//   4 Columns    : Rect 를 y 개 막대로 나눠 막대마다 높이를 바꾼다. x 속도, z 꼭대기 색(Color2) 비율, w 0 = 아래에서, 1 = 가운데에서 위아래로
//   5 Select     : Rect 를 y 칸으로 나눠 한 칸만 보인다. x 초당 바뀌는 횟수
//   6 Radar      : Rect.xy 중심, Rect.z 반지름(u 단위)에 회전 부채꼴(Color2)을 그리고, 층은 부채꼴이 지날 때 밝아진다. x 회전(회/s), y 꼬리 길이(바퀴 비율), w 텍스처 세로/가로 비율
//   7 Glitch     : 가끔 가로 띠별로 어긋난다. x 초당 판정 횟수, y 띠 개수, z 어긋날 확률, w 최대 어긋남(u)
//   8 Stripes    : 층의 알파를 모양으로 대각 줄무늬(Color / Color2)가 흐른다. x 속도(주기/s), y 주기(u 단위), w 텍스처 세로/가로 비율
//   9 CellPulse  : 세로로 반복되는 무늬가 칸마다 커졌다 작아진다. x 속도(Hz), y 칸 높이(v), z 크기 변화량
//  10 ScalePulse : Rect.xy 중심으로 커졌다 작아진다. x 속도(Hz), z 크기 변화량
Shader "Project/MazeWallPanel"
{
    Properties
    {
        _BaseMap ("Base", 2D) = "white" {}
        _BaseColor ("Base Tint", Color) = (1, 1, 1, 1)
        _Smoothness ("Smoothness", Range(0, 1)) = 0.25
        _EmissionStrength ("Base Emission", Range(0, 2)) = 0.55
        _FxGlow ("Fx Emission", Range(0, 4)) = 0.3

        [Header(Fx1)]
        _Fx1Map ("Fx1 Layer", 2D) = "black" {}
        _Fx1Mode ("Fx1 Mode", Float) = 0
        _Fx1Rect ("Fx1 Rect (xMin, yMin, xMax, yMax)", Vector) = (0, 0, 1, 1)
        _Fx1Params ("Fx1 Params", Vector) = (1, 1, 0, 0)
        _Fx1Color ("Fx1 Color", Color) = (1, 1, 1, 1)
        _Fx1Color2 ("Fx1 Color 2", Color) = (1, 1, 1, 1)

        [Header(Fx2)]
        _Fx2Map ("Fx2 Layer", 2D) = "black" {}
        _Fx2Mode ("Fx2 Mode", Float) = 0
        _Fx2Rect ("Fx2 Rect (xMin, yMin, xMax, yMax)", Vector) = (0, 0, 1, 1)
        _Fx2Params ("Fx2 Params", Vector) = (1, 1, 0, 0)
        _Fx2Color ("Fx2 Color", Color) = (1, 1, 1, 1)
        _Fx2Color2 ("Fx2 Color 2", Color) = (1, 1, 1, 1)
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
            "RenderPipeline" = "UniversalPipeline"
        }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        TEXTURE2D(_BaseMap);
        SAMPLER(sampler_BaseMap);
        TEXTURE2D(_Fx1Map);
        SAMPLER(sampler_Fx1Map);
        TEXTURE2D(_Fx2Map);
        SAMPLER(sampler_Fx2Map);

        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            half4 _BaseColor;
            half _Smoothness;
            half _EmissionStrength;
            half _FxGlow;
            float _Fx1Mode;
            float4 _Fx1Rect;
            float4 _Fx1Params;
            half4 _Fx1Color;
            half4 _Fx1Color2;
            float _Fx2Mode;
            float4 _Fx2Rect;
            float4 _Fx2Params;
            half4 _Fx2Color;
            half4 _Fx2Color2;
        CBUFFER_END
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile _ _LIGHT_LAYERS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Fog.hlsl"

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            #define MODE_PULSE 1
            #define MODE_SCAN 2
            #define MODE_SCROLL 3
            #define MODE_COLUMNS 4
            #define MODE_SELECT 5
            #define MODE_RADAR 6
            #define MODE_GLITCH 7
            #define MODE_STRIPES 8
            #define MODE_CELL_PULSE 9
            #define MODE_SCALE_PULSE 10

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                half fogFactor : TEXCOORD3;
                float phase : TEXCOORD4;
            };

            float Hash11(float p)
            {
                p = frac(p * 0.1031);
                p *= p + 33.33;
                p *= p + p;
                return frac(p);
            }

            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            bool IsInRect(float2 uv, float4 rect)
            {
                return all(uv >= rect.xy) && all(uv <= rect.zw);
            }

            // 막대 col 의 지금 높이 (0.25 ~ 1). 두 사인을 섞어 막대마다 다르게 출렁인다
            float ColumnLevel(float col, float t)
            {
                float h = Hash11(col + 1.7);
                float wave = (0.5 + (0.5 * sin((t * (1.0 + (h * 1.3)) * TWO_PI) + (h * TWO_PI)))) * 0.6
                           + (0.5 + (0.5 * sin((t * 2.3 * (1.0 + h)) + (col * 1.9)))) * 0.4;
                return lerp(0.25, 1.0, wave);
            }

            // 층 하나를 모드대로 움직여 color 에 겹치고, 밝게 빛날 몫을 glow 에 더한다
            void ApplyFx(TEXTURE2D_PARAM(fxMap, fxSampler), float mode, float4 rect, float4 p, half4 color1, half4 color2,
                         float2 uv, float t, inout half3 color, inout half3 glow)
            {
                if (mode < 0.5)
                {
                    return;
                }

                float2 dx = ddx(uv);
                float2 dy = ddy(uv);
                float2 sampleUV = uv;
                float alphaScale = 1.0;
                half3 tint = color1.rgb;
                float2 size = rect.zw - rect.xy;

                if (mode == MODE_PULSE)
                {
                    float s = (p.w > 0.5) ? step(0.5, frac(t * p.x)) : (0.5 + (0.5 * sin(t * p.x * TWO_PI)));
                    alphaScale = lerp(p.z, 1.0, s);
                }
                else if (mode == MODE_SCAN)
                {
                    float f = (uv.y - rect.y) / size.y;
                    float cycle = frac(t * p.x);
                    float center = (p.w >= 0.0) ? lerp(1.0 + p.y, -p.y, cycle) : lerp(-p.y, 1.0 + p.y, cycle);
                    float band = saturate(1.0 - (abs(f - center) / p.y));
                    alphaScale = IsInRect(uv, rect) ? max(p.z, band * band) : 0.0;
                }
                else if (mode == MODE_SCROLL)
                {
                    if (IsInRect(uv, rect))
                    {
                        sampleUV = rect.xy + (frac(((uv - rect.xy) / size) - (t * p.xy)) * size);
                    }
                }
                else if (mode == MODE_COLUMNS)
                {
                    float col = floor(((uv.x - rect.x) / size.x) * p.y);
                    float level = ColumnLevel(col, t * p.x);
                    float f = (p.w > 0.5) ? (abs(uv.y - ((rect.y + rect.w) * 0.5)) / (size.y * 0.5)) : ((uv.y - rect.y) / size.y);
                    alphaScale = (IsInRect(uv, rect) && (f <= level)) ? 1.0 : 0.0;
                    if ((p.w < 0.5) && (f > (level - p.z)))
                    {
                        tint = color2.rgb;
                    }
                }
                else if (mode == MODE_SELECT)
                {
                    float col = floor(((uv.x - rect.x) / size.x) * p.y);
                    float active = floor(Hash11(floor(t * p.x) + 0.37) * p.y);
                    alphaScale = (IsInRect(uv, rect) && (col == active)) ? 1.0 : 0.0;
                }
                else if (mode == MODE_RADAR)
                {
                    float2 d = (uv - rect.xy) * float2(1.0, p.w);
                    float r = length(d);
                    float angle = (atan2(d.y, d.x) / TWO_PI) + 0.5;
                    float behind = frac(frac(t * p.x) - angle);
                    float trail = (r <= rect.z) ? saturate(1.0 - (behind / p.y)) : 0.0;
                    color = lerp(color, color2.rgb, color2.a * trail * trail);
                    glow += color2.rgb * color2.a * trail * trail * 0.5;
                    alphaScale = 0.2 + (0.8 * trail);
                }
                else if (mode == MODE_GLITCH)
                {
                    float tick = floor(t * p.x);
                    float burst = step(1.0 - p.z, Hash11(tick + 0.5));
                    float band = floor(uv.y * p.y);
                    sampleUV.x += (Hash21(float2(band, tick)) - 0.5) * 2.0 * p.w * burst;
                    alphaScale = 1.0 - (burst * 0.35 * Hash11(band + tick));
                }
                else if (mode == MODE_CELL_PULSE)
                {
                    float cell = floor(uv.y / p.y);
                    float local = frac(uv.y / p.y) - 0.5;
                    float s = 1.0 + (p.z * sin((t * p.x * TWO_PI) + (cell * 0.9)));
                    sampleUV = float2(0.5 + ((uv.x - 0.5) / s), (cell + 0.5 + (local / s)) * p.y);
                }
                else if (mode == MODE_SCALE_PULSE)
                {
                    float wave = sin(t * p.x * TWO_PI);
                    sampleUV = rect.xy + ((uv - rect.xy) / (1.0 + (p.z * wave)));
                    alphaScale = 0.8 + (0.2 * wave);
                }

                half4 fx = SAMPLE_TEXTURE2D_GRAD(fxMap, fxSampler, sampleUV, dx, dy);
                half3 fxColor = fx.rgb * tint;

                if (mode == MODE_STRIPES)
                {
                    float stripe = step(0.5, frac(((uv.x + (uv.y * p.w)) / p.y) - (t * p.x)));
                    fxColor = lerp(color2.rgb, color1.rgb, stripe);
                }

                half alpha = saturate(fx.a * alphaScale);
                color = lerp(color, fxColor, alpha);
                glow += fxColor * alpha;
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = positionInputs.positionCS;
                output.positionWS = positionInputs.positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.fogFactor = ComputeFogFactor(positionInputs.positionCS.z);

                // 오브젝트 위치로 벽마다 다른 위상(초)
                float3 objectPosition = TransformObjectToWorld(float3(0.0, 0.0, 0.0));
                output.phase = Hash21(objectPosition.xz * 0.173) * 37.0;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float t = _Time.y + input.phase;
                half3 color = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv).rgb * _BaseColor.rgb;
                half3 glow = 0;

                ApplyFx(TEXTURE2D_ARGS(_Fx1Map, sampler_Fx1Map), _Fx1Mode, _Fx1Rect, _Fx1Params, _Fx1Color, _Fx1Color2, input.uv, t, color, glow);
                ApplyFx(TEXTURE2D_ARGS(_Fx2Map, sampler_Fx2Map), _Fx2Mode, _Fx2Rect, _Fx2Params, _Fx2Color, _Fx2Color2, input.uv, t, color, glow);

                InputData inputData = (InputData)0;
                inputData.positionWS = input.positionWS;
                inputData.positionCS = input.positionCS;
                inputData.normalWS = NormalizeNormalPerPixel(input.normalWS);
                inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
                inputData.shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                inputData.fogCoord = input.fogFactor;
                inputData.bakedGI = SampleSH(inputData.normalWS);
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
                inputData.shadowMask = half4(1, 1, 1, 1);

                SurfaceData surfaceData = (SurfaceData)0;
                surfaceData.albedo = color;
                surfaceData.smoothness = _Smoothness;
                surfaceData.occlusion = 1.0;
                surfaceData.alpha = 1.0;
                surfaceData.normalTS = half3(0.0, 0.0, 1.0);
                surfaceData.emission = (color * _EmissionStrength) + (glow * _FxGlow);

                half4 result = UniversalFragmentPBR(inputData, surfaceData);
                result.rgb = MixFog(result.rgb, inputData.fogCoord);
                result.a = 1.0;
                return result;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            ZWrite On
            ColorMask R

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            float4 vert(float4 positionOS : POSITION) : SV_POSITION
            {
                return TransformObjectToHClip(positionOS.xyz);
            }

            half frag() : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }

            ZWrite On

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float3 normalWS = normalize(input.normalWS);
                #if defined(_GBUFFER_NORMALS_OCT)
                    float2 octNormalWS = PackNormalOctQuadEncode(normalWS);
                    half3 packedNormalWS = PackFloat2To888((octNormalWS * 0.5) + 0.5);
                    return half4(packedNormalWS, 0.0);
                #else
                    return half4(normalWS, 0.0);
                #endif
            }
            ENDHLSL
        }
    }
}
