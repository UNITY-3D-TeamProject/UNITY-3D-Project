// 시스템 심연 3D 하늘의 바탕 — 청록 대기 그라데이션만 그린다. 고리·기둥은 씬의 AbyssSkyRig 메시가 담당.
// 고리가 떠 있는 고도 부근에 옅은 안개 띠, 아래는 검은 심연.
// 전역 _HoloDarkness 면 완전히 검고, _HoloAlarm 이면 붉어진다. (HoloCommon.hlsl)
Shader "Project/AbyssAtmosphereSkybox"
{
    Properties
    {
        _TopColor ("Top Color", Color) = (0.02, 0.085, 0.11, 1)
        _HorizonColor ("Horizon Color", Color) = (0.015, 0.06, 0.08, 1)
        _BottomColor ("Bottom Color", Color) = (0.003, 0.01, 0.016, 1)
        _HazeColor ("Haze Band Color", Color) = (0.04, 0.15, 0.19, 1)
        _HazeHeight ("Haze Band Height (sin 고도)", Range(-1, 1)) = 0.45
        _HazeWidth ("Haze Band Width", Range(0.05, 1)) = 0.35
        _Exposure ("Exposure", Range(0, 3)) = 1
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

            CBUFFER_START(UnityPerMaterial)
                half4 _TopColor;
                half4 _HorizonColor;
                half4 _BottomColor;
                half4 _HazeColor;
                float _HazeHeight;
                float _HazeWidth;
                float _Exposure;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 dir : TEXCOORD0; };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.dir = input.positionOS.xyz;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float y = normalize(input.dir).y;
                float height = abs(y);
                half3 col = (y >= 0.0) ? lerp(_HorizonColor.rgb, _TopColor.rgb, saturate(pow(height, 0.6))) : lerp(_HorizonColor.rgb, _BottomColor.rgb, saturate(pow(height, 0.4)));
                float band = (y - _HazeHeight) / _HazeWidth;
                col += _HazeColor.rgb * exp(-band * band);

                col *= _Exposure * HoloVisibility(0.0);
                return half4(HoloAlarmTint(col), 1.0);
            }
            ENDHLSL
        }
    }
}
