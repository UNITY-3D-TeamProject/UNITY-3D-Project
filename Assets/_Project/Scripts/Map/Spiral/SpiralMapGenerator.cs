using UnityEngine;
using System.Collections.Generic;
using Map.Common;
using Map.Gimmicks;
using Map.Platforms;

namespace Map.Spiral
{
    /// <summary>태그(나선 한 바퀴) 하나의 설정. 태그의 끝에는 넓은 고정 발판과 악성 피드, 아이템이 놓인다.</summary>
    [System.Serializable]
    public class SpiralTagConfig
    {
        [Tooltip("태그 이름 (하이어라키 표시용)")]
        public string tagName;

        [Tooltip("이 태그의 구간에 쓸 발판 프리팹 목록. 구간마다 하나를 무작위로 고른다. 비워 두면 생성기의 기본 발판을 쓴다.")]
        public List<GameObject> platformPrefabs = new List<GameObject>();

        [Tooltip("이 태그의 구간에 쓸 패턴 목록. 구간마다 하나를 비중에 따라 무작위로 고른다. 비워 두면 설정 에셋의 Section Patterns 에서 고른다.")]
        public List<SOSpiralSectionPattern> sectionPatterns = new List<SOSpiralSectionPattern>();

        [Tooltip("이 태그에 나올 기믹 묶음 프리팹 목록. 묶음 구간에서 고정 발판 사이에 무작위로 끼운다. 비워 두면 묶음 구간이 나오지 않는다.")]
        public List<SpiralChunk> chunkPrefabs = new List<SpiralChunk>();

        [Tooltip("구간이 패턴 대신 기믹 묶음 구간이 될 확률 (0: 항상 패턴, 1: 항상 묶음)")]
        [Range(0f, 1f)] public float chunkSectionChance = 0.5f;

        [Tooltip("패턴 구간의 발판 위에 놓을 장애물 목록. Count 는 한 번 뽑혔을 때 놓는 개수, Offset 은 발판 윗면 중앙에서 띄울 거리다.")]
        public List<SMapPlacement> obstacles = new List<SMapPlacement>();

        [Tooltip("패턴 구간 하나마다 위 목록에서 무작위로 뽑는 횟수")]
        [Min(0)] public int obstaclesPerSection = 1;

        [Tooltip("이 태그의 빈 고정 발판 위에 무작위로 놓을 아이템. Count 는 이 태그에 놓는 개수, Offset 은 발판 윗면 중앙에서 띄울 거리다. 비워 두면 설정 에셋의 Tag Items 를 쓴다.")]
        public List<SMapPlacement> tagItems = new List<SMapPlacement>();

        [Tooltip("태그의 마지막에 놓는 넓은 고정 발판. 비워 두면 생성기의 기본 발판을 쓴다.")]
        public GameObject endPlatformPrefab;

        [Tooltip("태그 끝 발판 위에서 길을 막는 악성 피드 프리팹 (선택)")]
        public GameObject maliciousFeedPrefab;

        [Tooltip("악성 피드 뒤에 놓는 아이템 프리팹 (선택)")]
        public GameObject itemPrefab;

        [Tooltip("이 태그의 주위에 떠다니는 피드 프리팹 (선택). 개수와 거리는 설정 에셋의 Ambient Feeds 에서 정한다.")]
        public GameObject ambientFeedPrefab;
    }

    /// <summary>
    /// 중심을 감싸며 반시계 방향으로 올라가는 나선형 발판 맵을 생성한다.
    /// 맨 아래에 시작 발판을 두고, 태그마다 구간(90도) 여러 개를 만든다. 구간마다 패턴(SOSpiralSectionPattern) 하나를 무작위로 골라
    /// 발판을 놓는 규칙과 움직임, 배치물을 정한다. 태그 끝에는 넓은 고정 발판과 악성 피드·아이템을, 꼭대기 중앙에는 라운드 발판을 둔다.
    /// </summary>
    public class SpiralMapGenerator : MapGeneratorBase
    {
        // 플레이어가 발판에 파묻히지 않도록 시작 위치를 윗면에서 띄우는 높이
        private const float START_HEIGHT_OFFSET = 0.1f;

        // 라운드 발판 윗면을 도착 발판 윗면보다 살짝 낮춰, 겹치는 면이 깜빡이지 않게 한다
        private const float ROUND_PLATFORM_DROP = 0.02f;

        // 직전 구간과 같은 패턴이 뽑혔을 때 다시 뽑아 보는 횟수
        private const int PATTERN_REROLL_COUNT = 4;

        // 2단 점프가 필요한 패턴에서 한 번 점프의 한계(틈·높이)에 곱하는 값
        private const float DOUBLE_JUMP_FACTOR = 1.6f;

        // 패턴 구간의 틈이 목표 틈의 이 배율을 넘으면 발판을 하나 더 놓는다. 그렇게 다시 재 보는 횟수의 한계
        private const float GAP_TOLERANCE = 1.35f;
        private const int GAP_RETRY_COUNT = 3;

        // 기믹 묶음 구간에서 한 걸음에 올라가는 높이의 한계 (Max Jump Height 에 대한 비율)
        private const float CHUNK_STEP_HEIGHT_RATIO = 0.75f;

        // 흘러가는 발판이 마지막 발판 자리를 지나 더 가는 거리 (발판 간격에 대한 비율)
        private const float STREAM_END_OVERRUN = 0.35f;

        // 사격 표적을 발판 윗면에서 띄우는 높이
        private const float SHOOT_TARGET_HEIGHT = 2.5f;

        // 주위 피드와 라운드 발판 사이에 두는 여유. 피드가 위아래로 떠다니는 높이(FeedPost 의 Bob Height)보다 커야 한다.
        private const float ROUND_FEED_CLEARANCE = 0.5f;

        [Header("Settings")]
        [Tooltip("나선의 크기, 태그 구성, 구간 패턴, 프리팹을 담은 설정 에셋")]
        [SerializeField] private SOSpiralMapSettings _settings;

        [Header("Scene References")]
        [Tooltip("나선의 중심 (선택). 비워 두면 이 오브젝트의 위치가 중심이 된다.")]
        public Transform centerPillar;

        [Tooltip("씬에 미리 놓아 둔 체크포인트 (선택). 차례대로 각 태그 끝 발판의 입구로 옮기고, 그다음 하나는 라운드 발판 위로 옮긴다.")]
        [SerializeField] private Transform[] _checkpointAnchors;

        // 아래 세 값은 Build 가 도는 동안만 쓴다: 마지막으로 놓은 발판의 각도·높이·진행 방향 길이의 절반
        private float _currentAngle;
        private float _currentHeight;
        private float _previousHalfLength;

        // 프리팹 크기를 매번 다시 재지 않도록 Build 동안 보관한다
        private readonly Dictionary<GameObject, Bounds> _prefabBounds = new Dictionary<GameObject, Bounds>();

        // 주위 피드는 라운드 발판과 겹치지 않는 자리를 정한 뒤에 만들도록, Build 동안 놓을 자리만 모아 둔다
        private readonly List<(GameObject Prefab, Vector3 Position, Transform Parent)> _ambientFeeds =
            new List<(GameObject Prefab, Vector3 Position, Transform Parent)>();

        // 고를 패턴이 하나도 없을 때 쓰는 기본 패턴(고정 발판). 처음 필요할 때 만든다.
        private SOSpiralSectionPattern _fallbackPattern;

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

            if (_settings == null)
            {
                Debug.LogError($"[{name}] 설정 에셋(Settings)이 연결되지 않았습니다.", this);
                return;
            }

            if (_settings.DefaultPlatformPrefab == null)
            {
                Debug.LogError($"[{name}] 기본 발판 프리팹(Default Platform Prefab)이 연결되지 않았습니다.", this);
                return;
            }

