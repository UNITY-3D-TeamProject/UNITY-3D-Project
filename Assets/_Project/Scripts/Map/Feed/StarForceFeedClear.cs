using UnityEngine;
using Attribute.Core;
using Attribute.Effect;
using Interaction;
using Map.Gimmicks;

namespace Map.Feed
{
    /// <summary>
    /// 악성 피드 미니게임 3: 막대 위를 왕복하는 표시가 가운데 성공 구간에 있을 때 상호작용하면 별 하나를 얻는다.
    /// 별을 모두 얻으면 제거된다. 구간 밖에서 누르면 실패 Effect 를 플레이어에게 적용하고 별은 유지한다.
    /// 같은 오브젝트에 PlayerInteractRelay 를 붙여 상호작용 입력을 받는다.
    /// </summary>
    [RequireComponent(typeof(MaliciousFeed))]
    public class StarForceFeedClear : MonoBehaviour, IInteractable
    {
        #region Serialized Fields
        [Header("Marker")]
        [Tooltip("막대 위를 왕복하는 표시. 부모(막대) 기준 X축으로 움직인다.")]
        [SerializeField] private Transform _marker;

        [Tooltip("표시가 가운데에서 한쪽 끝까지 가는 거리")]
        [SerializeField, Min(0.01f)] private float _barHalfLength = 1.5f;

        [Tooltip("가운데에서 이 거리 안에 표시가 있으면 성공")]
        [SerializeField, Min(0f)] private float _successHalfWidth = 0.3f;

        [Tooltip("표시의 기본 이동 속도")]
        [SerializeField, Min(0f)] private float _markerSpeed = 2.5f;

        [Tooltip("얻은 별 수에 따라 이동 속도에 곱하는 값 (0번 = 별이 없을 때). 모자라면 마지막 값을 쓴다.")]
        [SerializeField] private float[] _speedMultipliers = { 1.0f, 1.3f, 1.6f };

        [Header("Stars")]
        [Tooltip("성공할 때마다 하나씩 켜는 별 오브젝트. 이 수만큼 성공하면 제거된다.")]
        [SerializeField] private GameObject[] _stars;

        [Header("Fail")]
        [Tooltip("실패했을 때 플레이어에게 위에서부터 순서대로 적용할 Effect (ValueSource는 Float)")]
        [SerializeField] private SOAttributeEffect[] _failEffects;
        #endregion

        #region Private Fields
        private MaliciousFeed _feed;
        private int _starCount;
        private float _travelDistance;
        private float _markerOffset;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            _feed = GetComponent<MaliciousFeed>();

            Debug.Assert(_marker != null, $"[{name}] Marker가 연결되지 않았습니다.");

            foreach (GameObject star in _stars)
            {
                star.SetActive(false);
            }
        }

        private void Update()
        {
            float multiplier = 1.0f;
            if (_speedMultipliers.Length > 0)
            {
                multiplier = _speedMultipliers[Mathf.Min(_starCount, _speedMultipliers.Length - 1)];
            }

            // 한쪽 끝에서 반대쪽 끝까지 같은 속도로 왕복한다
            _travelDistance += _markerSpeed * multiplier * Time.deltaTime;
            _markerOffset = Mathf.PingPong(_travelDistance, _barHalfLength * 2.0f) - _barHalfLength;

            Vector3 localPosition = _marker.localPosition;
            localPosition.x = _markerOffset;
            _marker.localPosition = localPosition;
        }
        #endregion

        #region Public Methods
        /// <inheritdoc />
        public void Interact(GameObject interactor)
        {
            if (_feed.IsCleared) return;

            if (Mathf.Abs(_markerOffset) > _successHalfWidth)
            {
                // AttributeSet은 플레이어 루트가 아니라 자식(AttributePrefab)에 있다
                GimmickUtility.ApplyEffects(_failEffects, interactor.GetComponentInChildren<IEffectTarget>());
                return;
            }

            _stars[_starCount].SetActive(true);
            _starCount++;

            if (_starCount < _stars.Length) return;

            _feed.Clear();
        }
        #endregion
    }
}
