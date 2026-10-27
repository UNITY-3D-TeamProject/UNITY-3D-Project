#ifndef PROJECT_BOSS_A_CORRUPTED_ARMOR_INCLUDED
#define PROJECT_BOSS_A_CORRUPTED_ARMOR_INCLUDED

// Supplied per renderer by BossAMaterialSpace. Skinning can render with an
// identity object matrix, so world space must be mapped back to the bind pose.
float4x4 _BossAWorldToRest;
float4x4 _BossANormalToRest;

// Classic Shader Graph Custom Function (URP 17.4). Coordinates use the existing
// Gripper Robot mesh: Z is up, and the front faces negative Y in mesh space.
float2 BossAHash(float2 cell)
{
    return frac(sin(float2(dot(cell, float2(127.1, 311.7)),
        dot(cell, float2(269.5, 183.3)))) * 43758.5453);
}

float BossACrackDistance(float2 position)
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
            float2 delta = neighbor + 0.18 + BossAHash(cell + neighbor) * 0.64 - local;
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

float BossAHexRadius(float2 position)
{
    position = abs(position);
    return max(position.x * 0.8660254 + position.y * 0.5, position.y);
}

float BossABand(float coordinate, float lower, float upper)
{
    return step(lower, coordinate) * (1.0 - step(upper, coordinate));
}

void BossACorruptedArmor_float(
    float3 Position, float3 Normal, float2 UV, float3 SourceColor, float Time,
    float3 ArmorTint, float3 MetalTint, float3 EnergyColor,
    float Metallic, float Smoothness, float CrackScale, float CrackWidth,
    float CrackIntensity, float EyeIntensity, float CoreIntensity,
    float PulseSpeed, float PulseAmount, float3 CoreCenter, float CoreRadius,
    out float3 BaseColor, out float3 Emission,
    out float SurfaceMetallic, out float SurfaceSmoothness)
{
    Position = mul(_BossAWorldToRest, float4(Position, 1.0)).xyz;
    Normal = mul((float3x3)_BossANormalToRest, Normal);
    // Preserve the original palette's component boundaries and painted shading.
    float metal = BossABand(UV.x, 0.414, 0.516);
    float joints = BossABand(UV.x, 0.103, 0.207);
    float black = step(0.89, UV.x);
    float luminance = dot(SourceColor, float3(0.2126, 0.7152, 0.0722));
    float paintedShade = lerp(0.64, 1.0, saturate(luminance * 1.6));
    float3 armor = lerp(ArmorTint, MetalTint, metal);
    armor = lerp(armor, ArmorTint * 0.48, joints);
    armor = lerp(armor, ArmorTint * 0.18, black);
    BaseColor = armor * paintedShade;

    // Evaluate the fracture field in the body's bind-pose coordinate system.
    float3 normal = normalize(Normal);
    float3 absoluteNormal = abs(normal);
    float2 crackUV = Position.xz;
    if (absoluteNormal.x > max(absoluteNormal.y, absoluteNormal.z))
    {
        crackUV = Position.yz;
    }
    else if (absoluteNormal.z > absoluteNormal.y)
    {
        crackUV = Position.xy;
    }

    float2 fracturePosition = crackUV * max(CrackScale, 0.01);
    float distanceToCrack = BossACrackDistance(fracturePosition);
    float antialias = max(fwidth(distanceToCrack), 0.0005);
    float width = max(CrackWidth, 0.0);
    float cracks = 1.0 - smoothstep(width, width + antialias, distanceToCrack);
    float crackHalo = 1.0 - smoothstep(width, width + 0.055, distanceToCrack);
    float breakup = smoothstep(0.2, 0.7,
        sin(dot(crackUV, float2(19.7, 13.1))) * 0.5 + 0.5);
    float armorMask = (1.0 - metal) * (1.0 - joints) * (1.0 - black);
    armorMask *= step(0.0001, CrackIntensity);
    cracks *= armorMask * breakup;
    crackHalo *= armorMask * breakup;

    // Only the yellow visor palette is emissive; orange joints stay metallic.
    float eyes = BossABand(UV.x, 0.207, 0.31) * step(1.68, Position.z);

    // The core is a surface design on the existing chest plate, not a new weak point.
    float2 corePosition = (Position.xz - CoreCenter.xz) / max(CoreRadius, 0.001);
    float radius = BossAHexRadius(corePosition);
    float front = smoothstep(0.45, 0.85, -normal.y);
    float depth = 1.0 - smoothstep(0.04, 0.11, abs(Position.y - CoreCenter.y));
    float chest = front * depth;
    float coreAA = max(fwidth(radius), 0.005);
    float socket = (1.0 - smoothstep(1.08, 1.08 + coreAA, radius)) * chest;
    float ringOuter = 1.0 - smoothstep(0.025, 0.025 + coreAA, abs(radius - 0.94));
    float ringInner = 1.0 - smoothstep(0.018, 0.018 + coreAA, abs(radius - 0.69));
    float iris = 1.0 - smoothstep(0.14, 0.46, radius);
    float center = pow(saturate(1.0 - radius / 0.24), 2.0);
    float angle = atan2(corePosition.y, corePosition.x);
    float spokes = pow(saturate(cos(angle * 6.0)), 22.0)
        * (1.0 - smoothstep(0.58, 0.88, radius));
    float core = (ringOuter * 0.45 + ringInner * 0.55 + iris * 0.7 + spokes * 0.3) * chest;

    BaseColor = lerp(BaseColor, ArmorTint * 0.13, socket);
    BaseColor += EnergyColor * (crackHalo * 0.035 + core * 0.06 + eyes * 0.12);
    float pulse = 1.0 + sin(Time * PulseSpeed) * saturate(PulseAmount);
    Emission = EnergyColor * (cracks * max(CrackIntensity, 0.0)
        + eyes * max(EyeIntensity, 0.0) + core * max(CoreIntensity, 0.0)) * pulse;
    Emission += float3(1.0, 0.28, 0.1) * center * chest * max(CoreIntensity, 0.0) * pulse;
    SurfaceMetallic = saturate(Metallic + metal * 0.12 - socket * 0.3);
    SurfaceSmoothness = saturate(Smoothness + metal * 0.12 - cracks * 0.2);
}

#endif
