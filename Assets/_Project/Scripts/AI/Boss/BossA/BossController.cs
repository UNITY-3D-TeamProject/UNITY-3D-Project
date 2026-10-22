using System;
using UnityEngine;
using Mediator;
using Mediator.SubMediators;
using Skill.Core;
using AI.Boss.Arena;
using AI.Boss.Logic;

namespace AI.Boss.BossA
{
    /// <summary>
    /// A 의 컨트롤러. 패턴을 고르고(BossAPatternSelector) 스킬 이름으로 실행을 요청하며, 손·어깨 포·피격 범위를 묶는다.
    /// 어깨 포는 패턴과 따로, 최종 단계에서 일정 간격마다 좌·우 번갈아 플레이어 위치로 쏜다.
    /// AIController 와 달리 이동이 없고, BT 노드는 이 클래스의 공개 API 에만 의존한다.
    /// </summary>
    public class BossController : MonoBehaviour, ISkillRequestController
    {
        #region Constants
        public const string SKILL_A1 = "A1";
        public const string SKILL_A1_DOUBLE = "A1Double";
        public const string SKILL_A2 = "A2";
        public const string SKILL_A4 = "A4";
        public const string SKILL_A5 = "A5";
        public const string SKILL_A6 = "A6";
        public const string SKILL_GROGGY_PALM = "GroggyPalm";
        #endregion

        #region Serialized Fields
        [Header("References")]
        [SerializeField] private CharacterMediator _mediator;
        [SerializeField] private BossHand _leftHand;
        [SerializeField] private BossHand _rightHand;
        [SerializeField] private BossHitRelay[] _relays;
        [SerializeField] private BossShoulderCannon _leftCannon;
        [SerializeField] private BossShoulderCannon _rightCannon;

        [Header("Settings")]
        [Tooltip("최종 단계 손 패턴 예고 시간 배율")]
        [SerializeField, Min(0.0f)] private float _telegraphScaleFinal = 0.8f;
        [Tooltip("1페이즈에서 손이 내려친 자리에 머무는 시간(초)")]
        [SerializeField, Min(0.0f)] private float _handStayPhase1 = 2.0f;
        [Tooltip("최종에서 손이 머무는 시간(초). 0 이면 바로 회수한다")]
        [SerializeField, Min(0.0f)] private float _handStayFinal = 0.0f;
        [Tooltip("그로기 중 HP 가 내려가지 않는 최대 HP 대비 비율")]
        [SerializeField, Range(0.0f, 1.0f)] private float _groggyHpFloorRatio = 0.5f;
        [SerializeField, Min(0.0f)] private float _restSeconds = 1.0f;
        [Tooltip("임시값. A4 가 아닌 패턴이 이 수만큼 끝까지 이어지면 상황 규칙보다 먼저 A4(1-2·최종). 0 이면 끈다")]
        [SerializeField, Min(0)] private int _grabGuaranteeCount = 3;
        [Tooltip("임시값. 최종 단계에서 어깨 포를 플레이어 위치로 쏘는 간격(초). 첫 발도 최종 진입 후 이만큼 지나서 쏜다")]
        [SerializeField, Min(0.1f)] private float _cannonIntervalFinal = 6.0f;
        #endregion

        #region Private Fields
        private Action<string> _requestExecuteSkill;
        private Action<string> _requestStopSkill;
        private BossAPatternSelector _selector;
        private SkillBase _runningSkill;
        private bool _isSelectedPattern;
        private bool _isResting;
        private float _restRemaining;
        private bool _isCannonActive;
        private float _cannonRemaining;
        private bool _isNextCannonLeft = true;
        #endregion

        #region Properties
        public SBossAPatternChoice CurrentChoice { get; private set; }
        public IBossArena Arena { get; private set; }
        public IBossSituation Situation { get; private set; }

        /// <summary>최종 단계면 <c>_telegraphScaleFinal</c>, 아니면 1.</summary>
        public float TelegraphScale { get; private set; } = 1.0f;

        /// <summary>손이 내려친 자리에 머무는 시간(초). 현재 단계 값.</summary>
        public float HandStayDuration { get; private set; }

