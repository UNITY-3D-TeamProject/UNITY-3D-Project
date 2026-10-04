using UnityEngine;

namespace Interaction
{
    /// <summary>
    /// 플레이어가 가까이에서 상호작용(E)할 수 있는 대상.
    /// </summary>
    public interface IInteractable
    {
        /// <summary>
        /// 상호작용을 실행한다.
        /// </summary>
        /// <param name="interactor">상호작용을 건 오브젝트 (플레이어)</param>
        void Interact(GameObject interactor);
    }
}
