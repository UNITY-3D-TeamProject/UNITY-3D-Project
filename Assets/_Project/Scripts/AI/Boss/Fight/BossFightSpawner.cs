using System;
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
        #endregion

        #region Private Fields
        private GameObject _bossA;
        private GameObject _bossB;
        private bool _hasSpawned;
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
            if (_hasSpawned) Despawn();

            base.OnDisable();
        }
        #endregion

        #region Protected Methods
        protected override void Spawn()
        {
            PlayerFacade player = CurrentPlayerFacade;
            if (!_bossAPrefab || !_bossBPrefab || !_bossAPoint || !_bossBPoint ||
                !_bossAData || !_bossBData || !_encounter)
            {
                LogSpawnFailureOnce("보스 프리팹·생성 위치·몬스터 데이터·진행(BossEncounter)이 연결되지 않아 보스를 만들 수 없다.");
                return;
            }

            if (!player)
            {
                LogSpawnFailureOnce("플레이어가 없어 보스를 만들 수 없다.");
                return;
            }

            _bossA = Instantiate(_bossAPrefab, _bossAPoint.position, _bossAPoint.rotation);
            _bossB = Instantiate(_bossBPrefab, _bossBPoint.position, _bossBPoint.rotation);

            AttributeSet attributesA = _bossA.GetComponentInChildren<AttributeSet>(true);
            AttributeSet attributesB = _bossB.GetComponentInChildren<AttributeSet>(true);
            BossController controllerA = _bossA.GetComponent<BossController>();
            BossBController controllerB = _bossB.GetComponent<BossBController>();

            if (!attributesA || !attributesB || !controllerA || !controllerB)
            {
                LogSpawnFailureOnce("A 프리팹에 BossController / AttributeSet, B 프리팹에 BossBController / AttributeSet 이 없어 보스를 만들 수 없다.");
                DestroyBosses();
                return;
            }

            ApplyMonsterData(attributesA, _bossAData);
            ApplyMonsterData(attributesB, _bossBData);

            _encounter.Begin(controllerA, controllerB, player);
            if (!_encounter.IsBegun)
            {
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
        }

        protected override void OnPlayerSpawned(PlayerFacade playerFacade)
        {
            if (_hasSpawned && _encounter) _encounter.SetPlayer(playerFacade);
        }
        #endregion

        #region Private Methods
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
