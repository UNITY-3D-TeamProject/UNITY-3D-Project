using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using AI.BT;
using AI.Boss.BossB;

namespace AI.Boss.BT
{
    /// <summary>
    /// 발밑 칸이 예고 중이면 가장 가까운 멀쩡한 칸으로 걸어서 피한다. 걸어서 피할 길이 없거나 정체되면 회피 도약을 요청한다.
    /// 발밑 칸이 더는 예고 중이 아니면 성공한다. 칸이 무너지는 순간의 낙하는 BossBController 안전망이 따로 막는다.
    /// 정체 판정이 너무 늦으면 AIController 의 목적지 제한 시간(_destinationTimeout)을 줄인다.
    /// </summary>
    [Serializable, GeneratePropertyBag]
    [NodeDescription(
        name: "B 예고 칸 피하기",
        description: "가장 가까운 멀쩡한 칸으로 이동한다. 못 가면 회피 도약 스킬을 쓴다. 발밑 칸이 안전해지면 성공한다.",
        story: "멀쩡한 칸으로 피한다. 못 가면 [EvadeSkillName] 스킬을 쓴다",
        category: "Action/보스",
        id: "7d4b1665ccf54da594108ba13e202175")]
    public partial class BossBEscapeTileAction : AIActionBase
    {
        #region Serialized Fields
        [SerializeReference] public BlackboardVariable<string> EvadeSkillName;
        #endregion

        #region Private Fields
        private BossBController _boss;
        private bool _isEvading;
        #endregion

        #region Node Lifecycle
        /// <inheritdoc />
        protected override Status OnStart()
        {
            if (!TryResolveController()) return Status.Failure;

            if (!_boss) _boss = this.GameObject.GetComponent<BossBController>();
            if (!_boss || (_boss.Arena == null)) return Status.Failure;

            _isEvading = false;

            int tile = _boss.Arena.FindNearestSafe(Controller.Position);
            bool isWalking = (tile >= 0) && Controller.MoveTo(_boss.Arena.GetTileCenter(tile));
            if (isWalking) return Status.Running;

            return TryEvade() ? Status.Running : Status.Failure;
        }

        /// <inheritdoc />
        protected override Status OnUpdate()
        {
            if (_isEvading) return Controller.IsHolding ? Status.Running : Status.Success;

            if (!IsOnBadTile()) return Status.Success;

            if (Controller.IsStuck && !TryEvade()) return Status.Failure;

            return Status.Running;
        }

        /// <inheritdoc />
        protected override void OnEnd()
        {
            // 회피 도약은 몸을 붙잡은 채 스스로 끝나므로 이동만 멈춘다
            if (Controller) Controller.StopMove();
        }
        #endregion

        #region Private Methods
        private bool IsOnBadTile()
        {
            if (!_boss.Arena.TryGetTile(Controller.Position, out int tile)) return false;

            return _boss.Arena.IsHoleOrTelegraphed(tile);
        }

        private bool TryEvade()
        {
            Controller.StopMove();
            Controller.ExecuteSkill(EvadeSkillName.Value);

            // 스킬이 시작되었다면 몸을 붙잡는다
            _isEvading = Controller.IsHolding;
            return _isEvading;
        }
        #endregion
    }
}