        /// <summary>패턴(또는 쉼)이 진행 중인지.</summary>
        public bool IsPatternRunning => _runningSkill || _isResting;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            if (!_mediator) _mediator = GetComponent<CharacterMediator>();

            HandStayDuration = _handStayPhase1;
        }

        private void OnEnable()
        {
            if (_mediator) _mediator.OnDeath += OnDeath;
        }

        private void OnDisable()
        {
            if (_mediator) _mediator.OnDeath -= OnDeath;
            CancelPattern();
            CancelCannons();
        }

        private void Update()
        {
            TickCannon(Time.deltaTime);

            if (!_isResting) return;

            _restRemaining -= Time.deltaTime;
            if (_restRemaining <= 0.0f) _isResting = false;
        }
        #endregion

        #region ISkillRequestController
        /// <inheritdoc />
        public void SetRequestExecuteSkill(Action<string> requestExecuteSkill)
        {
            if (requestExecuteSkill == null) return;
            _requestExecuteSkill = requestExecuteSkill;
        }

        /// <inheritdoc />
        public void ClearRequestExecuteSkill()
        {
            _requestExecuteSkill = null;
        }

        /// <inheritdoc />
        public void SetRequestStopSkill(Action<string> requestStopSkill)
        {
            if (requestStopSkill == null) return;
            _requestStopSkill = requestStopSkill;
        }

