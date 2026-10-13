using System.Collections.Generic;
using UnityEngine;
using Map.Common;

namespace Map.Spiral
{
    /// <summary>구간의 발판이 움직이는 방식.</summary>
    public enum EPlatformMotion
    {
        None,       // 고정
        Oscillate,  // 한 축으로 왕복
        Orbit,      // 제자리를 중심으로 원을 그림 (윗면은 수평 유지)
        Blink,      // 주기적으로 사라졌다 나타남
        Falling,    // 밟으면 잠시 뒤 떨어지고 다시 생김
        Crumble,    // 밟으면 잠시 뒤 사라지고 다시 생김
        StepMove,   // 밟으면 Axis 방향으로 Distance 만큼 이동했다가 돌아옴
        Sink,       // 서 있는 동안 가라앉고 내리면 올라옴
        Scale,      // 둘씩 짝지어 저울처럼 움직임 (한쪽에 서면 그쪽은 내려가고 반대쪽은 올라감)
        ShootReveal,// 사라져 있다가 표적(Aux Prefab)을 쏘면 잠시 나타남
        Wheel,      // 발판 여러 개가 한 축을 중심으로 도는 관람차
        Stream,     // 구간의 길을 따라 한 방향으로 흘러가다 끝에서 처음으로 돌아감
        JumpSwap,   // 플레이어가 점프할 때마다 두 묶음이 번갈아 나타남
        Stretch,    // 길이가 늘었다 줄었다 함
    }

    /// <summary>같은 구간의 발판들이 서로 박자를 어긋나게 하는 방식.</summary>
    public enum EPhaseMode
    {
        Same,       // 모두 같은 박자
        ByHeight,   // 발판 높이에 따라 조금씩 어긋남 (완만한 물결)
        Alternate,  // 이웃 발판끼리 정반대 박자
        Sequential, // 진행 방향으로 Phase Step 만큼씩 차례로 늦어짐
    }

    /// <summary>
    /// 나선 구간(90도) 하나를 만드는 방법. 발판을 놓는 규칙, 발판의 움직임, 이 구간에 놓을 배치물을 담는다.
    /// SpiralMapGenerator 가 구간마다 패턴 하나를 무작위로 골라 쓴다.
    /// </summary>
    [CreateAssetMenu(fileName = "SOSpiralSectionPattern", menuName = "Map/Spiral Section Pattern")]
    public class SOSpiralSectionPattern : ScriptableObject
    {
        #region Serialized Fields
        [Header("Random")]
        [Tooltip("무작위로 뽑힐 비중. 다른 패턴보다 값이 클수록 자주 나온다. 0이면 나오지 않는다.")]
        [SerializeField, Min(0f)] private float _weight = 1.0f;

        [Header("Layout")]
        [Tooltip("발판 사이 틈에 곱하는 값. 1이면 설정 에셋의 Platform Gap 그대로다.")]
        [SerializeField, Min(0f)] private float _gapScale = 1.0f;

        [Tooltip("발판을 반지름 안쪽·바깥쪽으로 번갈아 옮기는 거리. 0이면 한 줄로 놓는다.")]
        [SerializeField, Min(0f)] private float _radialZigzag;

        [Tooltip("구간 첫 발판의 크기 배율 (x: 폭, y: 진행 방향 길이)")]
        [SerializeField] private Vector2 _startScale = Vector2.one;

        [Tooltip("구간 마지막 발판의 크기 배율. 첫 발판과 다르면 그 사이를 고르게 바꿔 간다.")]
        [SerializeField] private Vector2 _endScale = Vector2.one;

        [Tooltip("구간이 올라가는 높이에 곱하는 값. 1이면 설정 에셋의 높이 범위 그대로다.")]
        [SerializeField, Min(0f)] private float _heightScale = 1.0f;

        [Tooltip("구간 중간에서 이만큼 내려갔다가 다시 올라간다. 0이면 고르게 올라간다.")]
        [SerializeField, Min(0f)] private float _dipDepth;

        [Tooltip("켜면 2단 점프로만 넘을 수 있는 틈·높이를 허용한다. (틈 경고와 발판 수 계산에 2단 점프 범위를 쓴다)")]
        [SerializeField] private bool _requiresDoubleJump;

        [Header("Motion")]
        [Tooltip("발판이 움직이는 방식")]
        [SerializeField] private EPlatformMotion _motion = EPlatformMotion.None;

        [Tooltip("0보다 크면 이 간격마다 하나씩 움직이지 않는 고정 발판으로 둔다. (2: 고정-움직임-고정-움직임…, 첫 발판이 고정)")]
        [SerializeField, Min(0)] private int _fixedInterval;

        [Tooltip("Oscillate / StepMove: 움직이는 방향 (x: 반지름 바깥쪽, y: 위, z: 진행 방향)\nOrbit: 원을 그리는 면의 첫째 축. 둘째 축은 위쪽이다.")]
        [SerializeField] private Vector3 _axis = Vector3.up;

        [Tooltip("Oscillate: 가운데에서 한쪽 끝까지의 거리 / Orbit·Wheel: 원의 반지름 / StepMove: 이동 거리 / Sink·Scale: 위아래로 움직이는 최대 거리")]
        [SerializeField, Min(0f)] private float _distance = 1.5f;

        [Tooltip("움직이는 빠르기. Oscillate·Orbit·Wheel·Stretch 는 초당 라디안, StepMove·Sink·Scale·Stream 은 초당 미터다.")]
        [SerializeField, Min(0f)] private float _speed = 2.0f;

        [Tooltip("Wheel: 바퀴 하나에 달린 발판 수")]
        [SerializeField, Min(1)] private int _groupSize = 4;

