Shader "Project/DataPacketUnlit"
{
    Properties
    {
        [HDR] _BaseColor ("Packet Color", Color) = (1, 1, 1, 1)
        _Intensity ("Intensity", Float) = 2.2
        _EdgeWidth ("Edge Width", Range(0.005, 0.2)) = 0.045
        _Fill ("Interior Fill", Range(0, 1)) = 0.11
        _Details ("Packet Details", Range(0, 1)) = 1
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "DataPacket"
            Tags { "LightMode" = "SRPDefaultUnlit" }

            Blend SrcAlpha One
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                float _Intensity;
                float _EdgeWidth;
                float _Fill;
                float _Details;
            CBUFFER_END

            struct SAttributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct SVaryings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            SVaryings Vert(SAttributes input)
            {
                SVaryings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                output.color = input.color * _BaseColor;
                return output;
            }

            float GetRectangleMask(float2 uv, float2 center, float2 halfSize)
            {
                float2 edgeDistance = abs(uv - center) - halfSize;
                float distance = max(edgeDistance.x, edgeDistance.y);
                float antialiasWidth = max(fwidth(distance), 0.0001);
                return 1.0 - smoothstep(-antialiasWidth, antialiasWidth, distance);
            }

            half4 Frag(SVaryings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                // Keep the outline inside the quad so every edge is antialiased.
                float outer = GetRectangleMask(input.uv, float2(0.5, 0.5), float2(0.46, 0.46));
                float innerSize = 0.46 - _EdgeWidth;
                float inner = GetRectangleMask(input.uv, float2(0.5, 0.5), float2(innerSize, innerSize));
                float border = saturate(outer - inner);

                // Small header bars and one inset corner leave most of the packet empty.
                float details = GetRectangleMask(input.uv, float2(0.215, 0.80), float2(0.075, 0.012));
                details = max(details, GetRectangleMask(input.uv, float2(0.345, 0.80), float2(0.025, 0.012)));
                details = max(details, GetRectangleMask(input.uv, float2(0.415, 0.80), float2(0.015, 0.012)));
                details = max(details, GetRectangleMask(input.uv, float2(0.76, 0.19), float2(0.055, 0.012)));
                details = max(details, GetRectangleMask(input.uv, float2(0.803, 0.23), float2(0.012, 0.052)));

                float coverage = max(border, max(inner * _Fill, details * _Details * 0.55));
                return half4(input.color.rgb * max(_Intensity, 0.0), input.color.a * saturate(coverage));
            }
            ENDHLSL
        }
    }

    FallBack Off
}
