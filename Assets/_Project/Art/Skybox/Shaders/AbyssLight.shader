// 시스템 심연 3D 하늘 — 고리 빛 테두리·수직 빛 기둥 (가산).
// 메시(AbyssSkyMeshBuilder): uv.x = 띠 가로(0~1), uv.y = 길이 방향 거리(m), 색 r = 난수, a = 밝기(기둥은 아래로 갈수록 0).
// 길이 방향으로 데이터 펄스가 흐른다. 경보면 붉어지고 펄스는 호박색. 어둠이면 사라진다. (HoloCommon.hlsl)
Shader "Project/AbyssLight"
{
    Properties
    {
        [HDR] _BaseColor ("Color", Color) = (0.25, 0.9, 1.3, 1)
        [HDR] _PulseColor ("Pulse Color", Color) = (1.2, 2.4, 3.0, 1)
        [HDR] _AlarmPulseColor ("Alarm Pulse Color (호박색)", Color) = (2.0, 0.9, 0.15, 1)
        _PulseSpacing ("Pulse Spacing (m)", Float) = 60
        _PulseSpeed ("Pulse Speed", Float) = 0.3
        _PulseSharpness ("Pulse Sharpness", Float) = 40
        _CoreSharpness ("Core Sharpness", Range(0.5, 6)) = 2
        _FadeDistance ("Fade Distance (m)", Float) = 1000
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }
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
            #include "Assets/_Project/Art/Tutorial/Shaders/HoloCommon.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _PulseColor;
                half4 _AlarmPulseColor;
                float _PulseSpacing;
                float _PulseSpeed;
                float _PulseSharpness;
                float _CoreSharpness;
                float _FadeDistance;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                half4 color : COLOR;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.uv = input.uv;
                output.color = input.color;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float profile = pow(saturate(1.0 - abs((input.uv.x * 2.0) - 1.0)), _CoreSharpness);
                float phase = frac((input.uv.y / _PulseSpacing) - (_Time.y * _PulseSpeed) + (input.color.r * 7.31));
                float pulse = pow(phase, _PulseSharpness);

                half3 pulseColor = lerp(_PulseColor.rgb, _AlarmPulseColor.rgb, saturate(_HoloAlarm));
                half3 color = (HoloAlarmTint(_BaseColor.rgb) + (pulseColor * pulse)) * profile * input.color.a;

                float distance = length(input.positionWS - GetCameraPositionWS());
                color *= 1.0 - (saturate(distance / _FadeDistance) * 0.7);
                return half4(color * HoloVisibility(0.0), 0.0);
            }
            ENDHLSL
        }
    }
}
