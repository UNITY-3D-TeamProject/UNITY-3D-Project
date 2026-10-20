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
    /// B 의 다음 공격을 BossBAttackRule 로 고르고 결과를 블랙보드 열거형에 쓴 뒤 즉시 성공한다.
    /// 쿨타임 준비 시각과 보스전 단계는 블랙보드에서 읽는다. 대상이 안 보이거나 아레나가 없으면 None 으로 쓴다.
    /// 대상을 처음 발견해 고르는 첫 공격만 규칙과 무관하게 도약이다(첫 조우).
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
        #endregion

        #region Node Lifecycle
        /// <inheritdoc />
        protected override Status OnStart()
        {
            if (!TryResolveController()) return Status.Failure;

            if (!_boss) _boss = this.GameObject.GetComponent<BossBController>();

            Attack.Value = EBossBTAttack.None;

            Transform target = Controller.VisibleTarget;
            if (!_boss || (_boss.Arena == null) || !target) return Status.Success;

            bool hasHole = _boss.Arena.HasHoleBetween(Controller.Position, target.position);
            bool isLeapOpen = (Stage.Value == EBossBTStage.Stage1_2) || (Stage.Value == EBossBTStage.Final);

            ResolveFirstEncounter();

            EBossBAttack choice;
            if (_hasEncountered)
            {
                choice = BossBAttackRule.Choose(
                    Controller.DistanceToTarget,
                    ComboRange.Value,
                    Time.time >= ChargeReadyTime.Value,
                    Time.time >= LeapReadyTime.Value,
                    isLeapOpen,
                    hasHole);
            }
            else
            {
                // 첫 조우는 단계와 쿨타임에 상관없이 도약으로 시작한다
                _firstLeapTries++;
                choice = EBossBAttack.Leap;
            }

            Attack.Value = (EBossBTAttack)(int)choice;
            return Status.Success;
        }
        #endregion

        #region Private Methods
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
