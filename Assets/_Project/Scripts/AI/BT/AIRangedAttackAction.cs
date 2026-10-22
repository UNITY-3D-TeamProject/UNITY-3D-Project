using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;

namespace AI.BT
{
    /// <summary>
    /// 사거리 안의 대상을 예고 후 사격한다. 원거리 적 전용.
    /// 대기(랜덤 간격, 대상을 바라봄) → 예비 동작(고정 시간, 조준 방향 고정·조준선 표시) → 발사를 반복한다.
    /// 발사 직후 발사 모션(AttackMotionDuration) 동안은 대기 중이어도 조준 방향을 유지한다.
    /// 언제 쏠지는 예측할 수 없지만, 조준선을 보고 움직이면 피할 수 있게 하기 위한 구조다.
    /// 대상이 사거리를 벗어나거나 노드가 중단되면(사선 차단 등) 예비 동작은 취소되고 총알은 나가지 않는다.
    /// 이 캐릭터에 "Attack"(사격)과 "AttackWindup"(조준선) 스킬이 등록되어 있어야 한다.
    /// 실행 중에는 이동 방향이 아니라 대상 쪽을 바라본다. KeepMoving 이면 다른 노드의 이동을 멈추지 않는다
    /// (후퇴와 Run In Parallel 로 묶어 뒷걸음질 사격용).
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

        /// <summary>그래프에 AttackMotionDuration 값이 없을 때 쓰는 발사 모션 시간(초).</summary>
        private const float DEFAULT_ATTACK_MOTION_DURATION = 0.85f;
        #endregion

        #region Serialized Fields
        [SerializeReference] public BlackboardVariable<float> Range;
        [SerializeReference] public BlackboardVariable<float> MinInterval = new BlackboardVariable<float>(1.5f);
        [SerializeReference] public BlackboardVariable<float> MaxInterval = new BlackboardVariable<float>(3.0f);
        [SerializeReference] public BlackboardVariable<float> WindupDuration = new BlackboardVariable<float>(0.6f);
        [SerializeReference] public BlackboardVariable<bool> KeepMoving = new BlackboardVariable<bool>(false);
        [SerializeReference] public BlackboardVariable<float> AttackMotionDuration = new BlackboardVariable<float>(DEFAULT_ATTACK_MOTION_DURATION);
        #endregion

        #region Private Fields
        [CreateProperty] private bool _isWindingUp;
        [CreateProperty] private float _phaseEndTime;
        [CreateProperty] private Vector3 _aimDirection;
        [CreateProperty] private float _superArmorEndTime;
        #endregion

        #region Node Lifecycle
        /// <inheritdoc />
        protected override Status OnStart()
        {
            if (!TryResolveController()) return Status.Failure;
            if (Controller.DistanceToTarget > Range.Value) return Status.Failure;

            if (!KeepMoving.Value) Controller.StopMove();
            Controller.ShouldFaceMoveDirection = false;
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

            if (Controller)
            {
                Controller.ShouldFaceMoveDirection = true;
                Controller.IsSuperArmor = false;
            }
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
            // 발사 모션이 끝나면 슈퍼아머를 풀어 다시 경직될 수 있게 한다
            if (Controller.IsSuperArmor && (Time.time >= _superArmorEndTime)) Controller.IsSuperArmor = false;

            Vector3 toTarget = Vector3.ProjectOnPlane(Controller.VisibleTarget.position - Controller.Position, Vector3.up);
            // 발사 모션 중(슈퍼아머 유지 중)에는 쏜 방향을 유지해, 모션보다 늦게 나가는 총알과 몸 방향이 어긋나지 않게 한다
            Controller.FaceDirection(Controller.IsSuperArmor ? _aimDirection : toTarget);

            if (Time.time < _phaseEndTime) return;
            // 경직 중에는 조준선 스킬이 무시되므로, 조준선 없이 쏘지 않도록 풀릴 때까지 예비 동작을 미룬다
            if (Controller.IsStunned) return;

            _aimDirection = toTarget;
            _isWindingUp = true;
            _phaseEndTime = Time.time + WindupDuration.Value;
            // 예비 동작 중에는 경직되지 않아 조준한 사격이 끊기지 않는다
            Controller.IsSuperArmor = true;
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
            // 발사 모션 동안에도 슈퍼아머를 유지해 힛모션이 공격 모션을 덮거나 뒤로 밀리지 않게 한다. 해제는 UpdateWait 가 맡는다
            float motionDuration = (AttackMotionDuration != null) ? AttackMotionDuration.Value : DEFAULT_ATTACK_MOTION_DURATION;
            _superArmorEndTime = Time.time + motionDuration;

            BeginWait();
        }
        #endregion
    }
}