            if (_settings.Tags.Count == 0)
            {
                Debug.LogError($"[{name}] 설정 에셋에 태그(Tags)가 하나도 없습니다.", this);
                return;
            }

            if (_settings.PlatformGap > _settings.MaxJumpDistance)
            {
                Debug.LogWarning($"[{name}] Platform Gap({_settings.PlatformGap})이 Max Jump Distance({_settings.MaxJumpDistance})보다 큽니다.", this);
            }

            _prefabBounds.Clear();
            _ambientFeeds.Clear();
            _currentAngle = 0f;
            _currentHeight = 0f;

            // 시작 발판: 플레이어가 서서 시작하는 고정 발판
            GameObject startPlatform = CreatePlatform(_settings.DefaultPlatformPrefab, root, _currentAngle, _currentHeight, 0f, "Platform_Start");
            startPosition = GetSurfacePoint(startPlatform) + (Vector3.up * START_HEIGHT_OFFSET);
            _previousHalfLength = GetPrefabSize(_settings.DefaultPlatformPrefab).z * 0.5f;

            goalPosition = BuildTags(root, random, placementPoints);
        }

        // 태그마다 무작위 구간을 만들고 태그 끝에 체크포인트·악성 피드·아이템을 놓는다. 마지막에 계단과 라운드 발판을 만든다.
        // 돌려주는 값은 도착 지점(라운드 발판 윗면 중앙)이다.
        private Vector3 BuildTags(Transform root, System.Random random, List<Vector3> placementPoints)
        {
            GameObject lastPlatform = null;
            SOSpiralSectionPattern previousPattern = null;
            SpiralChunk previousChunk = null;

            for (int t = 0; t < _settings.Tags.Count; t++)
            {
                SpiralTagConfig tag = _settings.Tags[t];
                Transform tagRoot = new GameObject($"Tag_{t + 1:00}_{tag.tagName}").transform;
                tagRoot.SetParent(root, false);

                GameObject endPrefab = (tag.endPlatformPrefab != null) ? tag.endPlatformPrefab : _settings.DefaultPlatformPrefab;

                // 이 태그에서 아이템을 놓을 수 있는 고정 발판들
                var itemSpots = new List<GameObject>();

                // 주위 피드를 놓을 때 쓰는, 이 태그가 시작하는 각도와 높이
                float tagStartAngle = _currentAngle;
                float tagStartHeight = _currentHeight;

                for (int s = 0; s < _settings.SectionsPerTag; s++)
                {
                    // 구간이 올라가는 높이, 구간 종류, 발판 프리팹을 시드로 무작위로 정한다
                    Vector2 heightRange = _settings.RandomHeightRange;
                    float heightIncrease = Mathf.Lerp(heightRange.x, heightRange.y, (float)random.NextDouble());
                    bool hasChunks = (tag.chunkPrefabs != null) && (tag.chunkPrefabs.Count > 0);
                    bool isChunkSection = hasChunks && (random.NextDouble() < tag.chunkSectionChance);
                    SOSpiralSectionPattern pattern = isChunkSection ? null : PickPattern(tag, random, previousPattern);
                    GameObject prefab = PickPlatformPrefab(tag, random);
                    previousPattern = pattern;

                    // 같은 태그 안에서는 구간 사이에 고정 발판을 놓아, 한 패턴을 끝내고 다음 패턴으로 들어가기 전에 쉴 곳을 준다
                    float arcLength = (Mathf.PI * 2f * _settings.SpiralRadius) / 4f;
                    if ((s > 0) && (_settings.ConnectorCount > 0))
                    {
                        float connectorArc = PlaceConnectors(tagRoot, s, prefab, placementPoints, itemSpots);

                        // 연결 발판을 90도 안에 넣는 설정이면 그만큼 패턴 구간을 줄인다
                        if (_settings.IsConnectorInsideSection)
                        {
                            arcLength = Mathf.Max(arcLength - connectorArc, 0f);
                        }
                    }

                    string sectionName = isChunkSection ? "Chunks" : pattern.name;
                    Transform sectionRoot = new GameObject($"Section_{s:00}_{sectionName}").transform;
                    sectionRoot.SetParent(tagRoot, false);

                    // 태그의 마지막 구간은 넓은 고정 발판으로 끝난다
                    bool isLastSection = (s == _settings.SectionsPerTag - 1);
                    GameObject lastPrefab = isLastSection ? endPrefab : null;

                    if (isChunkSection)
                    {
                        lastPlatform = GenerateChunkSection(
                            tag,
                            heightIncrease,
                            arcLength,
                            sectionRoot,
                            prefab,
                            lastPrefab,
                            placementPoints,
                            itemSpots,
                            random,
                            ref previousChunk);
                    }
                    else
                    {
                        lastPlatform = GenerateSection(
                            tag,
                            pattern,
                            heightIncrease,
                            arcLength,
                            sectionRoot,
                            prefab,
                            lastPrefab,
                            placementPoints,
                            itemSpots,
                            random);
                    }
                }

                PlaceTagItems(tag, itemSpots, tagRoot, random);
                PlaceTagEnd(tag, t, lastPlatform, endPrefab, tagRoot);

                if (tag.ambientFeedPrefab != null)
                {
                    PlaceAmbientFeeds(tag.ambientFeedPrefab, tagRoot, tagStartAngle, tagStartHeight, _settings.OuterFeedCount, _settings.OuterFeedDistance, 1f, random);
                    PlaceAmbientFeeds(tag.ambientFeedPrefab, tagRoot, tagStartAngle, tagStartHeight, _settings.InnerFeedCount, _settings.InnerFeedDistance, -1f, random);
                }
            }

            // 도착 발판까지 올라가는 계단
            Transform exitRoot = new GameObject("Exit").transform;
            exitRoot.SetParent(root, false);

            float stepHeight = Mathf.Min(_settings.ExitStepHeight, _settings.MaxJumpHeight);
            float stepLength = GetPrefabSize(_settings.DefaultPlatformPrefab).z;
            for (int i = 0; i < _settings.ExitStepCount; i++)
            {
                string platformName = (i == _settings.ExitStepCount - 1) ? "Platform_Goal" : $"Platform_Exit_{i:00}";
                lastPlatform = PlaceNextPlatform(_settings.DefaultPlatformPrefab, exitRoot, _settings.PlatformGap, stepHeight, stepLength, 0f, platformName);
            }

            // 라운드 발판: 나선 중심, 도착 발판과 같은 높이
            Vector3 goalSurface = GetSurfacePoint(lastPlatform);
            Vector3 center = GetCenter();
            Vector3 top = new Vector3(center.x, goalSurface.y - ROUND_PLATFORM_DROP, center.z);

            GameObject roundPlatform = null;
            if (_settings.RoundPlatformPrefab != null)
            {
                roundPlatform = Instantiate(_settings.RoundPlatformPrefab, top, Quaternion.identity, root);
                roundPlatform.name = "RoundPlatform";
            }

            SpawnAmbientFeeds(roundPlatform);

            // 라운드 체크포인트: 도착 발판에서 건너오는 쪽 가장자리의 안쪽, 중심을 바라보게 둔다
            Vector3 toGoal = goalSurface - top;
            toGoal.y = 0f;
            Vector3 goalDirection = toGoal.normalized;
            Vector3 checkpointPosition = top + (goalDirection * Mathf.Max(0f, _settings.RoundPlatformRadius - _settings.RoundCheckpointInset));
            MoveCheckpointAnchor(_settings.Tags.Count, checkpointPosition, Quaternion.LookRotation(-goalDirection));

            return top;
        }

