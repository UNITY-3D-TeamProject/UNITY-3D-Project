using UnityEngine;
using Attribute.Core;
using Attribute.Effect;
using Interaction;
using Map.Gimmicks;

namespace Map.Feed
{
    /// <summary>
    /// 악성 피드 미니게임 5: 상호작용하면 피드 사진과 거의 같은 사진이 오른쪽에 나타난다. 오른쪽 사진에서 다른 곳을 쏴서 모두 찾으면 제거된다.
    /// 오른쪽 사진의 다른 곳이 아닌 자리를 쏘면 하트를 하나 잃고, 하트를 모두 잃으면 실패 Effect 를 적용한다(사망).
    /// 정답 표적(DamageButton, One Shot)은 사진 판(DamageButton, One Shot 끔)보다 앞쪽에 두꺼운 콜라이더로 둔다.
    /// </summary>
    [RequireComponent(typeof(MaliciousFeed))]
    public class SpotDiffFeedClear : MonoBehaviour, IInteractable
    {
        #region Serialized Fields
        [Header("Panel")]
        [Tooltip("상호작용 전에는 꺼 두는 비교 사진·하트 묶음")]
        [SerializeField] private GameObject _gameRoot;

        [Tooltip("오른쪽 사진 전체를 덮는 판. 정답이 아닌 곳에 맞았을 때 쓴다.")]
        [SerializeField] private DamageButton _missArea;

        [Tooltip("다른 곳마다 하나씩 놓은 정답 표적")]
        [SerializeField] private DamageButton[] _answers;

        [Tooltip("정답 표적과 같은 순서로, 찾았을 때 켜는 표시")]
        [SerializeField] private GameObject[] _answerMarks;

        [Tooltip("남은 기회 표시")]
        [SerializeField] private FeedHeartGauge _hearts;

        [Header("Fail")]
        [Tooltip("하트를 모두 잃었을 때 플레이어에게 위에서부터 순서대로 적용할 Effect (ValueSource는 Float)")]
        [SerializeField] private SOAttributeEffect[] _failEffects;
        #endregion

        #region Private Fields
        private MaliciousFeed _feed;
        private IEffectTarget _player;
        private bool _isStarted;
        private int _foundCount;

        // 총알 하나가 같은 물리 단계에서 정답 표적과 판을 함께 건드릴 수 있어, 판에 맞은 것은 프레임 끝에 확정한다
        private float _lastAnswerStepTime = -1.0f;
        private float _pendingMissStepTime = -1.0f;
        private bool _hasPendingMiss;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            _feed = GetComponent<MaliciousFeed>();

            Debug.Assert(_gameRoot != null, $"[{name}] Game Root가 연결되지 않았습니다.");
            Debug.Assert(_missArea != null, $"[{name}] Miss Area가 연결되지 않았습니다.");
            Debug.Assert(_hearts != null, $"[{name}] Hearts가 연결되지 않았습니다.");
            Debug.Assert(_answers.Length == _answerMarks.Length, $"[{name}] 정답 표적과 표시 수가 다릅니다.");

            for (int i = 0; i < _answers.Length; i++)
            {
                // 어느 정답이 맞았는지 알 수 있도록 표적마다 따로 연결한다
                int answerIndex = i;
                _answers[i].OnPressed.AddListener(() => HandleAnswerShot(answerIndex));
                _answerMarks[i].SetActive(false);
            }
            _missArea.OnPressed.AddListener(HandleMissShot);

            _gameRoot.SetActive(false);
        }

        private void Update()
        {
            if (!_hasPendingMiss) return;
            _hasPendingMiss = false;

            if (Mathf.Approximately(_pendingMissStepTime, _lastAnswerStepTime)) return;
            if (_feed.IsCleared) return;

            if (_hearts.LoseHeart())
            {
                GimmickUtility.ApplyEffects(_failEffects, _player);
            }
        }
        #endregion

        #region Public Methods
        /// <inheritdoc />
        public void Interact(GameObject interactor)
        {
            if (_isStarted || _feed.IsCleared) return;
            _isStarted = true;

            // AttributeSet은 플레이어 루트가 아니라 자식(AttributePrefab)에 있다
            _player = interactor.GetComponentInChildren<IEffectTarget>();
            _gameRoot.SetActive(true);
        }
        #endregion

        #region Private Methods
        private void HandleAnswerShot(int index)
        {
            _lastAnswerStepTime = Time.fixedTime;

            _answers[index].gameObject.SetActive(false);
            _answerMarks[index].SetActive(true);
            _foundCount++;

            if (_foundCount < _answers.Length) return;

            _feed.Clear();
        }

        private void HandleMissShot()
        {
            _pendingMissStepTime = Time.fixedTime;
            _hasPendingMiss = true;
        }
        #endregion
    }
}
