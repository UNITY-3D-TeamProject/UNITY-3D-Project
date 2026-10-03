using System;
using UnityEngine;
using Map.Gimmicks;

namespace Map.Common
{
    /// <summary>
    /// 한 씬 안에서 맵을 여러 라운드에 걸쳐 다시 생성한다. (예: 미로 3라운드)
    /// 도착 지점에 닿으면 다음 라운드의 맵을 새로 만들고 플레이어를 새 시작 지점으로 옮긴다.
    /// 마지막 라운드의 도착 지점에는 Final Goal Prefab(로비로 가는 포탈 등)을 놓는다.
    /// 생성기의 Goal Prefab 은 비워 둔다. 도착 지점은 이 컴포넌트가 놓는다.
    /// </summary>
    public class MapRoundController : MonoBehaviour
    {
        #region Serialized Fields
        [Header("References")]
        [Tooltip("라운드마다 다시 생성할 맵 생성기")]
        [SerializeField] private MapGeneratorBase _generator;

        [Header("Rounds")]
        [Tooltip("전체 라운드 수")]
        [SerializeField, Min(1)] private int _roundCount = 3;

        [Tooltip("마지막이 아닌 라운드의 도착 지점에 놓을 프리팹. TriggerPlate 가 있어야 한다.")]
        [SerializeField] private TriggerPlate _nextRoundGoalPrefab;

        [Tooltip("마지막 라운드의 도착 지점에 놓을 프리팹. 예: 로비로 가는 포탈")]
        [SerializeField] private GameObject _finalGoalPrefab;
        #endregion

        #region Private Fields
        private GameObject _goalInstance;
        #endregion

        #region Properties
        /// <summary>현재 라운드 (1부터 시작).</summary>
        public int CurrentRound { get; private set; } = 1;

        /// <summary>전체 라운드 수.</summary>
        public int RoundCount => _roundCount;
        #endregion

        #region Events
        /// <summary>라운드가 시작될 때 라운드 번호와 함께 호출된다. 첫 라운드도 포함한다.</summary>
        public event Action<int> OnRoundStarted;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            Debug.Assert(_generator != null, $"[{name}] 맵 생성기가 연결되지 않았습니다.");
        }

        private void Start()
        {
            if (_generator == null) return;

            // 첫 라운드의 맵은 생성기가 Awake 에서 이미 만들었다
            if (!_generator.IsGenerated)
            {
                _generator.Generate();
            }

            PlaceGoal();
            OnRoundStarted?.Invoke(CurrentRound);
        }
        #endregion

        #region Private Methods
        private void AdvanceRound()
        {
            if (CurrentRound >= _roundCount) return;

            CurrentRound++;

            _generator.Generate();
            PlaceGoal();
            MovePlayerToStart();

            OnRoundStarted?.Invoke(CurrentRound);
        }

        private void PlaceGoal()
        {
            if (_goalInstance != null)
            {
                Destroy(_goalInstance);
            }

            bool isFinalRound = CurrentRound >= _roundCount;
            if (isFinalRound)
            {
                if (_finalGoalPrefab != null)
                {
                    _goalInstance = Instantiate(_finalGoalPrefab, _generator.GoalPosition, Quaternion.identity);
                }

                return;
            }

            if (_nextRoundGoalPrefab == null) return;

            TriggerPlate plate = Instantiate(_nextRoundGoalPrefab, _generator.GoalPosition, Quaternion.identity);
            plate.OnPlayerEntered.AddListener(AdvanceRound);
            _goalInstance = plate.gameObject;
        }

        // 새 맵의 시작 지점이 리스폰 위치도 된다
        private void MovePlayerToStart()
        {
            PlayerRespawner respawner = FindAnyObjectByType<PlayerRespawner>();
            if (respawner == null) return;

            respawner.SetRespawnPosition(_generator.StartPosition);
            respawner.Respawn();
        }
        #endregion
    }
}
