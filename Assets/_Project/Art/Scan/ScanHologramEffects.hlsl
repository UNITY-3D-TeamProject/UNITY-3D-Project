// 스캔 홀로그램 셰이더들이 함께 쓰는 연출 함수. 벽 뒤 아이템(ScanSilhouetteHologram)과 상호작용 외곽선(ScanOutline)이 쓴다.
#ifndef SCAN_HOLOGRAM_EFFECTS_INCLUDED
#define SCAN_HOLOGRAM_EFFECTS_INCLUDED

// 0~1 사이 의사 난수
float ScanHash(float value)
{
    return frac(sin(value * 12.9898) * 43758.5453);
}

// 홀로그램 깜빡임 배율. 초당 rate 번 주사위를 굴려 가끔(20%) strength 만큼 어두워진다
float ScanFlicker(float rate, float strength)
{
    float roll = ScanHash(floor(_Time.y * rate));
    return 1.0 - (strength * step(0.8, roll));
}

// 화면 가로줄 무늬 배율. period 픽셀마다 아래 절반 줄을 dim 만큼 어둡게 한다.
// 화면에 고정된 줄이라 얇은 가로 외곽선이 줄 사이를 오가며 통째로 깜빡이지 않는다
float ScanLines(float screenY, float period, float dim)
{
    return lerp(dim, 1.0, step(0.5, frac(screenY / period)));
}

// 월드 높이를 따라 위로 지나가는 밝은 띠. spacing 미터마다 하나씩, speed(m/s)로 올라가며 띠 중앙이 1 이다
float ScanSweep(float positionY, float spacing, float width, float speed)
{
    float phase = frac(((positionY - (_Time.y * speed)) / spacing) + 0.5) - 0.5;
    float distanceToBand = abs(phase) * spacing;
    return 1.0 - smoothstep(0.0, width, distanceToBand);
}

#endif