        // 태그 끝 발판 위에 들어오는 쪽부터 체크포인트, 악성 피드, 아이템을 차례로 놓는다.
        private void PlaceTagEnd(SpiralTagConfig tag, int tagIndex, GameObject endPlatform, GameObject endPrefab, Transform tagRoot)
        {
            Transform platform = endPlatform.transform;
            Vector3 forward = platform.forward;
            Vector3 entry = GetSurfacePoint(endPlatform) - (forward * (GetPrefabSize(endPrefab).z * 0.5f));

            MoveCheckpointAnchor(tagIndex, entry + (forward * _settings.CheckpointDistance), platform.rotation);

            if (tag.maliciousFeedPrefab != null)
            {
                Instantiate(tag.maliciousFeedPrefab, entry + (forward * _settings.FeedDistance), platform.rotation, tagRoot);
            }

            if (tag.itemPrefab != null)
            {
                Instantiate(tag.itemPrefab, entry + (forward * _settings.ItemDistance), platform.rotation, tagRoot);
            }
        }

        // 태그가 지나온 길(시작 각도·높이 ~ 지금 각도·높이)을 따라 주위 피드를 count 개 띄운다.
        // side 가 1 이면 나선의 바깥쪽, -1 이면 안쪽이다. 길을 개수만큼 나눈 칸마다 하나씩 놓아 한곳에 몰리지 않게 한다.
        private void PlaceAmbientFeeds(
            GameObject prefab,
            Transform tagRoot,
            float startAngle,
            float startHeight,
            int count,
            Vector2 distanceRange,
            float side,
            System.Random random)
        {
            Vector3 center = GetCenter();
            Vector2 heightRange = _settings.AmbientFeedHeightRange;

            for (int i = 0; i < count; i++)
            {
                float progress = (i + (float)random.NextDouble()) / count;
                float distance = Mathf.Lerp(distanceRange.x, distanceRange.y, (float)random.NextDouble());
                float lift = Mathf.Lerp(heightRange.x, heightRange.y, (float)random.NextDouble());

                float rad = Mathf.Lerp(startAngle, _currentAngle, progress) * Mathf.Deg2Rad;
                float radius = _settings.SpiralRadius + (side * distance);
                float height = Mathf.Lerp(startHeight, _currentHeight, progress) + lift;

                Vector3 position = new Vector3(
                    center.x + (Mathf.Cos(rad) * radius),
                    transform.position.y + height,
                    center.z + (Mathf.Sin(rad) * radius));

                _ambientFeeds.Add((prefab, position, tagRoot));
            }
        }

        // 미뤄 둔 주위 피드를 만든다. 마지막 태그의 안쪽 피드는 라운드 발판 아래에 놓이므로, 발판과 겹치는 피드는 발판 아랫면 밑으로 내려서 만든다.
        // 피드(FeedPost)는 만들어질 때의 자리를 기준으로 떠다니므로, 만든 뒤에 옮기면 안 되고 처음부터 맞는 자리에 만들어야 한다.
        private void SpawnAmbientFeeds(GameObject roundPlatform)
        {
            Renderer platformRenderer = (roundPlatform != null) ? roundPlatform.GetComponentInChildren<Renderer>() : null;
            Vector3 center = GetCenter();

            foreach ((GameObject prefab, Vector3 position, Transform parent) in _ambientFeeds)
            {
                Vector3 spawnPosition = position;

                if (platformRenderer != null)
                {
                    Bounds platformBounds = platformRenderer.bounds;
                    Bounds feedBounds = GetPrefabBounds(prefab);

                    // 피드는 카메라를 향해 돌므로 수평으로는 가장 넓은 폭의 절반까지 차지한다고 본다
                    float feedHalfWidth = Mathf.Max(feedBounds.extents.x, feedBounds.extents.z);
                    float distance = new Vector2(position.x - center.x, position.z - center.z).magnitude;
                    // 피드는 제자리에서 위아래로 떠다니므로 그만큼 여유를 두고 잰다
                    float feedTop = position.y + feedBounds.max.y + ROUND_FEED_CLEARANCE;
                    float feedBottom = position.y + feedBounds.min.y - ROUND_FEED_CLEARANCE;

                    bool isOverlapping = (distance < (_settings.RoundPlatformRadius + feedHalfWidth)) &&
                        (feedTop > platformBounds.min.y) &&
                        (feedBottom < platformBounds.max.y);

                    if (isOverlapping)
                    {
                        spawnPosition.y -= feedTop - platformBounds.min.y;
                    }
                }

                Instantiate(prefab, spawnPosition, Quaternion.identity, parent);
            }
        }

        // 태그의 발판 목록에서 하나를 시드로 고른다. 목록이 비어 있으면 기본 발판을 쓴다.
        private GameObject PickPlatformPrefab(SpiralTagConfig tag, System.Random random)
        {
            if ((tag.platformPrefabs == null) || (tag.platformPrefabs.Count == 0)) return _settings.DefaultPlatformPrefab;

            GameObject prefab = tag.platformPrefabs[random.Next(tag.platformPrefabs.Count)];
            return (prefab != null) ? prefab : _settings.DefaultPlatformPrefab;
        }

        // 태그의 발판 목록의 평균 길이. 목록이 비어 있으면 fallback 의 길이다.
        private float GetAveragePlatformLength(SpiralTagConfig tag, GameObject fallback)
        {
            float sum = 0f;
            int validCount = 0;

            if (tag.platformPrefabs != null)
            {
                foreach (GameObject candidate in tag.platformPrefabs)
                {
                    if (candidate == null) continue;

                    sum += GetPrefabSize(candidate).z;
                    validCount++;
                }
            }

            return (validCount > 0) ? (sum / validCount) : GetPrefabSize(fallback).z;
        }

        // 구간의 발판마다 쓸 프리팹을 정한다. 태그의 발판 목록을 무작위로 섞은 순서로 돌려 쓰므로,
        // 크기가 고르게 섞이고 길이의 합이 평균에서 크게 벗어나지 않는다.
        private static GameObject[] PickSectionPlatformPrefabs(SpiralTagConfig tag, GameObject fallback, int count, bool isSinglePrefab, System.Random random)
        {
            var order = new List<GameObject>();
            if (!isSinglePrefab && (tag.platformPrefabs != null))
            {
                foreach (GameObject candidate in tag.platformPrefabs)
                {
                    if (candidate != null) order.Insert(random.Next(order.Count + 1), candidate);
                }
            }

            var picked = new GameObject[count];
            for (int i = 0; i < count; i++)
            {
                picked[i] = (order.Count > 0) ? order[i % order.Count] : fallback;
            }

            return picked;
        }

        // 구간에 쓸 패턴을 비중에 따라 시드로 고른다. 태그에 패턴 목록이 있으면 그것에서, 없으면 설정 에셋의 목록에서 고른다.
        // 고를 수 있는 패턴이 둘 이상이면 직전 구간과 같은 패턴은 되도록 피한다.
        private SOSpiralSectionPattern PickPattern(SpiralTagConfig tag, System.Random random, SOSpiralSectionPattern previousPattern)
        {
            bool hasTagPatterns = (tag.sectionPatterns != null) && (tag.sectionPatterns.Count > 0);
            IReadOnlyList<SOSpiralSectionPattern> pool = hasTagPatterns ? tag.sectionPatterns : _settings.SectionPatterns;

            float totalWeight = 0f;
            int candidateCount = 0;
            foreach (SOSpiralSectionPattern candidate in pool)
            {
                if ((candidate == null) || (candidate.Weight <= 0f)) continue;

                totalWeight += candidate.Weight;
                candidateCount++;
            }

            if (candidateCount == 0)
            {
                if (_fallbackPattern == null)
                {
                    _fallbackPattern = ScriptableObject.CreateInstance<SOSpiralSectionPattern>();
                    _fallbackPattern.name = "Default";
                    _fallbackPattern.hideFlags = HideFlags.HideAndDontSave;
                }

                return _fallbackPattern;
            }

            SOSpiralSectionPattern picked = null;
            for (int attempt = 0; attempt <= PATTERN_REROLL_COUNT; attempt++)
            {
                float roll = (float)random.NextDouble() * totalWeight;
                foreach (SOSpiralSectionPattern candidate in pool)
                {
                    if ((candidate == null) || (candidate.Weight <= 0f)) continue;

                    picked = candidate;
                    roll -= candidate.Weight;
                    if (roll < 0f) break;
                }

                if ((candidateCount == 1) || (picked != previousPattern)) break;
            }

            return picked;
        }

