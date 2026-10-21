using System;
using Unity.Behavior;
using Action = System.Action;
using UnityEngine;
using AI.RoundSpawning;
using AI.Boss.BossA;
using AI.Boss.BossB;
using Attribute.Core;
using Core;
using Facade.Player;

namespace AI.Boss.Fight
{
    /// <summary>
    /// 지정한 라운드에 보스 A·B 를 만들고 BossEncounter 에 넘겨 보스전을 시작한다.
    /// 라운드가 끝나거나 플레이어가 재스폰되면 전부 제거 후 다시 만든다.
    /// Prespawn Round Index 를 정하면 그 라운드에 보스를 미리 만들어 BT 를 멈춘 채 세워 두고, 보스전 라운드가 되면 그 보스로 시작한다.
    /// </summary>
    public class BossFightSpawner : RoundSpawnerBase
    {
        #region Serialized Fields
        [Header("References")]
        [SerializeField] private GameObject _bossAPrefab;
        [SerializeField] private GameObject _bossBPrefab;
        [SerializeField] private Transform _bossAPoint;
        [SerializeField] private Transform _bossBPoint;
        [Tooltip("A 초기값(HP 등). 생성 직후 SetValue 로 넣는다.")]
        [SerializeField] private SOMonsterData _bossAData;
        [Tooltip("B 초기값(HP, 이동 속도 등). 생성 직후 SetValue 로 넣는다.")]
        [SerializeField] private SOMonsterData _bossBData;
        [SerializeField] private BossEncounter _encounter;

        [Header("Prespawn")]
        [Tooltip("이 라운드가 되면 보스를 미리 만들어 BT 를 멈춘 채 세워 둔다. 보스전 라운드(Active Round Index)가 되면 그 보스로 보스전을 시작한다. -1 이면 미리 만들지 않는다.")]
        [SerializeField] private int _prespawnRoundIndex = -1;
        #endregion

        #region Private Fields
        private GameObject _bossA;
        private GameObject _bossB;
        private bool _hasSpawned;
        private bool _isPrespawned;
        private bool _isBeginFailed;
        #endregion

        #region Properties
        protected override bool IsSpawned => _hasSpawned;
        #endregion

        #region Events
        /// <summary>A 가 쓰러져 보스전이 끝났을 때. 이후 처리는 맵 담당이 한다.</summary>
        public event Action OnBossDefeated;
        #endregion

        #region Unity Lifecycle
        /// <summary>비활성화되면 보스를 남기지 않는다. 다시 켜지면 라운드 조건에 맞을 때 새로 만든다.</summary>
        protected override void OnDisable()
        {
            if (_hasSpawned || _isPrespawned) Despawn();
            _isBeginFailed = false;

            base.OnDisable();
        }

        /// <summary>보스전 라운드 처리는 부모가 하고, 미리 생성 라운드가 되면 보스를 세워 둔다.</summary>
        protected override void Update()
        {
            base.Update();

            bool isPrespawnRound = StageManager && StageManager.IsRunning &&
                                   (StageManager.CurrentRoundIndex == _prespawnRoundIndex);
            if (isPrespawnRound && !_hasSpawned && !_isPrespawned) Prespawn();
        }
        #endregion

        #region Protected Methods
        protected override void Spawn()
        {
            if (_isBeginFailed) return;

            PlayerFacade player = CurrentPlayerFacade;
            if (!player)
            {
                LogSpawnFailureOnce("플레이어가 없어 보스를 만들 수 없다.");
                return;
            }

            // 미리 만들어 둔 보스가 있으면 그 보스로 시작한다
            if (!_isPrespawned && !TryCreateBosses()) return;

            _isPrespawned = false;
            SetBossLogicEnabled(true);

            BossController controllerA = _bossA.GetComponent<BossController>();
            BossBController controllerB = _bossB.GetComponent<BossBController>();

            _encounter.Begin(controllerA, controllerB, player);
            if (!_encounter.IsBegun)
            {
                LogSpawnFailureOnce("BossEncounter 가 보스전을 시작하지 못했다.");
                _isBeginFailed = true;
                DestroyBosses();
                return;
            }

            _encounter.OnBossDefeated += HandleBossDefeated;
            _hasSpawned = true;
        }

