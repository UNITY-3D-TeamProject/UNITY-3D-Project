#ifndef PROJECT_BOSS_B_CORRUPTED_ARMOR_INCLUDED
#define PROJECT_BOSS_B_CORRUPTED_ARMOR_INCLUDED

// Updated from the existing rig by BossBMaterialSpace. No mesh changes are needed.
float4x4 _BossBBodyWorldToRest;
float4x4 _BossBLeftPalmWorldToRest;
float4x4 _BossBRightPalmWorldToRest;

float BossBBand(float value, float lower, float upper)
{
    return step(lower, value) * (1.0 - step(upper, value));
}

float2 BossBHash(float2 cell)
{
    return frac(sin(float2(dot(cell, float2(127.1, 311.7)),
        dot(cell, float2(269.5, 183.3)))) * 43758.5453);
}

float BossBCrackDistance(float2 position)
{
    float2 cell = floor(position);
    float2 local = frac(position);
    float first = 8.0;
    float second = 8.0;
    [unroll]
    for (int y = -1; y <= 1; y++)
    {
        [unroll]
        for (int x = -1; x <= 1; x++)
        {
            float2 neighbor = float2(x, y);
            float2 delta = neighbor + 0.18 + BossBHash(cell + neighbor) * 0.64 - local;
            float distanceSquared = dot(delta, delta);
            if (distanceSquared < first)
            {
                second = first;
                first = distanceSquared;
            }
            else
            {
                second = min(second, distanceSquared);
            }
        }
    }
    return sqrt(second) - sqrt(first);
}

float BossBWrist(float3 position, float centerX, float3 settings)
{
    float local = step(abs(position.x - centerX), 0.37)
        * step(abs(position.y), 0.37);
    float edgeDistance = min(abs(position.z - settings.x), abs(position.z - settings.y));
    float antialias = max(fwidth(edgeDistance), 0.0005);
    return local * (1.0 - smoothstep(settings.z, settings.z + antialias, edgeDistance));
}

void BossBCorruptedArmor_float(
    float3 Position, float3 Normal, float2 UV, float3 SourceColor, float Time,
    float3 ArmorTint, float3 MetalTint, float3 EnergyColor,
    float Metallic, float Smoothness, float CrackScale, float CrackWidth,
    float CrackIntensity, float HeadRingIntensity, float HeadCenterIntensity,
    float PulseSpeed, float PulseAmount, float3 WristSettings, float WristIntensity,
    out float3 BaseColor, out float3 Emission,
    out float SurfaceMetallic, out float SurfaceSmoothness)
{
    // The Tanker Robot palette uses different bands from Boss A.
    float joints = BossBBand(UV.x, 0.103, 0.207);
    float orange = BossBBand(UV.x, 0.207, 0.310);
    float green = BossBBand(UV.x, 0.310, 0.414);
    float hub = BossBBand(UV.x, 0.414, 0.516);
    float cuffs = BossBBand(UV.x, 0.516, 0.620);
    float black = step(0.89, UV.x);
    float luminance = dot(SourceColor, float3(0.2126, 0.7152, 0.0722));
    float paintedShade = lerp(0.64, 1.0, saturate(luminance * 1.6));

    float3 bodyPosition = mul(_BossBBodyWorldToRest, float4(Position, 1.0)).xyz;
    float3 leftPosition = mul(_BossBLeftPalmWorldToRest, float4(Position, 1.0)).xyz;
    float3 rightPosition = mul(_BossBRightPalmWorldToRest, float4(Position, 1.0)).xyz;

    // Only the existing orange head rim glows; orange ankle joints stay copper.
    float head = step(2.22, bodyPosition.z) * step(bodyPosition.z, 3.21)
        * step(abs(bodyPosition.x), 0.51) * step(bodyPosition.y, -0.44);
    float headRing = orange * head;
    float wrist = cuffs * max(BossBWrist(leftPosition, -0.93, WristSettings),
        BossBWrist(rightPosition, 0.93, WristSettings));

    float3 armor = lerp(ArmorTint, ArmorTint * 0.48, joints);
    armor = lerp(armor, MetalTint, orange * (1.0 - head));
    armor = lerp(armor, ArmorTint * 0.18, black);
    BaseColor = armor * paintedShade;

    // Surface fractures remain confined to one existing claw, below its cuff.
    float claw = step(abs(leftPosition.x + 0.93), 0.49)
        * step(abs(leftPosition.y), 0.43) * BossBBand(leftPosition.z, 0.28, 0.705);
    float2 crackUV = float2(leftPosition.x, leftPosition.z);
    float distanceToCrack = BossBCrackDistance(crackUV * max(CrackScale, 0.01));
    float antialias = max(fwidth(distanceToCrack), 0.0005);
    float cracks = 1.0 - smoothstep(max(CrackWidth, 0.0),
        max(CrackWidth, 0.0) + antialias, distanceToCrack);
    float breakup = smoothstep(0.4, 0.8,
        sin(dot(crackUV, float2(19.7, 13.1))) * 0.5 + 0.5);
    cracks *= claw * joints * breakup;

    float pulse = 1.0 + sin(Time * PulseSpeed) * saturate(PulseAmount);
    float energy = headRing * max(HeadRingIntensity, 0.0)
        + hub * max(HeadCenterIntensity, 0.0)
        + wrist * max(WristIntensity, 0.0)
        + green * max(HeadCenterIntensity, 0.0) * 0.2
        + cracks * max(CrackIntensity, 0.0);
    BaseColor = lerp(BaseColor, ArmorTint * 0.35, headRing);
    BaseColor += EnergyColor * saturate(headRing + hub + wrist) * 0.08;
    Emission = EnergyColor * energy * pulse;
    SurfaceMetallic = saturate(Metallic + orange * (1.0 - head) * 0.12);
    SurfaceSmoothness = saturate(Smoothness + orange * 0.12 - cracks * 0.2);
}

#endif
