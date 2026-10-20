using System.Collections.Generic;
using UnityEngine;
using Map.Common;

namespace Map.Spiral
{
    /// <summary>
    /// SpiralMapGenerator 의 설정 에셋. 나선의 크기, 태그 구성, 구간 패턴, 프리팹을 담는다.
    /// </summary>
    [CreateAssetMenu(fileName = "SOSpiralMapSettings", menuName = "Map/Spiral Map Settings")]
    public class SOSpiralMapSettings : SOMapSettings
    {
        #region Serialized Fields
        [Header("Global Settings")]
        [Tooltip("중심에서 발판까지의 거리")]
        [SerializeField] private float _spiralRadius = 5f;

        [Tooltip("기본 발판 프리팹. 시작·도착 발판과, 전용 프리팹이 없는 구간에 쓴다.")]
        [SerializeField] private GameObject _defaultPlatformPrefab;

        [Header("Player Jump Constraints (For Reference/Validation)")]
        [Tooltip("플레이어가 뛸 수 있는 최대 거리. Platform Gap 이 이 값을 넘으면 경고한다.")]
        [SerializeField] private float _maxJumpDistance = 4f;

        [Tooltip("플레이어가 뛸 수 있는 최대 높이. 발판 개수 자동 계산에 쓰인다.")]
        [SerializeField] private float _maxJumpHeight = 2f;

        [Tooltip("발판과 발판 사이의 틈(끝에서 끝). 발판 프리팹의 길이를 읽어, 틈이 이 값을 넘지 않도록 개수를 정한다.")]
        [SerializeField, Min(0f)] private float _platformGap = 0.75f;

        [Header("Sections (Each covers 90 degrees CCW)")]
        [Tooltip("구간 하나가 올라가는 높이의 범위 (최소, 최대). 구간마다 이 사이에서 무작위로 정한다.")]
        [SerializeField] private Vector2 _randomHeightRange = new Vector2(3.0f, 5.0f);

        [Tooltip("구간에 쓸 패턴 목록. 구간마다 하나를 비중에 따라 무작위로 고른다. 태그에 패턴 목록이 따로 있으면 그 태그는 그것을 쓴다. 비워 두면 모든 구간이 고정 발판이다.")]
        [SerializeField] private List<SOSpiralSectionPattern> _sectionPatterns = new List<SOSpiralSectionPattern>();

        [Header("Connectors (구간과 구간 사이의 고정 발판)")]
        [Tooltip("같은 태그 안에서 구간과 구간 사이에 놓는 고정 발판 수. 평평하게 이어지며 패턴이 적용되지 않는다. 0이면 놓지 않는다.")]
        [SerializeField, Min(0)] private int _connectorCount = 2;

        [Tooltip("켜면 연결 발판이 다음 구간의 90도 안에 들어가 그만큼 패턴 구간이 짧아진다(태그 = 정확히 한 바퀴). 끄면 90도 밖에 더해져 태그가 한 바퀴를 넘는다.")]
        [SerializeField] private bool _isConnectorInsideSection;

        [Header("Tags")]
        [Tooltip("아래에서 위로 차례대로 이어지는 태그 목록")]
        [SerializeField] private List<SpiralTagConfig> _tags = new List<SpiralTagConfig>();

        [Tooltip("태그 하나를 이루는 구간(90도) 수. 4면 한 바퀴다.")]
        [SerializeField, Min(1)] private int _sectionsPerTag = 4;

        [Tooltip("태그마다 빈 고정 발판 위에 무작위로 놓을 아이템. Count 는 태그 하나에 놓는 개수, Offset 은 발판 윗면 중앙에서 띄울 거리다. 태그에 Tag Items 가 따로 있으면 그 태그는 그것을 쓴다.")]
        [SerializeField] private SMapPlacement[] _tagItems;

        [Header("Tag End (태그 끝 발판의 들어오는 끝에서 잰 거리)")]
        [Tooltip("체크포인트를 놓는 거리")]
        [SerializeField, Min(0f)] private float _checkpointDistance = 1.5f;

        [Tooltip("악성 피드를 놓는 거리")]
        [SerializeField, Min(0f)] private float _feedDistance = 6.0f;

        [Tooltip("아이템을 놓는 거리")]
        [SerializeField, Min(0f)] private float _itemDistance = 9.0f;

        [Header("Ambient Feeds (태그 주위에 떠다니는 피드. 프리팹은 태그마다 지정한다)")]
        [Tooltip("태그 하나에서 나선의 바깥쪽에 띄우는 피드 수")]
        [SerializeField, Min(0)] private int _outerFeedCount = 2;

        [Tooltip("태그 하나에서 나선의 안쪽에 띄우는 피드 수")]
        [SerializeField, Min(0)] private int _innerFeedCount = 2;

        [Tooltip("바깥쪽 피드를 발판이 놓이는 원에서 떨어뜨리는 거리의 범위 (최소, 최대)")]
        [SerializeField] private Vector2 _outerFeedDistance = new Vector2(7.0f, 11.0f);

        [Tooltip("안쪽 피드를 발판이 놓이는 원에서 떨어뜨리는 거리의 범위 (최소, 최대)")]
        [SerializeField] private Vector2 _innerFeedDistance = new Vector2(7.0f, 11.0f);

