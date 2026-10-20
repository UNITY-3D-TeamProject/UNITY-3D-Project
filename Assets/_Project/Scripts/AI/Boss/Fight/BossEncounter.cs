using System;
using Unity.Behavior;
using Action = System.Action;
using UnityEngine;
using Attribute.Core;
using Attribute.Effect;
using Facade.Player;
using Mediator;
using Movement;
using AI.Boss.Arena;
using AI.Boss.BossA;
using AI.Boss.BossB;
using AI.Boss.BT;
using AI.Boss.Logic;

namespace AI.Boss.Fight
{
    /// <summary>
    /// 보스전 진행. 단계·그로기(BossFightModel)와 플레이어 칸 추적(PlayerArenaTracker)을 갖고,
    /// B 의 다운·돌진 이벤트를 받아 A·B 의 블랙보드, A 의 단계·피격 범위, A 선택기 상황에 반영한다.
    /// A 와 B 는 서로 모르고 이 클래스만 둘을 안다. BossFightSpawner 가 생성 직후 Begin 을 부른다.
    /// </summary>
    public class BossEncounter : MonoBehaviour, IBossSituation
    {
        #region Constants
        private const string MAX_HP_KEY = "MaxHp";
        #endregion

        #region Serialized Fields
        [Header("References")]
        [SerializeField] private BossArena _arena;

        [Header("Settings")]
        [SerializeField, Min(0.0f)] private float _groggySeconds = 10.0f;
        [Tooltip("끄면 점프 중에도 착지 예상 칸 대신 지금 서 있는 칸을 쓴다")]
        [SerializeField] private bool _useLandingPrediction = true;

        [Header("Blackboard Variables")]
        [SerializeField] private string _stageVariable = "Stage";
        [SerializeField] private string _isGroggyVariable = "IsGroggy";
        [SerializeField] private string _playerVariable = "Player";
        #endregion

        #region Private Fields
        private BossFightModel _model;
        private PlayerArenaTracker _tracker;
        private BossController _bossA;
        private BossBController _bossB;
        private CharacterMediator _mediatorA;
        private CharacterMediator _mediatorB;
        private BehaviorGraphAgent _graphA;
        private BehaviorGraphAgent _graphB;
        private PlayerFacade _player;
        private CharacterMotor _playerMotor;
        private bool _isBegun;
        private bool _isChargePreparing;
        private bool _chargeDirectionIsRow;
        #endregion

        #region Properties
        public bool IsBegun => _isBegun;

        public EBossStage Stage => _model != null ? _model.Stage : EBossStage.Stage1_1;

        /// <inheritdoc />
        public Transform PlayerBody => _player ? _player.transform : null;

        /// <inheritdoc />
        public CharacterMotor PlayerMotor => _playerMotor;
        #endregion

        #region Events
        public event Action<EBossStage> OnStageChanged;
        public event Action OnBossDefeated;
        #endregion

        #region Unity Lifecycle
        private void Update()
        {
            if (!_isBegun) return;

            SamplePlayer();
            _model.Tick(Time.deltaTime);
        }

