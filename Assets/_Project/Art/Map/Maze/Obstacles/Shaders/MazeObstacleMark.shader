// 파일 맵 미로 장애물 표시 (URP, 조명 없음). 색은 _BaseColor 를 쓰므로 CycleHazardIndicator·GimmickSwitch 가
// MaterialPropertyBlock 으로 바꾸는 상태색(대기·예고·위험)이 그대로 들어온다. 알파도 _BaseColor.a 를 곱한다.
//
// Mode 와 Params
//   0 Stripes : 월드 위치 기준 사선 띠(_BaseColor / _DarkColor). 늘린 상자에서도 띠 폭이 같다. x 띠 폭(m), z 흐르는 속도(m/s)
//   1 Border  : 오브젝트 크기(m) 기준 점선 테두리가 흐른다(드래그 선택 상자). x 테두리 두께(m), y 점선 길이(m), z 속도(m/s), w 안쪽 채움 알파
//   2 Flicker : 텍스처 × 색이 불규칙하게 깜빡인다. x 초당 판정 횟수, y 켜져 있을 확률, z 꺼졌을 때 남는 밝기
//   3 Pulse   : 텍스처 × 색이 부드럽게 밝아졌다 어두워진다. x 속도(Hz), y 가장 어두울 때 밝기
//   4 Solid   : 텍스처 × 색
// 투명으로 쓸 때는 머티리얼에서 Src/Dst Blend, ZWrite 를 바꾼다(SrcAlpha / OneMinusSrcAlpha 또는 One / One).
Shader "Project/MazeObstacleMark"
{
    Properties
    {
        _BaseMap ("Texture", 2D) = "white" {}
        _BaseColor ("Color (상태색)", Color) = (0.15, 0.9, 1, 1)
        _DarkColor ("Stripe Dark", Color) = (0.067, 0.082, 0.106, 1)
        _Intensity ("Intensity", Range(0, 8)) = 1.5
        _Mode ("Mode", Float) = 0
        _Params ("Params", Vector) = (0.25, 0.4, 0.6, 0)

        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Src Blend", Float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Dst Blend", Float) = 0
        [Enum(Off, 0, On, 1)] _ZWrite ("ZWrite", Float) = 1
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 2
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

        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            half4 _BaseColor;
            half4 _DarkColor;
            half _Intensity;
            float _Mode;
            float4 _Params;
        CBUFFER_END
        ENDHLSL

        Pass
        {
            Name "Unlit"
            Tags { "LightMode" = "UniversalForward" }

            Blend [_SrcBlend] [_DstBlend]
            ZWrite [_ZWrite]
            Cull [_Cull]

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog

            #define MODE_STRIPES 0
            #define MODE_BORDER 1
            #define MODE_FLICKER 2
            #define MODE_PULSE 3

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
                float2 sizeOS : TEXCOORD3;
                float phase : TEXCOORD4;
                half fogFactor : TEXCOORD5;
            };

            float Hash11(float p)
            {
                p = frac(p * 0.1031);
                p *= p + 33.33;
                p *= p + p;
                return frac(p);
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = positionInputs.positionCS;
                output.positionWS = positionInputs.positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);

                // Quad 의 UV 0~1 이 실제로 몇 m 인지 (오브젝트 x, y 축 크기)
                output.sizeOS = float2(length(UNITY_MATRIX_M._m00_m10_m20), length(UNITY_MATRIX_M._m01_m11_m21));

                // 같은 머티리얼을 쓰는 장애물끼리 똑같이 깜빡이지 않게 위치로 위상을 나눈다
                float3 objectPosition = TransformObjectToWorld(float3(0.0, 0.0, 0.0));
                output.phase = Hash11(dot(objectPosition, float3(0.137, 0.271, 0.193))) * 31.0;
                output.fogFactor = ComputeFogFactor(positionInputs.positionCS.z);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float t = _Time.y;
                half4 color = _BaseColor;

                if (_Mode == MODE_STRIPES)
                {
                    // 면이 향하는 축을 빼고 남은 두 축으로 사선을 만든다
                    float3 n = abs(input.normalWS);
                    float3 p = input.positionWS;
                    float2 plane = (n.y >= max(n.x, n.z)) ? p.xz : ((n.x >= n.z) ? p.zy : p.xy);
                    float s = frac((((plane.x + plane.y) / _Params.x) - (t * _Params.z / _Params.x)) * 0.5);
                    color.rgb = (s < 0.5) ? _BaseColor.rgb : _DarkColor.rgb;
                }
                else if (_Mode == MODE_BORDER)
                {
                    float2 p = input.uv * input.sizeOS;
                    float2 size = input.sizeOS;
                    float2 edge = min(p, size - p);
                    float d = min(edge.x, edge.y);

                    // 위·아래 변은 x, 좌·우 변은 y 를 따라 점선을 센다
                    float along = (edge.y < edge.x) ? p.x : (p.y + size.x);
                    float dash = step(0.5, frac((along / (_Params.y * 2.0)) - (t * _Params.z / (_Params.y * 2.0))));
                    float border = step(d, _Params.x) * dash;
                    color.a *= max(border, _Params.w * step(_Params.x, d));
                }
                else
                {
                    half4 tex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                    color *= tex;

                    if (_Mode == MODE_FLICKER)
                    {
                        float tick = floor((t + input.phase) * _Params.x);
                        float isOn = step(Hash11(tick + 0.5), _Params.y);
                        color.rgb *= lerp(_Params.z, 1.0, isOn);
                        color.a *= lerp(_Params.z, 1.0, isOn);
                    }
                    else if (_Mode == MODE_PULSE)
                    {
                        float wave = 0.5 + (0.5 * sin((t + input.phase) * _Params.x * TWO_PI));
                        color.rgb *= lerp(_Params.y, 1.0, wave);
                    }
                }

                color.rgb *= _Intensity;
                color.rgb = MixFog(color.rgb, input.fogFactor);
                return color;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            ZWrite On
            ColorMask R
            Cull [_Cull]

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
    }
}
