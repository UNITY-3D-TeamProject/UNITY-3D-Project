#ifndef PROJECT_BOSS_A_CORRUPTED_ARMOR_INCLUDED
#define PROJECT_BOSS_A_CORRUPTED_ARMOR_INCLUDED

// Supplied per renderer by BossAMaterialSpace. Skinning can render with an
// identity object matrix, so world space must be mapped back to the bind pose.
float4x4 _BossAWorldToRest;
float4x4 _BossANormalToRest;

// Supplied per renderer by BossAGlitch. An unset mask (0) leaves the mesh untouched.
float _BossAGlitchMask;    // selected cells, bit index = row (bottom to top) * 3 + column
float4 _BossAGlitchBounds; // bind-pose min X, min Z, size X, size Z
float4 _BossAGlitchShift;  // bands per cell row, shift (cell widths), shifts per second, chance
float4 _BossAGlitchSpark;  // burst period (s), burst duration (s), core width (cell widths), intensity
float4 _BossAGlitchSparkColor;

// Classic Shader Graph Custom Function (URP 17.4). Coordinates use the existing
// Gripper Robot mesh: Z is up, and the front faces negative Y in mesh space.

// Bind-pose position mapped to the 3x3 grid (0 to 3 on each axis).
float2 BossAGlitchGrid(float3 restPosition)
{
    float2 size = max(_BossAGlitchBounds.zw, 0.0001);
    return saturate((restPosition.xz - _BossAGlitchBounds.xy) / size) * 3.0;
}

float BossAGlitchCellIndex(float2 grid)
{
    float2 cell = min(floor(grid), 2.0);
    return cell.y * 3.0 + cell.x;
}

float BossAGlitchIsSelected(float cellIndex)
{
    return (float)(((uint)_BossAGlitchMask >> (uint)cellIndex) & 1u);
}

// Vertex stage: horizontal bands inside selected cells occasionally jump sideways,
// like the falling 0/1 hazards. Position is world space, Out is object space.
void BossAGlitchVertex_float(float3 Position, float Time, out float3 Out)
{
    float3 rest = mul(_BossAWorldToRest, float4(Position, 1.0)).xyz;
    float2 grid = BossAGlitchGrid(rest);
    float cellIndex = BossAGlitchCellIndex(grid);
    float band = floor(grid.y * max(_BossAGlitchShift.x, 1.0));
    float tick = floor(Time * _BossAGlitchShift.z);
    float random = frac(sin(dot(float2(band + cellIndex * 31.0, tick),
        float2(12.9898, 78.233))) * 43758.5453);
    float isShifted = step(1.0 - _BossAGlitchShift.w, random) * BossAGlitchIsSelected(cellIndex);
    float shift = (frac(random * 7.13) - 0.5) * 2.0 * _BossAGlitchShift.y * isShifted;

    // Bind-pose X axis in world space (first column of rest-to-world), one cell wide.
    float3 worldRight = mul(float3(1.0, 0.0, 0.0), (float3x3)_BossANormalToRest);
    Out = TransformWorldToObject(Position + worldRight * (shift * _BossAGlitchBounds.z / 3.0));
}

float BossAGlitchHash(float3 value)
{
    return frac(sin(dot(value, float3(12.9898, 78.233, 37.719))) * 43758.5453);
}

float BossASegmentDistance(float2 position, float2 from, float2 to)
{
    float2 segment = to - from;
    float along = saturate(dot(position - from, segment) / max(dot(segment, segment), 0.000001));
    return length(position - from - segment * along);
}

// Short jagged sparks like Boss B's hand bolts (LightningBolt: 5 segments, jitter 0.18)
// in Boss B's spark color by default.
// Each cell flashes 2-3 bolts once per period at a random moment, reshaping them about
// every 0.05 s and fading over the last 45% of the burst. Local is 0 to 1 inside the cell,
// antialias is the pixel footprint in the same units (taken before any branch).
float BossAGlitchSpark(float2 local, float cellIndex, float time, float antialias)
{
    float period = max(_BossAGlitchSpark.x, 0.05);
    float duration = clamp(_BossAGlitchSpark.y, 0.01, period);
    float cycleTime = time / period + BossAGlitchHash(float3(cellIndex, 1.7, 0.0));
    float cycle = floor(cycleTime);
    float start = BossAGlitchHash(float3(cellIndex, cycle, 2.3)) * (period - duration);
    float progress = (frac(cycleTime) * period - start) / duration;
    if ((progress < 0.0) || (progress >= 1.0)) return 0.0;

    float shapeTick = floor(time * 18.0);
    float distanceToBolt = 8.0;
    [unroll]
    for (int bolt = 0; bolt < 3; bolt++)
    {
        float3 seed = float3(cellIndex, cycle, bolt);
        if ((bolt == 2) && (BossAGlitchHash(seed + 5.1) < 0.5)) continue;

        float2 from = float2(BossAGlitchHash(seed + 0.1), BossAGlitchHash(seed + 0.2)) * 0.7 + 0.15;
        float angle = BossAGlitchHash(seed + 0.3) * 6.2831853;
        float boltLength = lerp(0.25, 0.5, BossAGlitchHash(seed + 0.4));
        float2 direction = float2(cos(angle), sin(angle));
        float2 side = float2(-direction.y, direction.x);
        float2 previous = from;

        [unroll]
        for (int i = 1; i <= 5; i++)
        {
            float t = i / 5.0;
            float jitter = (BossAGlitchHash(float3(cycle + shapeTick, bolt * 7.0 + i, cellIndex)) * 2.0 - 1.0)
                * 0.18 * boltLength * sin(t * 3.1415927);
            float2 current = from + direction * (boltLength * t) + side * jitter;
            distanceToBolt = min(distanceToBolt, BossASegmentDistance(local, previous, current));
            previous = current;
        }
    }

    float width = max(_BossAGlitchSpark.z, 0.0);
    float core = 1.0 - smoothstep(width, width + antialias, distanceToBolt);
    float glow = saturate(1.0 - distanceToBolt / max(width * 4.0, antialias));
    float fade = 1.0 - smoothstep(0.55, 1.0, progress);
    return (core + glow * glow * 0.35) * fade;
}

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

