// 보스 그로기 과녁 — 텍스처 없이 수식(SDF)으로 그린 크로스헤어. 쿼드(-0.5~0.5) 하나에 씌운다.
// 쿼드는 정점 셰이더에서 카메라를 향해 돌려(빌보드) 오브젝트 회전과 무관하게 항상 정면으로 보인다. 크기는 오브젝트 월드 스케일을 따른다.
// 바깥 고리 + 십자선 + 중앙 점은 천천히 돌고, 안쪽으로 수축하는 고리가 반복해 "여기를 조준하라"로 읽힌다.
// ZTest Always 라 손 메시에 가려지지 않는다.
Shader "Project/BossGroggyTarget"
{
    Properties
    {
        [HDR] _Color ("Color", Color) = (4, 3, 0.4, 1)
        _RingRadius ("Outer Ring Radius", Range(0.1, 1)) = 0.82
        _RingWidth ("Ring Width", Range(0.005, 0.2)) = 0.035
        _BarWidth ("Cross Bar Width", Range(0.005, 0.2)) = 0.035
        _BarInner ("Cross Bar Inner Radius", Range(0, 1)) = 0.16
        _BarOuter ("Cross Bar Outer Radius", Range(0, 1)) = 0.98
        _PulseSpeed ("Shrink Ring Speed (cycle/s)", Float) = 0.8
        _RotateSpeed ("Rotate Speed (rad/s)", Float) = 0.5
    }

    SubShader
    {
        Tags { "Queue" = "Transparent+100" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "Target"
            Tags { "LightMode" = "SRPDefaultUnlit" }
            Blend SrcAlpha One
            ZWrite Off
            ZTest Always
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                float _RingRadius;
                float _RingWidth;
                float _BarWidth;
                float _BarInner;
                float _BarOuter;
                float _PulseSpeed;
                float _RotateSpeed;
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
            };

            Varyings vert(Attributes input)
            {
                float4x4 objectToWorld = GetObjectToWorldMatrix();
                float3 centerWS = float3(objectToWorld._m03, objectToWorld._m13, objectToWorld._m23);
                float scaleX = length(float3(objectToWorld._m00, objectToWorld._m10, objectToWorld._m20));
                float scaleY = length(float3(objectToWorld._m01, objectToWorld._m11, objectToWorld._m21));
                float3 cameraRight = UNITY_MATRIX_V[0].xyz;
                float3 cameraUp = UNITY_MATRIX_V[1].xyz;

                float3 positionWS = centerWS
                    + cameraRight * (input.positionOS.x * scaleX)
                    + cameraUp * (input.positionOS.y * scaleY);

                Varyings output;
                output.positionCS = TransformWorldToHClip(positionWS);
                output.uv = input.uv;
                return output;
            }

            // 거리 d 가 선 두께 안이면 1, 화면 픽셀 한 칸 폭으로 부드럽게 0 이 된다
            float Line(float distanceToLine, float width, float pixel)
            {
                return 1.0 - smoothstep(width, width + pixel, distanceToLine);
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 p = (input.uv - 0.5) * 2.0;
                float pixel = max(fwidth(length(p)), 0.0005);

                float angle = _Time.y * _RotateSpeed;
                float sine = sin(angle);
                float cosine = cos(angle);
                float2 rotated = float2(p.x * cosine - p.y * sine, p.x * sine + p.y * cosine);
                float radius = length(p);

                float outerRing = Line(abs(radius - _RingRadius), _RingWidth, pixel);

                // 십자선: 안쪽 반지름은 비워 중앙을 열어 둔다
                float2 absolute = abs(rotated);
                float inBarRange = step(_BarInner, max(absolute.x, absolute.y)) * step(max(absolute.x, absolute.y), _BarOuter);
                float horizontalBar = Line(absolute.y, _BarWidth, pixel) * inBarRange * step(absolute.y, absolute.x);
                float verticalBar = Line(absolute.x, _BarWidth, pixel) * inBarRange * step(absolute.x, absolute.y);

                float centerDot = Line(radius, _BarWidth * 1.6, pixel);

                // 바깥에서 안쪽으로 수축하는 고리. 나타날 때 서서히 켜지고 안쪽에서 사라진다
                float phase = frac(_Time.y * _PulseSpeed);
                float shrinkRadius = lerp(_RingRadius, _BarInner, phase);
                float shrinkRing = Line(abs(radius - shrinkRadius), _RingWidth * 0.8, pixel)
                    * saturate(phase * 6.0) * (1.0 - phase);

                float shape = saturate(outerRing + horizontalBar + verticalBar + centerDot + shrinkRing);
                return half4(_Color.rgb, _Color.a * shape);
            }
            ENDHLSL
        }
    }
}
