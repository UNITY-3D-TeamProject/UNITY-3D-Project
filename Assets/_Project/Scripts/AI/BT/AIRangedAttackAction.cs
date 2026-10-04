using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;

namespace AI.BT
{
    /// <summary>
    /// 사거리 안의 대상을 예고 후 사격한다. 원거리 적 전용.
    /// 대기(랜덤 간격, 대상을 바라봄) → 예비 동작(고정 시간, 조준 방향 고정·조준선 표시) → 발사를 반복한다.
    /// 언제 쏠지는 예측할 수 없지만, 조준선을 보고 움직이면 피할 수 있게 하기 위한 구조다.
    /// 대상이 사거리를 벗어나거나 노드가 중단되면(사선 차단 등) 예비 동작은 취소되고 총알은 나가지 않는다.
    /// 이 캐릭터에 "Attack"(사격)과 "AttackWindup"(조준선) 스킬이 등록되어 있어야 한다.
    /// </summary>
    [Serializable, GeneratePropertyBag]
    [NodeDescription(
        name: "원거리 공격",
        description: "랜덤 간격으로 고정 시간 동안 조준(조준선 표시)한 뒤 고정된 방향으로 쏜다. 대상이 사거리를 벗어나면 끝난다.",
        story: "[Range] 안의 대상을 [MinInterval]~[MaxInterval]초 간격, [WindupDuration]초 조준 후 쏜다",
        category: "Action/적 AI",
        id: "aa8521ddf2b34b6f9e410537b228184d")]
    public partial class AIRangedAttackAction : AIActionBase
    {
        #region Constants
        /// <summary>사격 스킬 이름.</summary>
        private const string ATTACK_SKILL = "Attack";

        /// <summary>예비 동작 동안 켜 두는 조준선 스킬 이름.</summary>
        private const string WINDUP_SKILL = "AttackWindup";
        #endregion

        #region Serialized Fields
        [SerializeReference] public BlackboardVariable<float> Range;
        [SerializeReference] public BlackboardVariable<float> MinInterval = new BlackboardVariable<float>(1.5f);
        [SerializeReference] public BlackboardVariable<float> MaxInterval = new BlackboardVariable<float>(3.0f);
        [SerializeReference] public BlackboardVariable<float> WindupDuration = new BlackboardVariable<float>(0.6f);
        #endregion

        #region Private Fields
        [CreateProperty] private bool _isWindingUp;
        [CreateProperty] private float _phaseEndTime;
        [CreateProperty] private Vector3 _aimDirection;
        #endregion

        #region Node Lifecycle
        /// <inheritdoc />
        protected override Status OnStart()
        {
            if (!TryResolveController()) return Status.Failure;
            if (Controller.DistanceToTarget > Range.Value) return Status.Failure;

            Controller.StopMove();
            BeginWait();

            return Status.Running;
        }

        /// <inheritdoc />
        protected override Status OnUpdate()
        {
            // 대상을 잃으면 DistanceToTarget 이 무한대라 여기서 함께 끝난다
            if (Controller.DistanceToTarget > Range.Value) return Status.Success;

            if (_isWindingUp)
            {
                UpdateWindup();
            }
            else
            {
                UpdateWait();
            }

            return Status.Running;
        }

        /// <inheritdoc />
        protected override void OnEnd()
        {
            // 예비 동작 중 끝났으면(사거리 이탈, 사선 차단으로 인한 중단 등) 쏘지 않고 조준선만 끈다
            if (_isWindingUp && Controller) Controller.StopSkill(WINDUP_SKILL);
            _isWindingUp = false;
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// 대기 단계로 들어가며 다음 예비 동작까지의 랜덤 간격을 정한다.
        /// </summary>
        private void BeginWait()
        {
            _isWindingUp = false;
            _phaseEndTime = Time.time + UnityEngine.Random.Range(MinInterval.Value, MaxInterval.Value);
        }

        /// <summary>
        /// 대기 중에는 대상을 바라보고, 간격이 지나면 그 순간의 대상 방향으로 조준을 고정해 예비 동작을 시작한다.
        /// </summary>
        private void UpdateWait()
        {
            Vector3 toTarget = Vector3.ProjectOnPlane(Controller.VisibleTarget.position - Controller.Position, Vector3.up);
            Controller.FaceDirection(toTarget);

            if (Time.time < _phaseEndTime) return;

            _aimDirection = toTarget;
            _isWindingUp = true;
            _phaseEndTime = Time.time + WindupDuration.Value;
            Controller.ExecuteSkill(WINDUP_SKILL);
        }

        /// <summary>
        /// 예비 동작 중에는 고정된 방향만 바라보고(대상을 따라 돌지 않음), 시간이 되면 그 방향으로 쏜다.
        /// </summary>
        private void UpdateWindup()
        {
            Controller.FaceDirection(_aimDirection);

            if (Time.time < _phaseEndTime) return;

            Controller.StopSkill(WINDUP_SKILL);
            Controller.ExecuteSkill(ATTACK_SKILL);
            // 누르고 있는 동안 반복되는 스킬이어도 한 번만 발동하도록 바로 뗀다
            Controller.StopSkill(ATTACK_SKILL);

            BeginWait();
        }
        #endregion
    }
}
