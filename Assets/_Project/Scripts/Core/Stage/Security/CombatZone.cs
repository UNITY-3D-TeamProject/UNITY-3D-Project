using System;
using System.Collections.Generic;
using UnityEngine;
using Attribute.Core;
using Core.ObjectPool;
using Map.Gimmicks;
using Mediator;

namespace Core.Stage.Security
{
    /// <summary>
    /// 전투 구역 하나의 적 묶음. 플레이어가 구역 트리거에 들어오면 적을 만들고, 만든 적이 모두 죽으면 클리어를 알린다.
    /// 문·발판 등 클리어 뒤 처리와 사망 리셋 시점은 이 구역을 가진 라운드(MobRoomRound, SpecialMobRound)가 정한다.
    /// 적 종류·마리 수·생성 위치는 인스펙터에서 바꿔도 된다. 클리어는 "이번에 실제로 만든 적이 모두 죽었는가"로만 판정한다.
    /// 구역 전체를 덮는 콜라이더를 같은 오브젝트에 둔다 (Is Trigger 는 자동으로 켠다).
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class CombatZone : MonoBehaviour
    {
        #region Nested Types
        /// <summary>구역에 만들 적 한 종류와 마리 수.</summary>
        [Serializable]
        private struct SZoneEnemy
        {
            [Tooltip("만들 몬스터의 데이터. 프리팹은 이 데이터의 Monster Prefab 을 쓰고, 값은 생성 직후 SetValue 로 넣는다.\n씬에 ObjectPoolManager 가 있으면 그 프리팹이 등록돼 있어야 한다.")]
            public SOMonsterData Monster;

            [Tooltip("이 종류를 몇 마리 만들지. 0 이면 만들지 않는다.")]
            [Min(0)] public int Count;
        }
        #endregion

        #region Serialized Fields
        [Header("Enemies")]
        [Tooltip("이 구역에 만들 적 종류와 마리 수. 적이 하나도 없으면 들어오자마자 클리어된다.")]
        [SerializeField] private SZoneEnemy[] _enemies;

        [Tooltip("적을 만들 위치. 적이 위치보다 많으면 처음 위치부터 다시 돌아가며 쓴다. 비우면 이 오브젝트 위치에 만든다.")]
        [SerializeField] private Transform[] _spawnPoints;
        #endregion

        #region Private Fields
        // 살아 있는 적과, 사망 구독을 풀 때 쓸 핸들러
        private readonly Dictionary<CharacterMediator, Action> _aliveEnemies = new Dictionary<CharacterMediator, Action>();

        private bool _isStarted;
        #endregion

        #region Properties
        /// <summary>이 구역을 깼는지 여부.</summary>
        public bool IsCleared { get; private set; }

        /// <summary>이번에 실제로 만든 적 수. 시작 전에는 0.</summary>
        public int TotalCount { get; private set; }

        /// <summary>아직 살아 있는 적 수.</summary>
        public int AliveCount => _aliveEnemies.Count;
        #endregion

        #region Events
        /// <summary>적이 생기거나 죽거나 리셋·클리어되면 호출된다. 화면 갱신용.</summary>
        public event Action OnProgressChanged;

        /// <summary>구역을 깬 순간 한 번 호출된다.</summary>
        public event Action OnCleared;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            GetComponent<Collider>().isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_isStarted || IsCleared) return;
            if (!GimmickUtility.TryGetPlayer(other, out _)) return;

