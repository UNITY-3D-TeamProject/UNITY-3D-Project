using UnityEngine;
using Interaction;

namespace Map.Feed
{
    /// <summary>
    /// 악성 피드 미니게임 1: 가까이에서 상호작용하면 바로 제거된다.
    /// 같은 오브젝트에 PlayerInteractRelay 를 붙여 상호작용 입력을 받는다.
    /// </summary>
    [RequireComponent(typeof(MaliciousFeed))]
    public class InteractFeedClear : MonoBehaviour, IInteractable
    {
        #region Private Fields
        private MaliciousFeed _feed;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            _feed = GetComponent<MaliciousFeed>();
        }
        #endregion

        #region Public Methods
        /// <inheritdoc />
        public void Interact(GameObject interactor)
        {
            _feed.Clear();
        }
        #endregion
    }
}
