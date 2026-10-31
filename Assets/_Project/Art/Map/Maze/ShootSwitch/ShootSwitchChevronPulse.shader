Shader "Project/ShootSwitchChevronPulse"
{
    Properties
    {
        _BaseColor ("Crosshair Color", Color) = (0.98, 0.98, 0.94, 1)
        _Travel ("Outward Travel", Range(0, 0.1)) = 0.05
        _CycleSeconds ("Cycle Seconds", Range(0.5, 5)) = 1.8
        _PhaseOffset ("Phase Offset", Range(0, 1)) = 0
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }

        HLSLINCLUDE
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                float _Travel;
                float _CycleSeconds;
                float _PhaseOffset;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half fogFactor : TEXCOORD0;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                float2 position = input.positionOS.xy;

                // 중앙 점은 고정하고 각 꺾쇠를 자기 축 방향으로 평행 이동한다.
                if (dot(position, position) > 0.0036)
                {
                    float phase = (_Time.y / max(_CycleSeconds, 0.001)) + _PhaseOffset;
                    float travel = _Travel * (0.5 - 0.5 * cos(phase * TWO_PI));
                    float2 direction = (abs(position.x) > abs(position.y))
                        ? float2(sign(position.x), 0)
                        : float2(0, sign(position.y));
                    position += direction * travel;
                }

                // 이동한 위치에서도 돔 표면 바로 위에 붙도록 깊이를 다시 계산한다.
                float dome = sqrt(saturate(1.0 - dot(position, position) / (0.46 * 0.46)));
                float3 positionOS = float3(position, -0.14 - 0.18 * dome - 0.005);
                output.positionCS = TransformObjectToHClip(positionOS);
                output.fogFactor = ComputeFogFactor(output.positionCS.z);
                return output;
            }

        ENDHLSL

        Pass
        {
            Tags { "LightMode" = "UniversalForward" }
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog

            half4 Frag(Varyings input) : SV_Target
            {
                return half4(MixFog(_BaseColor.rgb, input.fogFactor), _BaseColor.a);
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            Cull Off
            ZWrite On
            ColorMask R

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment DepthFrag
            #pragma multi_compile_fog

            half DepthFrag() : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }
    }
}
