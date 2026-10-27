// 보스 경고 줄무늬 — 반투명 사선 줄무늬가 한 방향으로 흐른다.
// 월드 좌표로 줄을 그어 오브젝트를 어떤 비율로 늘려도 줄 폭이 같다(박수 상자 예고처럼 크기를 바꿔 쓰는 예고용).
// 밖에서는 앞면만 그린다. 양면을 그리면 뒷면 줄과 엇갈려 격자처럼 보인다.
// 카메라가 상자 안에 들어오면 안쪽 면을 그려 둘러싸인 것이 보이게 한다. 상자 판정은 유니티 기본 Cube(-0.5~0.5) 메시 기준이다.
Shader "Project/BossWarningStripe"
{
    Properties
    {
        _StripeColor ("Stripe Color", Color) = (1, 0.5, 0, 0.6)
        _GapColor ("Gap Color", Color) = (0, 0, 0, 0.35)
        _StripeWidth ("Stripe Width (m)", Float) = 0.6
        _StripeRatio ("Stripe Ratio", Range(0, 1)) = 0.5
        _ScrollSpeed ("Scroll Speed (m/s)", Float) = 1
        _InsideOpacity ("Inside Opacity", Range(0, 1)) = 0.5
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        CBUFFER_START(UnityPerMaterial)
            half4 _StripeColor;
            half4 _GapColor;
            float _StripeWidth;
            float _StripeRatio;
            float _ScrollSpeed;
            float _InsideOpacity;
        CBUFFER_END

        struct Attributes
        {
            float4 positionOS : POSITION;
        };

        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            float3 positionWS : TEXCOORD0;
            float3 positionOS : TEXCOORD1;
        };

        Varyings vert(Attributes input)
        {
            Varyings output;
            output.positionOS = input.positionOS.xyz;
            output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
            output.positionCS = TransformWorldToHClip(output.positionWS);
            return output;
        }

        half4 GetStripeColor(float3 positionWS)
        {
            // x·y·z 를 더하면 상자의 어느 면에서도 사선이 된다
            float position = (positionWS.x + positionWS.y + positionWS.z + (_Time.y * _ScrollSpeed)) /
                             max(_StripeWidth * 2.0, 0.0001);
            float cell = frac(position);

            // 경계를 화면 픽셀 한 칸 폭으로 부드럽게 해 멀리서 계단처럼 보이지 않게 한다
            float edge = max(fwidth(position), 0.0001);
            float stripe = smoothstep(_StripeRatio + edge, _StripeRatio - edge, cell) *
                           smoothstep(-edge, edge, cell);
            return lerp(_GapColor, _StripeColor, stripe);
        }
        ENDHLSL

        // 안쪽 면. 카메라가 상자 밖이면 그리지 않는다.
        // 바닥면은 실제 바닥과 같은 높이라 겹쳐 깜빡이므로 그리지 않고, 다가오는 손을 가리지 않게 _InsideOpacity 만큼 옅게 그린다
        Pass
        {
            Name "Inside"
            Tags { "LightMode" = "SRPDefaultUnlit" }
            Cull Front

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment fragInside

            half4 fragInside(Varyings input) : SV_Target
            {
                float3 cameraOS = TransformWorldToObject(_WorldSpaceCameraPos);
                bool isCameraInside = all(abs(cameraOS) <= 0.5);
                bool isBottomFace = input.positionOS.y < -0.499;
                clip((isCameraInside && !isBottomFace) ? 1.0 : -1.0);

                half4 color = GetStripeColor(input.positionWS);
                color.a *= _InsideOpacity;
                return color;
            }
            ENDHLSL
        }

        // 바깥쪽 면. 카메라가 상자 안이면 모든 면이 뒤를 보여 자연히 그려지지 않는다
        Pass
        {
            Name "Outside"
            Tags { "LightMode" = "UniversalForward" }
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment fragOutside

            half4 fragOutside(Varyings input) : SV_Target
            {
                return GetStripeColor(input.positionWS);
            }
            ENDHLSL
        }
    }
}
