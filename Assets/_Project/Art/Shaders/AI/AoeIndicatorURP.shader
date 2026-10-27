Shader "Project/AoeIndicator"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Filled Color", Color) = (1,1,1,1)
        _FillColor ("Empty Color", Color) = (1,1,1,0.2)
        _FillProgress ("Fill Progress", Range(0,1)) = 1
        _Progress ("Arc Fill Progress", Range(0,1)) = 1
        _Arc ("Arc Mask", Range(0,1)) = 0
        _Angle ("Arc Direction", Range(0,1)) = 0
        _FadeAmount ("Fade Amount", Range(0,1)) = 0
        [HideInInspector] _ShapeMode ("Shape Mode", Float) = 0
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Tags { "LightMode"="SRPDefaultUnlit" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            Offset -1, -1

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4 _Color;
                half4 _FillColor;
                float _FillProgress;
                float _Progress;
                float _Arc;
                float _Angle;
                float _FadeAmount;
                float _ShapeMode;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; float fog : TEXCOORD1; };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.fog = ComputeFogFactor(output.positionCS.z);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                float filled = 1;
                if (_ShapeMode > 0.5 && _ShapeMode < 2.5)
                {
                    float2 offset = input.uv - 0.5;
                    float progress = _ShapeMode < 1.5 ? _FillProgress : _Progress;
                    filled = step(length(offset), progress * 0.5);
                    if (_ShapeMode > 1.5)
                    {
                        float angle = atan2(offset.y, offset.x);
                        float normalized = frac((angle / PI + 1) * 0.5 + _Angle + _Arc * 0.5);
                        tex.a *= step(_Arc, normalized);
                    }
                }
                else if (_ShapeMode > 2.5)
                {
                    filled = step(input.uv.y, _FillProgress);
                    if (_ShapeMode > 3.5 && _FadeAmount > 0)
                        tex.a *= saturate(input.uv.y / max(_FadeAmount, 0.0001));
                }
                half4 color = tex * lerp(_FillColor, _Color, filled);
                color.rgb = MixFog(color.rgb, input.fog);
                return color;
            }
            ENDHLSL
        }
    }
}
