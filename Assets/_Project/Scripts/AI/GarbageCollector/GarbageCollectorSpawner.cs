using System.Collections.Generic;
using Unity.Behavior;
using UnityEngine;
using Core;
using Core.Stage;
using Facade.Player;

namespace AI.GarbageCollector
{
    /// <summary>
    /// 지정한 라운드에만 가비지 컬렉터를 1마리 생성하고, 라운드가 끝나면 제거한다.
    /// 라운드 중 플레이어 재스폰 요청이 오면 제거 후 바로 다시 만들어 속도 시계를 0부터 시작한다.
    /// 웨이포인트는 NavMesh 위에 있어야 한다. 그렇지 않으면 생성 시 NavMeshAgent 가 경고를 낸다.
    /// </summary>
    public class GarbageCollectorSpawner : MonoBehaviour
    {
        #region Constants
        /// <summary>BT 그래프 블랙보드에서 플레이어 몸체를 담는 GameObject 변수 이름.</summary>
        private const string PLAYER_VARIABLE_NAME = "Player";
        #endregion

        #region Serialized Fields
        [Header("References")]
        [SerializeField] private StageManager _stageManager;
        [SerializeField] private GameObject _prefab;

        [Header("Spawn")]
        [Tooltip("StageManager.CurrentRoundIndex 가 이 값일 때만 존재한다. 0 부터 시작하므로 라운드 2 는 1 이다.")]
        [SerializeField] private int _activeRoundIndex = 1;
        [Tooltip("임시 입력. 맵 담당 웨이포인트 형식이 정해지면 교체한다.")]
        [SerializeField] private Transform[] _waypointTransforms;
        #endregion

        #region Private Fields
        private GameObject _instance;
        private BehaviorGraphAgent _graphAgent;
        private GameManager _gameManager;
        private bool _hasLoggedSpawnFailure;
        #endregion

        #region Unity Lifecycle
        private void OnEnable()
        {
            if (_stageManager) _stageManager.OnPlayerRespawnRequested += HandlePlayerRespawnRequested;

            _gameManager = GameManager.Instance;
            if (_gameManager) _gameManager.OnPlayerSpawned += HandlePlayerSpawned;
        }

        private void OnDisable()
        {
            if (_stageManager) _stageManager.OnPlayerRespawnRequested -= HandlePlayerRespawnRequested;

            if (_gameManager) _gameManager.OnPlayerSpawned -= HandlePlayerSpawned;
            _gameManager = null;
        }

        private void Update()
        {
            if (!_stageManager) return;

            bool shouldExist = _stageManager.IsRunning && (_stageManager.CurrentRoundIndex == _activeRoundIndex);
            bool isExisting = _instance != null;

            if (shouldExist && !isExisting)
            {
                Spawn();
            }
            else if (!shouldExist && isExisting)
            {
                Despawn();
            }
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// 라운드 중 플레이어 재스폰 요청이 오면 가비지 컬렉터를 새로 만든다.
        /// </summary>
        private void HandlePlayerRespawnRequested()
        {
            if (_instance == null) return;

            Despawn();
            Spawn();
        }

        /// <summary>
        /// 사망 후 재스폰하면 플레이어가 새로 만들어지므로, 가비지 컬렉터가 있으면 블랙보드의 플레이어를 바꿔 넣는다.
        /// </summary>
        private void HandlePlayerSpawned(PlayerFacade playerFacade)
        {
            if (_instance == null) return;

            SetPlayerVariable();
        }

        /// <summary>
        /// 웨이포인트 중 플레이어 스폰 지점에서 가장 먼 곳에 가비지 컬렉터를 만들고 순찰 경로를 넘긴다.
        /// 실패하면 로그를 한 번만 남긴다. 매 프레임 재시도하며 로그가 쌓이지 않게 한다.
        /// </summary>
        private void Spawn()
        {
            if (!_prefab)
            {
                LogSpawnFailureOnce("프리팹이 연결되지 않아 가비지 컬렉터를 만들 수 없다.");
                return;
            }

            List<Vector3> waypoints = BuildWaypoints();
            if (waypoints.Count == 0)
            {
                LogSpawnFailureOnce("웨이포인트가 없어 가비지 컬렉터를 만들 수 없다.");
                return;
            }

            Vector3 position = FindFarthestWaypoint(waypoints);
            GameObject instance = Instantiate(_prefab, position, Quaternion.identity);

            if (!instance.TryGetComponent(out WaypointPatrolRoute route))
            {
                LogSpawnFailureOnce("프리팹 루트에 WaypointPatrolRoute 가 없어 만든 가비지 컬렉터를 제거한다.");
                Destroy(instance);
                return;
            }

            route.SetWaypoints(waypoints);
            _instance = instance;
            _graphAgent = instance.GetComponentInChildren<BehaviorGraphAgent>();
            SetPlayerVariable();
        }

        private void Despawn()
        {
            Destroy(_instance);
            _instance = null;
            _graphAgent = null;
        }

        /// <summary>
        /// 현재 플레이어의 몸체를 BT 블랙보드의 Player 변수에 넣는다. 플레이어가 아직 없으면 다음 스폰 이벤트를 기다린다.
        /// 위치 조회만 하므로 중재자를 거치지 않는다.
        /// </summary>
        private void SetPlayerVariable()
        {
            if (!_graphAgent)
            {
                LogSpawnFailureOnce("프리팹에 BehaviorGraphAgent 가 없어 플레이어를 블랙보드에 넣을 수 없다.");
                return;
            }

            PlayerFacade playerFacade = _gameManager ? _gameManager.CurrentPlayerFacade : null;
            if (!playerFacade) return;

            // 실제로 움직이는 몸체는 CharacterController 가 붙은 오브젝트다 (StageManager.BindPlayer 와 같은 방식)
            CharacterController body = playerFacade.transform.root.GetComponentInChildren<CharacterController>();
            if (!body) return;

            if (!_graphAgent.SetVariableValue(PLAYER_VARIABLE_NAME, body.gameObject))
            {
                LogSpawnFailureOnce($"BT 그래프에 GameObject 변수 '{PLAYER_VARIABLE_NAME}' 이 없어 플레이어를 넣을 수 없다.");
            }
        }

        private List<Vector3> BuildWaypoints()
        {
            List<Vector3> waypoints = new List<Vector3>();
            if (_waypointTransforms == null) return waypoints;

            for (int i = 0; i < _waypointTransforms.Length; i++)
            {
                if (_waypointTransforms[i]) waypoints.Add(_waypointTransforms[i].position);
            }

            return waypoints;
        }

        /// <summary>
        /// 플레이어 스폰 지점에서 직선거리가 가장 먼 웨이포인트를 고른다. 기준점이 없으면 0번을 쓴다.
        /// </summary>
        private Vector3 FindFarthestWaypoint(List<Vector3> waypoints)
        {
            Transform spawnPoint = _stageManager.GetSpawnPoint();
            if (!spawnPoint) return waypoints[0];

            Vector3 farthest = waypoints[0];
            float farthestDistance = 0.0f;

            for (int i = 0; i < waypoints.Count; i++)
            {
                float distance = (waypoints[i] - spawnPoint.position).sqrMagnitude;
                if (distance <= farthestDistance) continue;

                farthestDistance = distance;
                farthest = waypoints[i];
            }

            return farthest;
        }

        private void LogSpawnFailureOnce(string message)
        {
            if (_hasLoggedSpawnFailure) return;

            _hasLoggedSpawnFailure = true;
            Debug.LogError($"[{name}] {message}", this);
        }
        #endregion
    }
}
