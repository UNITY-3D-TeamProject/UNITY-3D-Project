using UnityEngine;
using System.Collections.Generic;
using Map.Common;
using Map.Platforms;

namespace Map.Spiral
{
    public enum SectionGimmickType
    {
        Normal,
        MovingVertical,
        MovingHorizontal,
        Disappearing
    }

    [System.Serializable]
    public class SpiralSectionConfig
    {
        public SectionGimmickType gimmickType = SectionGimmickType.Normal;

        [Tooltip("The platform prefab to use for this section. If null, uses the generator's default.")]
        public GameObject customPlatformPrefab;

        public float heightIncrease = 4f;

        [Tooltip("Number of platforms in this 90-degree section. Set to 0 to auto-calculate based on Max Jump Distance/Height.")]
        public int platformCount = 0;
    }

    /// <summary>
    /// 중심을 감싸며 반시계 방향으로 올라가는 나선형 발판 맵을 생성한다.
    /// 맨 아래에 시작 발판, 맨 위에 도착 발판(둘 다 고정 발판)을 두고 그 사이를 구간(90도)들로 채운다.
    /// Random Section Count 가 0보다 크면 구간 구성을 시드로 무작위로 정한다.
    /// </summary>
    public class SpiralMapGenerator : MapGeneratorBase
    {
        private const int GIMMICK_TYPE_COUNT = 4;
        private const float START_HEIGHT_OFFSET = 0.1f;

        [Header("Global Settings")]
        public Transform centerPillar;
        public float spiralRadius = 5f;
        public GameObject defaultPlatformPrefab;

        [Header("Player Jump Constraints (For Reference/Validation)")]
        [Tooltip("Max jump distance player can achieve (used by designer to adjust radius/platform count)")]
        public float maxJumpDistance = 4f;
        [Tooltip("Max jump height player can achieve (used by designer to adjust heightIncrease)")]
        public float maxJumpHeight = 2f;

        [Header("Sections (Each covers 90 degrees CCW)")]
        public List<SpiralSectionConfig> sections = new List<SpiralSectionConfig>();

        [Header("Random Sections")]
        [Tooltip("0보다 크면 위의 Sections 대신, 이 개수만큼의 구간을 무작위 기믹으로 만든다.")]
        [SerializeField, Min(0)] private int _randomSectionCount;

        [Tooltip("무작위 구간 하나가 올라가는 높이의 범위 (최소, 최대)")]
        [SerializeField] private Vector2 _randomHeightRange = new Vector2(3.0f, 5.0f);

        /// <inheritdoc />
        protected override void Build(
            Transform root,
            System.Random random,
            List<Vector3> placementPoints,
            out Vector3 startPosition,
            out Vector3 goalPosition)
        {
            startPosition = transform.position;
            goalPosition = transform.position;

            if (defaultPlatformPrefab == null)
            {
                Debug.LogError("Default Platform Prefab is missing! Please assign one.");
                return;
            }

            List<SpiralSectionConfig> sectionConfigs = (_randomSectionCount > 0) ? CreateRandomSections(random) : sections;

            float currentAngle = 0f;
            float currentHeight = 0f;

            // 시작 발판: 플레이어가 서서 시작하는 고정 발판
            GameObject startPlatform = CreatePlatform(defaultPlatformPrefab, root, currentAngle, currentHeight, "Platform_Start");
            startPosition = GetSurfacePoint(startPlatform) + (Vector3.up * START_HEIGHT_OFFSET);

            float lastAngleStep = 30f;

            for (int i = 0; i < sectionConfigs.Count; i++)
            {
                var config = sectionConfigs[i];
                GameObject sectionRoot = new GameObject($"Section_{i:00}_{config.gimmickType}");
                sectionRoot.transform.SetParent(root, false);

                GameObject pPrefab = config.customPlatformPrefab != null ? config.customPlatformPrefab : defaultPlatformPrefab;

                lastAngleStep = GenerateSection(config, sectionRoot.transform, ref currentAngle, ref currentHeight, pPrefab, placementPoints);
            }

            // 도착 발판: 도착 지점 프리팹(포탈 등)이 놓이는 고정 발판
            currentAngle += lastAngleStep;
            GameObject goalPlatform = CreatePlatform(defaultPlatformPrefab, root, currentAngle, currentHeight, "Platform_Goal");
            goalPosition = GetSurfacePoint(goalPlatform);
        }

        private List<SpiralSectionConfig> CreateRandomSections(System.Random random)
        {
            var result = new List<SpiralSectionConfig>();

            for (int i = 0; i < _randomSectionCount; i++)
            {
                float heightRatio = (float)random.NextDouble();

                result.Add(new SpiralSectionConfig
                {
                    gimmickType = (SectionGimmickType)random.Next(GIMMICK_TYPE_COUNT),
                    heightIncrease = Mathf.Lerp(_randomHeightRange.x, _randomHeightRange.y, heightRatio),
                });
            }

            return result;
        }

