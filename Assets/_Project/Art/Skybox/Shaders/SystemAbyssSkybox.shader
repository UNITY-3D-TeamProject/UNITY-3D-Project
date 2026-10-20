// 시스템 심연: C 시안의 미세 구조와 대기를 보존하는 파노라마 기반 하늘.
// 전역 어둠·경보는 기존 TutorialAtmosphere와 동일하게 반영한다.
Shader "Project/SystemAbyssSkybox"
{
    Properties
    {
        [NoScaleOffset] _SkyTex ("System Abyss Panorama", 2D) = "black" {}
        [NoScaleOffset] _ReferenceTex ("C Reference Board (Upper Cyan Panel)", 2D) = "black" {}
        _Exposure ("Exposure", Range(0, 3)) = 1
        _Rotation ("Rotation", Range(0, 360)) = 0
        _RotationSpeed ("Gentle Drift Speed", Range(0, 0.2)) = 0.025
        _PulseStrength ("Circuit Pulse Strength", Range(0, 0.3)) = 0.06
        _PulseSpeed ("Circuit Pulse Speed", Range(0, 1)) = 0.06
    }

    SubShader
    {
        Tags { "Queue" = "Background" "RenderType" = "Background" "PreviewType" = "Skybox" "RenderPipeline" = "UniversalPipeline" }
        Cull Off
        ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Assets/_Project/Art/Tutorial/Shaders/HoloCommon.hlsl"

            TEXTURE2D(_SkyTex);
            SAMPLER(sampler_SkyTex);
            TEXTURE2D(_ReferenceTex);
            SAMPLER(sampler_ReferenceTex);

            CBUFFER_START(UnityPerMaterial)
                float _Exposure;
                float _Rotation;
                float _RotationSpeed;
                float _PulseStrength;
                float _PulseSpeed;
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

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.direction = input.positionOS.xyz;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float3 direction = normalize(input.direction);
                float longitude = atan2(direction.x, direction.z) / TWO_PI;
                float latitude = asin(clamp(direction.y, -1.0, 1.0)) / PI + 0.5;
                float turn = (_Rotation + sin(_Time.y * _RotationSpeed) * 0.6) / 360.0;
                float2 uv = float2(frac(longitude + 0.5 + turn), latitude);
                half3 color = SAMPLE_TEXTURE2D(_SkyTex, sampler_SkyTex, uv).rgb;
                half3 seamColor = (SAMPLE_TEXTURE2D(_SkyTex, sampler_SkyTex, float2(0.01, latitude)).rgb
                    + SAMPLE_TEXTURE2D(_SkyTex, sampler_SkyTex, float2(0.99, latitude)).rgb) * 0.5;
                float seamBlend = 1.0 - smoothstep(0.0, 0.012, min(uv.x, 1.0 - uv.x));
                color = lerp(color, seamColor, seamBlend);

                // 정면의 세밀한 회로는 선택한 C 원본을 투영해 보존한다.
                // 원본 위쪽 패널만 읽고 가장자리는 전체 파노라마로 부드럽게 연결한다.
                float azimuth = (longitude + turn) * TWO_PI;
                float horizontalLength = length(direction.xz);
                float3 rotatedDirection = float3(sin(azimuth) * horizontalLength,
                    direction.y, cos(azimuth) * horizontalLength);
                const float referencePitch = 0.34202014; // sin(20도)
                const float referenceCosine = 0.93969262;
                float referenceForward = dot(rotatedDirection, float3(0.0, referencePitch, referenceCosine));
                float referenceUp = dot(rotatedDirection, float3(0.0, referenceCosine, -referencePitch));
                float2 referenceUV = float2(rotatedDirection.x / 2.0, referenceUp / 0.65)
                    / max(referenceForward, 0.001) * 0.5 + 0.5;
                float2 referenceBorder = min(referenceUV, 1.0 - referenceUV);
                float referenceBlend = smoothstep(0.0, 0.12, referenceBorder.x) * step(0.0, referenceForward);
                float2 boardUV = float2(saturate(referenceUV.x), 0.502 + saturate(referenceUV.y) * 0.496);
                half3 referenceColor = SAMPLE_TEXTURE2D(_ReferenceTex, sampler_ReferenceTex, boardUV).rgb;
                referenceColor = lerp(referenceColor, half3(0.003, 0.018, 0.028), smoothstep(0.99, 1.08, referenceUV.y));
                color = lerp(color, referenceColor, referenceBlend);

                // 원본 형태를 움직여 왜곡하지 않고 밝은 회로 부분에만 약한 주사 펄스.
                float luminance = dot(color, float3(0.2126, 0.7152, 0.0722));
                float circuitMask = smoothstep(0.06, 0.28, luminance);
                float scanPhase = frac(latitude * 5.0 + _Time.y * _PulseSpeed);
                float scanPulse = pow(saturate(1.0 - abs(scanPhase - 0.5) * 2.0), 12.0);
                color *= 1.0 + circuitMask * scanPulse * _PulseStrength;
                color *= _Exposure * HoloVisibility(0.0);
                return half4(HoloAlarmTint(color), 1.0);
            }
            ENDHLSL
        }
    }
}

