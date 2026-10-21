using System;
using System.Collections.Generic;
using UnityEngine;
using AI;
using Attribute.Core;
using Core.ObjectPool;
using Map.Gimmicks;
using Mediator;

namespace Core.Stage.Security
{
    /// <summary>
    /// 전투 구역 하나의 적 묶음. 순서는 대기(Prepare) → 시작 → 클리어다.
    /// - 대기: 라운드가 이 구역 차례가 되면 Prepare 로 적을 미리 만들어 두고, 플레이어가 들어오기 전까지 제자리에 묶어 둔다 (생성 장면이 보이지 않게).
    /// - 시작: 플레이어가 구역 트리거에 들어오면 묶음을 풀고 OnStarted 를 알린다. 대기 없이 들어오면 그때 만든다.
    /// - 클리어: 만든 적이 모두 죽으면 OnCleared 를 알린다. 적 종류·마리 수·위치를 바꿔도 "실제로 만든 적이 다 죽었는가"로만 판정한다.
    /// 문·발판 등 앞뒤 처리와 사망 리셋 시점은 이 구역을 가진 라운드(MobRoomRound, SpecialMobRound)가 정한다.
    /// 구역 전체를 덮는 콜라이더를 같은 오브젝트에 둔다 (Is Trigger 는 자동으로 켠다).
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class CombatZone : MonoBehaviour
    {
        #region Constants
        // 대기 중에는 매 프레임 이 시간만큼 다시 묶는다. 적의 스킬이 묶음을 덮어쓰거나 풀어도 다음 프레임에 다시 묶인다
        private const float WAITING_HOLD_SECONDS = 0.5f;
        #endregion

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

        /// <summary>살아 있는 적 하나. 사망 구독을 풀 핸들러와 묶어 둘 AI 를 함께 든다.</summary>
        private class AliveEnemy
        {
            public Action OnDeathHandler;
            public AIController Controller;
        }
        #endregion

        #region Serialized Fields
        [Header("Enemies")]
        [Tooltip("이 구역에 만들 적 종류와 마리 수. 적이 하나도 없으면 들어오자마자 클리어된다.")]
        [SerializeField] private SZoneEnemy[] _enemies;

        [Tooltip("적을 만들 위치. 적은 위치의 앞(+Z)을 보고 기다린다. 적이 위치보다 많으면 처음 위치부터 다시 돌아가며 쓴다. 비우면 이 오브젝트 위치에 만든다.")]
        [SerializeField] private Transform[] _spawnPoints;
        #endregion

        #region Private Fields
        private readonly Dictionary<CharacterMediator, AliveEnemy> _aliveEnemies = new Dictionary<CharacterMediator, AliveEnemy>();

        private bool _isPrepared;
        #endregion

        #region Properties
        /// <summary>플레이어가 들어와 싸움이 시작됐는지 여부. 리셋되면 false 로 돌아간다.</summary>
        public bool IsStarted { get; private set; }

        /// <summary>이 구역을 깼는지 여부.</summary>
        public bool IsCleared { get; private set; }

        /// <summary>이번에 실제로 만든 적 수. 만들기 전에는 0.</summary>
        public int TotalCount { get; private set; }

        /// <summary>아직 살아 있는 적 수.</summary>
        public int AliveCount => _aliveEnemies.Count;
        #endregion

        #region Events
        /// <summary>적이 생기거나 죽거나 리셋·클리어되면 호출된다. 화면 갱신용.</summary>
        public event Action OnProgressChanged;

        /// <summary>플레이어가 들어와 싸움이 시작된 순간 이 구역을 넘겨 호출된다.</summary>
        public event Action<CombatZone> OnStarted;

        /// <summary>구역을 깬 순간 이 구역을 넘겨 한 번 호출된다.</summary>
        public event Action<CombatZone> OnCleared;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            GetComponent<Collider>().isTrigger = true;
        }

