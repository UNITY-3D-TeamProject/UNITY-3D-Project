using System;
using UnityEngine;
using Core.Audio;
using Map.Gimmicks;

namespace Map.Feed
{
    /// <summary>
    /// 태그 끝에서 길을 막고 있는 악성 피드. 길을 막는 벽 콜라이더는 이 오브젝트의 자식으로 둔다.
    /// 미니게임 성공 시 길을 열고 악성 데이터를 방출한다. 연결된 일반 피드가 있으면 정화한 뒤 옆으로 보낸다.
    /// </summary>
    public class MaliciousFeed : MonoBehaviour
    {
        #region Serialized Fields
        [Header("Malicious Data")]
        [Tooltip("제거될 때 주변에 만들 악성 데이터 프리팹 (선택)")]
        [SerializeField] private GameObject _dataPrefab;

        [Tooltip("만들 악성 데이터 수")]
        [SerializeField, Min(0)] private int _dataCount = 3;

        [Tooltip("소환 순서마다 악성 데이터 속도에 곱할 배율. 데이터가 더 많으면 처음부터 다시 쓴다. 비우면 모두 프리팹 속도. 같은 순간에 닿아 무적에 막히지 않도록 서로 다르게 둔다")]
        [SerializeField] private float[] _speedMultipliers = { 0.9f, 1.0f, 1.1f };

        [Tooltip("악성 데이터를 이 오브젝트의 뒤쪽 반원에 놓을 때의 반지름")]
        [SerializeField, Min(0f)] private float _spawnRadius = 4.0f;

        [Tooltip("악성 데이터를 놓는 높이 (이 오브젝트 기준)")]
        [SerializeField] private float _spawnHeight = 1.5f;

        [Header("Purification")]
        [Tooltip("악성 카드의 외형만 담은 루트. 미니게임 장식과 분리한다.")]
        [SerializeField] private Transform _cardVisual;

        [Tooltip("카드 중심과 크기를 맞출 몸체")]
        [SerializeField] private Transform _cardBody;

        [SerializeField] private Material _purificationMaterial;
        [SerializeField, Min(0.01f)] private float _purificationDuration = 3.0f;
        [SerializeField, Range(1.0f, 1.5f)] private float _purifiedTravelDuration = 1.25f;

        [Tooltip("생성기가 안쪽 자리에 준비한 일반 피드")]
        [SerializeField] private FeedPost _purifiedPost;

        [Header("Sound")]
        [Tooltip("미니게임을 성공해 제거될 때 낼 소리 (비우면 무음)")]
        [SerializeField] private SOAudioData _successSound;

        [Tooltip("악성 데이터를 삭제·방출할 때 낼 소리 (비우면 무음)")]
        [SerializeField] private SOAudioData _clearSound;
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
        /// <summary>안쪽 목적지에 준비된 피드를 연결한다. 성공하기 전에는 비활성 상태로 둔다.</summary>
        public void SetPurifiedPost(FeedPost post)
        {
            _purifiedPost = post;
        }

        /// <summary>
        /// 성공 즉시 길을 열고 데이터를 만든다. 연결된 일반 피드가 있으면 정화 연출을 시작한다.
        /// </summary>
        public void Clear()
        {
            if (IsCleared) return;
            IsCleared = true;

            GimmickUtility.PlaySfx(_successSound, transform.position);
            GimmickUtility.PlaySfx(_clearSound, transform.position);

            bool canPurify = (_purifiedPost != null) && (_cardVisual != null) &&
                (_cardBody != null) && (_purificationMaterial != null);

            FeedPurification purification = null;
            if (canPurify)
            {
                DisableMinigame();
                _purifiedPost.gameObject.SetActive(true);
                purification = _purifiedPost.BeginPurification(this, _cardVisual, _cardBody,
                    _purificationMaterial, _purificationDuration, _purifiedTravelDuration);
            }

            SpawnData(purification);
            OnCleared?.Invoke();
            if (!canPurify) Destroy(gameObject);
        }
        #endregion

        #region Private Methods
        private void DisableMinigame()
        {
            foreach (Collider collider in GetComponentsInChildren<Collider>(true))
            {
                collider.enabled = false;
            }

            foreach (MonoBehaviour behaviour in GetComponents<MonoBehaviour>())
            {
                if (behaviour == this) continue;
                behaviour.StopAllCoroutines();
                behaviour.enabled = false;
            }

            foreach (Transform child in transform)
            {
                if (child != _cardVisual) child.gameObject.SetActive(false);
            }
        }

        // 악성 데이터를 이 오브젝트의 뒤쪽(forward 쪽) 반원에 같은 간격으로 놓는다. 앞쪽에 서 있는 플레이어의 몸 위에 바로 생기지 않게 하기 위해서다.
        // 맵을 다시 생성하면 함께 지워지도록 이 오브젝트의 부모 아래에 둔다.
        private void SpawnData(FeedPurification purification)
        {
            if (_dataPrefab == null) return;

            for (int i = 0; i < _dataCount; i++)
            {
                Quaternion direction = Quaternion.Euler(0.0f, -90.0f + ((180.0f / _dataCount) * (i + 0.5f)), 0.0f);
                Vector3 position = transform.position +
                    (direction * (transform.forward * _spawnRadius)) +
                    (Vector3.up * _spawnHeight);

                GameObject data = Instantiate(_dataPrefab, position, Quaternion.identity, transform.parent);
                ApplySpeedMultiplier(data, i);

                // 추적 본체는 기존 안전한 생성 위치에 두고, 외형만 카드 표면에서 방출한다.
                if ((_purifiedPost != null) && (_cardBody != null) && (data.transform.childCount > 0))
                {
                    float side = ((i + 0.5f) / _dataCount) - 0.5f;
                    Vector3 surface = _cardBody.position - (transform.forward * 0.3f) +
                        (transform.right * side * 1.6f) + (Vector3.up * side * 0.8f);
                    data.AddComponent<FeedDataEmission>().Begin(data.transform.GetChild(0), surface, purification);
                }
            }
        }

        // 반원의 왼쪽 끝부터 순서대로 배율을 넣어, 좌우 대칭 위치의 데이터가 같은 순간에 도착하지 않게 한다.
        private void ApplySpeedMultiplier(GameObject data, int index)
        {
            if ((_speedMultipliers == null) || (_speedMultipliers.Length == 0)) return;

            HomingHazard homingHazard = data.GetComponent<HomingHazard>();
            if (homingHazard == null) return;

            homingHazard.ScaleSpeed(_speedMultipliers[index % _speedMultipliers.Length]);
        }
        #endregion
    }
}
