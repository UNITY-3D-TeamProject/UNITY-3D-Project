using UnityEngine;
using Map.Maze;

namespace Core.Stage
{
    /// <summary>
    /// 파일 앱 스테이지. 미로 하나가 라운드 하나다 (Maze_0 = 1라운드, Maze_1 = 2라운드, Maze_2 = 3라운드).
    /// 지금 라운드 번호는 StageBase 가 따라가고, 이 컴포넌트는 번호를 보고 그 라운드의 로직을 직접 부른다. 설정은 인스펙터의 각 라운드 항목에서 한다.
    /// - 1라운드: 따로 로직이 없다. 출구에 닿으면 문이 열린다.
    /// - 2라운드: 숫자 퍼즐(NumberPuzzleRound). 맵에 놓인 노드(MazePuzzleItem)와 패널(MazePuzzleBoard)을 찾아 쓰고, 패널은 출구 문 앞으로 옮긴다.
    /// - 3라운드: 경찰과 도둑(ThiefChaseRound). 씬의 ThiefSpawner 를 찾아 쓰고, 제한 시간 안에 못 잡으면 플레이어가 죽는다.
    /// 2·3라운드의 미로 출구 문은 막아 두었다가, 그 라운드를 깨면 연다.
    /// </summary>
    public class FileApp : StageBase
    {
        #region Constants
        /// <summary>숫자 퍼즐을 하는 라운드 번호 (0부터, 2라운드).</summary>
        public const int NUMBER_PUZZLE_ROUND_INDEX = 1;

        /// <summary>도둑 잡기를 하는 라운드 번호 (0부터, 3라운드).</summary>
        public const int THIEF_CHASE_ROUND_INDEX = 2;

        private const string MAZE_NAME_PREFIX = "Maze_";  // MazeChainGenerator 가 붙이는 이름
        #endregion

        #region Serialized Fields
        [Header("2라운드 · 숫자 퍼즐")]
        [SerializeField] private NumberPuzzleRound _numberPuzzleRound = new NumberPuzzleRound();

        [Tooltip("패널을 놓을 자리. 2라운드 출구 칸 가운데에서 문 쪽으로 이만큼 (m). 문짝은 칸 끝(칸 크기의 절반)에 있다.")]
        [SerializeField] private float _puzzleBoardOffset = 1.5f;

        [Header("3라운드 · 경찰과 도둑")]
        [SerializeField] private ThiefChaseRound _thiefChaseRound = new ThiefChaseRound();

        [Tooltip("클리어할 때 3번째 미로의 내부 벽과 내부 기둥을 아래로 내리는 거리 (m)")]
        [SerializeField, Min(0.0f)] private float _innerWallLowerDistance = 12.0f;

        [Tooltip("3번째 미로의 내부 벽과 내부 기둥이 내려가는 시간 (초)")]
        [SerializeField, Min(0.0f)] private float _innerWallLowerDuration = 1.25f;
        #endregion

        #region Private Fields
        private StageManager _stageManager;

        // 라운드를 깨면 여는 미로 출구 문. 미로가 없는 씬(테스트 씬 등)에서는 null.
        private MazeExitGate _numberPuzzleGate;
        private MazeExitGate _thiefChaseGate;
        private MazeInnerWallLowering _thiefMazeWallLowering;
        #endregion

        #region Properties
        public override EStageType StageType => EStageType.FileApp;

        /// <summary>2라운드 숫자 퍼즐.</summary>
        public NumberPuzzleRound NumberPuzzleRound => _numberPuzzleRound;

        /// <summary>3라운드 도둑 잡기.</summary>
        public ThiefChaseRound ThiefChaseRound => _thiefChaseRound;
        #endregion

        #region Unity Lifecycle
        // 라운드 번호 비교와 화면 알림은 StageBase 가 한다.
        protected override void Update()
        {
            base.Update();

            // 타이머는 3라운드에 들어왔을 때만 돈다
            _thiefChaseRound.Tick(Time.deltaTime);
        }

        private void OnDestroy()
        {
            // StartStage 에서 준비하지 않았으면 풀 구독도 없다
            if (_stageManager == null) return;

            _stageManager.OnPlayerRespawnRequested -= HandlePlayerRespawnRequested;

            _numberPuzzleRound.OnProgressChanged -= HandleRoundProgressChanged;
            _numberPuzzleRound.OnCleared -= HandleNumberPuzzleCleared;
            _numberPuzzleRound.End();

            _thiefChaseRound.OnProgressChanged -= HandleRoundProgressChanged;
            _thiefChaseRound.OnCleared -= HandleThiefChaseCleared;
            _thiefChaseRound.End();
        }
        #endregion

        #region Public Methods
        public override void StartStage()
        {
            StageProgress = 0;

            // StartStage 는 스테이지마다 한 번 불린다. 혹시 다시 불려도 두 번 준비하지 않는다
            if (_stageManager != null) return;

            _stageManager = GameManager.Instance.CurrentStageManager;
            if (_stageManager == null)
            {
                Debug.LogWarning($"[{name}] StageManager가 없어 라운드를 시작하지 않습니다.", this);
                return;
            }

            _stageManager.OnPlayerRespawnRequested += HandlePlayerRespawnRequested;

            // 미로와 맵 배치물은 MapGeneratorBase.Awake 에서 이미 만들어져 있다
            _numberPuzzleRound.OnProgressChanged += HandleRoundProgressChanged;
            _numberPuzzleRound.OnCleared += HandleNumberPuzzleCleared;
            _numberPuzzleRound.Begin();

            _thiefChaseRound.OnProgressChanged += HandleRoundProgressChanged;
            _thiefChaseRound.OnCleared += HandleThiefChaseCleared;
            _thiefChaseRound.Begin();

            LockMazeGates();
        }