        /// <summary>
        /// 구간 하나(90도)의 발판을 패턴에 따라 만든다. 구간은 직전 발판의 중심에서 시작해 마지막 발판의 중심에서 끝난다.
        /// </summary>
        /// <param name="arcLength">이 구간이 차지하는 호의 길이</param>
        /// <param name="lastPrefab">구간의 마지막 발판에 쓸 프리팹. null 이 아니면 그 발판은 패턴을 적용하지 않고 고정한다.</param>
        /// <returns>이 구간의 마지막 발판</returns>
        private GameObject GenerateSection(
            SpiralTagConfig tag,
            SOSpiralSectionPattern pattern,
            float heightIncrease,
            float arcLength,
            Transform parent,
            GameObject prefab,
            GameObject lastPrefab,
            List<Vector3> placementPoints,
            List<GameObject> itemSpots,
            System.Random random)
        {
            bool isWheel = pattern.Motion == EPlatformMotion.Wheel;

            // 발판마다 크기가 다른 프리팹을 섞어 쓰므로, 발판 개수는 평균 길이로 정한다. 관람차는 한 가지 프리팹만 쓴다.
            float prefabLength = isWheel ? GetPrefabSize(prefab).z : GetAveragePlatformLength(tag, prefab);

            // 관람차는 바퀴 하나를 긴 발판 하나처럼 놓는다 (지름 + 칸 하나의 길이). 크기 배율은 칸에만 적용한다.
            float baseLength = isWheel ? ((pattern.Distance * 2f) + (prefabLength * pattern.StartScale.y)) : prefabLength;
            Vector2 startScale = isWheel ? Vector2.one : pattern.StartScale;
            Vector2 endScale = isWheel ? Vector2.one : pattern.EndScale;

            // 2단 점프가 필요한 패턴은 틈과 높이의 한계를 그만큼 넓게 본다
            float jumpFactor = pattern.RequiresDoubleJump ? DOUBLE_JUMP_FACTOR : 1f;
            float targetGap = _settings.PlatformGap * pattern.GapScale;
            float sectionHeight = heightIncrease * pattern.HeightScale;

            // 발판 개수: 평균 길이를 기준으로 틈이 목표를 넘지 않게, 그리고 한 번에 뛰어오를 수 있는 높이를 넘지 않게 정한다
            float averageLength = baseLength * (startScale.y + endScale.y) * 0.5f;
            float lastLength = (lastPrefab != null) ? GetPrefabSize(lastPrefab).z : (baseLength * endScale.y);
            float freeLength = arcLength - _previousHalfLength - (lastLength * 0.5f);

            // 내려갔다 올라가는 구간은 가장 가파른 곳을 기준으로 한다
            float steepestRise = sectionHeight + (Mathf.PI * pattern.DipDepth);

            int countByDist = Mathf.CeilToInt((freeLength + averageLength) / Mathf.Max(averageLength + targetGap, 0.1f));
            int countByHeight = Mathf.CeilToInt(steepestRise / Mathf.Max(_settings.MaxJumpHeight * jumpFactor, 0.1f));
            int count = Mathf.Max(countByDist, countByHeight, 1);

            float maxGap = _settings.MaxJumpDistance * jumpFactor;
            float gapLimit = Mathf.Min(maxGap, targetGap * GAP_TOLERANCE);

            int patternCount;
            GameObject[] platformPrefabs;
            Vector2[] scales;
            float[] lengths;
            float gap;

            // 발판 크기가 섞여서 실제 길이의 합이 평균과 다를 수 있다. 틈이 목표보다 많이 벌어지면 발판을 하나 더 놓고 다시 잰다.
            for (int attempt = 0; ; attempt++)
            {
                // 발판마다의 크기 배율과 길이. 마지막 발판이 고정 발판이면 그 앞까지만 패턴의 크기 변화를 적용한다.
                patternCount = (lastPrefab != null) ? (count - 1) : count;
                platformPrefabs = PickSectionPlatformPrefabs(tag, prefab, count, isWheel, random);
                scales = new Vector2[count];
                lengths = new float[count];
                float lengthSum = 0f;

                for (int p = 0; p < count; p++)
                {
                    bool isFixedLast = (lastPrefab != null) && (p == count - 1);
                    if (isFixedLast)
                    {
                        scales[p] = Vector2.one;
                        lengths[p] = lastLength;
                    }
                    else
                    {
                        float ratio = (patternCount > 1) ? ((float)p / (patternCount - 1)) : 0f;
                        float ownLength = isWheel ? baseLength : GetPrefabSize(platformPrefabs[p]).z;
                        scales[p] = Vector2.Lerp(pattern.StartScale, pattern.EndScale, ratio);
                        lengths[p] = ownLength * Mathf.Lerp(startScale.y, endScale.y, ratio);
                    }

                    // 마지막 발판은 중심까지만 이 구간에 들어간다
                    if (p < count - 1) lengthSum += lengths[p];
                }

                // 틈은 발판 개수와 같다 (직전 발판 뒤에 하나, 그 뒤 발판마다 하나)
                gap = (arcLength - _previousHalfLength - (lengths[count - 1] * 0.5f) - lengthSum) / count;

                if ((gap <= gapLimit) || (attempt >= GAP_RETRY_COUNT)) break;

                count++;
            }

            if (gap < 0f)
            {
                Debug.LogWarning($"[{name}] {parent.name}: 발판이 구간에 다 들어가지 않아 서로 겹칩니다. 발판 크기를 줄이거나 Spiral Radius 를 키우세요.", this);
            }
            else if (gap > maxGap)
            {
                Debug.LogWarning($"[{name}] {parent.name}: 발판 사이 틈({gap:F1})이 이 패턴에서 뛸 수 있는 거리({maxGap:F1})보다 큽니다.", this);
            }

            GameObject platform = null;
            var patternPlatforms = new List<GameObject>();
            var movingPlatforms = new List<GameObject>();
            var movingAngles = new List<float>();
            var movingHeights = new List<float>();
            float previousProfile = 0f;

            for (int p = 0; p < count; p++)
            {
                // 이번 발판까지 올라가야 하는 높이에서 직전 발판까지의 높이를 빼면 이번 걸음의 높이다
                float profile = GetHeightProfile(pattern, sectionHeight, (p + 1f) / count);
                float heightStep = profile - previousProfile;
                previousProfile = profile;

                bool isFixedLast = (lastPrefab != null) && (p == count - 1);
                if (isFixedLast)
                {
                    platform = PlaceNextPlatform(lastPrefab, parent, gap, heightStep, lengths[p], 0f, "Platform_TagEnd");
                    ApplyRole(platform, EPlatformRole.Rest);
                    continue;
                }

                if (isWheel)
                {
                    AdvanceCursor(gap, heightStep, lengths[p]);
                    platform = CreateWheel(pattern, prefab, parent, p, scales[p]);
                    continue;
                }

                // 안팎 지그재그: 짝수 번째는 바깥쪽, 홀수 번째는 안쪽
                float radialOffset = ((p % 2) == 0) ? pattern.RadialZigzag : -pattern.RadialZigzag;

                // 반시계 방향으로 나아간다 (Y축 기준 양의 각도)
                platform = PlaceNextPlatform(platformPrefabs[p], parent, gap, heightStep, lengths[p], radialOffset, $"Platform_{p:00}");

                // 움직임을 붙이기 전에 크기를 먼저 정해야, 밟기 감지 트리거가 맞는 크기로 만들어진다
                Vector3 scale = platform.transform.localScale;
                platform.transform.localScale = new Vector3(scale.x * scales[p].x, scale.y, scale.z * scales[p].y);
                patternPlatforms.Add(platform);

                // Fixed Interval 이 있으면 그 간격마다 하나는 움직이지 않는 발판으로 둔다 (첫 발판부터)
                bool isFixed = (pattern.Motion == EPlatformMotion.None) ||
                    ((pattern.FixedInterval > 0) && ((p % pattern.FixedInterval) == 0));

                if (isFixed)
                {
                    // 움직이지 않는 발판 위에만 맵 공통 배치물(Placements)을 놓을 수 있다
                    placementPoints.Add(GetSurfacePoint(platform));
                    ApplyRole(platform, EPlatformRole.Basic);
                    continue;
                }

                ApplyRole(platform, GetRole(pattern.Motion));
                ApplyMotion(platform, pattern, movingPlatforms.Count);
                movingPlatforms.Add(platform);
                movingAngles.Add(_currentAngle);
                movingHeights.Add(_currentHeight);
            }

            LinkPlatforms(pattern, parent, movingPlatforms, movingAngles, movingHeights);
            PlaceObjectsOnPlatforms(pattern.Placements, patternPlatforms, random);
            PlaceTagObstacles(tag, patternPlatforms, random);

            // 아무것도 놓이지 않은 고정 발판에는 아이템을 놓을 수 있다
            if (pattern.Motion == EPlatformMotion.None)
            {
                itemSpots.AddRange(patternPlatforms);
            }

            return platform;
        }

