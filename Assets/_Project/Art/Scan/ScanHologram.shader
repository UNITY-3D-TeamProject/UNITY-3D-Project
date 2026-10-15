// 스캔된 적 몸체에 씌우는 파란 홀로그램 셸 셰이더: 바깥 외곽선 + 가장자리 빛 + 흐르는 스캔 격자.
// 적과 같은 메시를 쓰는 자식 렌더러에 지정하고, SilhouetteReaction 으로 스캔 중에만 켜고 _Color 알파로 페이드한다.
// 외곽선은 SRPDefaultUnlit 패스(앞면 컬링 + 법선 방향 확장), 격자는 UniversalForward 패스로 같은 렌더러에서 두 번 그린다.
// 약점 구(_WeakPoints: xyz 월드 위치, w 반경) 안에 들어온 부분은 _WeakColor 로 칠한다. 구 값은 WeakPointShellMask 가 매 프레임 넘긴다.
Shader "Project/ScanHologram"
{
    Properties
    {
        [HDR] _Color ("Color", Color) = (0.2, 0.75, 1.0, 1.0)
        _OutlineWidth ("Outline Width (m)", Float) = 0.03
        _OutlineStrength ("Outline Strength", Range(0, 1)) = 0.9
        _RimPower ("Rim Power", Float) = 3
        _RimStrength ("Rim Strength", Range(0, 2)) = 0.9
        _GridDensity ("Grid Density (줄 / m)", Float) = 8
        _GridColumns ("Grid Columns (둘레 줄 수)", Float) = 24
        _GridWidth ("Grid Line Width", Range(0.01, 0.5)) = 0.06
        _GridStrength ("Grid Strength", Range(0, 1)) = 0.45
        _GridSpeed ("Grid Scroll Speed", Float) = 0.3
        _BaseAlpha ("Base Alpha", Range(0, 1)) = 0.08
        [HDR] _WeakColor ("Weak Point Color", Color) = (1.0, 0.15, 0.2, 1.0)
        _WeakEdge ("Weak Point Edge Softness (m)", Float) = 0.03
        _WeakIntensity ("Weak Point Intensity", Float) = 2.5
        _WeakFillAlpha ("Weak Point Fill", Range(0, 1)) = 0.45
        _WeakRingWidth ("Weak Point Ring Width (m)", Float) = 0.035
        _WeakPulseSpeed ("Weak Point Pulse Speed", Float) = 5
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

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        CBUFFER_START(UnityPerMaterial)
            half4 _Color;
            float _OutlineWidth;
            float _OutlineStrength;
            float _RimPower;
            float _RimStrength;
            float _GridDensity;
            float _GridColumns;
            float _GridWidth;
            float _GridStrength;
            float _GridSpeed;
            float _BaseAlpha;
            half4 _WeakColor;
            float _WeakEdge;
            float _WeakIntensity;
            float _WeakFillAlpha;
            float _WeakRingWidth;
            float _WeakPulseSpeed;
        CBUFFER_END

        // 머티리얼 값이 아니라 렌더러별로 MaterialPropertyBlock 으로 넘기는 값. 크기는 WeakPointShellMask.MAX_WEAK_POINTS 와 같아야 한다.
        #define MAX_WEAK_POINTS 4
        float4 _WeakPoints[MAX_WEAK_POINTS];
        float _WeakPointCount;

        // x: 월드 위치가 약점 구 안이면 1, 바깥이면 0(경계는 _WeakEdge 만큼 부드럽게).
        // y: 구 경계(표면과 구가 만나는 선) 근처면 1. 약점 범위를 테두리로 또렷하게 보이게 한다.
        float2 WeakPointMask(float3 positionWS)
        {
            float2 mask = 0.0;
            int count = min((int)_WeakPointCount, MAX_WEAK_POINTS);
            for (int i = 0; i < count; i++)
            {
                float distanceToCenter = distance(positionWS, _WeakPoints[i].xyz);
                float radius = _WeakPoints[i].w;
                float inside = 1.0 - smoothstep(radius - _WeakEdge, radius, distanceToCenter);
                float ring = 1.0 - smoothstep(0.0, _WeakRingWidth, abs(distanceToCenter - radius));
                mask = max(mask, float2(inside, ring));
            }
            return mask;
        }

        // 약점 색이 숨 쉬듯 밝아졌다 어두워지는 정도(0.6~1)
        float WeakPointPulse()
        {
            return 0.8 + (0.2 * sin(_Time.y * _WeakPulseSpeed));
        }

        struct Attributes
        {
            float4 positionOS : POSITION;
            float3 normalOS : NORMAL;
        };
        ENDHLSL

        // 외곽선: 법선 방향으로 부풀린 뒷면만 그려 몸체 바깥 테두리만 남긴다
        Pass
        {
            Name "Outline"
            Tags { "LightMode" = "SRPDefaultUnlit" }

            Blend SrcAlpha One
            ZWrite Off
            Cull Front

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 surfaceWS : TEXCOORD0;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;

                // 월드 공간에서 확장해야 오브젝트 스케일과 상관없이 두께가 일정하다
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = normalize(TransformObjectToWorldNormal(input.normalOS));

                // 약점 판정은 부풀리기 전 표면 위치로 해야 콜라이더 범위와 맞는다
                output.surfaceWS = positionWS;
                output.positionCS = TransformWorldToHClip(positionWS + (normalWS * _OutlineWidth));
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 weak = WeakPointMask(input.surfaceWS);
                half3 weakRgb = _WeakColor.rgb * _WeakIntensity * WeakPointPulse();
                half3 rgb = lerp(_Color.rgb, weakRgb, max(weak.x, weak.y));
                return half4(rgb, _Color.a * _OutlineStrength);
            }
            ENDHLSL
        }

        // 셸: 몸체 표면 바로 바깥에 가장자리 빛과 격자를 가산으로 덧그린다
        Pass
        {
            Name "Shell"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha One
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            // 몸체 표면과 깊이가 겹쳐 깜빡이지 않도록 아주 조금만 띄운다
            #define SHELL_OFFSET 0.005

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float3 positionOS : TEXCOORD2;
            };

            // 0~1 로 반복되는 좌표에서 정수 경계 근처만 1 이 되는 선
            float GridLine(float coordinate, float width)
            {
                float distanceToLine = abs(frac(coordinate + 0.5) - 0.5);
                return 1.0 - smoothstep(width * 0.5, width, distanceToLine);
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.normalWS = normalize(TransformObjectToWorldNormal(input.normalOS));
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz) + (output.normalWS * SHELL_OFFSET);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.positionOS = input.positionOS.xyz;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // 가장자리 빛: 비스듬히 보이는 면일수록 밝다
                float3 viewDirection = normalize(GetWorldSpaceViewDir(input.positionWS));
                float rim = pow(1.0 - saturate(dot(normalize(input.normalWS), viewDirection)), _RimPower) * _RimStrength;

                // 가로줄은 월드 높이를 따라 위로 흐르고, 세로줄은 오브젝트 둘레 각도로 나눈다
                float rows = GridLine((input.positionWS.y * _GridDensity) - (_Time.y * _GridSpeed), _GridWidth);
                float angle = atan2(input.positionOS.x, input.positionOS.z) / 6.2831853;
                float columns = GridLine(angle * _GridColumns, _GridWidth);
                float grid = max(rows, columns) * _GridStrength;

                // 약점 범위는 전체를 진하게 채우고(맥동), 경계선은 가장 밝게 그어 범위를 또렷하게 한다
                float2 weak = WeakPointMask(input.positionWS);
                float weakAmount = max(weak.x, weak.y);
                float pulse = WeakPointPulse();
                half normalAlpha = saturate(_BaseAlpha + rim + grid);
                half weakAlpha = saturate((_WeakFillAlpha * pulse) + rim + grid + weak.y);

                half alpha = lerp(normalAlpha, weakAlpha, weakAmount) * _Color.a;
                half3 rgb = lerp(_Color.rgb, _WeakColor.rgb * _WeakIntensity * pulse, weakAmount);
                return half4(rgb, alpha);
            }
            ENDHLSL
        }
    }
}