        public override void EndStage()
        {
        }

        public override bool IsRoundCleared(int roundIndex)
        {
            return roundIndex switch
            {
                NUMBER_PUZZLE_ROUND_INDEX => _numberPuzzleRound.IsCleared,
                THIEF_CHASE_ROUND_INDEX => _thiefChaseRound.IsCleared,
                _ => true,
            };
        }
        #endregion

        #region Protected Methods
        // 바뀐 라운드 번호의 로직만 진행 중으로 둔다. 로직이 없는 라운드(1라운드)면 모두 꺼진다.
        protected override void OnRoundChanged()
        {
            _numberPuzzleRound.SetActive(CurrentRoundIndex == NUMBER_PUZZLE_ROUND_INDEX);
            _thiefChaseRound.SetActive(CurrentRoundIndex == THIEF_CHASE_ROUND_INDEX);
        }
        #endregion

        #region Private Methods
        // 2·3라운드 미로의 출구 문을 막아 둔다. 미로가 없는 씬(테스트 씬 등)에서는 건너뛴다.
        private void LockMazeGates()
        {
            var generator = FindAnyObjectByType<MazeChainGenerator>();
            if ((generator == null) || (generator.GeneratedRoot == null)) return;

            _numberPuzzleGate = LockMazeGate(generator.GeneratedRoot, NUMBER_PUZZLE_ROUND_INDEX);
            _thiefChaseGate = LockMazeGate(generator.GeneratedRoot, THIEF_CHASE_ROUND_INDEX);
            _thiefMazeWallLowering = FindMazeWallLowering(generator.GeneratedRoot, THIEF_CHASE_ROUND_INDEX);
            PlacePuzzleBoard();
        }

        // 출구 칸은 생성할 때마다 바뀌므로, 맵에 하나 놓인 퍼즐 패널을 2라운드 출구 문 앞으로 옮긴다. 패널의 +Z 가 문 쪽이다.
        private void PlacePuzzleBoard()
        {
            MazePuzzleBoard board = _numberPuzzleRound.Board;
            if ((board == null) || (_numberPuzzleGate == null)) return;

            Transform gateTransform = _numberPuzzleGate.transform;
            board.transform.SetPositionAndRotation(
                gateTransform.TransformPoint(new Vector3(0.0f, 0.0f, _puzzleBoardOffset)),
                gateTransform.rotation);
        }

        // roundIndex 번 미로의 출구 문을 찾아 돌려준다. 출구 문은 들어가기만 하면 열리므로 트리거를 꺼 두고, 라운드를 깨면 직접 연다.
        private MazeExitGate LockMazeGate(Transform generatedRoot, int roundIndex)
        {
            Transform mazeRoot = generatedRoot.Find(MAZE_NAME_PREFIX + roundIndex);
            MazeExitGate gate = (mazeRoot != null) ? mazeRoot.GetComponentInChildren<MazeExitGate>() : null;
            if (gate == null)
            {
                Debug.LogWarning($"[{name}] {roundIndex + 1}라운드 미로 또는 출구 문이 없어 문을 연결하지 않습니다.", this);
                return null;
            }

            if (gate.TryGetComponent(out Collider gateTrigger))
            {
                gateTrigger.enabled = false;
            }

            return gate;
        }

        private MazeInnerWallLowering FindMazeWallLowering(Transform generatedRoot, int roundIndex)
        {
            Transform mazeRoot = generatedRoot.Find(MAZE_NAME_PREFIX + roundIndex);
            MazeInnerWallLowering wallLowering = (mazeRoot != null) ? mazeRoot.GetComponent<MazeInnerWallLowering>() : null;
            if (wallLowering == null)
            {
                Debug.LogWarning($"[{name}] {roundIndex + 1}라운드 미로 내부 벽 하강 장치를 찾지 못했습니다.", this);
            }

            return wallLowering;
        }

        // 라운드 중 죽으면 그 라운드만 처음부터 다시 한다. 적은 각 스포너가 다시 만든다.
        private void HandlePlayerRespawnRequested()
        {
            switch (CurrentRoundIndex)
            {
                case NUMBER_PUZZLE_ROUND_INDEX:
                    _numberPuzzleRound.ResetPuzzle();
                    break;

                case THIEF_CHASE_ROUND_INDEX:
                    _thiefChaseRound.RestartTimer();
                    break;
            }
        }

        private void HandleRoundProgressChanged()
        {
            NotifyProgressChanged();
        }

        private void HandleNumberPuzzleCleared()
        {
            if (_numberPuzzleGate != null)
            {
                _numberPuzzleGate.Open();
            }
        }

        private void HandleThiefChaseCleared()
        {
            if (_thiefChaseGate != null)
            {
                _thiefChaseGate.Open();
            }

            _thiefMazeWallLowering?.Lower(_innerWallLowerDistance, _innerWallLowerDuration);
        }
        #endregion
    }
}
