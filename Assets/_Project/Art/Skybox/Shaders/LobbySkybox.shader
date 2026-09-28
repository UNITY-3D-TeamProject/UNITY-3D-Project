Shader "Project/LobbySkybox"
{
    Properties
    {
        [Header(Cyber Background)]
        _BgColor ("Background Color", Color) = (0.02, 0.02, 0.04, 1) // Almost Black
        _GridColor ("Cyber Grid Color", Color) = (0.0, 0.6, 1.0, 0.1) // Neon Cyan Grid
        _SparkleColor ("Sparkle (Data) Color", Color) = (0.5, 1.0, 1.0, 1) // Twinkle

        [Header(UI Elements)]
        _ClockColor ("Clock Widget Color", Color) = (0.9, 0.9, 1.0, 1.0)
        _UiBaseColor ("UI Glass Color", Color) = (0.1, 0.8, 1.0, 0.3) // Cyber Blue glass
        _UiHighlight1 ("UI Highlight 1 (Yellow)", Color) = (1.0, 0.85, 0.2, 1)
        _UiHighlight2 ("UI Highlight 2 (Pink)", Color) = (1.0, 0.3, 0.6, 1)
        _BadgeColor ("Notification Badge Color", Color) = (1.0, 0.2, 0.3, 1)

        [Header(Movement Settings)]
        _TimeScale ("Global Time Scale", Float) = 1.0
        _UiSpeed ("Fast UI Speed Multiplier", Float) = 2.5
        _ClockSpeed ("Slow Clock Drift Speed", Float) = 0.05
        _Exposure ("Exposure", Range(0, 8)) = 1.0
    }
    SubShader
    {
        Tags 
        { 
            "RenderType"="Background" 
            "Queue"="Background" 
            "PreviewType"="Skybox" 
            "RenderPipeline"="UniversalPipeline"
        }
        Cull Off ZWrite Off

        Pass
        {
            Name "Skybox"
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            #define TAU 6.28318530718

            CBUFFER_START(UnityPerMaterial)
                half4 _BgColor;
                half4 _GridColor;
                half4 _SparkleColor;
                half4 _ClockColor;
                half4 _UiBaseColor;
                half4 _UiHighlight1;
                half4 _UiHighlight2;
                half4 _BadgeColor;
                float _TimeScale;
                float _UiSpeed;
                float _ClockSpeed;
                float _Exposure;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 direction : TEXCOORD0;
            };

            // --- Utilities ---
            float Hash11(float x) { return frac(sin(x * 127.1) * 43758.5453); }
            float Hash21(float2 p) 
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float SdRoundBox(float2 p, float2 halfSize, float radius)
            {
                float2 q = abs(p) - halfSize + radius;
                return length(max(q, 0.0)) + min(max(q.x, q.y), 0.0) - radius;
            }

            // 7-segment display logic for digits (0-9)
            float Segment(float2 p, float2 a, float2 b, float thickness)
            {
                float2 pa = p - a, ba = b - a;
                float h = clamp(dot(pa, ba) / dot(ba, ba), 0.0, 1.0);
                return length(pa - ba * h) - thickness;
            }

            float DrawDigit(float2 p, int digit)
            {
                if (p.x < -0.3 || p.x > 0.3 || p.y < -0.5 || p.y > 0.5) return 1.0;
                float w = 0.25; float h = 0.45; float th = 0.04;
                float d = 1.0;
                int segs[10] = { 0x3F, 0x06, 0x5B, 0x4F, 0x66, 0x6D, 0x7D, 0x07, 0x7F, 0x6F };
                int s = segs[digit];
                if(s & 1) d = min(d, Segment(p, float2(-w, h), float2(w, h), th));
                if(s & 2) d = min(d, Segment(p, float2(w, h), float2(w, 0), th));
                if(s & 4) d = min(d, Segment(p, float2(w, 0), float2(w, -h), th));
                if(s & 8) d = min(d, Segment(p, float2(w, -h), float2(-w, -h), th));
                if(s & 16) d = min(d, Segment(p, float2(-w, -h), float2(-w, 0), th));
                if(s & 32) d = min(d, Segment(p, float2(-w, 0), float2(-w, h), th));
                if(s & 64) d = min(d, Segment(p, float2(-w, 0), float2(w, 0), th));
                return d;
            }

            float DrawClock(float2 p, float t)
            {
                int min1 = (int)(t * 0.5) % 6; 
                int min2 = (int)(t * 5.0) % 10; 
                float d = 1.0;
                d = min(d, DrawDigit(p - float2(-0.8, 0.0), 1));
                d = min(d, DrawDigit(p - float2(-0.3, 0.0), 2));
                d = min(d, length(p - float2(0.1, 0.15)) - 0.05);
                float blink = step(0.5, frac(t * 0.5));
                if (blink > 0.0) d = min(d, length(p - float2(0.1, -0.15)) - 0.05);
                d = min(d, DrawDigit(p - float2(0.5, 0.0), min1));
                d = min(d, DrawDigit(p - float2(1.0, 0.0), min2));
                return smoothstep(0.015, -0.01, d);
            }

            // 반짝이는 데이터 파티클 (별)
            float3 Sparkles(float3 dir, float t)
            {
                float3 scaled = dir * 100.0;
                float3 cell = floor(scaled);
                float3 local = frac(scaled);
                
                float h = Hash21(cell.xy + cell.z * 13.0);
                float exists = step(0.95, h); // 5% 밀도
                
                float d = length(local - float3(0.5, 0.5, 0.5));
                float star = exists * smoothstep(0.25, 0.0, d);
                
                // 빠르게 깜빡이는 효과 (Cyber 느낌)
                float twinkle = sin(t * (30.0 + h * 50.0) + h * 100.0) * 0.5 + 0.5;
                
                return _SparkleColor.rgb * star * pow(twinkle, 2.0) * 2.0;
            }

            // 사이버 디지털 그리드 무늬
            float CyberGrid(float2 uv, float alphaFade)
            {
                float2 grid = abs(frac(uv * 12.0) - 0.5);
                float lineX = smoothstep(0.46, 0.5, grid.x);
                float lineY = smoothstep(0.46, 0.5, grid.y);
                float lines = max(lineX, lineY);
                return lines * alphaFade;
            }

            // 날아다니는 UI 레이어 렌더링 함수
            float4 FlyingUILayer(float2 uv, float scale, float speedX, float speedY, float t, float seed)
            {
                uv *= scale;
                uv.x += t * speedX;
                uv.y += t * speedY;
                
                float2 id = floor(uv);
                float2 local = frac(uv) - 0.5;
                
                float h = Hash21(id + seed);
                if (h < 0.7) return float4(0,0,0,0);
                
                local += (float2(Hash11(h * 13.0), Hash11(h * 7.0)) - 0.5) * 0.4;
                float sizeMod = lerp(0.6, 1.2, Hash11(h * 51.0));
                local /= sizeMod;
                
                float d = 1.0;
                float type = Hash11(h * 37.0);
                
                float3 col = _UiBaseColor.rgb;
                float alpha = 0.0;
                float hasBadge = 0.0;
                
                // UI 형태 결정
                if (type < 0.2) 
                {
                    d = SdRoundBox(local, float2(0.2, 0.15), 0.08);
                    float tail = length(local - float2(0.15, -0.15)) - 0.06;
                    d = min(d, tail);
                    col = _UiHighlight1.rgb;
                } 
                else if (type < 0.5) 
                {
                    d = SdRoundBox(local, float2(0.15, 0.15), 0.06);
                    col = lerp(_UiHighlight2.rgb, float3(0.2, 1.0, 0.8), Hash11(h * 88.0));
                    hasBadge = step(0.6, Hash11(h * 42.0));
                } 
                else if (type < 0.7) 
                {
                    d = SdRoundBox(local, float2(0.35, 0.1), 0.04);
                    col = _UiBaseColor.rgb;
                    float line1 = SdRoundBox(local - float2(-0.05, 0.02), float2(0.2, 0.01), 0.01);
                    float line2 = SdRoundBox(local - float2(-0.1, -0.03), float2(0.15, 0.01), 0.01);
                    d = min(d, min(line1, line2));
                }
                else 
                {
                    d = length(local) - 0.15;
                    col = lerp(_UiBaseColor.rgb, _UiHighlight1.rgb, 0.5);
                }
                
                float fill = smoothstep(0.015, -0.01, d);
                float border = smoothstep(0.03, 0.0, abs(d + 0.005));
                
                alpha = fill * _UiBaseColor.a + border * 0.8; 
                
                if (hasBadge > 0.0) 
                {
                    float badge = length(local - float2(0.13, 0.13)) - 0.05;
                    float badgeFill = smoothstep(0.01, -0.01, badge);
                    if (badgeFill > 0.0) 
                    {
                        col = _BadgeColor.rgb;
                        alpha = max(alpha, badgeFill * 0.9);
                    }
                }
                
                return float4(col, alpha);
            }

            Varyings vert (Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.direction = input.positionOS.xyz;
                return output;
            }

            half4 frag (Varyings input) : SV_Target
            {
                float3 dir = normalize(input.direction);
                float t = _Time.y * _TimeScale;
                
                // 1. Black/Dark Cyber Background
                float3 finalCol = _BgColor.rgb;
                
                // 극점(위/아래) 마스크
                float poleMask = 1.0 - smoothstep(0.4, 0.8, abs(dir.y));

                float cylU = atan2(dir.x, dir.z) / TAU; // 0 to 1
                float cylV = dir.y / length(dir.xz);
                float2 flyUV = float2(cylU, cylV);

                // 2. Cyber Grid (배경에 은은하게 깔리는 디지털 격자)
                float gridAlpha = CyberGrid(flyUV + float2(t * 0.02, 0), poleMask);
                finalCol += _GridColor.rgb * gridAlpha;

                // 3. Fast Twinkling Data Particles (반짝이는 파티클)
                finalCol += Sparkles(dir, t);
                
                // 4. Fast Flying UI Layers (빠르게 휙휙 지나가는 UI들)
                float speed = _UiSpeed;
                float4 layer1 = FlyingUILayer(flyUV, 15.0,  0.3 * speed,  0.05 * speed, t, 11.0);
                float4 layer2 = FlyingUILayer(flyUV, 8.0,  -0.5 * speed,  0.02 * speed, t, 22.0);
                float4 layer3 = FlyingUILayer(flyUV, 4.0,   0.8 * speed, -0.08 * speed, t, 33.0);

                // 5. Slow Moving Clock Widget (천천히 유영하는 시계)
                float3 clockCol = float3(0,0,0);
                float clockAlpha = 0.0;
                
                // 시계 자체가 가로로 천천히 이동하고 세로로 둥둥 떠다님
                float clockU = frac(cylU + 0.5 + t * _ClockSpeed); 
                float elev = asin(dir.y);
                float clockElev = elev - 0.2 - sin(t * _ClockSpeed * 15.0) * 0.05;

                if (abs(clockU - 0.5) < 0.15 && abs(clockElev) < 0.3)
                {
                    float2 clockUV = float2((clockU - 0.5) * 10.0, clockElev * 5.0);
                    
                    float panelD = SdRoundBox(clockUV, float2(1.2, 0.6), 0.2);
                    float panelAlpha = (1.0 - smoothstep(0.0, 0.02, panelD)) * 0.2;
                    float panelBorder = (1.0 - smoothstep(0.0, 0.02, abs(panelD))) * 0.7; // 진한 테두리
                    
                    float digits = DrawClock(clockUV, t);
                    
                    clockAlpha = max(panelAlpha + panelBorder, digits);
                    clockCol = lerp(_UiBaseColor.rgb * 0.5, _ClockColor.rgb, digits);
                }

                // --- 알파 블렌딩 (뒤에서 앞으로) ---
                finalCol = lerp(finalCol, layer1.rgb, layer1.a * poleMask * 0.6);
                finalCol = lerp(finalCol, layer2.rgb, layer2.a * poleMask * 0.8);
                finalCol = lerp(finalCol, layer3.rgb, layer3.a * poleMask);
                
                // 시계는 최상단
                finalCol = lerp(finalCol, clockCol, clockAlpha);

                return half4(finalCol * _Exposure, 1.0);
            }
            ENDHLSL
        }
    }
}
