// 소각로 표면 위에 얹는 회로 패턴 발광 셰이더 (가산 블렌드, 오버레이 메시용).
// 메시 UV 규약: u = 회전 방향 0~1, v = 프로파일을 따라 잰 거리(미터).
// _Grid.x = 둘레를 나눈 셀 개수(정수여야 이음매가 맞음), _Grid.y = 미터당 셀 개수.
Shader "Project/DataCircuitGlow"
{
    Properties
    {
        [HDR] _Color ("Trace Color", Color) = (0.07, 0.63, 0.7, 1)
        [HDR] _PulseColor ("Pulse Color", Color) = (2.5, 2.5, 2.5, 1)
        _Grid ("Grid (cells around, cells per meter)", Vector) = (48, 1.8, 0, 0)
        _LineWidth ("Line Width", Range(0.01, 0.2)) = 0.06
        _NodeRadius ("Node Radius", Range(0.05, 0.3)) = 0.14
        _Speed ("Flow Speed", Float) = 0.6
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        Blend One One
        ZWrite Off
        Cull Off

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
                half4 _PulseColor;
                float4 _Grid;
                float _LineWidth;
                float _NodeRadius;
                float _Speed;
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

            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
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
                float2 gridUV = input.uv * _Grid.xy;
                float2 cell = floor(gridUV);
                float2 local = frac(gridUV);

                float h = Hash21(cell);
                float exists = step(0.15, h);
                float isHorizontal = step(frac(h * 13.7), 0.5);

                // 셀마다 가로선 또는 세로선 하나 + 가끔 노드(점)
                float horizontalLine = step(abs(local.y - 0.5), _LineWidth);
                float verticalLine = step(abs(local.x - 0.5), _LineWidth);
                float lineMask = lerp(verticalLine, horizontalLine, isHorizontal);
                float nodeMask = step(length(local - 0.5), _NodeRadius) * step(0.7, frac(h * 31.3));
                float trace = saturate(lineMask + nodeMask) * exists;

                // 선 방향을 따라 흐르는 빛 띠
                float along = lerp(gridUV.y, gridUV.x, isHorizontal);
                float flow = frac((along * 0.25) - (_Time.y * _Speed) + (h * 7.0));
                float pulse = smoothstep(0.8, 1.0, flow);

                half3 color = trace * (_Color.rgb + (_PulseColor.rgb * pulse));
                return half4(color, 1.0);
            }
            ENDHLSL
        }
    }
}
