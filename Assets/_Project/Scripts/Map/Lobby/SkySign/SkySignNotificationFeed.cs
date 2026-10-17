using System.Collections;
using TMPro;
using UnityEngine;

namespace Map.Lobby.SkySign
{
    /// <summary>
    /// 알림 전광판. 목록에서 무작위 알림을 골라 맨 위에 쌓고, 넘치는 오래된 알림은 지운다.
    /// 아래 카드일수록 "n분 전"으로 시각 표시를 바꾼다.
    /// </summary>
    public class SkySignNotificationFeed : MonoBehaviour
    {
        #region Serialized Fields
        [Header("Data")]
        [SerializeField] private SONotificationList _notificationList;

        [Header("References")]
        [Tooltip("알림 카드 프리팹")]
        [SerializeField] private SkySignNotificationItem _itemPrefab;

        [Tooltip("카드가 쌓이는 곳 (VerticalLayoutGroup)")]
        [SerializeField] private RectTransform _container;

        [Tooltip("받은 알림 개수 배지")]
        [SerializeField] private TMP_Text _countText;

        [Header("Settings")]
        [Tooltip("화면에 남겨 둘 최대 카드 수")]
        [SerializeField] private int _maxCount = 4;

        [Tooltip("다음 알림까지 걸리는 시간 범위 (초)")]
        [SerializeField] private Vector2 _intervalRange = new Vector2(1.2f, 3.8f);

        [Tooltip("시작할 때 미리 깔아 둘 카드 수")]
        [SerializeField] private int _initialCount = 3;

        [Tooltip("카드 한 칸 아래로 갈 때마다 늘어나는 분")]
        [SerializeField] private int _minutesPerStep = 2;
        #endregion

        #region Private Fields
        private int _receivedCount;
        private int _lastIndex = -1;
        #endregion

        #region Unity Lifecycle
        private void Start()
        {
            Debug.Assert(_notificationList != null && _notificationList.Count > 1, $"[{name}] 알림 목록 누락");
            Debug.Assert(_itemPrefab != null && _container != null, $"[{name}] 참조 누락");

            for (int i = 0; i < _initialCount; i++)
            {
                AddNotification();
            }
            StartCoroutine(CoSpawnLoop());
        }
        #endregion

        #region Private Methods
        private void AddNotification()
        {
            SkySignNotificationItem item = Instantiate(_itemPrefab, _container);
            item.transform.SetAsFirstSibling();
            item.Setup(_notificationList.Get(PickIndex()));

            while (_container.childCount > _maxCount)
            {
                Transform oldest = _container.GetChild(_container.childCount - 1);
                oldest.SetParent(null);
                Destroy(oldest.gameObject);
            }

            for (int i = 0; i < _container.childCount; i++)
            {
                string label = i == 0 ? "지금" : $"{i * _minutesPerStep}분 전";
                _container.GetChild(i).GetComponent<SkySignNotificationItem>().SetTimeLabel(label);
            }

            _receivedCount++;
            if (_countText != null)
            {
                _countText.text = _receivedCount.ToString();
            }
        }

        // 같은 알림이 연달아 나오지 않게 고른다.
        private int PickIndex()
        {
            int index;
            do
            {
                index = Random.Range(0, _notificationList.Count);
            }
            while (index == _lastIndex);

            _lastIndex = index;
            return index;
        }
        #endregion

        #region Coroutines
        private IEnumerator CoSpawnLoop()
        {
            while (true)
            {
                yield return new WaitForSeconds(Random.Range(_intervalRange.x, _intervalRange.y));
                AddNotification();
            }
        }
        #endregion
    }
}
