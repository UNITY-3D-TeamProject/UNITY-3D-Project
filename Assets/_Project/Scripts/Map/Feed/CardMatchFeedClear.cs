using System.Collections;
using UnityEngine;
using Attribute.Core;
using Attribute.Effect;
using Interaction;
using Map.Gimmicks;

namespace Map.Feed
{
    /// <summary>
    /// 악성 피드 미니게임 4: 상호작용하면 카드판과 하트가 나타난다. 카드를 쏴서 2장씩 열고, 같은 그림이면 사라진다.
    /// 모든 짝을 맞추면 제거된다. 다르면 잠시 보여 준 뒤 덮고 하트를 하나 잃으며, 하트를 모두 잃으면 실패 Effect 를 적용한다(사망).
    /// 입력은 플레이어의 Interact 스킬이 카메라 레이로 찾아 넘겨준다. 콜라이더가 있는 오브젝트를 Interact 레이어로 둔다.
    /// </summary>
    [RequireComponent(typeof(MaliciousFeed))]
    public class CardMatchFeedClear : MonoBehaviour, IInteractable
    {
        #region Serialized Fields
        [Header("Board")]
        [Tooltip("상호작용 전에는 꺼 두는 카드판·하트 묶음")]
        [SerializeField] private GameObject _gameRoot;

        [Tooltip("카드들. 수는 그림 수 × 2 여야 한다.")]
        [SerializeField] private FeedMatchCard[] _cards;

        [Tooltip("카드 앞면 그림. 한 그림이 카드 2장에 들어간다.")]
        [SerializeField] private Material[] _faceMaterials;

        [Tooltip("남은 기회 표시")]
        [SerializeField] private FeedHeartGauge _hearts;

        [Header("Timing")]
        [Tooltip("다른 그림 2장을 덮기 전에 보여 주는 시간(초)")]
        [SerializeField, Min(0f)] private float _mismatchShowTime = 0.8f;

        [Tooltip("같은 그림 2장이 사라지기 전에 보여 주는 시간(초)")]
        [SerializeField, Min(0f)] private float _matchShowTime = 0.4f;

        [Header("Fail")]
        [Tooltip("하트를 모두 잃었을 때 플레이어에게 위에서부터 순서대로 적용할 Effect (ValueSource는 Float)")]
        [SerializeField] private SOAttributeEffect[] _failEffects;
        #endregion

        #region Private Fields
        private MaliciousFeed _feed;
        private IEffectTarget _player;
        private FeedMatchCard _openedCard;
        private bool _isStarted;
        private bool _isResolving;
        private int _matchedPairCount;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            _feed = GetComponent<MaliciousFeed>();

            Debug.Assert(_gameRoot != null, $"[{name}] Game Root가 연결되지 않았습니다.");
            Debug.Assert(_hearts != null, $"[{name}] Hearts가 연결되지 않았습니다.");
            Debug.Assert(_cards.Length == _faceMaterials.Length * 2,
                $"[{name}] 카드 수({_cards.Length})가 그림 수({_faceMaterials.Length}) × 2 가 아닙니다.");

            foreach (FeedMatchCard card in _cards)
            {
                card.OnShot += HandleCardShot;
            }

            _gameRoot.SetActive(false);
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

            ShuffleFaces();
            _gameRoot.SetActive(true);
        }
        #endregion

        #region Private Methods
        // 그림 번호를 2장씩 넣은 뒤 섞어서 카드에 나눠 준다
        private void ShuffleFaces()
        {
            int[] pairIds = new int[_cards.Length];
            for (int i = 0; i < pairIds.Length; i++)
            {
                pairIds[i] = i / 2;
            }

            for (int i = pairIds.Length - 1; i > 0; i--)
            {
                int swapIndex = Random.Range(0, i + 1);
                (pairIds[i], pairIds[swapIndex]) = (pairIds[swapIndex], pairIds[i]);
            }

            for (int i = 0; i < _cards.Length; i++)
            {
                _cards[i].SetFace(pairIds[i], _faceMaterials[pairIds[i]]);
            }
        }

        private void HandleCardShot(FeedMatchCard card)
        {
            if (!_isStarted || _isResolving || card.IsFaceUp) return;

            card.Reveal();

            if (_openedCard == null)
            {
                _openedCard = card;
                return;
            }

            StartCoroutine(CoResolve(_openedCard, card));
            _openedCard = null;
        }
        #endregion

        #region Coroutines
        private IEnumerator CoResolve(FeedMatchCard first, FeedMatchCard second)
        {
            _isResolving = true;

            if (first.PairId == second.PairId)
            {
                yield return new WaitForSeconds(_matchShowTime);

                first.gameObject.SetActive(false);
                second.gameObject.SetActive(false);
                _matchedPairCount++;
                _isResolving = false;

                if (_matchedPairCount >= _faceMaterials.Length)
                {
                    _feed.Clear();
                }
                yield break;
            }

            yield return new WaitForSeconds(_mismatchShowTime);

            first.Conceal();
            second.Conceal();
            _isResolving = false;

            if (_hearts.LoseHeart())
            {
                GimmickUtility.ApplyEffects(_failEffects, _player);
            }
        }
        #endregion
    }
}