        private void Update()
        {
            if (!_isPrepared || IsStarted) return;

            HoldWaitingEnemies();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (IsStarted || IsCleared) return;
            if (!GimmickUtility.TryGetPlayer(other, out _)) return;

            StartZone();
        }

        private void OnDestroy()
        {
            foreach (KeyValuePair<CharacterMediator, AliveEnemy> pair in _aliveEnemies)
            {
                if (pair.Key != null) pair.Key.OnDeath -= pair.Value.OnDeathHandler;
            }

            _aliveEnemies.Clear();
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// 이 구역 차례가 되면 적을 미리 만들어 제자리에 묶어 둔다. 이미 만들었거나 깼으면 아무것도 하지 않는다.
        /// </summary>
        public void Prepare()
        {
            if (_isPrepared || IsCleared) return;

            _isPrepared = true;
            SpawnEnemies();

            // 만든 프레임부터 묶어 둔다 (Update 는 다음 프레임부터 돈다)
            HoldWaitingEnemies();

            TotalCount = _aliveEnemies.Count;
            OnProgressChanged?.Invoke();
        }

        /// <summary>
        /// 아직 못 깬 구역이면 남은 적을 없애고 만들기 전 상태로 돌린다. 다시 Prepare 하거나 들어오면 새로 만든다. 깬 구역은 그대로 둔다.
        /// </summary>
        public void ResetIfNotCleared()
        {
            if (IsCleared || !_isPrepared) return;

            DespawnEnemies();
            _isPrepared = false;
            IsStarted = false;
            TotalCount = 0;
            OnProgressChanged?.Invoke();
        }
        #endregion

        #region Private Methods
        private void StartZone()
        {
            Prepare();
            IsStarted = true;

            foreach (AliveEnemy enemy in _aliveEnemies.Values)
            {
                if (enemy.Controller != null) enemy.Controller.ReleaseHold();
            }

            OnStarted?.Invoke(this);

            // 적이 없는 구역(또는 하나도 만들지 못한 구역)은 바로 깬 것으로 본다
            if (_aliveEnemies.Count == 0)
            {
                Clear();
            }
        }

        private void HoldWaitingEnemies()
        {
            foreach (AliveEnemy enemy in _aliveEnemies.Values)
            {
                // 방향을 0 으로 주면 지금 몸이 보는 방향(생성 위치의 앞)을 유지한다
                if (enemy.Controller != null) enemy.Controller.Hold(Vector3.zero, WAITING_HOLD_SECONDS);
            }
        }

        private void Clear()
        {
            IsCleared = true;
            OnCleared?.Invoke(this);
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

            AliveEnemy enemy = new AliveEnemy { Controller = instance.GetComponent<AIController>() };
            enemy.OnDeathHandler = () => HandleEnemyDeath(mediator);
            mediator.OnDeath += enemy.OnDeathHandler;
            _aliveEnemies.Add(mediator, enemy);
        }

        private void HandleEnemyDeath(CharacterMediator mediator)
        {
            // 죽은 적은 사망 연출 뒤 EnemyPoolReturner 가 풀에 돌려보낸다. 다른 구역이 다시 꺼내 쓸 수 있으니 여기서 구독을 푼다
            if (!_aliveEnemies.TryGetValue(mediator, out AliveEnemy enemy)) return;

            mediator.OnDeath -= enemy.OnDeathHandler;
            _aliveEnemies.Remove(mediator);
            OnProgressChanged?.Invoke();

            // 들어오기 전(대기 중)에 문 너머로 다 잡아도 깬 것으로 본다
            if (_aliveEnemies.Count == 0)
            {
                Clear();
            }
        }

        // 살아 있는 적을 전부 풀에 돌려보낸다.
        private void DespawnEnemies()
        {
            foreach (KeyValuePair<CharacterMediator, AliveEnemy> pair in _aliveEnemies)
            {
                if (pair.Key == null) continue;

                pair.Key.OnDeath -= pair.Value.OnDeathHandler;
                if (pair.Value.Controller != null) pair.Value.Controller.ReleaseHold();
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