// Sparks flash over the cells selected by BossAGlitch. Side and top faces use the
// bind-pose depth axis so the bolts are not smeared along the projection, and bevels
// between planes fade out because the projected bolts collapse into dashes there.
float3 BossAGlitchSparkEmission(float3 Position, float3 absoluteNormal, float Time)
{
    float2 glitchGrid = BossAGlitchGrid(Position);
    float glitchCell = BossAGlitchCellIndex(glitchGrid);
    float sparkScale = 3.0 / max(_BossAGlitchBounds.z, 0.0001);
    float2 sparkGrid = glitchGrid;
    if (absoluteNormal.x > max(absoluteNormal.y, absoluteNormal.z))
    {
        sparkGrid.x = Position.y * sparkScale;
    }
    else if (absoluteNormal.z > absoluteNormal.y)
    {
        sparkGrid.y = Position.y * sparkScale;
    }
    float sparkAntialias = max(max(fwidth(sparkGrid.x), fwidth(sparkGrid.y)), 0.0001);
    float spark = 0.0;
    if (BossAGlitchIsSelected(glitchCell) > 0.5)
    {
        float planeFacing = max(absoluteNormal.x, max(absoluteNormal.y, absoluteNormal.z));
        spark = BossAGlitchSpark(frac(sparkGrid), glitchCell, Time, sparkAntialias)
            * smoothstep(0.8, 0.95, planeFacing);
    }
    return _BossAGlitchSparkColor.rgb * spark * max(_BossAGlitchSpark.w, 0.0);
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

    Emission += BossAGlitchSparkEmission(Position, absoluteNormal, Time);
    SurfaceMetallic = saturate(Metallic + metal * 0.12 - socket * 0.3);
    SurfaceSmoothness = saturate(Smoothness + metal * 0.12 - cracks * 0.2);
}

// Variant for the Virus King remodel, whose body, arm and hand textures are painted
// rather than palette strips. The painted colour is kept (ArmorTint multiplies it), the
// bright warm paint on the visor and chest core turns into red energy, and the glitch
// sparks are shared with the original. MetalTint is unused. Set EyeIntensity and
// CoreIntensity to 0 on materials without a visor or core (arms, hands).
void BossAVirusKingArmor_float(
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
    float3 normal = normalize(Normal);
    float3 absoluteNormal = abs(normal);
    BaseColor = SourceColor * ArmorTint;

    // Bright yellow-orange paint is the glowing part of the texture; the darker
    // orange joints stay below the luminance threshold.
    float luminance = dot(SourceColor, float3(0.2126, 0.7152, 0.0722));
    float warm = saturate((SourceColor.r - SourceColor.b) * 2.5);
    float glow = smoothstep(0.38, 0.62, luminance) * warm;
    float front = smoothstep(0.45, 0.85, -normal.y);

    // Chest core: all warm paint inside the painted hexagon around CoreCenter.
    float2 corePosition = (Position.xz - CoreCenter.xz) / max(CoreRadius, 0.001);
    float radius = BossAHexRadius(corePosition);
    float chest = (1.0 - smoothstep(0.95, 1.1, radius)) * front;
    float core = max(glow, warm * 0.7) * chest;
    float center = pow(saturate(1.0 - radius / 0.35), 2.0) * chest;

    // Visor: the glowing paint on the front of the head, well above the core so the
    // bright paint on the shoulders stays unlit.
    float eyes = glow * front * step(CoreCenter.z + CoreRadius * 1.8, Position.z);

    float2 crackUV = Position.xz;
    if (absoluteNormal.x > max(absoluteNormal.y, absoluteNormal.z))
    {
        crackUV = Position.yz;
    }
    else if (absoluteNormal.z > absoluteNormal.y)
    {
        crackUV = Position.xy;
    }
    float distanceToCrack = BossACrackDistance(crackUV * max(CrackScale, 0.01));
    float antialias = max(fwidth(distanceToCrack), 0.0005);
    float width = max(CrackWidth, 0.0);
    float cracks = (1.0 - smoothstep(width, width + antialias, distanceToCrack))
        * step(0.0001, CrackIntensity) * (1.0 - glow);

    float energy = saturate(core * step(0.0001, CoreIntensity) + eyes * step(0.0001, EyeIntensity));
    BaseColor = lerp(BaseColor, EnergyColor * 0.12, energy);
    float pulse = 1.0 + sin(Time * PulseSpeed) * saturate(PulseAmount);
    Emission = EnergyColor * (cracks * max(CrackIntensity, 0.0)
        + eyes * max(EyeIntensity, 0.0) + core * max(CoreIntensity, 0.0)) * pulse;
    Emission += float3(1.0, 0.28, 0.1) * center * glow * max(CoreIntensity, 0.0) * pulse;
    Emission += BossAGlitchSparkEmission(Position, absoluteNormal, Time);
    SurfaceMetallic = saturate(Metallic - energy * 0.3);
    SurfaceSmoothness = saturate(Smoothness - cracks * 0.2);
}

#endif
