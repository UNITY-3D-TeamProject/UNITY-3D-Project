using UnityEngine;
using UnityEngine.Events;
using Core.Stage;
using Interaction;

namespace Map.Feed
{
    /// <summary>
    /// 가까이에서 상호작용(E)하면 피드 재료 아이템을 모두 모았는지 확인하는 화면.
    /// 다 모았으면 On Compose Ready, 모자라면 On Items Missing 을 실행한다.
    /// 실제 피드 작성 화면과 로직은 On Compose Ready 에 연결한다.
    /// 같은 오브젝트에 PlayerInteractRelay 를 붙여 상호작용 입력을 받는다.
    /// </summary>
    public class FeedComposeScreen : MonoBehaviour, IInteractable
    {
        #region Serialized Fields
        [Header("Condition")]
        [Tooltip("피드를 만드는 데 필요한 아이템")]
        [SerializeField] private EFeedItemType[] _requiredItems;

        [Header("Event")]
        [Tooltip("필요한 아이템을 모두 가진 채 상호작용했을 때 실행할 동작")]
        [SerializeField] private UnityEvent _onComposeReady = new UnityEvent();

        [Tooltip("아이템이 모자란 채 상호작용했을 때 실행할 동작")]
        [SerializeField] private UnityEvent _onItemsMissing = new UnityEvent();
        #endregion

        #region Properties
        /// <summary>필요한 아이템을 모두 가진 채 상호작용했을 때 실행되는 이벤트.</summary>
        public UnityEvent OnComposeReady => _onComposeReady;

        /// <summary>아이템이 모자란 채 상호작용했을 때 실행되는 이벤트.</summary>
        public UnityEvent OnItemsMissing => _onItemsMissing;
        #endregion

        #region Public Methods
        /// <inheritdoc />
        public void Interact(GameObject interactor)
        {
            if (HasAllItems())
            {
                _onComposeReady.Invoke();
            }
            else
            {
                _onItemsMissing.Invoke();
            }
        }
        #endregion

        #region Private Methods
        private bool HasAllItems()
        {
            FeedApp feedApp = FindAnyObjectByType<FeedApp>();
            if (feedApp == null)
            {
                Debug.LogError($"[{name}] 씬에서 FeedApp을 찾을 수 없어 아이템을 확인하지 못했습니다.", this);
                return false;
            }

            foreach (EFeedItemType itemType in _requiredItems)
            {
                if (!feedApp.HasItem(itemType)) return false;
            }

            return true;
        }
        #endregion
    }
}
