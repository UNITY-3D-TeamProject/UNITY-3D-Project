// 로비 바이러스 침공: 전광판·광고판 화면 위에 덮는 감염 화면 (URP, 조명 없음, 반투명, 양면).
// 평소엔 보이지 않다가 _CycleLength 초마다 지직(노이즈) → 감염 화면(가끔 가로 밀림) → 지직 → 사라짐을 반복한다.
// 오브젝트 위치로 위상을 달리해 화면끼리 동시에 바뀌지 않는다. 덮을 화면보다 늦게 그리도록 Queue 를 높여 둔다.
Shader "Project/InfectedOverlay"
{
    Properties
    {
        _BaseMap ("Infected Screen", 2D) = "white" {}
        _Intensity ("Intensity", Range(0, 4)) = 1.4
        _CycleLength ("Cycle Length (s)", Float) = 7
        _ShowStart ("Show Start (0 ~ 1 of cycle)", Range(0, 1)) = 0.45
        _ShowEnd ("Show End (0 ~ 1 of cycle)", Range(0, 1)) = 0.95
        _NoiseDuration ("Noise Duration (s)", Range(0, 1)) = 0.35
        _GlitchShift ("Glitch Shift", Range(0, 0.2)) = 0.03
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent+10"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "Unlit"
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half _Intensity;
                float _CycleLength;
                float _ShowStart;
                float _ShowEnd;
                float _NoiseDuration;
                float _GlitchShift;
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
                float phase : TEXCOORD1;
                half fogFactor : TEXCOORD2;
            };

            float Hash(float2 p)
            {
                return frac(sin(dot(p, float2(12.9898, 78.233))) * 43758.5453);
            }

            // 오브젝트 월드 위치로 0 ~ 1 위상. 좌표가 커도 GPU sin 정밀도에 기대지 않는 해시를 쓴다.
            float ObjectPhase()
            {
                float3 p = frac(float3(UNITY_MATRIX_M._m03, UNITY_MATRIX_M._m13, UNITY_MATRIX_M._m23) * 0.1031);
                p += dot(p, p.zyx + 31.32);
                return frac((p.x + p.y) * p.z);
            }

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.phase = ObjectPhase();
                output.fogFactor = ComputeFogFactor(output.positionCS.z);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float cycleTime = frac(_Time.y / _CycleLength + input.phase) * _CycleLength;
                float showStart = _ShowStart * _CycleLength;
                float showEnd = _ShowEnd * _CycleLength;

                half isShown = step(showStart, cycleTime) * step(cycleTime, showEnd);
                // 바뀌는 순간 앞뒤로 지직
                half isNoise = max(
                    step(showStart - _NoiseDuration, cycleTime) * step(cycleTime, showStart + _NoiseDuration * 0.4),
                    step(showEnd - _NoiseDuration * 0.4, cycleTime) * step(cycleTime, showEnd + _NoiseDuration * 0.6));

                // 보이는 동안 가끔 가로 띠가 밀림
                float step20 = floor(_Time.y * 20.0);
                float band = floor(input.uv.y * 18.0);
                float bandRand = Hash(float2(band, step20 + input.phase * 97.0));
                float glitchOn = step(0.93, Hash(float2(step20, input.phase * 31.0)));
                float shift = (bandRand - 0.5) * 2.0 * _GlitchShift * step(0.55, bandRand) * glitchOn;

                half4 screen = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv + float2(shift, 0.0));
                screen.rgb *= _Intensity;

                // 지직: 굵은 픽셀 노이즈 + 붉은 가로줄
                float2 cell = floor(input.uv * float2(90.0, 56.0));
                half grain = Hash(cell + floor(_Time.y * 30.0));
                half redLine = step(0.8, Hash(float2(floor(input.uv.y * 70.0), floor(_Time.y * 24.0))));
                half3 noiseRgb = lerp(grain.xxx * 0.9, half3(1.0, 0.24, 0.35) * 1.6, redLine);
                half4 noise = half4(noiseRgb, 0.92);

                half4 color = lerp(screen * isShown, noise, isNoise);
                return half4(MixFog(color.rgb, input.fogFactor), color.a);
            }
            ENDHLSL
        }
    }
}
