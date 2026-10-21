using System;
using System.Collections.Generic;
using UnityEngine;
using Map.Maze;

namespace Core.Stage
{
    /// <summary>
    /// 파일 앱 스테이지. 미로 하나가 라운드 하나다 (Maze_0 = 1라운드, Maze_1 = 2라운드, Maze_2 = 3라운드).
    /// 라운드 로직은 이 컴포넌트가 모두 들고 있다. 설정은 인스펙터의 각 라운드 항목에서 한다.
    /// - 1라운드: 따로 로직이 없다. 출구에 닿으면 문이 열린다.
    /// - 2라운드: 숫자 퍼즐. 맵에 놓인 노드(MazePuzzleItem)와 패널(MazePuzzleBoard)을 찾아 쓴다.
    /// - 3라운드: 경찰과 도둑. 씬의 ThiefSpawner 를 찾아 쓰고, 제한 시간 안에 못 잡으면 플레이어가 죽는다.
    /// 로직이 있는 라운드의 미로 출구 문은 막아 두었다가, 그 라운드를 깨면 연다.
    /// </summary>
    public class FileApp : StageBase
    {
        #region Constants
        private const string MAZE_NAME_PREFIX = "Maze_";  // MazeChainGenerator 가 붙이는 이름
        #endregion

        #region Serialized Fields
        [Header("2라운드 · 숫자 퍼즐")]
        [SerializeField] private NumberPuzzleRound _numberPuzzleRound = new NumberPuzzleRound();

        [Header("3라운드 · 경찰과 도둑")]
        [SerializeField] private ThiefChaseRound _thiefChaseRound = new ThiefChaseRound();
        #endregion

        #region Private Fields
        private StageRound[] _rounds = new StageRound[0];
        private StageManager _stageManager;

        // 라운드를 깨면 문을 여는 구독. 파괴될 때 풀기 위해 들고 있는다.
        private readonly List<(StageRound Round, Action Handler)> _gateHandlers = new List<(StageRound, Action)>();
        #endregion

        #region Properties
        public override EStageType StageType => EStageType.FileApp;

        /// <summary>지금 진행 중인 라운드. 로직이 없는 라운드(1라운드)이거나 시작 전이면 null.</summary>
        public StageRound ActiveRound
        {
            get
            {
                foreach (StageRound round in _rounds)
                {
                    if (round.IsActive) return round;
                }

                return null;
            }
        }
        #endregion

        #region Events
        /// <summary>라운드가 바뀌거나 진행 중인 라운드의 진행 상황이 바뀌면 호출된다. 화면 갱신용.</summary>
        public event Action OnProgressChanged;
        #endregion

        #region Unity Lifecycle
        // 라운드가 바뀌었다는 이벤트가 없어서 매 프레임 비교한다.
        private void Update()
        {
            if (_stageManager == null) return;

            int currentRoundIndex = _stageManager.IsRunning ? _stageManager.CurrentRoundIndex : -1;
            foreach (StageRound round in _rounds)
            {
                round.SetActive(round.RoundIndex == currentRoundIndex);

                if (round.IsActive)
                {
                    round.Tick(Time.deltaTime);
                }
            }
        }

        private void OnDestroy()
        {
            if (_stageManager != null)
            {
                _stageManager.OnPlayerRespawnRequested -= HandlePlayerRespawnRequested;
            }

            foreach ((StageRound round, Action handler) in _gateHandlers)
            {
                round.OnCleared -= handler;
            }

            _gateHandlers.Clear();

            foreach (StageRound round in _rounds)
            {
                round.OnProgressChanged -= HandleRoundProgressChanged;
                round.End();
            }
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
            _rounds = new StageRound[] { _numberPuzzleRound, _thiefChaseRound };
            foreach (StageRound round in _rounds)
            {
                round.OnProgressChanged += HandleRoundProgressChanged;
                round.Begin();
            }

            ConnectMazeGates();
        }

        public override void EndStage()
        {
        }
        #endregion

        #region Private Methods
        // 라운드마다 같은 번호의 미로 출구 문을 막고, 깨면 열리게 한다. 미로가 없는 씬(테스트 씬 등)에서는 건너뛴다.
        private void ConnectMazeGates()
        {
            var generator = FindAnyObjectByType<MazeChainGenerator>();
            if ((generator == null) || (generator.GeneratedRoot == null)) return;

            foreach (StageRound round in _rounds)
            {
                Transform mazeRoot = generator.GeneratedRoot.Find(MAZE_NAME_PREFIX + round.RoundIndex);
                MazeExitGate gate = (mazeRoot != null) ? mazeRoot.GetComponentInChildren<MazeExitGate>() : null;
                if (gate == null)
                {
                    Debug.LogWarning($"[{name}] {round.RoundNumber}라운드 미로 또는 출구 문이 없어 문을 연결하지 않습니다.", this);
                    continue;
                }

                // 출구 문은 들어가기만 하면 열리므로 트리거를 꺼 두고, 라운드를 깨면 직접 연다
                if (gate.TryGetComponent(out Collider gateTrigger))
                {
                    gateTrigger.enabled = false;
                }

                Action handler = gate.Open;
                round.OnCleared += handler;
                _gateHandlers.Add((round, handler));
            }
        }

        private void HandlePlayerRespawnRequested()
        {
            foreach (StageRound round in _rounds)
            {
                round.HandlePlayerRespawnRequested();
            }
        }

        private void HandleRoundProgressChanged()
        {
            OnProgressChanged?.Invoke();
        }
        #endregion
    }
}