        /// <returns>이 구간에서 발판 사이의 각도 간격</returns>
        private float GenerateSection(SpiralSectionConfig config, Transform parent, ref float currentAngle, ref float currentHeight, GameObject prefab, List<Vector3> placementPoints)
        {
            int count = config.platformCount;

            // Auto-calculate if count is 0 or less based on jump constraints
            if (count <= 0)
            {
                float arcLength = (Mathf.PI * 2f * spiralRadius) / 4f; // 90 degrees arc

                // Count based on distance
                int countByDist = Mathf.CeilToInt(arcLength / Mathf.Max(maxJumpDistance, 0.1f));

                // Count based on height
                int countByHeight = Mathf.CeilToInt(config.heightIncrease / Mathf.Max(maxJumpHeight, 0.1f));

                count = Mathf.Max(countByDist, countByHeight);

                // Minimum fallback
                if (count < 1) count = 1;
            }

            float angleStep = 90f / count; // 90 degrees total per section
            float heightStep = config.heightIncrease / count;

            for (int p = 0; p < count; p++)
            {
                // Advance angle counter-clockwise (positive angle around Y axis)
                currentAngle += angleStep;
                currentHeight += heightStep;

                GameObject platform = CreatePlatform(prefab, parent, currentAngle, currentHeight, $"Platform_{p:00}");

                // 움직이지 않는 발판 위에만 장애물·아이템을 놓을 수 있다
                if (config.gimmickType == SectionGimmickType.Normal)
                {
                    placementPoints.Add(GetSurfacePoint(platform));
                }

                ApplyGimmick(platform, config.gimmickType);
            }

            return angleStep;
        }

        /// <summary>
        /// 나선 위의 한 지점에 발판을 만든다. 중심은 centerPillar 가 있으면 그 위치, 없으면 이 오브젝트의 위치다.
        /// </summary>
        private GameObject CreatePlatform(GameObject prefab, Transform parent, float angle, float height, string platformName)
        {
            Vector3 center = (centerPillar != null) ? centerPillar.position : transform.position;
            float rad = angle * Mathf.Deg2Rad;

            // Counter-clockwise: x = cos, z = sin. Height is relative to the generator base
            Vector3 pos = new Vector3(
                center.x + (Mathf.Cos(rad) * spiralRadius),
                transform.position.y + height,
                center.z + (Mathf.Sin(rad) * spiralRadius));

            GameObject platform = Instantiate(prefab, pos, Quaternion.identity, parent);
            platform.name = platformName;

            // Look at center to make platform face inward/outward properly
            Vector3 lookTarget = center;
            lookTarget.y = pos.y; // Keep it level
            platform.transform.LookAt(lookTarget);
            // Rotate 90 degrees so the 'forward' axis points along the path
            platform.transform.Rotate(0, 90, 0);

            return platform;
        }

        // 발판 윗면의 중앙. 플레이어 시작 위치나 오브젝트 배치의 기준으로 쓴다.
        private static Vector3 GetSurfacePoint(GameObject platform)
        {
            Vector3 point = platform.transform.position;

            Renderer platformRenderer = platform.GetComponentInChildren<Renderer>();
            if (platformRenderer != null)
            {
                point.y = platformRenderer.bounds.max.y;
            }

            return point;
        }

        private void ApplyGimmick(GameObject platform, SectionGimmickType type)
        {
            switch (type)
            {
                case SectionGimmickType.MovingVertical:
                    var mv = platform.AddComponent<OscillatingPlatform>();
                    mv.moveAxis = Vector3.up;
                    mv.distance = 1.5f;
                    mv.speed = 2f;
                    // Offset based on position to create a wave effect
                    mv.timeOffset = platform.transform.position.y * 0.5f;
                    MakeRideable(platform);
                    break;
                case SectionGimmickType.MovingHorizontal:
                    var mh = platform.AddComponent<OscillatingPlatform>();
                    // Move in / out relative to the center. Since the platform was rotated 90 degrees,
                    // its 'right' or 'forward' will determine direction. 'right' points to the center.
                    mh.moveAxis = platform.transform.right;
                    mh.distance = 2f;
                    mh.speed = 1.5f;
                    mh.timeOffset = platform.transform.position.y * 0.5f;
                    MakeRideable(platform);
                    break;
                case SectionGimmickType.Disappearing:
                    var dp = platform.AddComponent<DisappearingPlatform>();
                    dp.activeDuration = 2f;
                    dp.inactiveDuration = 1.5f;
                    dp.timeOffset = platform.transform.position.y % 2f;
                    break;
            }
        }

        // 발판 프리팹에 이미 RideablePlatform 이 있으면 다시 붙이지 않는다
        private static void MakeRideable(GameObject platform)
        {
            if (!platform.TryGetComponent<RideablePlatform>(out _))
            {
                platform.AddComponent<RideablePlatform>();
            }
        }
    }
}