            StartZone();
        }

        private void OnDestroy()
        {
            foreach (KeyValuePair<CharacterMediator, Action> pair in _aliveEnemies)
            {
                if (pair.Key != null) pair.Key.OnDeath -= pair.Value;
            }

            _aliveEnemies.Clear();
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// 아직 못 깬 구역이면 남은 적을 없애고 처음 상태로 돌린다. 다시 들어오면 새로 만든다. 깬 구역은 그대로 둔다.
        /// </summary>
        public void ResetIfNotCleared()
        {
            if (IsCleared || !_isStarted) return;

            DespawnEnemies();
            _isStarted = false;
            TotalCount = 0;
            OnProgressChanged?.Invoke();
        }
        #endregion

        #region Private Methods
        private void StartZone()
        {
            _isStarted = true;
            SpawnEnemies();

            TotalCount = _aliveEnemies.Count;
            OnProgressChanged?.Invoke();

            // 적이 없는 구역(또는 하나도 만들지 못한 구역)은 바로 깬 것으로 본다
            if (_aliveEnemies.Count == 0)
            {
                Clear();
            }
        }

        private void Clear()
        {
            IsCleared = true;
            OnCleared?.Invoke();
            OnProgressChanged?.Invoke();
        }

        private void SpawnEnemies()
        {
            if (_enemies == null) return;

            int spawnIndex = 0;
            foreach (SZoneEnemy enemy in _enemies)
            {
                if (enemy.Monster == null || enemy.Monster.MonsterPrefab == null)
                {
                    Debug.LogError($"[{name}] 몬스터 데이터 또는 그 Monster Prefab 이 비어 있어 건너뜁니다.", this);
                    continue;
                }

                for (int i = 0; i < enemy.Count; i++)
                {
                    SpawnEnemy(enemy.Monster, GetSpawnPoint(spawnIndex));
                    spawnIndex++;
                }
            }
        }

        // 한 마리를 만들고 사망을 구독한다. 풀 매니저가 있으면 풀에서 꺼낸다 (MonsterSpawner 와 같은 방식).
        private void SpawnEnemy(SOMonsterData monster, Transform spawnPoint)
        {
            GameObject prefab = monster.MonsterPrefab;
            ObjectPoolManager poolManager = ObjectPoolManager.Instance;

            // 등록되지 않은 프리팹이면 풀 매니저가 에러를 남기고 null 을 돌려준다
            GameObject instance = (poolManager != null)
                ? poolManager.Get(prefab, spawnPoint.position, spawnPoint.rotation)
                : Instantiate(prefab, spawnPoint.position, spawnPoint.rotation);
            if (instance == null) return;

            AttributeSet attributes = instance.GetComponentInChildren<AttributeSet>(true);
            if (!instance.TryGetComponent(out CharacterMediator mediator) || (attributes == null))
            {
                Debug.LogError($"[{name}] '{prefab.name}' 루트에 CharacterMediator 또는 AttributeSet 이 없어 적으로 쓸 수 없습니다.", this);
                ReleaseEnemy(instance);
                return;
            }

            ApplyMonsterData(monster, attributes);

            Action onDeath = () => HandleEnemyDeath(mediator);
            mediator.OnDeath += onDeath;
            _aliveEnemies.Add(mediator, onDeath);
        }

        private void HandleEnemyDeath(CharacterMediator mediator)
        {
            // 죽은 적은 사망 연출 뒤 EnemyPoolReturner 가 풀에 돌려보낸다. 다른 구역이 다시 꺼내 쓸 수 있으니 여기서 구독을 푼다
            if (!_aliveEnemies.TryGetValue(mediator, out Action onDeath)) return;

            mediator.OnDeath -= onDeath;
            _aliveEnemies.Remove(mediator);
            OnProgressChanged?.Invoke();

            if (_aliveEnemies.Count == 0)
            {
                Clear();
            }
        }

        // 살아 있는 적을 전부 풀에 돌려보낸다.
        private void DespawnEnemies()
        {
            foreach (KeyValuePair<CharacterMediator, Action> pair in _aliveEnemies)
            {
                if (pair.Key == null) continue;

                pair.Key.OnDeath -= pair.Value;
                ReleaseEnemy(pair.Key.gameObject);
            }

            _aliveEnemies.Clear();
        }

        private static void ReleaseEnemy(GameObject enemy)
        {
            if (ObjectPoolManager.Instance != null)
                ObjectPoolManager.Instance.Release(enemy);
            else
                Destroy(enemy);
        }

        // 적 순서대로 위치를 돌아가며 쓴다. 위치가 없으면 이 오브젝트 위치를 쓴다.
        private Transform GetSpawnPoint(int spawnIndex)
        {
            if ((_spawnPoints == null) || (_spawnPoints.Length == 0)) return transform;

            Transform spawnPoint = _spawnPoints[spawnIndex % _spawnPoints.Length];
            return (spawnPoint != null) ? spawnPoint : transform;
        }

        /// <summary>몬스터 데이터의 초기값을 SetValue 로 넣는다. 없는 키는 건너뛴다.</summary>
        private static void ApplyMonsterData(SOMonsterData monster, AttributeSet attributes)
        {
            foreach (SOAttributeData.SAttribute entry in monster.Attributes)
            {
                if (!attributes.IsValidTarget(entry.AttributeName))
                {
                    Debug.LogWarning($"[{monster.name}] 어트리뷰트 '{entry.AttributeName}'가 몬스터에 없어 건너뜁니다.", monster);
                    continue;
                }

                attributes.SetValue(entry.AttributeName, entry.Value);
            }
        }
        #endregion
    }
}
