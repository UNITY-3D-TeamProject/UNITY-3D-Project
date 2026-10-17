// 데이터 도시 절차적 스카이박스 (URP). 사용자 이미지 시안 "네온 데이터 도시"를 움직이게 옮긴 것이다.
// 레이어: 어두운 남색 하늘(성운 + 별)
//         / 데이터 박스 타워: XZ 격자 칸마다 위아래로 끝없이 이어지는 사각 기둥을 레이 마칭(2D DDA)으로 그린다.
//           기둥은 박스를 쌓은 모양이고, 면마다 글자 줄·창 격자·랙 슬롯 패턴이 빛난다. 반투명이라 뒤 기둥과 안쪽 모서리가 비친다.
//         / 네온 데이터 줄기: 3D 직선(하늘에서는 대원) 위를 밝은 머리가 꼬리를 남기며 지나간다. 줄기마다 깊이가 있어 타워 앞뒤로 엮인다.
// 단위: 타워는 월드 단위(가상의 거리), 줄기 굵기는 라디안.
Shader "Project/DataCitySkybox"
{
    Properties
    {
        [Header(Sky)]
        _ZenithColor ("Zenith", Color) = (0.004, 0.008, 0.02, 1)
        _HorizonColor ("Horizon", Color) = (0.012, 0.03, 0.06, 1)
        _NadirColor ("Nadir (bottom)", Color) = (0.002, 0.006, 0.014, 1)
        _NebulaColor ("Nebula", Color) = (0.04, 0.08, 0.16, 1)
        _NebulaStrength ("Nebula Strength", Range(0, 2)) = 0.6
        _StarDensity ("Star Density", Range(0, 0.1)) = 0.02
        _StarStrength ("Star Strength", Range(0, 3)) = 1.2

        [Header(Towers)]
        _CellSize ("Cell Size", Float) = 24
        _TowerDensity ("Tower Density", Range(0, 1)) = 0.4
        _ClearRadius ("Clear Radius (cells)", Float) = 2.5
        _AvenueWidth ("Avenue Half Width (cells)", Float) = 0.6
        _SegmentHeightMin ("Box Height Min", Float) = 6
        _SegmentHeightMax ("Box Height Max", Float) = 16
        _BodyColor ("Body Color", Color) = (0.01, 0.025, 0.045, 1)
        _FaceAlpha ("Face Opacity", Range(0, 1)) = 0.5
        [HDR] _EdgeColor ("Edge Color", Color) = (0.3, 1.7, 2.2, 1)
        _EdgeWidth ("Edge Width", Float) = 0.12
        _BackEdgeStrength ("Inner Edge Strength", Range(0, 1)) = 0.35
        [HDR] _TextColorA ("Text Color A", Color) = (0.2, 0.95, 1.2, 1)
        [HDR] _TextColorB ("Text Color B", Color) = (1.5, 0.3, 1.3, 1)
        _TextColorBRatio ("Text Color B Ratio", Range(0, 1)) = 0.2
        _TextRowHeight ("Text Row Height", Float) = 0.7
        _TextStrength ("Text Strength", Range(0, 3)) = 0.6
        _ScrollSpeed ("Text Scroll Speed", Float) = 0.6
        _PulseSpeed ("Edge Pulse Speed", Float) = 8
        _PulseSpacing ("Edge Pulse Spacing", Float) = 40
        _FogColor ("Fog Color", Color) = (0.02, 0.055, 0.1, 1)
        _FogDistance ("Fog Distance", Float) = 200
        _ParallaxScale ("Camera Parallax", Range(0, 0.2)) = 0.02

        [Header(Data Streams)]
        [HDR] _StreamColorA ("Stream Color A", Color) = (0.15, 0.75, 1, 1)
        [HDR] _StreamColorB ("Stream Color B", Color) = (0.95, 0.2, 0.85, 1)
        _StreamColorBRatio ("Stream Color B Ratio", Range(0, 1)) = 0.3
        _StreamWidth ("Stream Width (rad)", Float) = 0.0012
        _StreamGlowWidth ("Stream Glow Width (rad)", Float) = 0.008
        _StreamBase ("Stream Base Line", Range(0, 1)) = 0.35
        _StreamStrength ("Stream Strength", Range(0, 3)) = 1
        _StreamSpeed ("Stream Speed", Float) = 0.08
        _StreamDistance ("Stream Distance (min, max)", Vector) = (30, 220, 0, 0)

        [Header(Global)]
        _TimeScale ("Time Scale", Float) = 1
        _Exposure ("Exposure", Float) = 1
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Background"
            "Queue" = "Background"
            "PreviewType" = "Skybox"
            "RenderPipeline" = "UniversalPipeline"
        }

        Cull Off
        ZWrite Off

        Pass
        {
            Name "Skybox"

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            #define TAU 6.28318530718
            #define MAX_CELL_STEPS 40
                        #define HORIZONTAL_STREAMS 14
            #define VERTICAL_STREAMS 6
            #define STRANDS 3

            CBUFFER_START(UnityPerMaterial)
                half4 _ZenithColor;
                half4 _HorizonColor;
                half4 _NadirColor;
                half4 _NebulaColor;
                float _NebulaStrength;
                float _StarDensity;
                float _StarStrength;
                float _CellSize;
                float _TowerDensity;
                float _ClearRadius;
                float _AvenueWidth;
                float _SegmentHeightMin;
                float _SegmentHeightMax;
                half4 _BodyColor;
                float _FaceAlpha;
                half4 _EdgeColor;
                float _EdgeWidth;
                float _BackEdgeStrength;
                half4 _TextColorA;
                half4 _TextColorB;
                float _TextColorBRatio;
                float _TextRowHeight;
                float _TextStrength;
                float _ScrollSpeed;
                float _PulseSpeed;
                float _PulseSpacing;
                half4 _FogColor;
                float _FogDistance;
                float _ParallaxScale;
                half4 _StreamColorA;
                half4 _StreamColorB;
                float _StreamColorBRatio;
                float _StreamWidth;
                float _StreamGlowWidth;
                float _StreamBase;
                float _StreamStrength;
                float _StreamSpeed;
                float4 _StreamDistance;
                float _TimeScale;
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

            // ---------- 유틸 ----------

            float Hash11(float x)
            {
                return frac(sin(x * 127.1) * 43758.5453);
            }

            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float Hash31(float3 p)
            {
                p = frac(p * float3(0.1031, 0.1030, 0.0973));
                p += dot(p, p.yzx + 33.33);
                return frac((p.x + p.y) * p.z);
            }

            float ValueNoise3(float3 p)
            {
                float3 i = floor(p);
                float3 f = frac(p);
                f = f * f * (3.0 - (2.0 * f));
                float n000 = Hash31(i);
                float n100 = Hash31(i + float3(1, 0, 0));
                float n010 = Hash31(i + float3(0, 1, 0));
                float n110 = Hash31(i + float3(1, 1, 0));
                float n001 = Hash31(i + float3(0, 0, 1));
                float n101 = Hash31(i + float3(1, 0, 1));
                float n011 = Hash31(i + float3(0, 1, 1));
                float n111 = Hash31(i + float3(1, 1, 1));
                float a = lerp(lerp(n000, n100, f.x), lerp(n010, n110, f.x), f.y);
                float b = lerp(lerp(n001, n101, f.x), lerp(n011, n111, f.x), f.y);
                return lerp(a, b, f.z);
            }

            // 거리 d 인 곳에 굵기 w 선을 그린다. 화소 크기(fp)보다 가늘면 밝기를 줄여 깜빡임 없이 사라지게 한다.
            float LineAA(float d, float w, float fp)
            {
                float k = max(w, fp);
                return saturate(1.0 - (d / k)) * (w / k);
            }

            // ---------- 하늘 ----------

            float3 Sky(float3 dir, float t)
            {
                float3 color = (dir.y >= 0.0)
                    ? lerp(_HorizonColor.rgb, _ZenithColor.rgb, saturate(dir.y * 1.4))
                    : lerp(_HorizonColor.rgb, _NadirColor.rgb, saturate(-dir.y * 1.4));

                float3 q = (dir * 2.2) + float3(t * 0.004, 0.0, 0.0);
                float nebula = (ValueNoise3(q) * 0.55) + (ValueNoise3(q * 2.3) * 0.3) + (ValueNoise3(q * 5.1) * 0.15);
                color += _NebulaColor.rgb * smoothstep(0.45, 0.85, nebula) * _NebulaStrength;

                float3 sp = dir * 260.0;
                float3 cell = floor(sp);
                float h = Hash31(cell);
                float3 starPos = cell + 0.5 + ((float3(Hash31(cell + 1.7), Hash31(cell + 3.1), Hash31(cell + 5.3)) - 0.5) * 0.6);
                float star = step(1.0 - _StarDensity, h) * saturate(1.0 - (length(sp - starPos) * 3.0));
                float twinkle = 0.55 + (0.45 * sin((t * (1.0 + (3.0 * h))) + (h * 40.0)));
                color += lerp(_TextColorA.rgb, float3(1, 1, 1), 0.6) * star * twinkle * _StarStrength * 0.5;
                return color;
            }

            // ---------- 타워 ----------

            // 기둥 한 면의 발광. fu: 면 가로 0~1, faceW: 면 너비(월드), y: 높이(월드), fp: 그 거리에서 화소 하나의 월드 크기.
            // isBack 이면 안쪽 면이라 모서리만 흐리게 돌려준다.
            float3 TowerFace(float fu, float faceW, float y, float fp, float2 id, float faceSeed, float t, bool isBack)
            {
                float towerHash = Hash21(id + 3.7);
                float segH = lerp(_SegmentHeightMin, _SegmentHeightMax, towerHash);
                float segIdx = floor(y / segH);
                float px = fu * faceW;
                float py = frac(y / segH) * segH;

                // 박스 모서리. 세로 모서리에는 위나 아래로 흐르는 데이터 펄스를 싣는다.
                float vertEdgeDist = min(px, faceW - px);
                float horEdgeDist = min(py, segH - py);
                float pulseDir = (Hash21(id + 9.1) > 0.5) ? 1.0 : -1.0;
                float pulsePhase = frac((y / _PulseSpacing) - (t * _PulseSpeed * pulseDir / _PulseSpacing) + Hash21(id + 4.4));
                float pulse = pow(saturate(pulsePhase), 12.0) * 3.0;
                float edge = (LineAA(vertEdgeDist, _EdgeWidth, fp) * (1.0 + pulse)) + LineAA(horEdgeDist, _EdgeWidth * 0.8, fp);
                float3 edgeColor = _EdgeColor.rgb * edge;
                if (isBack)
                {
                    return edgeColor * _BackEdgeStrength;
                }

                float segHash = Hash21(float2(segIdx, faceSeed) + (id * 1.7));
                float3 textColor = (Hash21(float2(segIdx * 0.37, faceSeed) + id) > (1.0 - _TextColorBRatio)) ? _TextColorB.rgb : _TextColorA.rgb;

                // 박스 안쪽 패널 영역
                float margin = min(faceW, segH) * 0.1;
                float inPanel = step(margin, px) * step(px, faceW - margin) * step(margin, py) * step(py, segH - margin);
                float panelX = px - margin;
                float panelY = py - margin;
                float panelW = faceW - (2.0 * margin);

                float pattern;
                float average;
                float cellSize;
                if (segHash < 0.25)
                {
                    // 빈 박스
                    pattern = 0.0;
                    average = 0.0;
                    cellSize = 1.0;
                }
                else if (segHash < 0.68)
                {
                    // 글자 줄: 줄마다 길이가 다르고, 일부 박스는 글자가 위로 흘러간다.
                    float scroll = (segHash > 0.45) ? (t * _ScrollSpeed * (0.5 + segHash)) : 0.0;
                    float rowF = (panelY / _TextRowHeight) + scroll;
                    float row = floor(rowF);
                    float rowLocal = frac(rowF);
                    float charW = _TextRowHeight * 0.55;
                    float col = floor(panelX / charW);
                    float colLocal = frac(panelX / charW);
                    float lineLen = panelW * Hash21(float2(row, segIdx) + id) * 1.2;
                    float blink = Hash21(float2(row * 3.1 + col, floor(t * 6.0 * (0.3 + segHash))) + id);
                    float charOn = step(0.28, Hash21(float2(row, col) + (id * 2.3) + segIdx)) * step(0.04, blink);
                    float glyph = step(0.25, rowLocal) * step(rowLocal, 0.7) * step(0.1, colLocal) * step(colLocal, 0.85);
                    pattern = charOn * glyph * step(panelX, lineLen);
                    average = 0.17;
                    cellSize = _TextRowHeight;
                }
                else if (segHash < 0.85)
                {
                    // 창 격자: 칸마다 켜짐/꺼짐이 천천히 바뀐다.
                    float cellW = _TextRowHeight * 1.4;
                    float2 g = float2(panelX, panelY) / cellW;
                    float2 gi = floor(g);
                    float2 gf = frac(g);
                    float on = step(0.45, Hash21(gi + id + segIdx + floor((t * 0.4) + Hash21(gi + 0.3) * 10.0)));
                    pattern = on * step(0.12, gf.x) * step(gf.x, 0.88) * step(0.12, gf.y) * step(gf.y, 0.88) * 0.6;
                    average = 0.18;
                    cellSize = cellW;
                }
                else
                {
                    // 서버 랙 슬롯: 가로 막대 + 오른쪽 끝 상태등
                    float slotH = _TextRowHeight * 1.4;
                    float slot = floor(panelY / slotH);
                    float slotLocal = frac(panelY / slotH);
                    float bar = step(0.2, slotLocal) * step(slotLocal, 0.55) * 0.35;
                    float led = step(panelW - (slotH * 0.6), panelX) * step(0.25, slotLocal) * step(slotLocal, 0.5);
                    led *= step(0.5, Hash21(float2(slot, floor(t * 3.0 + Hash21(float2(slot, 1.0) + id) * 5.0)) + id));
                    pattern = bar + (led * 2.0);
                    average = 0.2;
                    cellSize = slotH;
                }

                // 멀어서 무늬가 화소보다 작아지면 평균 밝기로 바꿔 지글거림을 막는다.
                float detail = saturate(1.6 - (fp / (cellSize * 0.4)));
                pattern = lerp(average, pattern, detail) * inPanel;
                return edgeColor + (textColor * pattern * _TextStrength);
            }

            // 레이를 XZ 격자로 따라가며 기둥들을 앞에서부터 합성한다.
            // hitDist/hitTrans 에 앞쪽 기둥 4개의 거리와 그 뒤 투과율을 남겨, 데이터 줄기의 가려짐에 쓴다.
            float3 Towers(float3 origin, float3 dir, float pixelAngle, float t, inout float transmittance, out float4 hitDist, out float4 hitTrans)
            {
                hitDist = float4(1e9, 1e9, 1e9, 1e9);
                hitTrans = float4(1, 1, 1, 1);
                float3 color = float3(0, 0, 0);

                float horizontal = max(length(dir.xz), 0.0005);
                float2 rd = dir.xz / horizontal;
                float slope = dir.y / horizontal;
                float2 ro = origin.xz / _CellSize;

                float2 cell = floor(ro);
                float2 stepDir = float2((rd.x >= 0.0) ? 1.0 : -1.0, (rd.y >= 0.0) ? 1.0 : -1.0);
                float2 safeRd = float2((abs(rd.x) < 1e-5) ? 1e-5 : rd.x, (abs(rd.y) < 1e-5) ? 1e-5 : rd.y);
                float2 invRd = 1.0 / safeRd;
                float2 tDelta = abs(invRd);
                float2 tMax = ((cell + max(stepDir, 0.0)) - ro) * invRd;

                float hitCount = 0.0;
                [loop]
                for (int i = 0; i < MAX_CELL_STEPS; i++)
                {
                    float2 id = cell;
                    bool isClear = (length(id + 0.5) < _ClearRadius) || (abs(id.y + 0.5) < _AvenueWidth);
                    float exists = Hash21(id);

                    if (!isClear && (exists < _TowerDensity))
                    {
                        float halfSize = lerp(0.16, 0.38, Hash21(id + 1.3));
                        float2 offset = (float2(Hash21(id + 2.1), Hash21(id + 2.9)) - 0.5) * (1.0 - (2.0 * halfSize));
                        float2 centre = id + 0.5 + offset;

                        float2 t1 = ((centre - halfSize) - ro) * invRd;
                        float2 t2 = ((centre + halfSize) - ro) * invRd;
                        float2 tNear2 = min(t1, t2);
                        float2 tFar2 = max(t1, t2);
                        float tNear = max(tNear2.x, tNear2.y);
                        float tFar = min(tFar2.x, tFar2.y);

                        if ((tNear < tFar) && (tNear > 0.0))
                        {
                            float faceW = halfSize * 2.0 * _CellSize;

                            // 앞면
                            float dist = (tNear * _CellSize) / horizontal;
                            float2 p = ro + (rd * tNear);
                            bool isXFace = tNear2.x > tNear2.y;
                            float fu = isXFace ? ((p.y - (centre.y - halfSize)) / (2.0 * halfSize)) : ((p.x - (centre.x - halfSize)) / (2.0 * halfSize));
                            float faceSeed = isXFace ? (stepDir.x * 1.0) : (stepDir.y * 2.0);
                            float y = origin.y + (slope * tNear * _CellSize);
                            float fog = exp(-dist / _FogDistance);
                            float fp = dist * pixelAngle;
                            float3 front = TowerFace(fu, faceW, y, fp, id, faceSeed, t, false);

                            // 뒷면 (반투명 앞면 너머로 보이는 안쪽 모서리)
                            float distBack = (tFar * _CellSize) / horizontal;
                            float2 pb = ro + (rd * tFar);
                            bool isXBack = tFar2.x < tFar2.y;
                            float fub = isXBack ? ((pb.y - (centre.y - halfSize)) / (2.0 * halfSize)) : ((pb.x - (centre.x - halfSize)) / (2.0 * halfSize));
                            float yb = origin.y + (slope * tFar * _CellSize);
                            float3 back = TowerFace(fub, faceW, yb, distBack * pixelAngle, id, 5.0, t, true);

                            float alpha = _FaceAlpha;
                            float3 body = lerp(_FogColor.rgb, _BodyColor.rgb, fog);
                            color += transmittance * ((front * fog) + (body * alpha) + (back * fog * (1.0 - alpha)));
                            transmittance *= 1.0 - alpha;

                            // 동적 인덱싱 대신 마스크로 hitCount 번째 칸에 기록한다.
                            float4 slotMask = float4(hitCount == 0.0, hitCount == 1.0, hitCount == 2.0, hitCount == 3.0);
                            hitDist = lerp(hitDist, dist.xxxx, slotMask);
                            hitTrans = lerp(hitTrans, transmittance.xxxx, slotMask);
                            hitCount += 1.0;

                            if ((transmittance < 0.03) || (dist > (_FogDistance * 5.0)))
                            {
                                break;
                            }
                        }
                    }

                    if (tMax.x < tMax.y)
                    {
                        tMax.x += tDelta.x;
                        cell.x += stepDir.x;
                    }
                    else
                    {
                        tMax.y += tDelta.y;
                        cell.y += stepDir.y;
                    }
                }
                return color;
            }

            // ---------- 데이터 줄기 ----------

            // 깊이 depth 지점에서 타워에 얼마나 가려지는지 (앞쪽 기둥 4개까지만 본다)
            float StreamVisibility(float depth, float4 hitDist, float4 hitTrans)
            {
                float visibility = 1.0;
                visibility = (hitDist.x < depth) ? hitTrans.x : visibility;
                visibility = (hitDist.y < depth) ? hitTrans.y : visibility;
                visibility = (hitDist.z < depth) ? hitTrans.z : visibility;
                visibility = (hitDist.w < depth) ? hitTrans.w : visibility;
                return visibility;
            }

            // 법선 n 둘레의 원(offset 0 이면 대원 = 3D 직선) 하나를 따라 가닥 STRANDS 개가 나란히 흐른다.
            float3 Stream(float3 dir, float3 n, float offset, float seed, float pixelAngle, float t)
            {
                float3 helper = (abs(n.y) < 0.9) ? float3(0, 1, 0) : float3(1, 0, 0);
                float3 a = normalize(cross(n, helper));
                float3 b = cross(n, a);
                float along = (atan2(dot(dir, b), dot(dir, a)) / TAU) + 0.5;
                float across = dot(dir, n) - offset;

                float3 color = lerp(_StreamColorA.rgb, _StreamColorB.rgb, step(1.0 - _StreamColorBRatio, Hash11(seed + 0.7)));
                float spacing = 0.004 + (0.008 * Hash11(seed + 1.9));
                float speed = _StreamSpeed * (0.5 + Hash11(seed + 2.3)) * ((Hash11(seed + 8.8) > 0.5) ? 1.0 : -1.0);
                float3 result = float3(0, 0, 0);

                for (int s = 0; s < STRANDS; s++)
                {
                    float strandSeed = seed + (s * 13.7);
                    if ((s > 0) && (Hash11(strandSeed + 0.2) < 0.4))
                    {
                        continue;
                    }

                    float d = abs(across - (s * spacing));
                    float core = LineAA(d, _StreamWidth, pixelAngle);
                    float glow = exp(-d / _StreamGlowWidth) * 0.05;

                    // 줄 위의 데이터 덩어리: 칸마다 있을 수도 없을 수도, 길이도 제각각
                    float packets = 2.0 + floor(Hash11(strandSeed + 3.3) * 4.0);
                    float x = (along * packets) - (t * speed * packets) + Hash11(strandSeed + 5.1);
                    float slot = floor(x);
                    float q = frac(x);
                    q = (speed >= 0.0) ? q : (1.0 - q);
                    float hasPacket = step(0.35, Hash11(slot + strandSeed));
                    float trailLen = 0.15 + (0.6 * Hash11(slot * 1.7 + strandSeed));
                    float trail = pow(saturate((q - (1.0 - trailLen)) / trailLen), 3.0) * hasPacket;
                    float head = smoothstep(0.985, 1.0, q) * hasPacket;

                    float intensity = (core + glow) * (_StreamBase + (trail * 1.0));
                    result += (color * intensity) + (float3(1, 1, 1) * head * core * 0.8);
                }
                return result;
            }

            float3 Streams(float3 dir, float pixelAngle, float t, float4 hitDist, float4 hitTrans)
            {
                float3 result = float3(0, 0, 0);

                // 가로로 휘감아 도는 줄기 (법선이 위쪽에 가까운 대원)
                for (int i = 0; i < HORIZONTAL_STREAMS; i++)
                {
                    float seed = (i * 7.31) + 1.0;
                    float azimuth = Hash11(seed) * TAU;
                    float tilt = (Hash11(seed + 4.2) - 0.5) * 0.9;
                    float3 n = normalize(float3(sin(azimuth) * tilt, 1.0, cos(azimuth) * tilt));
                    float depth = lerp(_StreamDistance.x, _StreamDistance.y, Hash11(seed + 6.6));
                    // offset 으로 지평선 위아래 여러 높이에 고리를 둔다.
                    float offset = (Hash11(seed + 9.4) - 0.5) * 1.1;
                    float3 streamColor = Stream(dir, n, offset, seed, pixelAngle, t);
                    result += streamColor * StreamVisibility(depth, hitDist, hitTrans);
                }

                // 위아래로 끝없이 흐르는 세로 줄기 (자오선)
                for (int j = 0; j < VERTICAL_STREAMS; j++)
                {
                    float seed = (j * 11.17) + 101.0;
                    float azimuth = Hash11(seed) * TAU;
                    float3 n = float3(cos(azimuth), 0.0, sin(azimuth));
                    float depth = lerp(_StreamDistance.x, _StreamDistance.y, Hash11(seed + 6.6));
                    // 천정·천저에서 모두 한 점에 모여 번쩍이지 않게 극 쪽은 흐리게 한다.
                    float poleFade = 1.0 - smoothstep(0.55, 0.9, abs(dir.y));
                    float3 streamColor = Stream(dir, n, 0.0, seed, pixelAngle, t) * 0.7 * poleFade;
                    result += streamColor * StreamVisibility(depth, hitDist, hitTrans);
                }
                return result * _StreamStrength;
            }

            // ---------- 본체 ----------

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.direction = input.positionOS.xyz;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float3 dir = normalize(input.direction);
                float t = _Time.y * _TimeScale;
                float pixelAngle = 2.0 / (abs(UNITY_MATRIX_P[1][1]) * _ScreenParams.y);
                float3 origin = _WorldSpaceCameraPos * _ParallaxScale;

                float transmittance = 1.0;
                float4 hitDist;
                float4 hitTrans;
                float3 color = Towers(origin, dir, pixelAngle, t, transmittance, hitDist, hitTrans);
                color += Sky(dir, t) * transmittance;
                color += Streams(dir, pixelAngle, t, hitDist, hitTrans);

                return half4(color * _Exposure, 1.0);
            }
            ENDHLSL
        }
    }
}
