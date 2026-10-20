// 튜토리얼 홀로그램 셰이더 공용: 전역 어둠·경보 값과 안개.
// _HoloDarkness (0~1): 1 이면 _DarkImmune 이 아닌 홀로그램이 보이지 않는다. (Map.Tutorial.TutorialAtmosphere 가 설정)
// _HoloAlarm    (0~1): 1 이면 색이 경보 색(_HoloAlarmColor)으로 바뀌고 맥박처럼 밝아진다.
#ifndef PROJECT_HOLO_COMMON_INCLUDED
#define PROJECT_HOLO_COMMON_INCLUDED

float _HoloDarkness;
float _HoloAlarm;
float4 _HoloAlarmColor;

// 어둠 속에서 보이는 정도 (1 = 다 보임)
float HoloVisibility(float darkImmune)
{
    return 1.0 - (saturate(_HoloDarkness) * (1.0 - darkImmune));
}

// 경보 색으로 물들인다. 밝기는 유지하고 색만 바꾼다.
half3 HoloAlarmTint(half3 color)
{
    float alarm = saturate(_HoloAlarm);
    if (alarm <= 0.0) return color;

    half3 alarmColor = (_HoloAlarmColor.a > 0.0) ? _HoloAlarmColor.rgb : half3(1.0, 0.16, 0.22);
    float luminance = dot(color, float3(0.3, 0.59, 0.11));
    float pulse = 0.85 + (0.15 * sin(_Time.y * 6.0));
    return lerp(color, alarmColor * luminance * 1.7 * pulse, alarm);
}

// 안개만큼 보이는 정도. 안개가 꺼져 있으면 URP 의 ComputeFogIntensity 가 0 을 돌려주므로 1 로 둔다.
float HoloFogVisibility(float fogFactor)
{
#if defined(FOG_LINEAR) || defined(FOG_EXP) || defined(FOG_EXP2)
    return ComputeFogIntensity(fogFactor);
#else
    return 1.0;
#endif
}

// 칸 단위 무작위 값 (어둠에서 드러날 때 조각조각 나타나게)
float HoloCellNoise(float3 positionWS, float cellSize)
{
    float3 cell = floor(positionWS / cellSize);
    return frac(sin(dot(cell, float3(12.9898, 78.233, 37.719))) * 43758.5453);
}

#endif
