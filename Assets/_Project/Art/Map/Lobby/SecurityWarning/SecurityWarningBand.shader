// 로비 보안 경고 띠 (URP, 조명 없음, 반투명, 양면).
// 보안 발판을 둘러싼 원통 띠에 글자 텍스처를 가로로 흘린다.
// - 바깥면: _BaseMap (글자 있는 띠), 안쪽면: _BackMap (글자 없는 띠, 비우면 안쪽은 안 보임 = 글자 뒤집힘 방지)
// - _ScrollSpeed: 초당 UV 가로 이동량(타일 단위, 음수면 반대 방향)
Shader "Project/SecurityWarningBand"
{
    Properties
    {
        _BaseMap ("Front Texture", 2D) = "white" {}
        _BackMap ("Back Texture (안쪽면)", 2D) = "black" {}
        [HDR] _Color ("Tint", Color) = (1, 1, 1, 1)
        [HDR] _BackColor ("Back Tint", Color) = (0.6, 0.6, 0.6, 0.8)
        _ScrollSpeed ("Scroll Speed (tile/s)", Float) = 0.05
        _FlickerStrength ("Flicker Strength", Range(0, 1)) = 0.08
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
            TEXTURE2D(_BackMap);
            SAMPLER(sampler_BackMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _BackMap_ST;
                half4 _Color;
                half4 _BackColor;
                float _ScrollSpeed;
                half _FlickerStrength;
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
                half fogFactor : TEXCOORD1;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                output.fogFactor = ComputeFogFactor(output.positionCS.z);
                return output;
            }

            half4 Frag(Varyings input, bool isFrontFace : SV_IsFrontFace) : SV_Target
            {
                float2 scroll = float2(_Time.y * _ScrollSpeed, 0.0);
                half4 color;
                if (isFrontFace)
                    color = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv * _BaseMap_ST.xy + _BaseMap_ST.zw + scroll) * _Color;
                else
                    color = SAMPLE_TEXTURE2D(_BackMap, sampler_BackMap, input.uv * _BackMap_ST.xy + _BackMap_ST.zw + scroll) * _BackColor;

                // 전광판처럼 미세하게 떨림
                half hum = 1.0 - _FlickerStrength * (0.5 + 0.5 * sin(_Time.y * 31.0 + input.uv.x * 6.2832));
                return half4(MixFog(color.rgb * hum, input.fogFactor), color.a);
            }
            ENDHLSL
        }
    }
}
