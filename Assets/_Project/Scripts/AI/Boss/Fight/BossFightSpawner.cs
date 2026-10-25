using System;
using System.Collections;
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
    /// Prespawn Round Index 를 정하면 그 라운드에 A 만 미리 만들어 BT 를 멈춘 채 세워 두고, 보스전 라운드가 되면 그 A 로 시작한다.
    /// B 는 보스전 라운드가 되어서야 A 머리 위에 나타나 Spell 을 재생하고 땅에 내려앉은 뒤, 그때 BT 를 켜고 보스전을 시작한다.
    /// 등장 연출 동안 플레이어는 움직이지 못한다. 연출 중 플레이어가 재스폰되면 보스를 다시 만들어 연출도 처음부터 다시 한다.
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
        [Tooltip("이 라운드가 되면 A 를 미리 만들어 BT 를 멈춘 채 세워 둔다. 보스전 라운드(Active Round Index)가 되면 그 A 로 보스전을 시작한다. -1 이면 미리 만들지 않는다.")]
        [SerializeField] private int _prespawnRoundIndex = -1;

        [Header("Entrance")]
        [Tooltip("B 가 나타나는 공중 위치. A 머리에서 이만큼(m) 위다.")]
        [SerializeField, Min(0.0f)] private float _entranceHeightAboveHead = 6.0f;
        [Tooltip("B 가 땅에 내려앉는 순간 플레이어 카메라를 흔드는 세기. 차지샷 쉐이크가 1 이다. 0 이면 흔들지 않는다.")]
        [SerializeField, Min(0.0f)] private float _landingShakeForce = 1.0f;
        #endregion

        #region Private Fields
        private GameObject _bossA;
        private GameObject _bossB;
        private bool _hasSpawned;
        private bool _isPrespawned;
        private bool _isBeginFailed;
        private Coroutine _entranceRoutine;
        private PlayerFacade _lockedPlayer;
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

        /// <summary>보스전 라운드 처리는 부모가 하고, 미리 생성 라운드가 되면 A 를 세워 둔다.</summary>
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

            // 미리 만들어 둔 A 가 없으면 지금 만든다. 어느 쪽이든 B 의 등장 연출이 끝날 때까지 BT 는 멈춰 둔다
            if (!_isPrespawned && !TryCreateBossA()) return;

            _isPrespawned = false;
            SetBossLogicEnabled(_bossA, false);

            // B 는 NavMesh 위(생성 위치)에서 만들어야 에이전트가 생긴다. 공중으로는 같은 프레임에 등장 연출이 옮기므로 땅에 보이지 않는다
            Vector3 hoverPosition = GetEntranceHoverPosition();
            _bossB = Instantiate(_bossBPrefab, _bossBPoint.position, _bossBPoint.rotation);
            ApplyMonsterData(_bossB.GetComponentInChildren<AttributeSet>(true), _bossBData);
            SetBossLogicEnabled(_bossB, false);

            _hasSpawned = true;
            _entranceRoutine = StartCoroutine(CoEnterAndBegin(player, hoverPosition));
        }

        protected override void Despawn()
        {
            if (_entranceRoutine != null)
            {
                StopCoroutine(_entranceRoutine);
                _entranceRoutine = null;
            }

            UnlockPlayer();
            UnsubscribeEntrance();

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

            // 연출 중에 플레이어가 새로 만들어졌으면 새 플레이어도 잠근다
            if (_entranceRoutine != null) LockPlayer(playerFacade);
        }
        #endregion

        #region Private Methods
        // A 만 만들되 BT 를 멈춰 제자리에 세워 둔다. B 는 보스전 라운드에 등장하고, 보스전은 시작하지 않는다.
        private void Prespawn()
        {
            if (!TryCreateBossA()) return;

            SetBossLogicEnabled(_bossA, false);
            _isPrespawned = true;
        }

        // A 를 만들고 몬스터 데이터를 넣는다. 연결·프리팹이 잘못되면 만들지 않고 false 를 돌려준다.
        private bool TryCreateBossA()
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
            ApplyMonsterData(_bossA.GetComponentInChildren<AttributeSet>(true), _bossAData);
            return true;
        }

        // A 머리 위. B 가 나타나는 공중 위치다.
        private Vector3 GetEntranceHoverPosition()
        {
            Transform head = _bossA.GetComponent<BossController>().HeadTransform;
            return head.position + (Vector3.up * _entranceHeightAboveHead);
        }

        // 보스 BT 를 켜고 끈다. 꺼진 BT 는 노드를 돌리지 않아 보스가 제자리에 서 있는다
        private static void SetBossLogicEnabled(GameObject boss, bool isEnabled)
        {
            if (!boss) return;

            foreach (BehaviorGraphAgent graph in boss.GetComponentsInChildren<BehaviorGraphAgent>(true))
            {
                graph.enabled = isEnabled;
            }
        }

        private void LockPlayer(PlayerFacade player)
        {
            UnlockPlayer();

            _lockedPlayer = player;
            if (_lockedPlayer) _lockedPlayer.SetMoveLocked(true);
        }

        private void UnlockPlayer()
        {
            if (_lockedPlayer) _lockedPlayer.SetMoveLocked(false);
            _lockedPlayer = null;
        }

        private void UnsubscribeEntrance()
        {
            if (!_bossB) return;

            BossBController controllerB = _bossB.GetComponent<BossBController>();
            if (controllerB) controllerB.OnEntranceLanded -= HandleEntranceLanded;
        }

        // 내려앉는 순간 카메라를 흔든다
        // [이벤트] BossBController.OnEntranceLanded (CoEnterAndBegin 에서 구독)
        private void HandleEntranceLanded()
        {
            PlayerFacade player = CurrentPlayerFacade;
            if (player && (_landingShakeForce > 0.0f)) player.PlayCameraShake(_landingShakeForce);
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

        #region Coroutines
        // B 의 등장 연출이 끝나면 A·B 의 BT 를 켜고 보스전을 시작한다. 연출 동안 플레이어는 움직이지 못한다.
        private IEnumerator CoEnterAndBegin(PlayerFacade player, Vector3 hoverPosition)
        {
            LockPlayer(player);

            BossBController controllerB = _bossB.GetComponent<BossBController>();
            controllerB.OnEntranceLanded += HandleEntranceLanded;

            // 땅에 내려앉을 자리는 B 생성 위치(_bossBPoint) 다
            bool isPlaying = controllerB.PlayEntrance(hoverPosition, _bossBPoint.position);
            if (!isPlaying)
            {
                Debug.LogWarning($"[{name}] B 등장 연출을 시작하지 못해 연출 없이 보스전을 시작한다.", this);
                _bossB.transform.SetPositionAndRotation(_bossBPoint.position, _bossBPoint.rotation);
            }

            while (controllerB && controllerB.IsEntrancePlaying) yield return null;

            controllerB.OnEntranceLanded -= HandleEntranceLanded;
            UnlockPlayer();
            _entranceRoutine = null;

            SetBossLogicEnabled(_bossA, true);
            SetBossLogicEnabled(_bossB, true);

            _encounter.Begin(_bossA.GetComponent<BossController>(), controllerB, CurrentPlayerFacade);
            if (!_encounter.IsBegun)
            {
                LogSpawnFailureOnce("BossEncounter 가 보스전을 시작하지 못했다.");
                _isBeginFailed = true;
                DestroyBosses();
                _hasSpawned = false;
                yield break;
            }

            _encounter.OnBossDefeated += HandleBossDefeated;
        }
        #endregion
    }
}
