using System;
using System.Collections.Generic;
using UnityEngine;
using AI;
using Core.ObjectPool;
using Map.Gimmicks;
using Mediator;

namespace Core.Stage.Security
{
    /// <summary>
    /// 전투 구역 하나의 적 묶음. 순서는 대기(Prepare) → 시작 → 클리어다.
    /// - 대기: 라운드가 이 구역 차례가 되면 Prepare 로 적을 미리 만들어 두고, 플레이어가 들어오기 전까지 제자리에 묶어 둔다 (생성 장면이 보이지 않게).
    /// - 시작: 플레이어가 구역 트리거에 들어오면 묶음을 풀고 OnStarted 를 알린다. 대기 없이 들어오면 그때 만든다.
    /// - 적: 이 오브젝트의 자식(손자 포함) MonsterSpawner 하나가 적 한 마리다. 몹을 늘리려면 스포너를 복제해 자리만 옮긴다.
    ///   자식 스포너는 씬 시작 때 스스로 소환하지 않고, 대기·시작 때 이 구역이 소환시킨다.
    /// - 클리어: 만든 적이 모두 죽으면 OnCleared 를 알린다. 스포너 수·종류·위치를 바꿔도 "실제로 만든 적이 다 죽었는가"로만 판정한다.
    ///   보상 아이템(_clearDropItem)을 지정하면 마지막으로 죽은 적 자리에 떨어뜨린다.
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
        /// <summary>살아 있는 적 하나. 사망 구독을 풀 핸들러와 묶어 둘 AI 를 함께 든다.</summary>
        private class AliveEnemy
        {
            public Action OnDeathHandler;
            public AIController Controller;
        }
        #endregion

        #region Serialized Fields
        [Header("Clear Drop")]
        [Tooltip("구역의 마지막 적이 죽으면 그 자리에 반드시 떨어뜨릴 아이템 프리팹 (닿으면 획득되는 것). 비우면 떨어뜨리지 않는다.")]
        [SerializeField] private GameObject _clearDropItem;
        [Tooltip("아이템이 떨어지는 위치의 높이 보정(m). 적 발 위치 기준")]
        [SerializeField] private float _clearDropHeight = 0.5f;
        [Tooltip("안 먹힌 아이템이 사라지기까지의 시간(초)")]
        [SerializeField, Min(0.1f)] private float _clearDropLifetime = 15.0f;
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

            // 자식 스포너의 Start 보다 먼저 돈다. 소환은 이 구역 차례가 되면 Prepare 에서 한다
            foreach (MonsterSpawner spawner in GetComponentsInChildren<MonsterSpawner>(true))
            {
                spawner.ShouldSpawnOnStart = false;
            }
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

        // 비활성 구역에 Prepare 가 먼저 불릴 수도 있으니 Awake 에서 모아 두지 않고 소환할 때마다 찾는다
        private void SpawnEnemies()
        {
            foreach (MonsterSpawner spawner in GetComponentsInChildren<MonsterSpawner>(true))
            {
                SpawnEnemy(spawner);
            }
        }

        // 스포너로 한 마리를 만들고 사망을 구독한다. 몬스터 값 적용은 스포너가 한다.
        private void SpawnEnemy(MonsterSpawner spawner)
        {
            GameObject instance = spawner.Spawn();
            if (instance == null) return;

            if (!instance.TryGetComponent(out CharacterMediator mediator))
            {
                Debug.LogError($"[{name}] '{instance.name}' 루트에 CharacterMediator 가 없어 적으로 쓸 수 없습니다.", spawner);
                ReleaseEnemy(instance);
                return;
            }

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
                DropClearItem(mediator.transform.position);
                Clear();
            }
        }

        // 마지막으로 죽은 적 자리에 깬 보상 아이템을 떨어뜨린다. 아이템은 획득되면 스스로 사라진다 (EnemyItemDropper 와 같은 방식)
        private void DropClearItem(Vector3 position)
        {
            if (_clearDropItem == null) return;

            GameObject item = Instantiate(_clearDropItem, position + (Vector3.up * _clearDropHeight), Quaternion.identity);
            Destroy(item, _clearDropLifetime);
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
        #endregion
    }
}
