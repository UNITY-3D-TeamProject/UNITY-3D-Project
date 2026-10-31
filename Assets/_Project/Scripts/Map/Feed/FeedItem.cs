using System;
using UnityEngine;
using Core.Audio;
using Core.Stage;
using Map.Gimmicks;

namespace Map.Feed
{
    /// <summary>
    /// 플레이어가 닿으면 획득되는 피드 재료 아이템. 획득을 FeedApp 에 기록하고 사라진다.
    /// 콜라이더의 Is Trigger를 켜야 한다.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class FeedItem : MonoBehaviour
    {
        #region Serialized Fields
        [Header("Item")]
        [Tooltip("이 아이템의 종류")]
        [SerializeField] private EFeedItemType _itemType;

        [Header("Sound")]
        [Tooltip("플레이어 몸에 들어가 획득되는 순간 낼 소리 (비우면 무음)")]
        [SerializeField] private SOAudioData _collectSound;
        #endregion

        public event Action OnCollected;

        #region Unity Lifecycle
        private void OnTriggerEnter(Collider other)
        {
            if (!GimmickUtility.TryGetPlayer(other, out _)) return;

            FeedApp feedApp = FindAnyObjectByType<FeedApp>();
            if (feedApp == null)
            {
                Debug.LogError($"[{name}] 씬에서 FeedApp을 찾을 수 없어 아이템 획득을 기록하지 못했습니다.", this);
            }
            else
            {
                feedApp.CollectItem(_itemType);
                OnCollected?.Invoke();
            }

            GimmickUtility.PlaySfx(_collectSound, transform.position);

            Destroy(gameObject);
        }
        #endregion
    }
}
