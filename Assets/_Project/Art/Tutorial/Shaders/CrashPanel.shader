// 튜토리얼 프리팹용 "크래시" 패널 셰이더 (URP, 발광형).
// 스카이박스 Project/TutorialCrashSkybox 의 빔과 같은 면 디자인·스타일 번호를 쓴다 (CrashPanelCommon.hlsl).
//   Box Mode: 단위 큐브 메시(스케일로 크기를 낸 Cube)면 면마다 모서리·박스 이음선·패널이 정확히 맞는다.
//   그 밖의 메시(원통·고리 등)는 Box Mode 를 끄면 Grid Size 간격의 패널 격자 + 메시의 각진 모서리 선으로 그린다.
// 상호작용 대상은 Accent 로 모서리·글자 색을 바꾼다 (쏘기 노랑, 근거리 민트, 스캔 하늘, 위험 분홍빨강).
// 전역 _TutorialCrashProgress 가 경고 지점(0.53)을 넘으면 스카이박스처럼 점점 붉어진다.
Shader "Project/CrashPanel"
{
    Properties
    {
        [IntRange] _BeamStyle ("Style (0 Hologram, 1 Neon, 2 Solid, 3 Wire, 4 Rack)", Range(0, 4)) = 0
        _DetailScale ("Detail Scale (pattern / line size)", Range(0.05, 1)) = 0.2
        [Toggle] _BoxMode ("Box Mode (unit cube mesh)", Float) = 1
        _GridSize ("Grid Size (non-box meshes)", Float) = 2
        [HDR] _AccentColor ("Accent Color", Color) = (1, 0.88, 0.3, 1)
        _AccentAmount ("Accent Amount", Range(0, 1)) = 0
        _MinOpacity ("Min Opacity", Range(0, 1)) = 0.6
        _CreaseStrength ("Mesh Crease Edges", Range(0, 2)) = 1
        _Seed ("Seed", Float) = 0
        [Header(Simplify)]
        _Clean ("Clean (more empty panels)", Range(0, 1)) = 0
        _PatternStrength ("Pattern Strength", Range(0, 2)) = 1
        _EdgeStrength ("Edge Strength", Range(0, 2)) = 1
        _SeamStrength ("Box Seam Strength", Range(0, 1)) = 1
        _SegmentScale ("Box Length Scale", Range(0.5, 6)) = 1
        _PulseStrength ("Edge Pulse Strength", Range(0, 1)) = 1
        [Toggle] _UseCustomColors ("Use Custom Colors", Float) = 0
        [HDR] _CustomBody ("Custom Body", Color) = (0.1, 0.15, 0.3, 1)
        _CustomAlpha ("Custom Opacity", Range(0, 1)) = 0.9
        [HDR] _CustomEdge ("Custom Edge", Color) = (0.9, 0.95, 1.1, 1)
        [HDR] _CustomTextA ("Custom Text A", Color) = (0.6, 0.7, 0.85, 1)
        [HDR] _CustomTextB ("Custom Text B", Color) = (1.1, 0.95, 0.5, 1)

        [Header(Script Driven)]
        // GimmickSwitch 가 켜짐/꺼짐 색을 넣는다. 알파가 0 보다 크면 강조색으로 쓴다.
        _BaseColor ("Base Color (switch indicator)", Color) = (0, 0, 0, 0)
        // CorePulse 등이 맥동 발광을 넣는다.
        [HDR] _EmissionColor ("Emission (pulse)", Color) = (0, 0, 0, 1)
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "Unlit"
            Blend One OneMinusSrcAlpha
            ZWrite On
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _BeamStyle;
                float _DetailScale;
                float _BoxMode;
                float _GridSize;
                half4 _AccentColor;
                float _AccentAmount;
                float _MinOpacity;
                float _CreaseStrength;
                float _Seed;
                float _Clean;
                float _PatternStrength;
                float _EdgeStrength;
                float _SeamStrength;
                float _SegmentScale;
                float _PulseStrength;
                float _UseCustomColors;
                half4 _CustomBody;
                float _CustomAlpha;
                half4 _CustomEdge;
                half4 _CustomTextA;
                half4 _CustomTextB;
                half4 _BaseColor;
                half4 _EmissionColor;
            CBUFFER_END

            // 스카이박스와 같은 진행도 (TutorialCrashSky 가 올린다)
            float _TutorialCrashProgress;

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

            #include "Assets/_Project/Art/Skybox/Shaders/CrashPanelCommon.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionOS : TEXCOORD0;
                float3 normalOS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                float3 positionWS : TEXCOORD3;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.positionOS = input.positionOS.xyz;
                output.normalOS = input.normalOS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // 오브젝트 스케일까지 반영한 월드 크기 좌표
                float3 scale = float3(
                    length(unity_ObjectToWorld._m00_m10_m20),
                    length(unity_ObjectToWorld._m01_m11_m21),
                    length(unity_ObjectToWorld._m02_m12_m22));
                float3 p = input.positionOS * scale;
                float3 n = normalize(input.normalOS);
                float3 an = abs(n);

                // 면 방향에 따라 면 위 2D 좌표를 고른다.
                float2 coord;
                float2 dims;
                float faceSeed;
                if (an.x >= an.y && an.x >= an.z)
                {
                    coord = p.zy;
                    dims = scale.zy;
                    faceSeed = (n.x > 0.0) ? 0.0 : 1.0;
                }
                else if (an.y >= an.z)
                {
                    coord = p.xz;
                    dims = scale.xz;
                    faceSeed = (n.y > 0.0) ? 2.0 : 3.0;
                }
                else
                {
                    coord = p.xy;
                    dims = scale.xy;
                    faceSeed = (n.z > 0.0) ? 4.0 : 5.0;
                }

                // 긴 쪽을 길이 방향(along)으로 쓴다.
                bool isSwapped = dims.y > dims.x;
                float2 faceCoord = isSwapped ? coord.yx : coord;
                float2 faceDims = isSwapped ? dims.yx : dims;

                float along;
                float across;
                float faceW;
                if (_BoxMode > 0.5)
                {
                    along = faceCoord.x + (faceDims.x * 0.5);
                    across = faceCoord.y + (faceDims.y * 0.5);
                    faceW = faceDims.y;
                }
                else
                {
                    along = faceCoord.x + 100.0;
                    faceW = _GridSize;
                    across = frac(faceCoord.y / _GridSize) * _GridSize;
                }

                float fp = max(length(fwidth(input.positionWS)), 0.0001) * 0.7;
                BeamLook look = GetBeamLook((int)_BeamStyle);
                float3 accent = (_BaseColor.a > 0.0) ? (_BaseColor.rgb * 1.5) : _AccentColor.rgb;
                float accentAmount = (_BaseColor.a > 0.0) ? 1.0 : _AccentAmount;
                look.edge = lerp(look.edge, accent, accentAmount);
                look.textA = lerp(look.textA, accent * 0.6, accentAmount * 0.6);

                float seed = _Seed + dot(floor(unity_ObjectToWorld._m03_m13_m23), float3(0.13, 0.71, 0.37));
                float3 glow = BeamFace(along, across, faceW, fp, seed, faceSeed, _Time.y, false, look, _DetailScale);

                // 원통·고리처럼 상자가 아닌 메시는 법선이 꺾이는 곳에 모서리 선을 얹는다.
                float crease = saturate((length(fwidth(input.normalWS)) - 0.25) * 3.0) * _CreaseStrength;
                glow += look.edge * crease;
                glow += _EmissionColor.rgb * 0.3;

                float3 light = normalize(float3(-0.4, 0.75, -0.5));
                float shade = 0.5 + (0.5 * dot(normalize(input.normalWS), light));
                float3 body = (look.isShaded > 0.5)
                    ? lerp(float3(0.006, 0.03, 0.2), look.body, shade)
                    : look.body * (0.7 + (0.6 * shade));
                float alpha = max(look.alpha, _MinOpacity);
                float3 color = glow + (body * alpha);

                // 경고 이후 스카이박스와 함께 붉어진다 (밝기 차이는 살린다).
                float red = smoothstep(0.0, 1.0, saturate((_TutorialCrashProgress - 0.53) / 0.47));
                if (red > 0.0)
                {
                    float3 srgb = pow(max(color, 0.0), 1.0 / 2.2);
                    float luma = dot(srgb, float3(0.3, 0.59, 0.11));
                    float3 shifted = float3(saturate((luma * 1.9) + 0.05), luma * 0.28, luma * 0.35);
                    color = pow(max(lerp(srgb, shifted, red * 0.9), 0.0), 2.2);
                }

                return half4(color, alpha);
            }
            ENDHLSL
        }
    }
}
