using System;
using UnityEngine;

namespace Map.Feed
{
    /// <summary>
    /// 태그 끝에서 길을 막고 있는 악성 피드. 길을 막는 벽 콜라이더는 이 오브젝트의 자식으로 둔다.
    /// 같은 오브젝트의 미니게임 컴포넌트가 Clear 를 부르면 주변에 악성 데이터를 만들고 자신(벽 포함)을 제거한다.
    /// </summary>
    public class MaliciousFeed : MonoBehaviour
    {
        #region Serialized Fields
        [Header("Malicious Data")]
        [Tooltip("제거될 때 주변에 만들 악성 데이터 프리팹 (선택)")]
        [SerializeField] private GameObject _dataPrefab;

        [Tooltip("만들 악성 데이터 수")]
        [SerializeField, Min(0)] private int _dataCount = 3;

        [Tooltip("악성 데이터를 이 오브젝트의 뒤쪽 반원에 놓을 때의 반지름")]
        [SerializeField, Min(0f)] private float _spawnRadius = 4.0f;

        [Tooltip("악성 데이터를 놓는 높이 (이 오브젝트 기준)")]
        [SerializeField] private float _spawnHeight = 1.5f;
        #endregion

        #region Properties
        /// <summary>이미 제거됐는지 여부.</summary>
        public bool IsCleared { get; private set; }
        #endregion

        #region Events
        /// <summary>제거될 때 한 번 호출된다.</summary>
        public event Action OnCleared;
        #endregion

        #region Public Methods
        /// <summary>
        /// 악성 피드를 제거한다. 악성 데이터를 만들고 이 오브젝트를 없앤다. 두 번째 호출부터는 무시한다.
        /// </summary>
        public void Clear()
        {
            if (IsCleared) return;
            IsCleared = true;

            SpawnData();
            OnCleared?.Invoke();
            Destroy(gameObject);
        }
        #endregion

        #region Private Methods
        // 악성 데이터를 이 오브젝트의 뒤쪽(forward 쪽) 반원에 같은 간격으로 놓는다. 앞쪽에 서 있는 플레이어의 몸 위에 바로 생기지 않게 하기 위해서다.
        // 맵을 다시 생성하면 함께 지워지도록 이 오브젝트의 부모 아래에 둔다.
        private void SpawnData()
        {
            if (_dataPrefab == null) return;

            for (int i = 0; i < _dataCount; i++)
            {
                Quaternion direction = Quaternion.Euler(0.0f, -90.0f + ((180.0f / _dataCount) * (i + 0.5f)), 0.0f);
                Vector3 position = transform.position +
                    (direction * (transform.forward * _spawnRadius)) +
                    (Vector3.up * _spawnHeight);

                Instantiate(_dataPrefab, position, Quaternion.identity, transform.parent);
            }
        }
        #endregion
    }
}
