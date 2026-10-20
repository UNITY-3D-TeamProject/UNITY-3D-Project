// 튜토리얼 홀로 발판 윗면용 셰이더: Quad 위에 모서리가 둥근 사각 테두리를 그리고, 안쪽에서 바깥으로 퍼지는 고리를 반복한다.
// 스크립트 없이 _Time 으로 움직이므로 발판이 많아도 비용이 거의 없다.
// 튜토리얼 하늘이 밝아 가산 블렌드는 색이 하얗게 날아가므로 알파 블렌드를 쓴다.
Shader "Project/HoloRings"
{
    Properties
    {
        [HDR] _Color ("Ring Color", Color) = (0.37, 0.95, 1.0, 1)
        [HDR] _BorderColor ("Border Color", Color) = (0.37, 0.95, 1.0, 1)
        _RingCount ("Ring Count", Float) = 3
        _RingSpeed ("Ring Speed (바퀴 / 초)", Float) = 0.5
        _RingWidth ("Ring Width (UV)", Float) = 0.012
        _BorderWidth ("Border Width (UV)", Float) = 0.018
        _CornerRadius ("Corner Radius (UV)", Range(0, 0.5)) = 0.17
        [Toggle] _DarkImmune ("Dark Immune (어둠에서도 보임)", Float) = 0
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
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "HoloCommon.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half4 _BorderColor;
                float _RingCount;
                float _RingSpeed;
                float _RingWidth;
                float _BorderWidth;
                float _CornerRadius;
                float _DarkImmune;
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
                float fogFactor : TEXCOORD1;
            };

            // 반 크기 halfSize, 모서리 반지름 radius 인 둥근 사각형까지의 거리 (안쪽 음수)
            float RoundedBox(float2 p, float halfSize, float radius)
            {
                float2 q = abs(p) - (halfSize - radius);
                return length(max(q, 0.0)) + min(max(q.x, q.y), 0.0) - radius;
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                output.fogFactor = ComputeFogFactor(output.positionCS.z);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 p = input.uv - 0.5;

                // 바깥 테두리
                float border = 1.0 - smoothstep(0.0, _BorderWidth, abs(RoundedBox(p, 0.49, _CornerRadius)));

                // 퍼지는 고리: 작게 시작해 테두리 쪽으로 커지며 흐려진다
                float rings = 0.0;
                for (int i = 0; i < 4; i++)
                {
                    if (i >= _RingCount) break;

                    float k = frac((_Time.y * _RingSpeed) + (i / _RingCount));
                    float halfSize = lerp(0.08, 0.47, k);
                    float radius = _CornerRadius * (halfSize / 0.49);
                    float ring = 1.0 - smoothstep(0.0, _RingWidth, abs(RoundedBox(p, halfSize, radius)));
                    rings += ring * (1.0 - k);
                }

                // 겹친 만큼 색을 섞고, 덮는 정도는 알파로
                float mask = border + rings;
                half3 rgb = ((_BorderColor.rgb * border) + (_Color.rgb * rings)) / max(mask, 0.0001);

                // 튜토리얼 어둠·경보·안개
                float alpha = saturate(mask) * HoloVisibility(_DarkImmune) * HoloFogVisibility(input.fogFactor);
                return half4(HoloAlarmTint(rgb), alpha);
            }
            ENDHLSL
        }
    }
}
