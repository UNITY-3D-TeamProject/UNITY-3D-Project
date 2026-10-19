Shader "Hidden/Project/SpeedEdgeBlur"
{
    Properties
    {
        _BlurStrength ("Blur Strength", Range(0, 0.2)) = 0.06
        _CenterRadius ("Center Radius", Range(0, 1)) = 0.30
        _Feather ("Feather", Range(0.01, 1)) = 0.25
        _SampleCount ("Sample Count", Range(2, 16)) = 8
        _Center ("Center", Vector) = (0.5, 0.5, 0, 0)
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "SpeedEdgeBlur"
            ZTest Always
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float _BlurStrength;
            float _CenterRadius;
            float _Feather;
            float _SampleCount;
            float4 _Center;
            // 머티리얼이 아닌 MaterialPropertyBlock 으로만 전달된다
            float _SpeedEdgeBlurWeight;

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 uv = input.texcoord;
                half4 color = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);

                // 화면 비율을 보정한 중심으로부터의 거리 (세로 기준 0.5 = 화면 가장자리)
                float2 offsetFromCenter = uv - _Center.xy;
                offsetFromCenter.x *= _ScreenParams.x / _ScreenParams.y;
                float dist = length(offsetFromCenter);

                float mask = smoothstep(_CenterRadius, _CenterRadius + _Feather, dist) * _SpeedEdgeBlurWeight;
                if (mask <= 0.0)
                    return color;

                // 중심에서 바깥으로 향하는 방향을 따라 샘플링하는 방사형 블러
                float2 direction = uv - _Center.xy;
                float2 stepUv = direction * (_BlurStrength * mask);
                int sampleCount = clamp((int)_SampleCount, 2, 16);

                half4 sum = color;
                float count = 1.0;
                [unroll]
                for (int i = 1; i < 16; i++)
                {
                    if (i >= sampleCount)
                        break;

                    float t = (float)i / (float)(sampleCount - 1);
                    sum += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv - stepUv * t);
                    count += 1.0;
                }

                return sum / count;
            }
            ENDHLSL
        }
    }

    Fallback Off
}
