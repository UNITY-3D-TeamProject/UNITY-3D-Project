using System;
using Unity.Behavior;
using UnityEngine;

namespace AI.BT
{
    /// <summary>
    /// AI 조건 노드의 공통 기반. 액션과 같은 방식으로 AIController 를 찾아 캐싱한다.
    /// 조건은 부수 효과 없는 읽기 전용 질의이므로 중재자를 거치지 않고 조종부에 직접 묻는다.
    /// </summary>
    [Serializable]
    public abstract class AIConditionBase : Condition
    {
        #region Properties
        /// <summary>조종 대상 컨트롤러. 확보하지 못했으면 null.</summary>
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
            return Controller;
        }
        #endregion
    }
}