        /// <summary>
        /// 기믹 묶음 구간 하나(90도)를 만든다. 묶음 → 고정 발판 → 묶음 → … 순서로 놓고 고정 발판으로 끝낸다.
        /// 묶음은 태그의 목록에서 무작위로 고르며, 구간에 들어가는 만큼만 놓는다.
        /// </summary>
        /// <param name="arcLength">이 구간이 차지하는 호의 길이</param>
        /// <param name="lastPrefab">구간의 마지막 발판에 쓸 프리팹. null 이면 태그의 발판을 쓴다.</param>
        /// <param name="previousChunk">직전에 놓은 묶음. 같은 묶음이 이어서 나오는 것을 피하는 데 쓴다.</param>
        /// <returns>이 구간의 마지막 발판</returns>
        private GameObject GenerateChunkSection(
            SpiralTagConfig tag,
            float heightIncrease,
            float arcLength,
            Transform parent,
            GameObject prefab,
            GameObject lastPrefab,
            List<Vector3> placementPoints,
            List<GameObject> itemSpots,
            System.Random random,
            ref SpiralChunk previousChunk)
        {
            float platformLength = GetPrefabSize(prefab).z;
            float lastLength = (lastPrefab != null) ? GetPrefabSize(lastPrefab).z : platformLength;
            float baseGap = _settings.PlatformGap;

            // 마지막 발판 앞의 틈까지 뺀, 묶음과 중간 발판에 쓸 수 있는 길이
            float remaining = arcLength - _previousHalfLength - (lastLength * 0.5f) - baseGap;

            // 놓을 조각 목록. null 은 고정 발판이다.
            var pieces = new List<SpiralChunk>();
            while (true)
            {
                // 묶음과 묶음 사이에는 고정 발판을 하나 둔다
                bool needsSeparator = pieces.Count > 0;
                float separatorLength = needsSeparator ? (platformLength + baseGap) : 0f;

                // 남은 길이에 들어가는 묶음 가운데서 고른다. 들어가는 것이 없으면 그만 놓는다.
                SpiralChunk chunk = PickChunk(tag, random, previousChunk, remaining - baseGap - separatorLength);
                if (chunk == null) break;

                if (needsSeparator) pieces.Add(null);
                pieces.Add(chunk);
                previousChunk = chunk;
                remaining -= chunk.Length + baseGap + separatorLength;
            }

            // 남는 길이는 고정 발판으로 채운다
            while (remaining >= (platformLength + baseGap))
            {
                pieces.Add(null);
                remaining -= platformLength + baseGap;
            }

            // 그래도 남는 길이는 틈에 고르게 나눈다. 틈은 조각마다 하나, 마지막 발판 앞에 하나다.
            int stepCount = pieces.Count + 1;
            float gap = baseGap + (Mathf.Max(remaining, 0f) / stepCount);
            // 묶음에 올라설 때는 조준이나 타이밍이 함께 필요하므로, 한 걸음의 높이를 뛸 수 있는 높이보다 낮게 잡는다
            float internalRise = 0f;
            foreach (SpiralChunk piece in pieces)
            {
                if (piece != null) internalRise += piece.HeightRise;
            }
            float heightStep = Mathf.Min(
                Mathf.Max(0f, heightIncrease - internalRise) / stepCount,
                _settings.MaxJumpHeight * CHUNK_STEP_HEIGHT_RATIO);

            if (remaining < 0f)
            {
                Debug.LogWarning($"[{name}] {parent.name}: 구간이 짧아 묶음을 하나도 놓지 못했습니다.", this);
            }

            // 묶음 안의 발판을 이 태그의 발판과 같은 모습으로 맞출 머티리얼. 역할별 겉모습이 없는 발판이면 기본 머티리얼만 맞춘다.
            Renderer tagRenderer = prefab.GetComponentInChildren<Renderer>();
            Material basicMaterial = (tagRenderer != null) ? tagRenderer.sharedMaterial : null;
            Material movingMaterial = null;
            Material unstableMaterial = null;

            if (prefab.TryGetComponent(out PlatformSkin tagSkin))
            {
                basicMaterial = tagSkin.GetMaterial(EPlatformRole.Basic);
                movingMaterial = tagSkin.GetMaterial(EPlatformRole.Moving);
                unstableMaterial = tagSkin.GetMaterial(EPlatformRole.Unstable);
            }

            for (int i = 0; i < pieces.Count; i++)
            {
                SpiralChunk chunk = pieces[i];
                if (chunk == null)
                {
                    GameObject separator = PlaceNextPlatform(prefab, parent, gap, heightStep, platformLength, 0f, $"Platform_{i:00}");
                    ApplyRole(separator, EPlatformRole.Rest);
                    placementPoints.Add(GetSurfacePoint(separator));
                    itemSpots.Add(separator);
                    continue;
                }

                AdvanceCursor(gap, heightStep, chunk.Length);

                // 묶음의 올라서는 끝과 내리는 끝의 가운데가 길 위에 오도록 놓는다
                GameObject instance = CreatePlatform(chunk.gameObject, parent, _currentAngle, _currentHeight, 0f, $"Chunk_{i:00}_{chunk.name}");
                instance.transform.position -= instance.transform.forward * chunk.CenterZ;
                instance.GetComponent<SpiralChunk>().ApplyPlatformMaterials(basicMaterial, movingMaterial, unstableMaterial);
                _currentHeight += chunk.HeightRise;
            }

            GameObject lastPlatform = PlaceNextPlatform(
                (lastPrefab != null) ? lastPrefab : prefab,
                parent,
                gap,
                heightStep,
                lastLength,
                0f,
                (lastPrefab != null) ? "Platform_TagEnd" : "Platform_End");

            ApplyRole(lastPlatform, EPlatformRole.Rest);

            if (lastPrefab == null)
            {
                placementPoints.Add(GetSurfacePoint(lastPlatform));
                itemSpots.Add(lastPlatform);
            }

            return lastPlatform;
        }

