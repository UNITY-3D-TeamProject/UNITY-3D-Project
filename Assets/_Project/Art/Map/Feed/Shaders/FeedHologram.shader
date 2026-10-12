// 피드 맵 2라운드의 홀로그램(피드, 태그 벽)용 셰이더: 월드 높이 기준 스캔라인 + 가장자리 빛 + 약한 깜빡임.
// 피드 맵은 배경이 밝아서 가산 블렌드 대신 알파 블렌드를 쓴다.
Shader "Project/FeedHologram"
{
    Properties
    {
        [HDR] _BaseColor ("Color", Color) = (0.3, 0.9, 1.0, 0.45)
        _ScanlineDensity ("Scanline Density (줄 / m)", Float) = 6
        _ScanlineSpeed ("Scanline Speed", Float) = 1.5
        _ScanlineStrength ("Scanline Strength", Range(0, 1)) = 0.35
        _RimPower ("Rim Power", Float) = 2.5
        _RimStrength ("Rim Strength", Range(0, 2)) = 0.8
        _FlickerSpeed ("Flicker Speed", Float) = 12
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

        Blend SrcAlpha OneMinusSrcAlpha
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
                half4 _BaseColor;
                float _ScanlineDensity;
                float _ScanlineSpeed;
                float _ScanlineStrength;
                float _RimPower;
                float _RimStrength;
                float _FlickerSpeed;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // 스캔라인: 월드 높이를 따라 흐르므로 오브젝트의 크기와 상관없이 줄 간격이 같다
                float wave = sin(((input.positionWS.y * _ScanlineDensity) - (_Time.y * _ScanlineSpeed)) * 6.2831853);
                float scan = 1.0 - (_ScanlineStrength * (0.5 + (0.5 * wave)));

                // 가장자리 빛: 비스듬히 보이는 면일수록 밝고 진하다
                float3 viewDirection = normalize(GetWorldSpaceViewDir(input.positionWS));
                float rim = pow(1.0 - abs(dot(normalize(input.normalWS), viewDirection)), _RimPower) * _RimStrength;

                float flicker = 0.92 + (0.08 * sin(_Time.y * _FlickerSpeed) * sin(_Time.y * 7.3));

                half3 rgb = _BaseColor.rgb * (1.0 + rim) * flicker;
                half alpha = saturate(_BaseColor.a * scan * (1.0 + rim) * flicker);
                return half4(rgb, alpha);
            }
            ENDHLSL
        }
    }
}
