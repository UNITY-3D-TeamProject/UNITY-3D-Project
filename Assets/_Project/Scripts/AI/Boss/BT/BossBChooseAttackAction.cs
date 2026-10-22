using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using AI.BT;
using AI.Boss.BossB;
using AI.Boss.Logic;

namespace AI.Boss.BT
{
    /// <summary>
    /// B 의 다음 공격을 BossBAttackRule 로 고르고 결과를 블랙보드 열거형에 쓴 뒤 성공한다.
    /// 쿨타임 준비 시각과 보스전 단계는 블랙보드에서 읽는다. 대상이 안 보이거나 아레나가 없거나 스킬이 진행 중이면 None 으로 쓴다.
    /// 대상을 처음 발견해 고르는 첫 공격만 규칙과 무관하게 도약이다(첫 조우).
    /// 사이의 구멍 때문에 고를 공격이 없고 돌진이 준비됐으면, 대상까지 직선이 깨끗한 칸으로 걸어가는 동안 Running 이다가
    /// 직선이 깨끗해지면 다시 골라 성공한다. 정체·도착·그 칸이 위험해지면 None 으로 성공한다.
    /// </summary>
    [Serializable, GeneratePropertyBag]
    [NodeDescription(
        name: "B 공격 고르기",
        description: "거리·쿨타임·구멍 유무로 콤보/돌진/도약 중 하나를 골라 블랙보드에 쓴다.",
        story: "[Attack] 을 고른다 (콤보 거리 [ComboRange], 단계 [Stage], 돌진 준비 [ChargeReadyTime], 도약 준비 [LeapReadyTime])",
        category: "Action/보스",
        id: "ab65b6c3419e433cb8692c0039f7da78")]
    public partial class BossBChooseAttackAction : AIActionBase
    {
        #region Serialized Fields
        [SerializeReference] public BlackboardVariable<EBossBTAttack> Attack;
        [SerializeReference] public BlackboardVariable<float> ComboRange;
        [SerializeReference] public BlackboardVariable<float> ChargeReadyTime;
        [SerializeReference] public BlackboardVariable<float> LeapReadyTime;
        [SerializeReference] public BlackboardVariable<EBossBTStage> Stage;
        #endregion

        #region Constants
        private const int MAX_FIRST_LEAP_TRIES = 3;
        #endregion

        #region Private Fields
        private BossBController _boss;
        private BossBLeap _leap;
        private bool _hasEncountered;
        private int _firstLeapTries;
        // 직선이 깨끗한 자리로 걸어가는 중인 칸. 아니면 -1
        [CreateProperty] private int _clearLineTile = -1;
        #endregion

        #region Node Lifecycle
        /// <inheritdoc />
        protected override Status OnStart()
        {
            if (!TryResolveController()) return Status.Failure;

            if (!_boss) _boss = this.GameObject.GetComponent<BossBController>();

            Attack.Value = EBossBTAttack.None;
            _clearLineTile = -1;

            Transform target = Controller.VisibleTarget;
            if (!_boss || (_boss.Arena == null) || !target) return Status.Success;

            // 회피 도약 등 다른 스킬이 진행 중이면 공격은 시작되지 못하므로 쿨타임을 쓰지 않게 고르지 않는다
            if (_boss.IsSkillRunning) return Status.Success;

            bool hasHole = _boss.Arena.HasHoleBetween(Controller.Position, target.position);

            ResolveFirstEncounter();

            EBossBAttack choice;
            if (_hasEncountered)
            {
                choice = ChooseByRule(hasHole);
            }
            else
            {
                // 첫 조우는 단계와 쿨타임에 상관없이 도약으로 시작한다
                _firstLeapTries++;
                choice = EBossBAttack.Leap;
            }

            Attack.Value = (EBossBTAttack)(int)choice;

            // 구멍에 막혀 고를 공격이 없으면 구멍에 막히지 않는 방향으로 돌진할 수 있는 칸으로 먼저 옮긴다
            bool isBlockedByHole = (choice == EBossBAttack.None) && hasHole && IsChargeReady();
            if (isBlockedByHole && TryMoveToClearLine(target)) return Status.Running;

            return Status.Success;
        }

        /// <inheritdoc />
        protected override Status OnUpdate()
        {
            Transform target = Controller.VisibleTarget;
            if (!target || _boss.IsSkillRunning) return Status.Success;

            if (!_boss.Arena.HasHoleBetween(Controller.Position, target.position))
            {
                Attack.Value = (EBossBTAttack)(int)ChooseByRule(false);
                return Status.Success;
            }

            bool hasFailed = (_clearLineTile < 0) || Controller.IsStuck || _boss.Arena.IsHoleOrTelegraphed(_clearLineTile);
            if (hasFailed || Controller.HasArrived) return Status.Success;

            return Status.Running;
        }

        /// <inheritdoc />
        protected override void OnEnd()
        {
            if ((_clearLineTile >= 0) && Controller) Controller.StopMove();
            _clearLineTile = -1;
        }
        #endregion

        #region Private Methods
        private EBossBAttack ChooseByRule(bool hasHole)
        {
            bool isLeapOpen = (Stage.Value == EBossBTStage.Stage1_2) || (Stage.Value == EBossBTStage.Final);

            return BossBAttackRule.Choose(
                Controller.DistanceToTarget,
                ComboRange.Value,
                IsChargeReady(),
                Time.time >= LeapReadyTime.Value,
                isLeapOpen,
                hasHole);
        }

        private bool IsChargeReady()
        {
            return Time.time >= ChargeReadyTime.Value;
        }

        /// <summary>
        /// 대상까지 직선이 깨끗한 가장 가까운 칸으로 걷기 시작한다.
        /// </summary>
        /// <returns>칸이 있고 경로를 잡았으면 true</returns>
        private bool TryMoveToClearLine(Transform target)
        {
            int tile = _boss.Arena.FindClearLineTile(Controller.Position, target.position);
            if ((tile < 0) || !Controller.MoveTo(_boss.Arena.GetTileCenter(tile))) return false;

            _clearLineTile = tile;
            return true;
        }

        /// <summary>
        /// 첫 조우 도약이 실제로 시작됐거나 시도 한도를 넘겼으면 첫 조우를 끝낸다.
        /// 도약이 시작되지 못했으면(대상 놓침·착지 칸 없음 등) 다음 선택에서 다시 도약을 고른다.
        /// </summary>
        private void ResolveFirstEncounter()
        {
            if (_hasEncountered) return;

            if (!_leap) _leap = FindFirstEncounterLeap();

            bool hasLeapStarted = _leap && _leap.HasStarted;
            if (hasLeapStarted || (_firstLeapTries >= MAX_FIRST_LEAP_TRIES)) _hasEncountered = true;
        }

        private BossBLeap FindFirstEncounterLeap()
        {
            foreach (BossBLeap leap in _boss.GetComponentsInChildren<BossBLeap>(true))
            {
                if (!leap.IsEvade) return leap;
            }

            return null;
        }
        #endregion
    }
}