        private void OnDisable()
        {
            if (_isBegun) End();
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// 보스전을 시작한다. 아레나·모델·트래커를 처음 상태로 돌리고 A·B 를 초기화하며 이벤트를 구독한다.
        /// </summary>
        public void Begin(BossController bossA, BossBController bossB, PlayerFacade player)
        {
            if (_isBegun) End();

            if (!_arena || !bossA || !bossB)
            {
                Debug.LogError($"[{name}] 아레나 또는 보스가 연결되지 않아 보스전을 시작할 수 없다.", this);
                return;
            }

            _bossA = bossA;
            _bossB = bossB;
            _mediatorA = bossA.GetComponent<CharacterMediator>();
            _mediatorB = bossB.GetComponent<CharacterMediator>();
            _graphA = bossA.GetComponentInChildren<BehaviorGraphAgent>();
            _graphB = bossB.GetComponentInChildren<BehaviorGraphAgent>();

            _arena.ResetAll();
            _model = new BossFightModel(_groggySeconds);
            _tracker = null;
            _isChargePreparing = false;
            SetPlayer(player);

            _bossA.Initialize(this, _arena, new System.Random().Next());
            _bossB.Initialize(_arena);

            _model.OnGroggyStarted += HandleGroggyStarted;
            _model.OnGroggyEnded += HandleGroggyEnded;
            _model.OnStageChanged += HandleStageChanged;
            _bossB.OnDowned += HandleBDowned;
            _bossB.OnChargePrepared += HandleChargePrepared;
            _bossB.OnChargeEnded += HandleChargeEnded;
            if (_mediatorA) _mediatorA.OnDeath += HandleADeath;
            if (_mediatorB) _mediatorB.OnDeathEnded += HandleBDeathEnded;

            _isBegun = true;

            _bossA.SetDamageMode(EBossDamageMode.Blocked);
            WriteStage();
            SetVariable(_graphA, _isGroggyVariable, false);
        }

        /// <summary>
        /// 보스전을 끝낸다. 구독을 해제하고 참조를 비운다. A·B 의 파괴는 호출 측이 한다.
        /// </summary>
        public void End()
        {
            if (_model != null)
            {
                _model.OnGroggyStarted -= HandleGroggyStarted;
                _model.OnGroggyEnded -= HandleGroggyEnded;
                _model.OnStageChanged -= HandleStageChanged;
            }

            if (_bossB)
            {
                _bossB.OnDowned -= HandleBDowned;
                _bossB.OnChargePrepared -= HandleChargePrepared;
                _bossB.OnChargeEnded -= HandleChargeEnded;
            }

            if (_mediatorA) _mediatorA.OnDeath -= HandleADeath;
            if (_mediatorB) _mediatorB.OnDeathEnded -= HandleBDeathEnded;

            _isBegun = false;
            _model = null;
            _tracker = null;
            _bossA = null;
            _bossB = null;
            _mediatorA = null;
            _mediatorB = null;
            _graphA = null;
            _graphB = null;
            _player = null;
            _playerMotor = null;
            _isChargePreparing = false;
        }

        /// <summary>
        /// 플레이어가 다시 만들어졌을 때 교체한다. 위치 추적을 처음부터 다시 하고 블랙보드의 Player 를 갱신한다.
        /// </summary>
        public void SetPlayer(PlayerFacade player)
        {
            _player = player;
            _playerMotor = player ? player.GetComponentInChildren<CharacterMotor>() : null;

            // 중력은 플레이어 모터 값을 따라가야 착지 예측이 맞다
            float gravity = _playerMotor ? _playerMotor.Gravity : 0.0f;
            _tracker = new PlayerArenaTracker(gravity, _arena ? _arena.TileSize : 1.0f);

            if (!_isBegun && (_model == null)) return;

            GameObject body = GetPlayerBodyObject();
            SetVariable(_graphA, _playerVariable, body);
            SetVariable(_graphB, _playerVariable, body);
        }

        /// <inheritdoc />
        public SBossSituation GetSnapshot()
        {
            // 끝났거나(End) 아직 시작 전이면 A 가 쉬도록 Defeated 로 돌려준다
            if ((_model == null) || (_tracker == null)) return new SBossSituation { Stage = EBossStage.Defeated };

            int playerTile = _tracker.TileIndex;
            int landingTile = _useLandingPrediction ? _tracker.PredictLandingTile() : playerTile;

            return new SBossSituation
            {
                Stage = _model.Stage,
                Grid = _arena.Grid,
                PlayerTile = playerTile,
                IsPlayerJumping = _tracker.IsJumping,
                LandingTile = landingTile,
                StayRow = _tracker.StayRow,
                RowStaySeconds = _tracker.RowStaySeconds,
                StayCol = _tracker.StayCol,
                ColStaySeconds = _tracker.ColStaySeconds,
                IsBChargePreparing = _isChargePreparing,
                ChargeDirectionIsRow = _chargeDirectionIsRow,
            };
        }

        /// <inheritdoc />
        public bool TryApplyToPlayer(SOAttributeEffect effect)
        {
            if (!_player || !effect) return false;

            return _player.ApplyEffect(new SEffectContext(_player.EffectTarget, effect));
        }
        #endregion

        #region Private Methods
        private void SamplePlayer()
        {
            if (!_player || (_tracker == null)) return;

            Vector3 local = _arena.transform.InverseTransformPoint(_player.transform.position);
            bool isGrounded = !_playerMotor || _playerMotor.IsGrounded;
            _tracker.Sample(Time.time, local.x, local.z, local.y, 0.0f, isGrounded);
        }

        private void HandleBDowned()
        {
            _isChargePreparing = false;
            _model.NotifyBDown();
        }

        private void HandleChargePrepared(Vector3 origin, Vector3 direction)
        {
            Vector3 local = _arena.transform.InverseTransformDirection(direction);
            _chargeDirectionIsRow = Mathf.Abs(local.z) >= Mathf.Abs(local.x);
            _isChargePreparing = true;
        }

        private void HandleChargeEnded()
        {
            _isChargePreparing = false;
        }

        private void HandleGroggyStarted()
        {
            _bossA.SetStage(_model.Stage);
            _bossA.SetDamageMode(EBossDamageMode.HandsOnly);
            SetVariable(_graphA, _isGroggyVariable, true);
        }

        private void HandleGroggyEnded()
        {
            float maxHp = _mediatorB ? _mediatorB.GetAttribute(MAX_HP_KEY) : 0.0f;
            _bossB.Revive(maxHp);
            SetVariable(_graphA, _isGroggyVariable, false);
        }

        private void HandleStageChanged(EBossStage stage)
        {
            switch (stage)
            {
                case EBossStage.Stage1_2:
                    _bossA.SetStage(stage);
                    _bossA.SetDamageMode(EBossDamageMode.Blocked);
                    break;
                case EBossStage.Final:
                    _bossA.SetStage(stage);
                    _arena.SetStage(stage);
                    _bossA.SetDamageMode(EBossDamageMode.All);
                    break;
            }

            WriteStage();
            OnStageChanged?.Invoke(stage);

            if (stage == EBossStage.Defeated) OnBossDefeated?.Invoke();
        }

        private void HandleADeath()
        {
            if (_graphA) _graphA.End();
            _model.NotifyADeath();
        }

        private void HandleBDeathEnded()
        {
            // 1번째 다운은 그로기 뒤 기상하므로 두고, 2번째 다운(최종 이후)만 퇴장한다
            if (_model.DownCount >= 2) _bossB.Retire();
        }

        private void WriteStage()
        {
            EBossBTStage stage = (EBossBTStage)(int)_model.Stage;
            SetVariable(_graphA, _stageVariable, stage);
            SetVariable(_graphB, _stageVariable, stage);
        }

        private GameObject GetPlayerBodyObject()
        {
            if (!_player) return null;

            // 실제로 움직이는 몸체는 CharacterController 가 붙은 오브젝트다 (GarbageCollectorSpawner 와 같은 방식)
            CharacterController body = _player.transform.root.GetComponentInChildren<CharacterController>();
            return body ? body.gameObject : null;
        }

        private void SetVariable<T>(BehaviorGraphAgent agent, string variableName, T value)
        {
            if (!agent) return;

            if (!agent.SetVariableValue(variableName, value))
            {
                Debug.LogWarning($"[{name}] '{agent.name}' BT 에 블랙보드 변수 '{variableName}' 이 없어 값을 넣지 못했다.", this);
            }
        }
        #endregion
    }
}