        [Tooltip("Stretch: 가장 짧을 때의 길이 비율")]
        [SerializeField, Range(0.05f, 1f)] private float _stretchMinRatio = 0.4f;

        [Tooltip("ShootReveal: 발판마다 그 위에 하나씩 띄울 표적 프리팹 (DamageButton 필요). 쏘면 그 발판이 나타난다.")]
        [SerializeField] private GameObject _auxPrefab;

        [Tooltip("같은 구간의 발판들이 박자를 어긋나게 하는 방식 (Oscillate / Orbit / Blink)")]
        [SerializeField] private EPhaseMode _phaseMode = EPhaseMode.ByHeight;

        [Tooltip("Phase Mode 가 Sequential 일 때 발판 하나마다 늦어지는 양. Blink 는 초, Oscillate / Orbit 은 라디안이다.")]
        [SerializeField, Min(0f)] private float _phaseStep = 0.4f;

        [Header("Timing")]
        [Tooltip("Blink / ShootReveal: 나타나 있는 시간(초)")]
        [SerializeField, Min(0.1f)] private float _activeDuration = 2.0f;

        [Tooltip("Blink: 사라져 있는 시간(초). Phase Mode 가 Alternate 이면 Active Duration 과 같게 둬야 두 묶음이 정확히 교대한다.")]
        [SerializeField, Min(0.1f)] private float _inactiveDuration = 1.5f;

        [Tooltip("Blink / ShootReveal: 사라지기 전에 깜빡여 예고하는 시간(초). 0이면 예고하지 않는다.")]
        [SerializeField, Min(0f)] private float _warningDuration;

        [Tooltip("Falling / Crumble: 밟은 뒤 떨어지거나 사라지기까지의 시간(초) / StepMove: 밟은 뒤 출발하기까지의 시간(초)")]
        [SerializeField, Min(0f)] private float _triggerDelay = 0.6f;

        [Tooltip("Falling / Crumble: 사라진 뒤 다시 생기기까지의 시간(초) / StepMove: 도착한 뒤 돌아오기 시작할 때까지의 시간(초)")]
        [SerializeField, Min(0f)] private float _respawnDelay = 2.5f;

        [Header("Placements")]
        [Tooltip("이 패턴의 구간에서 발판 위에 무작위로 놓을 장애물·아이템. 태그 끝 발판에는 놓지 않고, 발판 하나에 하나만 놓는다. 발판과 함께 움직이고 사라진다.")]
        [SerializeField] private SMapPlacement[] _placements;
        #endregion

        #region Properties
        /// <summary>무작위로 뽑힐 비중.</summary>
        public float Weight => _weight;

        /// <summary>발판 사이 틈에 곱하는 값.</summary>
        public float GapScale => _gapScale;

        /// <summary>발판을 안팎으로 번갈아 옮기는 거리.</summary>
        public float RadialZigzag => _radialZigzag;

        /// <summary>구간 첫 발판의 크기 배율 (x: 폭, y: 길이).</summary>
        public Vector2 StartScale => _startScale;

        /// <summary>구간 마지막 발판의 크기 배율 (x: 폭, y: 길이).</summary>
        public Vector2 EndScale => _endScale;

        /// <summary>구간이 올라가는 높이에 곱하는 값.</summary>
        public float HeightScale => _heightScale;

        /// <summary>구간 중간에서 내려가는 깊이.</summary>
        public float DipDepth => _dipDepth;

        /// <summary>2단 점프로만 넘을 수 있는 틈·높이를 허용하는지 여부.</summary>
        public bool RequiresDoubleJump => _requiresDoubleJump;

        /// <summary>발판이 움직이는 방식.</summary>
        public EPlatformMotion Motion => _motion;

        /// <summary>고정 발판을 끼워 넣는 간격. 0이면 끼워 넣지 않는다.</summary>
        public int FixedInterval => _fixedInterval;

        /// <summary>Wheel: 바퀴 하나에 달린 발판 수.</summary>
        public int GroupSize => _groupSize;

        /// <summary>Stretch: 가장 짧을 때의 길이 비율.</summary>
        public float StretchMinRatio => _stretchMinRatio;

        /// <summary>ShootReveal: 발판 위에 띄울 표적 프리팹.</summary>
        public GameObject AuxPrefab => _auxPrefab;

        /// <summary>왕복 방향 또는 원을 그리는 면의 첫째 축 (x: 반지름 바깥쪽, y: 위, z: 진행 방향).</summary>
        public Vector3 Axis => _axis;

        /// <summary>왕복 거리 또는 원의 반지름.</summary>
        public float Distance => _distance;

        /// <summary>움직이는 빠르기.</summary>
        public float Speed => _speed;

        /// <summary>발판들이 박자를 어긋나게 하는 방식.</summary>
        public EPhaseMode PhaseMode => _phaseMode;

        /// <summary>Sequential 일 때 발판 하나마다 늦어지는 양.</summary>
        public float PhaseStep => _phaseStep;

        /// <summary>Blink: 나타나 있는 시간.</summary>
        public float ActiveDuration => _activeDuration;

        /// <summary>Blink: 사라져 있는 시간.</summary>
        public float InactiveDuration => _inactiveDuration;

        /// <summary>Blink: 사라지기 전에 깜빡이는 시간.</summary>
        public float WarningDuration => _warningDuration;

        /// <summary>Falling / Crumble: 밟은 뒤 반응하기까지의 시간.</summary>
        public float TriggerDelay => _triggerDelay;

        /// <summary>Falling / Crumble: 다시 생기기까지의 시간.</summary>
        public float RespawnDelay => _respawnDelay;

        /// <summary>이 패턴의 구간에 놓을 배치물.</summary>
        public IReadOnlyList<SMapPlacement> Placements => _placements;
        #endregion
    }
}