        protected override void Despawn()
        {
            if (_encounter)
            {
                _encounter.OnBossDefeated -= HandleBossDefeated;
                _encounter.End();
            }

            DestroyBosses();
            _hasSpawned = false;
            _isPrespawned = false;
        }

        protected override void OnPlayerSpawned(PlayerFacade playerFacade)
        {
            if (_hasSpawned && _encounter) _encounter.SetPlayer(playerFacade);
        }
        #endregion

        #region Private Methods
        // 보스를 만들되 BT 를 멈춰 제자리에 세워 둔다. 보스전은 시작하지 않는다.
        private void Prespawn()
        {
            if (!TryCreateBosses()) return;

            SetBossLogicEnabled(false);
            _isPrespawned = true;
        }

        // 보스 A·B 를 만들고 몬스터 데이터를 넣는다. 연결·프리팹이 잘못되면 만들지 않고 false 를 돌려준다.
        private bool TryCreateBosses()
        {
            if (!_bossAPrefab || !_bossBPrefab || !_bossAPoint || !_bossBPoint ||
                !_bossAData || !_bossBData || !_encounter)
            {
                LogSpawnFailureOnce("보스 프리팹·생성 위치·몬스터 데이터·진행(BossEncounter)이 연결되지 않아 보스를 만들 수 없다.");
                return false;
            }

            // 만들기 전에 프리팹을 검사한다. 실패해도 매 프레임 재시도되므로 인스턴스를 만들었다 지우지 않는다
            bool isPrefabValid = _bossAPrefab.GetComponent<BossController>() &&
                                 _bossAPrefab.GetComponentInChildren<AttributeSet>(true) &&
                                 _bossBPrefab.GetComponent<BossBController>() &&
                                 _bossBPrefab.GetComponentInChildren<AttributeSet>(true);
            if (!isPrefabValid)
            {
                LogSpawnFailureOnce("A 프리팹에 BossController / AttributeSet, B 프리팹에 BossBController / AttributeSet 이 없어 보스를 만들 수 없다.");
                return false;
            }

            _bossA = Instantiate(_bossAPrefab, _bossAPoint.position, _bossAPoint.rotation);
            _bossB = Instantiate(_bossBPrefab, _bossBPoint.position, _bossBPoint.rotation);

            ApplyMonsterData(_bossA.GetComponentInChildren<AttributeSet>(true), _bossAData);
            ApplyMonsterData(_bossB.GetComponentInChildren<AttributeSet>(true), _bossBData);
            return true;
        }

        // 보스 BT 를 켜고 끈다. 꺼진 BT 는 노드를 돌리지 않아 보스가 제자리에 서 있는다
        private void SetBossLogicEnabled(bool isEnabled)
        {
            foreach (GameObject boss in new[] { _bossA, _bossB })
            {
                if (!boss) continue;

                foreach (BehaviorGraphAgent graph in boss.GetComponentsInChildren<BehaviorGraphAgent>(true))
                {
                    graph.enabled = isEnabled;
                }
            }
        }

        private void HandleBossDefeated()
        {
            OnBossDefeated?.Invoke();
        }

        private void DestroyBosses()
        {
            if (_bossA) Destroy(_bossA);
            if (_bossB) Destroy(_bossB);
            _bossA = null;
            _bossB = null;
        }

        /// <summary>몬스터 데이터의 초기값을 SetValue 로 넣는다. 없는 키는 건너뛴다.</summary>
        private static void ApplyMonsterData(AttributeSet attributes, SOMonsterData data)
        {
            foreach (SOAttributeData.SAttribute entry in data.Attributes)
            {
                if (!attributes.IsValidTarget(entry.AttributeName))
                {
                    Debug.LogWarning($"[{data.name}] 어트리뷰트 '{entry.AttributeName}'가 보스에 없어 건너뜁니다.", data);
                    continue;
                }

                attributes.SetValue(entry.AttributeName, entry.Value);
            }
        }
        #endregion
    }
}
