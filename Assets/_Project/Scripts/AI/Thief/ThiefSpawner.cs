using System;
using System.Collections.Generic;
using UnityEngine;
using AI.RoundSpawning;
using Attribute.Core;
using Core;
using Mediator;

namespace AI.Thief
{
    /// <summary>
    /// 지정한 라운드에 도둑을 여러 마리 만들고, 잡힌 수를 세어 이벤트로 알린다.
    /// 시작점에서 일정 거리 안의 웨이포인트는 피하고, 한 웨이포인트에 최대 N마리까지 놓는다.
    /// 라운드 중 플레이어 재스폰 요청이 오면 전부 제거 후 다시 만든다. 전부 잡은 뒤에는 무시한다.
    /// 웨이포인트는 NavMesh 위에 있어야 한다. 그렇지 않으면 생성 시 NavMeshAgent 가 경고를 낸다.
    /// </summary>
    public class ThiefSpawner : RoundSpawnerBase
    {
        #region Nested Types
        /// <summary>도둑 한 마리와 그 이벤트 구독. 구독을 해제할 수 있게 핸들러를 들고 있다.</summary>
        private class ThiefEntry
        {
            public GameObject Root;
            public CharacterMediator Mediator;
            public Action OnDeathHandler;
            public Action OnDeathEndedHandler;
            public bool IsCaught;
        }
        #endregion

        #region Serialized Fields
        [Header("References")]
        [SerializeField] private GameObject _prefab;
        [Tooltip("도둑 초기값(HP, 이동 속도). 생성 직후 SetValue 로 넣는다.")]
        [SerializeField] private SOMonsterData _monsterData;

        [Header("Spawn")]
        [Min(1)]
        [SerializeField] private int _count = 20;
        [Tooltip("라운드 시작점에서 이 거리(수평) 안의 웨이포인트에는 만들지 않는다.")]
        [SerializeField] private float _spawnExclusionRadius = 25.0f;
        [Min(1)]
        [SerializeField] private int _maxPerWaypoint = 3;
        [Tooltip("임시 입력. 맵 담당 웨이포인트 형식이 정해지면 교체한다.")]
        [SerializeField] private Transform[] _waypointTransforms;
        #endregion

        #region Private Fields
        private readonly List<ThiefEntry> _thieves = new List<ThiefEntry>();
        private bool _hasSpawned;
        private bool _isCleared;
        #endregion

        #region Properties
        public int CaughtCount { get; private set; }
        public int TotalCount { get; private set; }

        /// <summary>
        /// 이번 라운드의 도둑을 전부 잡았는가. 생성 전에는 false 이다.
        /// CaughtCount == TotalCount 는 생성 전에도 0 == 0 으로 참이므로 문 판정에는 이것을 쓴다.
        /// </summary>
        public bool IsCleared => _isCleared;

        protected override bool IsSpawned => _hasSpawned;
        #endregion

        #region Events
        /// <summary>도둑이 잡힌 순간 그 위치.</summary>
        public event Action<Vector3> OnThiefCaught;

        /// <summary>(잡은 수, 전체 수). 잡힐 때와 생성 직후(리셋 포함)에 보낸다.</summary>
        public event Action<int, int> OnCountChanged;

        public event Action OnAllCaught;
        #endregion

        #region Protected Methods
        /// <summary>전부 잡은 뒤에는 무시한다. 아니면 제거 후 다시 만든다.</summary>
        protected override void HandlePlayerRespawnRequested()
        {
            if (_isCleared) return;

            base.HandlePlayerRespawnRequested();
        }

        protected override void Spawn()
        {
            if (!_prefab || !_monsterData)
            {
                LogSpawnFailureOnce("프리팹 또는 몬스터 데이터가 연결되지 않아 도둑을 만들 수 없다.");
                return;
            }

            List<Vector3> waypoints = BuildWaypoints();
            if (waypoints.Count == 0)
            {
                LogSpawnFailureOnce("웨이포인트가 없어 도둑을 만들 수 없다.");
                return;
            }

            List<int> openSlots = BuildSpawnCandidates(waypoints);
            int[] assignedCounts = new int[waypoints.Count];

            for (int i = 0; i < _count; i++)
            {
                if (openSlots.Count == 0)
                {
                    Debug.LogWarning($"[{name}] 자리가 모자라 도둑 {_thieves.Count}마리만 만들었다. (요청 {_count}마리)", this);
                    break;
                }

                int pick = UnityEngine.Random.Range(0, openSlots.Count);
                int waypointIndex = openSlots[pick];

                assignedCounts[waypointIndex]++;
                if (assignedCounts[waypointIndex] >= _maxPerWaypoint)
                {
                    openSlots[pick] = openSlots[openSlots.Count - 1];
                    openSlots.RemoveAt(openSlots.Count - 1);
                }

                if (!TrySpawnThief(waypoints[waypointIndex], waypoints))
                {
                    // 다음 프레임에 중복 생성되지 않도록 이미 만든 도둑까지 정리한다
                    DestroyAllThieves();
                    return;
                }
            }

            if (_thieves.Count == 0)
            {
                LogSpawnFailureOnce("만들 수 있는 자리가 없어 도둑을 만들지 못했다.");
                return;
            }

            TotalCount = _thieves.Count;
            CaughtCount = 0;
            _isCleared = false;
            _hasSpawned = true;
            OnCountChanged?.Invoke(CaughtCount, TotalCount);
        }

