using UnityEngine;
using System.Collections.Generic;
using Map.Common;
using Map.Platforms;

namespace Map.Spiral
{
    /// <summary>나선 구간 하나에 적용할 발판 기믹 종류.</summary>
    public enum SectionGimmickType
    {
        Normal,             // 고정 발판
        MovingVertical,     // 위아래로 움직이는 발판
        MovingHorizontal,   // 중심 쪽으로 들어갔다 나오는 발판
        Disappearing        // 주기적으로 사라졌다 나타나는 발판
    }

    /// <summary>나선 구간(90도) 하나의 설정.</summary>
    [System.Serializable]
    public class SpiralSectionConfig
    {
        [Tooltip("이 구간의 발판에 적용할 기믹")]
        public SectionGimmickType gimmickType = SectionGimmickType.Normal;

        [Tooltip("이 구간에 쓸 발판 프리팹. 비워 두면 생성기의 기본 발판을 쓴다.")]
        public GameObject customPlatformPrefab;

        [Tooltip("이 구간을 지나는 동안 올라가는 높이")]
        public float heightIncrease = 4f;

        [Tooltip("이 구간(90도)의 발판 개수. 0이면 Max Jump Distance / Height 를 기준으로 자동 계산한다.")]
        public int platformCount = 0;
    }

    /// <summary>
    /// 중심을 감싸며 반시계 방향으로 올라가는 나선형 발판 맵을 생성한다.
    /// 맨 아래에 시작 발판, 맨 위에 도착 발판(둘 다 고정 발판)을 두고 그 사이를 구간(90도)들로 채운다.
    /// Random Section Count 가 0보다 크면 구간 구성을 시드로 무작위로 정한다.
    /// </summary>
    public class SpiralMapGenerator : MapGeneratorBase
    {
        // SectionGimmickType 의 항목 수. 무작위 구간의 기믹을 고를 때 쓴다. enum 을 바꾸면 함께 고친다.
        private const int GIMMICK_TYPE_COUNT = 4;

        // 플레이어가 발판에 파묻히지 않도록 시작 위치를 윗면에서 띄우는 높이
        private const float START_HEIGHT_OFFSET = 0.1f;

        [Header("Global Settings")]
        [Tooltip("나선의 중심 (선택). 비워 두면 이 오브젝트의 위치가 중심이 된다.")]
        public Transform centerPillar;

        [Tooltip("중심에서 발판까지의 거리")]
        public float spiralRadius = 5f;

        [Tooltip("기본 발판 프리팹. 시작·도착 발판과, 전용 프리팹이 없는 구간에 쓴다.")]
        public GameObject defaultPlatformPrefab;

        [Header("Player Jump Constraints (For Reference/Validation)")]
        [Tooltip("플레이어가 뛸 수 있는 최대 거리. 발판 개수 자동 계산에 쓰인다.")]
        public float maxJumpDistance = 4f;
        [Tooltip("플레이어가 뛸 수 있는 최대 높이. 발판 개수 자동 계산에 쓰인다.")]
        public float maxJumpHeight = 2f;

        [Header("Sections (Each covers 90 degrees CCW)")]
        [Tooltip("아래에서 위로 차례대로 이어지는 구간 목록. Random Section Count 가 0일 때만 쓴다.")]
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
                Debug.LogError($"[{name}] 기본 발판 프리팹(Default Platform Prefab)이 연결되지 않았습니다.", this);
                return;
            }

            List<SpiralSectionConfig> sectionConfigs = (_randomSectionCount > 0) ? CreateRandomSections(random) : sections;

            float currentAngle = 0f;
            float currentHeight = 0f;

            // 시작 발판: 플레이어가 서서 시작하는 고정 발판
            GameObject startPlatform = CreatePlatform(defaultPlatformPrefab, root, currentAngle, currentHeight, "Platform_Start");
            startPosition = GetSurfacePoint(startPlatform) + (Vector3.up * START_HEIGHT_OFFSET);

            // 구간이 하나도 없을 때 시작 발판과 도착 발판 사이의 각도
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

        // 기믹 종류와 올라가는 높이를 시드로 무작위로 정한 구간 목록을 만든다. 발판 개수는 자동 계산에 맡긴다.
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

        /// <summary>
        /// 구간 하나(90도)의 발판을 만들고 기믹을 붙인다. currentAngle 과 currentHeight 는 구간 끝 값으로 갱신된다.
        /// </summary>
        /// <returns>이 구간에서 발판 사이의 각도 간격</returns>
        private float GenerateSection(SpiralSectionConfig config, Transform parent, ref float currentAngle, ref float currentHeight, GameObject prefab, List<Vector3> placementPoints)
        {
            int count = config.platformCount;

            // 개수가 0 이하이면 점프 한계를 기준으로 자동 계산한다
            if (count <= 0)
            {
                float arcLength = (Mathf.PI * 2f * spiralRadius) / 4f; // 90도 호의 길이

                // 거리 기준 개수
                int countByDist = Mathf.CeilToInt(arcLength / Mathf.Max(maxJumpDistance, 0.1f));

                // 높이 기준 개수
                int countByHeight = Mathf.CeilToInt(config.heightIncrease / Mathf.Max(maxJumpHeight, 0.1f));

                count = Mathf.Max(countByDist, countByHeight);

                // 최소 1개
                if (count < 1) count = 1;
            }

            float angleStep = 90f / count; // 구간 하나는 90도
            float heightStep = config.heightIncrease / count;

            for (int p = 0; p < count; p++)
            {
                // 반시계 방향으로 나아간다 (Y축 기준 양의 각도)
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

            // 반시계 방향: x = cos, z = sin. 높이는 생성기 위치 기준
            Vector3 pos = new Vector3(
                center.x + (Mathf.Cos(rad) * spiralRadius),
                transform.position.y + height,
                center.z + (Mathf.Sin(rad) * spiralRadius));

            GameObject platform = Instantiate(prefab, pos, Quaternion.identity, parent);
            platform.name = platformName;

            // 발판이 중심을 바라보게 한다
            Vector3 lookTarget = center;
            lookTarget.y = pos.y; // 수평 유지
            platform.transform.LookAt(lookTarget);
            // 90도 돌려 forward 축이 진행 방향을 향하게 한다
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

        // 기믹 종류에 맞는 컴포넌트를 발판에 붙인다. 시작 시점은 높이에 따라 어긋나게 해 발판마다 박자가 다르다.
        private void ApplyGimmick(GameObject platform, SectionGimmickType type)
        {
            switch (type)
            {
                case SectionGimmickType.MovingVertical:
                    var mv = platform.AddComponent<OscillatingPlatform>();
                    mv.moveAxis = Vector3.up;
                    mv.distance = 1.5f;
                    mv.speed = 2f;
                    // 높이에 따라 시작 시점을 어긋나게 해 물결처럼 움직이게 한다
                    mv.timeOffset = platform.transform.position.y * 0.5f;
                    MakeRideable(platform);
                    break;
                case SectionGimmickType.MovingHorizontal:
                    var mh = platform.AddComponent<OscillatingPlatform>();
                    // 중심 쪽으로 들어갔다 나왔다 한다. 발판을 90도 돌려 두었으므로 right 축이 반지름 방향이다.
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