        // 태그의 묶음 목록에서 길이가 maxLength 이하인 것 하나를 시드로 고른다. 없으면 null.
        // 고를 수 있는 것이 둘 이상이면 직전과 같은 묶음은 피한다.
        private static SpiralChunk PickChunk(SpiralTagConfig tag, System.Random random, SpiralChunk previousChunk, float maxLength)
        {
            var candidates = new List<SpiralChunk>();
            foreach (SpiralChunk chunk in tag.chunkPrefabs)
            {
                if ((chunk != null) && (chunk.Length <= maxLength)) candidates.Add(chunk);
            }

            if (candidates.Count > 1) candidates.Remove(previousChunk);
            if (candidates.Count == 0) return null;

            return candidates[random.Next(candidates.Count)];
        }

        // 태그의 장애물 목록에서 무작위로 뽑아 이 구간의 발판 위에 놓는다.
        private static void PlaceTagObstacles(SpiralTagConfig tag, List<GameObject> platforms, System.Random random)
        {
            if ((tag.obstacles == null) || (tag.obstacles.Count == 0)) return;

            var picked = new SMapPlacement[1];
            for (int i = 0; i < tag.obstaclesPerSection; i++)
            {
                picked[0] = tag.obstacles[random.Next(tag.obstacles.Count)];
                PlaceObjectsOnPlatforms(picked, platforms, random);
            }
        }

        // 태그의 아이템(없으면 설정 에셋의 태그 아이템)을 이 태그의 빈 고정 발판 위에 무작위로 놓는다. 발판 하나에 하나만 놓는다.
        private void PlaceTagItems(SpiralTagConfig tag, List<GameObject> itemSpots, Transform tagRoot, System.Random random)
        {
            bool hasOwnItems = (tag.tagItems != null) && (tag.tagItems.Count > 0);
            IReadOnlyList<SMapPlacement> items = hasOwnItems ? tag.tagItems : _settings.TagItems;

            foreach (SMapPlacement item in items)
            {
                if (item.Prefab == null) continue;

                for (int i = 0; i < item.Count; i++)
                {
                    if (itemSpots.Count == 0) return;

                    int index = random.Next(itemSpots.Count);
                    Transform platform = itemSpots[index].transform;
                    itemSpots.RemoveAt(index);

                    Vector3 position = GetSurfacePoint(platform.gameObject) + (platform.rotation * item.Offset);
                    Instantiate(item.Prefab, position, platform.rotation, tagRoot);
                }
            }
        }

        // 구간의 진행도(0 ~ 1)에서 구간 시작보다 얼마나 높은지. Dip Depth 가 있으면 가운데가 그만큼 꺼진다.
        private static float GetHeightProfile(SOSpiralSectionPattern pattern, float sectionHeight, float progress)
        {
            return (sectionHeight * progress) - (pattern.DipDepth * Mathf.Sin(Mathf.PI * progress));
        }

        // 같은 태그의 구간과 구간 사이에 고정 발판을 평평하게 놓는다. 돌려주는 값은 연결 발판들이 차지한 호의 길이다.
        private float PlaceConnectors(Transform tagRoot, int sectionIndex, GameObject prefab, List<Vector3> placementPoints, List<GameObject> itemSpots)
        {
            Transform connectorRoot = new GameObject($"Connector_{sectionIndex:00}").transform;
            connectorRoot.SetParent(tagRoot, false);

            float length = GetPrefabSize(prefab).z;
            float gap = _settings.PlatformGap;

            for (int i = 0; i < _settings.ConnectorCount; i++)
            {
                GameObject connector = PlaceNextPlatform(prefab, connectorRoot, gap, 0f, length, 0f, $"Platform_Connector_{i:00}");
                ApplyRole(connector, EPlatformRole.Rest);
                placementPoints.Add(GetSurfacePoint(connector));
                itemSpots.Add(connector);
            }

            return _settings.ConnectorCount * (length + gap);
        }

        // 관람차 바퀴 하나를 만든다. 지금 커서 위치가 바퀴의 축이고, 발판(칸)들이 진행 방향과 위쪽이 이루는 면에서 그 축을 돈다.
        // 칸은 들어오는 쪽에서 아래로 내려가 반대쪽으로 올라오므로, 들어오는 쪽에서 타고 반대쪽에서 내린다.
        private GameObject CreateWheel(SOSpiralSectionPattern pattern, GameObject prefab, Transform parent, int wheelIndex, Vector2 carScale)
        {
            Transform wheelRoot = new GameObject($"Wheel_{wheelIndex:00}").transform;
            wheelRoot.SetParent(parent, false);

            int carCount = Mathf.Max(pattern.GroupSize, 1);
            GameObject car = null;

            for (int c = 0; c < carCount; c++)
            {
                car = CreatePlatform(prefab, wheelRoot, _currentAngle, _currentHeight, 0f, $"Car_{c:00}");

                Vector3 scale = car.transform.localScale;
                car.transform.localScale = new Vector3(scale.x * carScale.x, scale.y, scale.z * carScale.y);

                Vector3 hub = car.transform.position;
                Vector3 forward = car.transform.forward;
                float phase = Mathf.PI + ((Mathf.PI * 2f * c) / carCount);

                ApplyRole(car, EPlatformRole.Moving);

                var orbit = car.AddComponent<OrbitPlatform>();
                orbit.Initialize(forward, Vector3.up, pattern.Distance, pattern.Speed, phase);
                orbit.SetCenter(hub);
                MakeRideable(car);

                // 에디터 미리보기에서도 바퀴 모양으로 보이도록 처음 자리로 옮겨 둔다
                car.transform.position = hub +
                    (forward * (Mathf.Cos(phase) * pattern.Distance)) +
                    (Vector3.up * (Mathf.Sin(phase) * pattern.Distance));
            }

            return car;
        }

        // 발판 여러 개가 함께 맞물려야 하는 패턴을 마무리한다. (저울의 짝, 흘러가는 길, 사격 표적)
        private void LinkPlatforms(
            SOSpiralSectionPattern pattern,
            Transform parent,
            List<GameObject> platforms,
            List<float> angles,
            List<float> heights)
        {
            if (platforms.Count == 0) return;

            switch (pattern.Motion)
            {
                case EPlatformMotion.Scale:
                    // 앞에서부터 둘씩 짝짓는다. 하나가 남으면 그 발판은 혼자 가라앉기만 한다.
                    for (int i = 0; i + 1 < platforms.Count; i += 2)
                    {
                        var first = platforms[i].GetComponent<SinkPlatform>();
                        var second = platforms[i + 1].GetComponent<SinkPlatform>();
                        first.SetCounterpart(second);
                        second.SetCounterpart(first);
                    }
                    break;

                case EPlatformMotion.Stream:
                    // 첫 발판 자리에서 마지막 발판을 조금 지난 자리까지가 흘러가는 길이다. 그 뒤의 고정 발판과 겹치지 않게 조금만 지난다.
                    int last = platforms.Count - 1;
                    float angleSpacing = (last > 0) ? (angles[last] - angles[last - 1]) : 0f;
                    float heightSpacing = (last > 0) ? (heights[last] - heights[last - 1]) : 0f;
                    var angleRange = new Vector2(angles[0], angles[last] + (angleSpacing * STREAM_END_OVERRUN));
                    var heightRange = new Vector2(
                        transform.position.y + heights[0],
                        transform.position.y + heights[last] + (heightSpacing * STREAM_END_OVERRUN));

                    for (int i = 0; i < platforms.Count; i++)
                    {
                        var stream = platforms[i].AddComponent<ArcStreamPlatform>();
                        stream.Initialize(
                            GetCenter(),
                            _settings.SpiralRadius,
                            angleRange,
                            heightRange,
                            pattern.Speed,
                            (float)i / platforms.Count);
                        MakeRideable(platforms[i]);
                    }
                    break;

                case EPlatformMotion.ShootReveal:
                    foreach (GameObject platform in platforms)
                    {
                        var reveal = platform.AddComponent<TimedRevealPlatform>();
                        reveal.Initialize(pattern.ActiveDuration, pattern.WarningDuration);

                        if (pattern.AuxPrefab == null) continue;

                        // 표적은 발판이 사라져 있는 동안에도 보여야 하므로 발판의 자식이 아니라 구간 아래에 둔다
                        Vector3 targetPosition = GetSurfacePoint(platform) + (Vector3.up * SHOOT_TARGET_HEIGHT);
                        GameObject target = Instantiate(pattern.AuxPrefab, targetPosition, platform.transform.rotation, parent);
                        target.name = $"Target_{platform.name}";

                        var button = target.GetComponentInChildren<DamageButton>();
                        if (button != null)
                        {
                            button.OnPressed.AddListener(reveal.Reveal);
                        }
                    }
                    break;
            }
        }

