using System;
using Unity.Behavior;
using AI.Boss.BossA;
using Action = Unity.Behavior.Action;

namespace AI.Boss.BT
{
    /// <summary>
    /// A 보스 액션 노드의 공통 기반. 그래프가 붙은 오브젝트에서 BossController 를 찾아 캐싱한다.
    /// 추상 클래스이므로 그래프 편집기의 노드 추가 메뉴에는 나타나지 않는다.
    /// </summary>
    [Serializable]
    public abstract class BossActionBase : Action
    {
        #region Properties
        /// <summary>조종 대상 컨트롤러. TryResolveController 성공 이후에만 유효하다.</summary>
        protected BossController Controller { get; private set; }
        #endregion

        #region Protected Methods
        /// <summary>
        /// 그래프가 붙은 오브젝트에서 BossController 를 찾아 캐싱한다.
        /// </summary>
        /// <returns>컨트롤러를 확보했으면 true</returns>
        protected bool TryResolveController()
        {
            if (Controller) return true;

            Controller = this.GameObject.GetComponent<BossController>();
            if (!Controller)
            {
                LogFailure($"[{this.GameObject.name}] BossController 가 없어 보스 액션을 실행할 수 없다.", true);
                return false;
            }

            return true;
        }
        #endregion
    }
}