        /// <summary>남은 도둑을 전부 제거한다. 집계는 다음 Spawn 이 0 으로 만든다.</summary>
        protected override void Despawn()
        {
            DestroyAllThieves();
            _hasSpawned = false;
        }
        #endregion

        #region Private Methods
        /// <summary>도둑 한 마리를 만들고 초기화한다. 프리팹 구성이 잘못되면 만든 개체를 지우고 false 를 돌려준다.</summary>
        private bool TrySpawnThief(Vector3 position, List<Vector3> waypoints)
        {
            GameObject root = Instantiate(_prefab, position, Quaternion.identity);

            AttributeSet attributes = root.GetComponentInChildren<AttributeSet>(true);
            if (!root.TryGetComponent(out WaypointPatrolRoute route) ||
                !root.TryGetComponent(out CharacterMediator mediator) ||
                !attributes)
            {
                LogSpawnFailureOnce("프리팹 루트에 WaypointPatrolRoute / CharacterMediator / AttributeSet 이 없어 도둑을 만들 수 없다.");
                Destroy(root);
                return false;
            }

            route.SetWaypoints(waypoints);
            ApplyMonsterData(attributes);

            ThiefEntry entry = new ThiefEntry { Root = root, Mediator = mediator };
            entry.OnDeathHandler = () => HandleThiefDeath(entry);
            entry.OnDeathEndedHandler = () => HandleThiefDeathEnded(entry);
            mediator.OnDeath += entry.OnDeathHandler;
            mediator.OnDeathEnded += entry.OnDeathEndedHandler;

            _thieves.Add(entry);
            return true;
        }

        /// <summary>몬스터 데이터의 초기값을 SetValue 로 넣는다. 없는 키는 건너뛴다.</summary>
        private void ApplyMonsterData(AttributeSet attributes)
        {
            foreach (SOAttributeData.SAttribute entry in _monsterData.Attributes)
            {
                if (!attributes.IsValidTarget(entry.AttributeName))
                {
                    Debug.LogWarning($"[{_monsterData.name}] 어트리뷰트 '{entry.AttributeName}'가 도둑에 없어 건너뜁니다.", _monsterData);
                    continue;
                }

                attributes.SetValue(entry.AttributeName, entry.Value);
            }
        }

        private void HandleThiefDeath(ThiefEntry entry)
        {
            if (entry.IsCaught) return;

            entry.IsCaught = true;
            CaughtCount++;

            OnThiefCaught?.Invoke(entry.Root.transform.position);
            OnCountChanged?.Invoke(CaughtCount, TotalCount);

            if (CaughtCount == TotalCount)
            {
                _isCleared = true;
                OnAllCaught?.Invoke();
            }
        }

        private void HandleThiefDeathEnded(ThiefEntry entry)
        {
            Unsubscribe(entry);
            _thieves.Remove(entry);
            Destroy(entry.Root);
        }

        private void DestroyAllThieves()
        {
            for (int i = 0; i < _thieves.Count; i++)
            {
                Unsubscribe(_thieves[i]);
                Destroy(_thieves[i].Root);
            }

            _thieves.Clear();
        }

        private static void Unsubscribe(ThiefEntry entry)
        {
            if (!entry.Mediator) return;

            entry.Mediator.OnDeath -= entry.OnDeathHandler;
            entry.Mediator.OnDeathEnded -= entry.OnDeathEndedHandler;
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
        /// 시작점에서 제외 반경보다 먼 웨이포인트의 인덱스 목록을 만든다. 시작점이 없으면 전부 후보다.
        /// </summary>
        private List<int> BuildSpawnCandidates(List<Vector3> waypoints)
        {
            List<int> candidates = new List<int>(waypoints.Count);
            Transform spawnPoint = StageManager.GetSpawnPoint();
            float sqrRadius = _spawnExclusionRadius * _spawnExclusionRadius;

            for (int i = 0; i < waypoints.Count; i++)
            {
                if (spawnPoint)
                {
                    Vector3 offset = waypoints[i] - spawnPoint.position;
                    offset.y = 0.0f;
                    if (offset.sqrMagnitude <= sqrRadius) continue;
                }

                candidates.Add(i);
            }

            return candidates;
        }
        #endregion
    }
}