        [Tooltip("피드를 그 자리의 발판 높이에서 띄우는 높이의 범위 (최소, 최대)")]
        [SerializeField] private Vector2 _ambientFeedHeightRange = new Vector2(1.0f, 5.0f);

        [Header("Round Platform (Tags 를 쓸 때만)")]
        [Tooltip("마지막 태그 뒤 도착 발판까지 올라가는 계단 발판 수. 라운드 발판을 마지막 태그 끝 발판보다 높여, 악성 피드를 건너뛰고 올라설 수 없게 한다.")]
        [SerializeField, Min(0)] private int _exitStepCount = 3;

        [Tooltip("계단 발판 하나가 올라가는 높이")]
        [SerializeField, Min(0f)] private float _exitStepHeight = 1.0f;

        [Tooltip("나선 중심 꼭대기에 놓을 발판 프리팹 (선택). 피벗이 윗면 중앙에 있어야 한다.")]
        [SerializeField] private GameObject _roundPlatformPrefab;

        [Tooltip("라운드 발판의 반지름. 체크포인트 위치 계산에 쓴다.")]
        [SerializeField, Min(0f)] private float _roundPlatformRadius = 27.0f;

        [Tooltip("라운드 발판의 가장자리에서 체크포인트까지 안쪽으로 들어가는 거리")]
        [SerializeField, Min(0f)] private float _roundCheckpointInset = 3.0f;
        #endregion

        #region Properties
        /// <summary>중심에서 발판까지의 거리.</summary>
        public float SpiralRadius => _spiralRadius;

        /// <summary>기본 발판 프리팹.</summary>
        public GameObject DefaultPlatformPrefab => _defaultPlatformPrefab;

        /// <summary>플레이어가 뛸 수 있는 최대 거리.</summary>
        public float MaxJumpDistance => _maxJumpDistance;

        /// <summary>플레이어가 뛸 수 있는 최대 높이.</summary>
        public float MaxJumpHeight => _maxJumpHeight;

        /// <summary>발판과 발판 사이의 목표 틈.</summary>
        public float PlatformGap => _platformGap;

        /// <summary>구간 하나가 올라가는 높이의 범위 (최소, 최대).</summary>
        public Vector2 RandomHeightRange => _randomHeightRange;

        /// <summary>구간에 쓸 패턴 목록.</summary>
        public IReadOnlyList<SOSpiralSectionPattern> SectionPatterns => _sectionPatterns;

        /// <summary>구간과 구간 사이에 놓는 고정 발판 수.</summary>
        public int ConnectorCount => _connectorCount;

        /// <summary>연결 발판이 다음 구간의 90도 안에 들어가는지 여부.</summary>
        public bool IsConnectorInsideSection => _isConnectorInsideSection;

        /// <summary>태그 목록.</summary>
        public IReadOnlyList<SpiralTagConfig> Tags => _tags;

        /// <summary>태그 하나를 이루는 구간 수.</summary>
        public int SectionsPerTag => _sectionsPerTag;

        /// <summary>태그마다 빈 고정 발판 위에 놓을 아이템.</summary>
        public IReadOnlyList<SMapPlacement> TagItems => _tagItems ?? System.Array.Empty<SMapPlacement>();

        /// <summary>태그 끝 발판의 입구에서 체크포인트까지의 거리.</summary>
        public float CheckpointDistance => _checkpointDistance;

        /// <summary>태그 끝 발판의 입구에서 악성 피드까지의 거리.</summary>
        public float FeedDistance => _feedDistance;

        /// <summary>태그 끝 발판의 입구에서 아이템까지의 거리.</summary>
        public float ItemDistance => _itemDistance;

        /// <summary>태그 하나에서 나선의 바깥쪽에 띄우는 주위 피드 수.</summary>
        public int OuterFeedCount => _outerFeedCount;

        /// <summary>태그 하나에서 나선의 안쪽에 띄우는 주위 피드 수.</summary>
        public int InnerFeedCount => _innerFeedCount;

        /// <summary>바깥쪽 주위 피드를 나선에서 떨어뜨리는 거리의 범위 (최소, 최대).</summary>
        public Vector2 OuterFeedDistance => _outerFeedDistance;

        /// <summary>안쪽 주위 피드를 나선에서 떨어뜨리는 거리의 범위 (최소, 최대).</summary>
        public Vector2 InnerFeedDistance => _innerFeedDistance;

        /// <summary>주위 피드를 발판 높이에서 띄우는 높이의 범위 (최소, 최대).</summary>
        public Vector2 AmbientFeedHeightRange => _ambientFeedHeightRange;

        /// <summary>마지막 태그 뒤의 계단 발판 수.</summary>
        public int ExitStepCount => _exitStepCount;

        /// <summary>계단 발판 하나가 올라가는 높이.</summary>
        public float ExitStepHeight => _exitStepHeight;

        /// <summary>나선 중심 꼭대기에 놓을 발판 프리팹.</summary>
        public GameObject RoundPlatformPrefab => _roundPlatformPrefab;

        /// <summary>라운드 발판의 반지름.</summary>
        public float RoundPlatformRadius => _roundPlatformRadius;

        /// <summary>라운드 발판의 가장자리에서 체크포인트까지의 거리.</summary>
        public float RoundCheckpointInset => _roundCheckpointInset;
        #endregion
    }
}
