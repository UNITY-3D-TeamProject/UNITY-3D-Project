using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;

namespace AI.BT
{
    /// <summary>
    /// AI 액션 노드의 공통 기반. 그래프가 붙은 오브젝트에서 AIController 를 찾아 캐싱한다.
    /// 노드는 Sensor 나 NavMeshAgent 가 아니라 오직 AIController 의 공개 API 에만 의존한다.
    /// 추상 클래스이므로 그래프 편집기의 노드 추가 메뉴에는 나타나지 않는다.
    /// </summary>
    [Serializable]
    public abstract class AIActionBase : Action
    {
        #region Properties
        /// <summary>조종 대상 컨트롤러. TryResolveController 성공 이후에만 유효하다.</summary>
        protected AIController Controller { get; private set; }
        #endregion

        #region Protected Methods
        /// <summary>
        /// 그래프가 붙은 오브젝트에서 AIController 를 찾아 캐싱한다.
        /// </summary>
        /// <returns>컨트롤러를 확보했으면 true</returns>
        protected bool TryResolveController()
        {
            if (Controller) return true;

            Controller = this.GameObject.GetComponent<AIController>();
            if (!Controller)
            {
                LogFailure($"[{this.GameObject.name}] AIController 가 없어 AI 액션을 실행할 수 없다.", true);
                return false;
            }

            return true;
        }
        #endregion
    }
}
