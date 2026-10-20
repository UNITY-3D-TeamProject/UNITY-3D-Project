using System;
using Unity.Behavior;
using Action = System.Action;
using UnityEngine;
using CharacterAnimation;
using Mediator;
using Movement;
using AI.Boss.Arena;

namespace AI.Boss.BossB
{
    /// <summary>
    /// B 의 보스 전용 부가 컨트롤러. 아레나 연결, 다운(사망) 중계와 부활·퇴장, 돌진 이벤트 중계,
    /// 발밑 칸이 무너질 때의 낙하 안전망을 맡는다. 이동·공격 판단은 AIController 와 BT 가 한다.
    /// </summary>
    public class BossBController : MonoBehaviour
    {
        #region Constants
        private const string CURRENT_HP_KEY = "CurrentHp";

        /// <summary>회피 도약 요청이 경직 등으로 무시될 때 다시 시도하는 최대 시간(초).</summary>
        private const float EVADE_RETRY_SECONDS = 1.0f;
        #endregion

        #region Serialized Fields
        [Header("References")]
        [SerializeField] private CharacterMediator _mediator;
        [SerializeField] private AIController _aiController;
        [Tooltip("부활할 때 사망 상태를 되돌릴 애니메이터 제어")]
        [SerializeField] private EnemyAnimator _enemyAnimator;
        [Tooltip("부활할 때 처음부터 다시 돌릴 BT. 비워두면 이 오브젝트에서 찾는다")]
        [SerializeField] private BehaviorGraphAgent _graphAgent;

        [Header("Settings")]
        [Tooltip("발밑 칸이 무너질 때 강제로 쓰는 회피 도약 스킬 이름")]
        [SerializeField] private string _evadeSkillName = "Evade";
        #endregion

        #region Private Fields
        private BossBSkillBase[] _skills;
        private CharacterMotor _motor;
        private IArenaQuery _arena;
        private bool _isEvadePending;
        private float _evadeRetryRemaining;
        #endregion

        #region Properties
        public IArenaQuery Arena => _arena;

        /// <summary>다운(사망 중)인지. Revive 가 풀어 준다.</summary>
        public bool IsDowned { get; private set; }

        /// <summary>
        /// 몸을 띄워 옮기는 스킬이 진행 중인지. 점프 직후 몇 프레임은 아직 접지로 남으므로 모터의 접지 여부만으로는 부족하다.
        /// 이 동안 지나는 칸이 구멍·예고여도 도약을 끊으면 안 된다(BT 조건도 이 값을 본다).
        /// </summary>
        public bool IsBodyInFlight
        {
            get
            {
                foreach (BossBSkillBase skill in _skills)
                {
                    if (skill.IsBodyInFlight) return true;
                }
                return false;
            }
        }
        #endregion

        #region Events
        /// <summary>중재자 OnDeath 때 낸다. 진행이 1번째/2번째 다운을 판단한다.</summary>
        public event Action OnDowned;

        /// <summary>돌진 예고가 시작될 때. 돌진 시작 위치와 수평 방향.</summary>
        public event Action<Vector3, Vector3> OnChargePrepared;

        public event Action OnChargeEnded;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            if (!_mediator) _mediator = GetComponent<CharacterMediator>();
            if (!_aiController) _aiController = GetComponent<AIController>();
            if (!_graphAgent) _graphAgent = GetComponent<BehaviorGraphAgent>();
            _skills = GetComponentsInChildren<BossBSkillBase>(true);
            _motor = GetComponentInChildren<CharacterMotor>();
        }

        private void OnEnable()
        {
            if (_mediator) _mediator.OnDeath += OnDeathCallback;
        }

        private void OnDisable()
        {
            if (_mediator) _mediator.OnDeath -= OnDeathCallback;
            Unsubscribe();
            _isEvadePending = false;
        }