        /// <inheritdoc />
        public void ClearRequestStopSkill()
        {
            _requestStopSkill = null;
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// 진행이 한 번만 부른다. 선택기를 만들고 상황·아레나 창구를 연결한다.
        /// </summary>
        public void Initialize(IBossSituation situation, IBossArena arena, int randomSeed)
        {
            if (_selector != null) return;

            Situation = situation;
            Arena = arena;
            _selector = new BossAPatternSelector(new System.Random(randomSeed), grabGuaranteeCount: _grabGuaranteeCount);
        }

        /// <summary>
        /// 단계가 바뀔 때 부른다. 진행 중인 패턴·포격을 취소하고 예고 배율·머묾 시간·선택기 기록을 갱신한다.
        /// 최종 단계면 어깨 포 주기 발사를 처음부터 시작하고, 아니면 끈다.
        /// </summary>
        public void SetStage(EBossStage stage)
        {
            bool isFinal = stage == EBossStage.Final;
            TelegraphScale = isFinal ? _telegraphScaleFinal : 1.0f;
            HandStayDuration = isFinal ? _handStayFinal : _handStayPhase1;

            CancelPattern();
            CancelCannons();
            _isCannonActive = isFinal;
            _cannonRemaining = _cannonIntervalFinal;
            _isNextCannonLeft = true;
            _selector?.ResetForStage();
        }

        /// <summary>
        /// 피격 범위를 정한다. 그로기(HandsOnly)에서만 HP 하한이 걸린다.
        /// </summary>
        public void SetDamageMode(EBossDamageMode mode)
        {
            float hpFloor = (mode == EBossDamageMode.HandsOnly) ? _groggyHpFloorRatio : 0.0f;

            if (_relays == null) return;

            foreach (BossHitRelay relay in _relays)
            {
                if (!relay) continue;

                relay.SetAllowed(IsAllowed(mode, relay.Part));
                relay.SetHpFloor(hpFloor);
            }
        }

        /// <summary>
        /// 선택기로 다음 패턴을 고르고 시작한다. Rest 면 쉼을 시작한다.
        /// 쉼이 아닌데 IsPatternRunning 이 false 면 스킬이 시작되지 못한 것이다.
        /// </summary>
        public EBossAPattern TryStartNextPattern()
        {
            if (_selector == null || Situation == null) return EBossAPattern.Rest;

            CancelPattern();

            SBossSituation snapshot = Situation.GetSnapshot();
            CurrentChoice = _selector.Choose(in snapshot);
            _isSelectedPattern = true;

            EBossAPattern pattern = CurrentChoice.Pattern;
            if (pattern == EBossAPattern.Rest)
            {
                _isResting = true;
                _restRemaining = _restSeconds;
                return pattern;
            }

            _requestExecuteSkill?.Invoke(GetSkillName(pattern));
            return pattern;
        }

        /// <summary>
        /// 선택기를 거치지 않고 이름으로 패턴(그로기 손바닥)을 시작한다.
        /// </summary>
        /// <returns>스킬이 시작되었으면 true</returns>
        public bool TryStartPattern(string skillName)
        {
            CancelPattern();

            CurrentChoice = default;
            _isSelectedPattern = false;
            _requestExecuteSkill?.Invoke(skillName);
            return IsPatternRunning;
        }

        /// <summary>
        /// 진행 중인 패턴(또는 쉼)을 취소한다. 예고를 끄고 손을 회수한다. 어깨 포격은 패턴과 따로 돌아 끊지 않는다.
        /// </summary>
        public void CancelPattern()
        {
            _isResting = false;

            SkillBase skill = _runningSkill;
            _runningSkill = null;
            if (skill) _requestStopSkill?.Invoke(skill.SkillName);

            if (_leftHand) _leftHand.CancelAndRecall();
            if (_rightHand) _rightHand.CancelAndRecall();
        }

        /// <summary>
        /// 발사 중인 어깨 포격을 취소한다. 예고를 끄고 피해·콜백을 버린다.
        /// </summary>
        public void CancelCannons()
        {
            if (_leftCannon) _leftCannon.Cancel();
            if (_rightCannon) _rightCannon.Cancel();
        }

        public BossHand GetHand(bool isLeft)
        {
            return isLeft ? _leftHand : _rightHand;
        }

        /// <summary>
        /// 스킬이 시작될 때 부른다.
        /// </summary>
        public void BeginPattern(SkillBase skill)
        {
            _runningSkill = skill;
        }

        /// <summary>
        /// 스킬이 끝날 때 부른다. 끝까지 마쳤으면 선택기에 알린다(A4 연속 금지 기록).
        /// 취소로 이미 정리된 스킬의 호출은 무시한다.
        /// </summary>
        /// <param name="isFinished">끝까지 마쳤으면 true, 취소·중단이면 false</param>
        public void EndPattern(SkillBase skill, bool isFinished)
        {
            if (_runningSkill != skill) return;

            _runningSkill = null;
            if (isFinished && _isSelectedPattern) _selector?.NotifyPatternFinished(CurrentChoice.Pattern);
        }
        #endregion

        #region Private Methods
        private void OnDeath()
        {
            CancelPattern();
            CancelCannons();
            _isCannonActive = false;
        }

        private void TickCannon(float deltaTime)
        {
            if (!_isCannonActive) return;

            _cannonRemaining -= deltaTime;
            if (_cannonRemaining > 0.0f) return;

            _cannonRemaining = _cannonIntervalFinal;
            FireNextCannon();
        }

        private void FireNextCannon()
        {
            if ((Situation == null) || (Arena == null)) return;

            Transform body = Situation.PlayerBody;
            if (!body) return;

            BossShoulderCannon cannon = _isNextCannonLeft ? _leftCannon : _rightCannon;
            _isNextCannonLeft = !_isNextCannonLeft;
            if (!cannon) return;

            // 플레이어가 공중에 있어도 예고 원은 바닥 높이에 띄운다
            Vector3 point = body.position;
            point.y = Arena.GetTileCenter(0).y;
            cannon.Fire(point, null);
        }

        private static bool IsAllowed(EBossDamageMode mode, EBossHitPart part)
        {
            switch (mode)
            {
                case EBossDamageMode.All:
                    return true;
                case EBossDamageMode.HandsOnly:
                    return part == EBossHitPart.Hand;
                default:
                    return false;
            }
        }

        private static string GetSkillName(EBossAPattern pattern)
        {
            switch (pattern)
            {
                case EBossAPattern.A1: return SKILL_A1;
                case EBossAPattern.A1Double: return SKILL_A1_DOUBLE;
                case EBossAPattern.A2: return SKILL_A2;
                case EBossAPattern.A4: return SKILL_A4;
                case EBossAPattern.A5Plus:
                case EBossAPattern.A5Cross: return SKILL_A5;
                case EBossAPattern.A6: return SKILL_A6;
                default: return string.Empty;
            }
        }
        #endregion
    }
}