        // 직전 발판의 끝에서 gap 만큼 띄운 자리에 다음 발판을 놓는다. 발판 길이만큼 각도를 더 나아간다.
        private GameObject PlaceNextPlatform(
            GameObject prefab,
            Transform parent,
            float gap,
            float heightStep,
            float length,
            float radialOffset,
            string platformName)
        {
            AdvanceCursor(gap, heightStep, length);

            return CreatePlatform(prefab, parent, _currentAngle, _currentHeight, radialOffset, platformName);
        }

        // 놓을 자리(각도·높이)를 다음 발판의 중심으로 옮긴다. 직전 발판의 끝에서 gap 만큼 띄우고, 놓을 발판 길이의 절반만큼 더 나아간다.
        private void AdvanceCursor(float gap, float heightStep, float length)
        {
            float halfLength = length * 0.5f;
            float distance = _previousHalfLength + gap + halfLength;

            _currentAngle += (distance / Mathf.Max(_settings.SpiralRadius, 0.1f)) * Mathf.Rad2Deg;
            _currentHeight += heightStep;
            _previousHalfLength = halfLength;
        }

        /// <summary>
        /// 나선 위의 한 지점에 발판을 만든다. 중심은 centerPillar 가 있으면 그 위치, 없으면 이 오브젝트의 위치다.
        /// </summary>
        /// <param name="radialOffset">나선의 반지름에서 바깥쪽(+) 또는 안쪽(-)으로 옮길 거리</param>
        private GameObject CreatePlatform(GameObject prefab, Transform parent, float angle, float height, float radialOffset, string platformName)
        {
            Vector3 center = GetCenter();
            float rad = angle * Mathf.Deg2Rad;
            float radius = _settings.SpiralRadius + radialOffset;

            // 발판마다 두께가 달라도 윗면의 높이가 같도록, 기본 발판보다 두꺼운 만큼 내려서 놓는다
            float thicknessOffset = (GetThickness(_settings.DefaultPlatformPrefab) - GetThickness(prefab)) * 0.5f;

            // 반시계 방향: x = cos, z = sin. 높이는 생성기 위치 기준
            Vector3 pos = new Vector3(
                center.x + (Mathf.Cos(rad) * radius),
                transform.position.y + height + thicknessOffset,
                center.z + (Mathf.Sin(rad) * radius));

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

        // 나선의 중심 (수평 위치만 쓴다)
        private Vector3 GetCenter()
        {
            return (centerPillar != null) ? centerPillar.position : transform.position;
        }

        // index 번째 체크포인트를 옮긴다. 연결된 체크포인트가 모자라면 아무것도 하지 않는다.
        private void MoveCheckpointAnchor(int index, Vector3 position, Quaternion rotation)
        {
            if ((_checkpointAnchors == null) || (index >= _checkpointAnchors.Length)) return;
            if (_checkpointAnchors[index] == null) return;

            _checkpointAnchors[index].SetPositionAndRotation(position, rotation);
        }

        // 프리팹을 놓았을 때의 크기 (x: 반지름 방향 폭, y: 두께, z: 진행 방향 길이). 메시가 없으면 0 이다.
        private Vector3 GetPrefabSize(GameObject prefab)
        {
            return GetPrefabBounds(prefab).size;
        }

        // 프리팹을 놓았을 때 루트(피벗) 기준으로 메시가 차지하는 범위. 루트의 회전은 없다고 본다. 메시가 없으면 크기 0 이다.
        private Bounds GetPrefabBounds(GameObject prefab)
        {
            if (_prefabBounds.TryGetValue(prefab, out Bounds cached)) return cached;

            Matrix4x4 worldToRoot = prefab.transform.worldToLocalMatrix;
            Bounds bounds = default;
            bool hasBounds = false;

            foreach (MeshFilter meshFilter in prefab.GetComponentsInChildren<MeshFilter>())
            {
                if (meshFilter.sharedMesh == null) continue;

                // 메시의 여덟 꼭짓점을 프리팹 루트 기준으로 옮겨 감싼다
                Matrix4x4 meshToRoot = worldToRoot * meshFilter.transform.localToWorldMatrix;
                Bounds meshBounds = meshFilter.sharedMesh.bounds;

                for (int i = 0; i < 8; i++)
                {
                    Vector3 corner = meshBounds.center + Vector3.Scale(meshBounds.extents, new Vector3(
                        ((i & 1) == 0) ? -1f : 1f,
                        ((i & 2) == 0) ? -1f : 1f,
                        ((i & 4) == 0) ? -1f : 1f));
                    Vector3 point = meshToRoot.MultiplyPoint3x4(corner);

                    if (hasBounds)
                    {
                        bounds.Encapsulate(point);
                    }
                    else
                    {
                        bounds = new Bounds(point, Vector3.zero);
                        hasBounds = true;
                    }
                }
            }

            // 루트 기준 범위에 루트의 스케일을 곱해야 놓았을 때의 범위가 된다
            Vector3 scale = prefab.transform.localScale;
            Bounds placed = hasBounds
                ? new Bounds(Vector3.Scale(bounds.center, scale), Vector3.Scale(bounds.size, scale))
                : new Bounds(Vector3.zero, Vector3.zero);

            _prefabBounds[prefab] = placed;
            return placed;
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

        // 패턴의 움직임에 맞는 컴포넌트를 발판에 붙인다. index 는 구간 안에서 이 발판의 순서로, 박자를 어긋나게 하는 데 쓴다.
        private static void ApplyMotion(GameObject platform, SOSpiralSectionPattern pattern, int index)
        {
            Transform platformTransform = platform.transform;

            // 패턴의 축은 발판 기준이다 (x: 반지름 바깥쪽, y: 위, z: 진행 방향). 발판을 90도 돌려 두었으므로 right 축이 반지름 바깥쪽이다.
            Vector3 worldAxis =
                (platformTransform.right * pattern.Axis.x) +
                (Vector3.up * pattern.Axis.y) +
                (platformTransform.forward * pattern.Axis.z);

            switch (pattern.Motion)
            {
                case EPlatformMotion.Oscillate:
                    var oscillating = platform.AddComponent<OscillatingPlatform>();
                    oscillating.moveAxis = worldAxis;
                    oscillating.distance = pattern.Distance;
                    oscillating.speed = pattern.Speed;
                    oscillating.timeOffset = GetAnglePhase(pattern, platformTransform, index);
                    MakeRideable(platform);
                    break;

                case EPlatformMotion.Orbit:
                    var orbit = platform.AddComponent<OrbitPlatform>();
                    orbit.Initialize(worldAxis, Vector3.up, pattern.Distance, pattern.Speed, GetAnglePhase(pattern, platformTransform, index));
                    MakeRideable(platform);
                    break;

                case EPlatformMotion.Blink:
                    var disappearing = platform.AddComponent<DisappearingPlatform>();
                    disappearing.activeDuration = pattern.ActiveDuration;
                    disappearing.inactiveDuration = pattern.InactiveDuration;
                    disappearing.warningDuration = pattern.WarningDuration;
                    disappearing.timeOffset = GetTimePhase(pattern, platformTransform, index);
                    break;

                case EPlatformMotion.Falling:
                    var falling = platform.AddComponent<FallingPlatform>();
                    falling.Initialize(pattern.TriggerDelay, pattern.RespawnDelay);
                    MakeRideable(platform);
                    break;

                case EPlatformMotion.Crumble:
                    var crumble = platform.AddComponent<CrumblePlatform>();
                    crumble.Initialize(pattern.TriggerDelay, pattern.RespawnDelay);
                    break;

                case EPlatformMotion.StepMove:
                    var stepMove = platform.AddComponent<StepMovePlatform>();
                    stepMove.Initialize(worldAxis.normalized * pattern.Distance, pattern.Speed, pattern.TriggerDelay, pattern.RespawnDelay);
                    MakeRideable(platform);
                    break;

                // 저울(Scale)의 짝은 구간의 발판이 모두 만들어진 뒤 LinkPlatforms 에서 맺는다
                case EPlatformMotion.Sink:
                case EPlatformMotion.Scale:
                    var sink = platform.AddComponent<SinkPlatform>();
                    sink.Initialize(pattern.Distance, pattern.Speed);
                    MakeRideable(platform);
                    break;

                case EPlatformMotion.JumpSwap:
                    var jumpSwap = platform.AddComponent<JumpSwapPlatform>();
                    jumpSwap.Initialize((index % 2) == 0);
                    break;

                case EPlatformMotion.Stretch:
                    var stretch = platform.AddComponent<StretchPlatform>();
                    stretch.Initialize(pattern.StretchMinRatio, pattern.Speed, GetAnglePhase(pattern, platformTransform, index));
                    break;

                // Stream, ShootReveal 은 구간 전체의 정보가 필요해 LinkPlatforms 에서 붙인다. Wheel 은 CreateWheel 에서 만든다.
            }
        }

        // 왕복·원운동 발판의 시작 각도(라디안)
        private static float GetAnglePhase(SOSpiralSectionPattern pattern, Transform platform, int index)
        {
            switch (pattern.PhaseMode)
            {
                case EPhaseMode.ByHeight:   return platform.position.y * 0.5f;      // 높이에 따라 어긋나 물결처럼 움직인다
                case EPhaseMode.Alternate:  return ((index % 2) == 0) ? 0f : Mathf.PI;
                case EPhaseMode.Sequential: return -index * pattern.PhaseStep;
                default:                    return 0f;
            }
        }

        // 점멸 발판의 시작 시간 어긋남(초)
        private static float GetTimePhase(SOSpiralSectionPattern pattern, Transform platform, int index)
        {
            switch (pattern.PhaseMode)
            {
                case EPhaseMode.ByHeight:   return platform.position.y % 2f;
                case EPhaseMode.Alternate:  return ((index % 2) == 0) ? 0f : pattern.ActiveDuration;
                case EPhaseMode.Sequential: return -index * pattern.PhaseStep;
                default:                    return 0f;
            }
        }

        // 배치물을 이 구간의 발판 위에 무작위로 놓는다. 발판 하나에 하나만 놓고, 발판의 자식으로 두어 함께 움직이고 사라지게 한다.
        // 놓은 발판은 platforms 에서 빠진다.
        private static void PlaceObjectsOnPlatforms(IReadOnlyList<SMapPlacement> placements, List<GameObject> platforms, System.Random random)
        {
            if (placements == null) return;

            foreach (SMapPlacement placement in placements)
            {
                if (placement.Prefab == null) continue;

                for (int i = 0; i < placement.Count; i++)
                {
                    if (platforms.Count == 0) return;

                    int index = random.Next(platforms.Count);
                    Transform platform = platforms[index].transform;
                    platforms.RemoveAt(index);

                    Vector3 position = GetSurfacePoint(platform.gameObject) + (platform.rotation * placement.Offset);
                    GameObject instance = Instantiate(placement.Prefab, position, platform.rotation, platform);

                    // 발판은 크기를 스케일로 정하므로, 자식이 그 스케일을 물려받아 찌그러지지 않게 되돌린다
                    // PlatformFit 이 붙은 프리팹은 발판 크기에 맞춘다
                    Vector3 platformScale = platform.lossyScale;
                    Vector3 fitFactor = placement.Prefab.TryGetComponent(out PlatformFit fit)
                        ? fit.GetScaleFactor(GetFootprint(platform.gameObject))
                        : Vector3.one;

                    instance.transform.localScale = new Vector3(
                        (placement.Prefab.transform.localScale.x * fitFactor.x) / platformScale.x,
                        (placement.Prefab.transform.localScale.y * fitFactor.y) / platformScale.y,
                        (placement.Prefab.transform.localScale.z * fitFactor.z) / platformScale.z);
                }
            }
        }

        // 발판 프리팹의 두께. 루트에 메시가 없는 프리팹(기믹 묶음 등)은 기본 발판과 같은 두께로 본다.
        private float GetThickness(GameObject prefab)
        {
            if (prefab.TryGetComponent(out MeshFilter meshFilter) && (meshFilter.sharedMesh != null))
            {
                return meshFilter.sharedMesh.bounds.size.y * prefab.transform.localScale.y;
            }

            GameObject reference = _settings.DefaultPlatformPrefab;
            if ((prefab != reference) && reference.TryGetComponent(out MeshFilter referenceFilter) && (referenceFilter.sharedMesh != null))
            {
                return referenceFilter.sharedMesh.bounds.size.y * reference.transform.localScale.y;
            }

            return 0f;
        }

        // 발판에 역할에 맞는 겉모습을 입힌다. PlatformSkin 이 없는 프리팹은 그대로 둔다.
        private static void ApplyRole(GameObject platform, EPlatformRole role)
        {
            if (platform.TryGetComponent(out PlatformSkin skin))
            {
                skin.SetRole(role);
            }
        }

        // 패턴의 움직임이 어떤 역할의 발판인지
        private static EPlatformRole GetRole(EPlatformMotion motion)
        {
            switch (motion)
            {
                case EPlatformMotion.None:
                    return EPlatformRole.Basic;

                case EPlatformMotion.Blink:
                case EPlatformMotion.Falling:
                case EPlatformMotion.Crumble:
                case EPlatformMotion.ShootReveal:
                case EPlatformMotion.JumpSwap:
                    return EPlatformRole.Unstable;

                default:
                    return EPlatformRole.Moving;
            }
        }

        // 놓인 발판 윗면의 크기 (x: 폭, y: 진행 방향 길이)
        private static Vector2 GetFootprint(GameObject platform)
        {
            Vector3 scale = platform.transform.lossyScale;
            Vector3 meshSize = Vector3.one;

            if (platform.TryGetComponent(out MeshFilter meshFilter) && (meshFilter.sharedMesh != null))
            {
                meshSize = meshFilter.sharedMesh.bounds.size;
            }

            return new Vector2(meshSize.x * scale.x, meshSize.z * scale.z);
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
