// 숨은 플랫폼 홀로그램 셰이더. MeshRevealReaction 이 넘기는 스캔 값(_ScanOrigin, _ScanRadius, _ScanRevealTime)으로 세 단계를 그린다.
// 1. 평소(스캔 반경 밖): 형태가 아주 희미하게 보인다. 가까이 와야 겨우 보이고 멀어지면 거의 안 보인다.
// 2. 채워지는 중(반경 안, 파동 앞면에서 _FillBand 미터까지): 모서리 → 격자 → 면 순서로 데이터가 채워진다.
// 3. 다 드러남(_ScanRevealTime >= 0): 반투명 홀로그램으로 유지되고, 다 드러난 순간 _FlashDuration 동안 밝게 빛난다.
// 모서리는 오브젝트 공간 상자(_BoxHalfSize, 기본값은 유니티 기본 Cube)의 모서리까지 거리로 판단한다.
// 모서리·격자 두께는 미터 단위라 보는 각도와 상관없다(화면 픽셀 기준이면 비스듬히 볼 때 면 전체가 선으로 판정돼 번쩍인다).
// 뒷면(SRPDefaultUnlit)을 먼저 옅게, 앞면(UniversalForward)을 다음에 그려 반대편 모서리도 비쳐 보이게 한다.
Shader "Project/ScanPlatformHologram"
{
    Properties
    {
        [HDR] _Color ("Color", Color) = (0.2, 0.75, 1.0, 1.0)

        [Header(Idle)]
        _IdleAlphaNear ("Idle Alpha (가까이)", Range(0, 1)) = 0.025
        _IdleAlphaFar ("Idle Alpha (멀리)", Range(0, 1)) = 0.005
        _IdleNearDistance ("Idle Near Distance (m)", Float) = 2
        _IdleFarDistance ("Idle Far Distance (m)", Float) = 10

        [Header(Fill)]
        _FillBand ("Fill Band (m)", Float) = 1.5
        _FrontWidth ("Front Line Width (m)", Float) = 0.1
        _BoxHalfSize ("Box Half Size (오브젝트 공간)", Vector) = (0.5, 0.5, 0.5, 0)
        _EdgeWidth ("Edge Line Width (m)", Float) = 0.025
        _EdgeGlow ("Edge Glow Width (m)", Float) = 0.12
        _GridSize ("Grid Size (m)", Float) = 0.5
        _GridWidth ("Grid Line Width (m)", Float) = 0.015

        [Header(Hologram)]
        _HoloAlpha ("Hologram Fill Alpha", Range(0, 1)) = 0.2
        _EdgeStrength ("Edge Strength", Float) = 1.5
        _GridStrength ("Grid Strength", Range(0, 1)) = 0.3
        _RimPower ("Rim Power", Float) = 3
        _RimStrength ("Rim Strength", Range(0, 1)) = 0.3
        _BackFaceAlpha ("Back Face Alpha", Range(0, 1)) = 0.35

        [Header(Flash)]
        _FlashIntensity ("Flash Intensity", Float) = 3
        _FlashDuration ("Flash Duration (s)", Float) = 0.5

        // 스캔 값은 MeshRevealReaction 이 PropertyBlock 으로 넘긴다. 기본값은 평소 상태(전부 숨김, 미완료)다.
        _ScanOrigin ("Scan Origin", Vector) = (0, 0, 0, 0)
        _ScanRadius ("Scan Radius", Float) = -1
        _ScanRevealTime ("Scan Reveal Time", Float) = -1
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
            float _IdleAlphaNear;
            float _IdleAlphaFar;
            float _IdleNearDistance;
            float _IdleFarDistance;
            float _FillBand;
            float _FrontWidth;
            float4 _BoxHalfSize;
            float _EdgeWidth;
            float _EdgeGlow;
            float _GridSize;
            float _GridWidth;
            float _HoloAlpha;
            float _EdgeStrength;
            float _GridStrength;
            float _RimPower;
            float _RimStrength;
            float _BackFaceAlpha;
            float _FlashIntensity;
            float _FlashDuration;
            float4 _ScanOrigin;
            float _ScanRadius;
            float _ScanRevealTime;
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
            // 오브젝트 축 기준이지만 스케일을 곱한 미터 단위 위치. 격자가 플랫폼을 따라 움직이고 늘어나지 않게 한다.
            float3 positionMeters : TEXCOORD2;
            float3 normalOS : TEXCOORD3;
            // 상자 절반 크기(m)
            float3 halfSizeMeters : TEXCOORD4;
        };

        Varyings vert(Attributes input)
        {
            Varyings output;
            float3 objectScale = float3(
                length(UNITY_MATRIX_M._m00_m10_m20),
                length(UNITY_MATRIX_M._m01_m11_m21),
                length(UNITY_MATRIX_M._m02_m12_m22));

            output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
            output.positionCS = TransformWorldToHClip(output.positionWS);
            output.normalWS = TransformObjectToWorldNormal(input.normalOS);
            output.positionMeters = input.positionOS.xyz * objectScale;
            output.normalOS = input.normalOS;
            output.halfSizeMeters = _BoxHalfSize.xyz * objectScale;
            return output;
        }

        // 선 중심까지의 거리(m)로 두께 width(m)인 선을 그린다. 한 픽셀이 선보다 넓어지면(멀거나 비스듬할 때)
        // 번지는 만큼 어둡게 해서, 선이 면 전체로 퍼져 밝아지지 않게 한다.
        float LineMask(float distanceToLine, float width)
        {
            float pixelSize = max(fwidth(distanceToLine), 1e-5);
            float halfWidth = width * 0.5;
            return saturate(((halfWidth - distanceToLine) / pixelSize) + 0.5) * saturate(width / pixelSize);
        }

        half4 Shade(Varyings input, float faceSign, float faceAlpha)
        {
            // 모서리: 상자 세 면까지의 거리 중 가장 가까운 것은 지금 그리는 면(≈0)이고,
            // 두 번째로 가까운 것이 이 면 위에서 가장 가까운 모서리까지의 거리(m)다
            float3 toFace = input.halfSizeMeters - abs(input.positionMeters);
            float nearestFace = min(toFace.x, min(toFace.y, toFace.z));
            float farthestFace = max(toFace.x, max(toFace.y, toFace.z));
            float edgeDistance = max((toFace.x + toFace.y + toFace.z) - nearestFace - farthestFace, 0.0);
            float edgeLine = LineMask(edgeDistance, _EdgeWidth * 2.0);
            float edge = max(edgeLine, exp(-edgeDistance / max(_EdgeGlow, 1e-3)) * 0.5);

            // 격자: 면과 수직인 축의 선은 면 전체를 덮으므로 뺀다
            float3 axisMask = 1.0 - abs(normalize(input.normalOS));
            float3 gridDistance = abs(frac((input.positionMeters / max(_GridSize, 1e-3)) + 0.5) - 0.5) * _GridSize;
            float3 lines = float3(
                LineMask(gridDistance.x, _GridWidth),
                LineMask(gridDistance.y, _GridWidth),
                LineMask(gridDistance.z, _GridWidth)) * axisMask;
            float grid = max(max(lines.x, lines.y), lines.z);

            // 스캔 상태: behind 는 파동 앞면이 이 픽셀을 지나간 거리(m). 다 드러났으면 전부 채워진 것으로 본다.
            float behind = _ScanRadius - distance(input.positionWS, _ScanOrigin.xyz);
            float isComplete = step(0.0, _ScanRevealTime);
            float fill = max(isComplete, saturate(behind / max(_FillBand, 1e-3)));
            float front = (1.0 - isComplete) * step(0.0, behind) * (1.0 - saturate(behind / max(_FrontWidth, 1e-3)));

            // 채워지는 순서: 모서리가 먼저 빛나고, 격자가 생긴 뒤, 면이 채워진다. 격자는 생길 때 밝았다가 홀로그램 세기로 내려간다.
            float edgeAmount = saturate(fill * 4.0);
            float gridAmount = saturate((fill * 2.0) - 0.25);
            float surfaceAmount = smoothstep(0.5, 1.0, fill);
            float gridBrightness = gridAmount * lerp(1.0, _GridStrength, surfaceAmount);

            // 평소: 카메라와 가까울수록 진한 희미한 형태
            float cameraDistance = distance(input.positionWS, _WorldSpaceCameraPos);
            float idleT = saturate((cameraDistance - _IdleNearDistance) / max(_IdleFarDistance - _IdleNearDistance, 1e-3));
            float idleAlpha = lerp(_IdleAlphaNear, _IdleAlphaFar, idleT) * (1.0 + edgeLine);

            // 가장자리 빛: 비스듬히 보이는 면일수록 밝다. 뒷면은 법선을 뒤집어 계산한다.
            float3 normalWS = normalize(input.normalWS) * faceSign;
            float3 viewDirection = normalize(GetWorldSpaceViewDir(input.positionWS));
            float rim = pow(1.0 - saturate(dot(normalWS, viewDirection)), _RimPower) * _RimStrength;

            float hologramAlpha = (_HoloAlpha * surfaceAmount) + (edge * _EdgeStrength * edgeAmount) + (grid * gridBrightness) + (rim * surfaceAmount);
            float alpha = (idleAlpha * (1.0 - fill)) + hologramAlpha + front;
            float brightness = 1.0 + (edge * edgeAmount) + (front * 2.0);

            // 다 드러난 순간부터 _FlashDuration 동안 밝게 빛났다가 원래 밝기로 돌아온다
            float flash = isComplete * saturate(1.0 - ((_Time.y - _ScanRevealTime) / max(_FlashDuration, 1e-3)));
            flash *= flash;
            brightness *= 1.0 + (flash * _FlashIntensity);
            alpha += flash * 0.5;

            return half4(_Color.rgb * brightness, saturate(alpha) * _Color.a * faceAlpha);
        }
        ENDHLSL

        // 뒷면: 반대편 모서리와 격자가 앞면 너머로 옅게 비친다
        Pass
        {
            Name "BackFace"
            Tags { "LightMode" = "SRPDefaultUnlit" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Front

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            half4 frag(Varyings input) : SV_Target
            {
                return Shade(input, -1.0, _BackFaceAlpha);
            }
            ENDHLSL
        }

        // 앞면
        Pass
        {
            Name "FrontFace"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            half4 frag(Varyings input) : SV_Target
            {
                return Shade(input, 1.0, 1.0);
            }
            ENDHLSL
        }
    }
}