        private void Update()
        {
            if (!_isEvadePending) return;

            _evadeRetryRemaining -= Time.deltaTime;
            if (IsDowned || IsEvading() || (_evadeRetryRemaining <= 0.0f))
            {
                _isEvadePending = false;
                return;
            }

            RequestEvade();
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// 진행이 부른다. 아레나 질의 창구를 연결하고 칸 붕괴를 구독한다. 다시 부르면 이전 구독을 해제하고 바꾼다.
        /// </summary>
        public void Initialize(IArenaQuery arena)
        {
            Unsubscribe();

            _arena = arena;
            if (_arena != null) _arena.OnTileCollapsed += OnTileCollapsedCallback;
        }

        /// <summary>
        /// 1번째 다운 뒤 기상시킨다. HP 를 채우고 사망 상태를 되돌린 뒤 BT 를 처음부터 다시 돌린다.
        /// </summary>
        /// <param name="maxHp">채울 HP</param>
        public void Revive(float maxHp)
        {
            if (_mediator)
            {
                _mediator.SetAttribute(CURRENT_HP_KEY, maxHp);
                _mediator.ResetForReuse();
            }

            // 사망 플래그는 EnemyAnimator.OnEnable 에서만 초기화되고 Animator 도 Death 상태에 남아 있다
            if (_enemyAnimator)
            {
                _enemyAnimator.enabled = false;
                _enemyAnimator.enabled = true;

                Animator animator = _enemyAnimator.GetComponentInParent<Animator>();
                if (animator) animator.Rebind();
            }

            if (_graphAgent) _graphAgent.Restart();

            IsDowned = false;
        }

        /// <summary>
        /// 2번째 다운의 사망 연출이 끝난 뒤 진행이 부른다.
        /// </summary>
        public void Retire()
        {
            gameObject.SetActive(false);
        }

        /// <summary>돌진 스킬이 예고를 시작할 때 부른다.</summary>
        public void NotifyChargePrepared(Vector3 origin, Vector3 direction)
        {
            OnChargePrepared?.Invoke(origin, direction);
        }

        /// <summary>돌진 스킬이 끝날 때(정상·중단) 부른다.</summary>
        public void NotifyChargeEnded()
        {
            OnChargeEnded?.Invoke();
        }
        #endregion

        #region Private Methods
        private void OnDeathCallback()
        {
            IsDowned = true;
            _isEvadePending = false;
            OnDowned?.Invoke();
        }

        /// <summary>
        /// 발밑 칸이 무너지는 순간 진행 중인 스킬을 끊고 회피 도약을 강제한다. 프레임 순서와 무관하게 B 가 떨어지지 않게 하는 안전망이다.
        /// 공중에 떠 있는 동안은 지나는 칸이 무너져도 끼어들지 않는다. 착지 칸은 도약이 스스로 안전한 칸으로 바꾸므로(BossBLeap)
        /// 여기서 끊으면 도약이 목표 칸에 닿기 전에 취소되어 엉뚱한 자리에 떨어진다.
        /// </summary>
        private void OnTileCollapsedCallback(int index)
        {
            if (IsDowned || !_aiController || (_arena == null)) return;
            if (!_arena.TryGetTile(_aiController.Position, out int bodyTile) || (bodyTile != index)) return;
            if (IsEvading() || IsAirborne() || IsBodyInFlight) return;

            foreach (BossBSkillBase skill in _skills)
            {
                if (skill.IsRunning) _aiController.StopSkill(skill.SkillName);
            }

            _isEvadePending = true;
            _evadeRetryRemaining = EVADE_RETRY_SECONDS;
            RequestEvade();
        }

        private void RequestEvade()
        {
            _aiController.ExecuteSkill(_evadeSkillName);

            // 시작했다면 스킬이 몸을 붙잡는다. 경직 중이면 무시되므로 Update 에서 다시 시도한다
            if (_aiController.IsHolding) _isEvadePending = false;
        }

        /// <summary>
        /// 공중에 떠 있는지. 모터를 찾지 못했으면 지면에 있는 것으로 본다(안전망을 끄지 않는다).
        /// </summary>
        private bool IsAirborne()
        {
            return _motor && !_motor.IsGrounded;
        }

        private bool IsEvading()
        {
            foreach (BossBSkillBase skill in _skills)
            {
                if (skill.IsEvade && skill.IsRunning) return true;
            }
            return false;
        }

        private void Unsubscribe()
        {
            if (_arena != null) _arena.OnTileCollapsed -= OnTileCollapsedCallback;
        }
        #endregion
    }
}
