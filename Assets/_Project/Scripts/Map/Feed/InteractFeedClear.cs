using UnityEngine;
using Core.Audio;
using Interaction;
using Map.Gimmicks;

namespace Map.Feed
{
    /// <summary>
    /// 악성 피드 미니게임 1: 가까이에서 상호작용하면 바로 제거된다.
    /// 입력은 플레이어의 Interact 스킬이 카메라 레이로 찾아 넘겨준다. 콜라이더가 있는 오브젝트를 Interact 레이어로 둔다.
    /// </summary>
    [RequireComponent(typeof(MaliciousFeed))]
    public class InteractFeedClear : MonoBehaviour, IInteractable
    {
        #region Serialized Fields
        [Header("Sound")]
        [Tooltip("상호작용했을 때 낼 소리 (비우면 무음)")]
        [SerializeField] private SOAudioData _interactSound;
        #endregion

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
            GimmickUtility.PlayUi(_interactSound);
            _feed.Clear();
        }
        #endregion
    }
}
