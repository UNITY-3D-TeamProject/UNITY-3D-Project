// 튜토리얼 홀로그램 몸체용 셰이더 (알파 블렌드, HDR).
// 가장자리 빛(Fresnel) + 가는 주사선 + 위로 지나가는 밝은 스캔 띠 + 월드 기준 격자선 + 가끔 가로로 어긋나는 글리치.
// 튜토리얼 하늘이 밝아 가산 블렌드는 색이 날아가므로 알파 블렌드에 HDR 색을 써서 Bloom 으로 빛나게 한다.
Shader "Project/Hologram"
{
    Properties
    {
        [HDR] _BaseColor ("Base Color (A = 몸체 투명도)", Color) = (0.25, 0.85, 1.0, 0.25)
        [HDR] _RimColor ("Rim Color", Color) = (1.2, 3.2, 4.0, 1)
        _RimPower ("Rim Power", Float) = 2.5

        [Header(Scanline)]
        _ScanlineDensity ("Scanline Density (줄 / m)", Float) = 8
        _ScanlineStrength ("Scanline Strength", Range(0, 1)) = 0.35

        [Header(Scan Band)]
        [HDR] _BandColor ("Band Color", Color) = (1.5, 3.5, 4.5, 1)
        _BandSpeed ("Band Speed (m / 초)", Float) = 2.5
        _BandPeriod ("Band Period (m)", Float) = 12
        _BandWidth ("Band Width (m)", Float) = 0.6
        _BandAxis ("Band Axis (월드 방향, 기본 위쪽)", Vector) = (0, 1, 0, 0)

        [Header(Grid)]
        [HDR] _GridColor ("Grid Color", Color) = (0.8, 2.4, 3.2, 1)
        _GridSize ("Grid Size (m)", Float) = 0.5
        _GridWidth ("Grid Line Width", Range(0, 0.2)) = 0.03

        [Header(Glitch)]
        _GlitchStrength ("Glitch Offset (m)", Float) = 0.05
        _FlickerSpeed ("Flicker Speed", Float) = 9

        [Header(Tutorial Atmosphere)]
        [Toggle] _DarkImmune ("Dark Immune (어둠에서도 보임)", Float) = 0
        [Toggle] _AlarmImmune ("Alarm Immune (경보에도 원래 색)", Float) = 0
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
                half4 _BaseColor;
                half4 _RimColor;
                float _RimPower;
                float _ScanlineDensity;
                float _ScanlineStrength;
                half4 _BandColor;
                float _BandSpeed;
                float _BandPeriod;
                float _BandWidth;
                float4 _BandAxis;
                half4 _GridColor;
                float _GridSize;
                float _GridWidth;
                float _GlitchStrength;
                float _FlickerSpeed;
                float _DarkImmune;
                float _AlarmImmune;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float fogFactor : TEXCOORD2;
            };

            float Hash(float n)
            {
                return frac(sin(n) * 43758.5453);
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);

                // 글리치: 시간 칸마다 무작위 높이의 얇은 가로 띠를 옆으로 살짝 민다
                float slot = floor(_Time.y * 6.0);
                float bandY = Hash(slot) * 40.0 - 20.0;
                float isOn = step(0.82, Hash(slot + 7.1));
                float inBand = step(abs(frac((positionWS.y - bandY) / 3.0) - 0.5), 0.04);
                positionWS.x += (Hash(slot + 3.3) - 0.5) * 2.0 * _GlitchStrength * isOn * inBand;

                output.positionWS = positionWS;
                output.positionCS = TransformWorldToHClip(positionWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.fogFactor = ComputeFogFactor(output.positionCS.z);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float3 p = input.positionWS;

                // 튜토리얼 어둠: 칸 단위로 조각조각 사라진다
                float visibility = HoloVisibility(_DarkImmune);
                clip(HoloCellNoise(p, 0.8) - (1.0 - visibility) - 0.0001);

                float3 n = normalize(input.normalWS);
                float3 viewDirection = normalize(GetWorldSpaceViewDir(p));

                float rim = pow(1.0 - saturate(abs(dot(n, viewDirection))), _RimPower);

                float scan = 1.0 - (_ScanlineStrength * (0.5 + (0.5 * sin(p.y * _ScanlineDensity * 6.2831853))));

                // _BandAxis 방향으로 지나가는 밝은 띠 (파이프에서는 길이 방향으로 흐르게 쓴다)
                float bandCoord = dot(p, normalize(_BandAxis.xyz));
                float bandPos = frac((bandCoord - (_Time.y * _BandSpeed)) / _BandPeriod) * _BandPeriod;
                float band = 1.0 - smoothstep(0.0, _BandWidth, abs(bandPos - (_BandPeriod * 0.5)));

                // 월드 기준 격자선 (면 방향에 맞는 두 축만 쓴다)
                float3 g = abs(frac(p / _GridSize) - 0.5);
                float3 lines = 1.0 - smoothstep(0.0, _GridWidth, 0.5 - g);
                float3 w = abs(n);
                float grid = (max(lines.y, lines.z) * w.x) + (max(lines.x, lines.z) * w.y) + (max(lines.x, lines.y) * w.z);
                grid = saturate(grid);

                float flicker = 0.93 + (0.07 * sin(_Time.y * _FlickerSpeed) * sin(_Time.y * 3.7 + p.x));

                half3 rgb = (_BaseColor.rgb * scan) + (_RimColor.rgb * rim) + (_BandColor.rgb * band * 0.6) + (_GridColor.rgb * grid);
                half alpha = saturate(((_BaseColor.a * scan) + (rim * 0.85) + (band * 0.35) + (grid * 0.5)) * flicker);

                // 경보 색, 안개만큼 옅어짐
                rgb = lerp(HoloAlarmTint(rgb * flicker), rgb * flicker, _AlarmImmune);
                alpha *= HoloFogVisibility(input.fogFactor);
                return half4(rgb, alpha);
            }
            ENDHLSL
        }
    }
}
