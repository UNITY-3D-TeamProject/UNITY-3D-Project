using System;
using UnityEngine;
using Map.Gimmicks;

namespace Map.Feed
{
    /// <summary>
    /// 카드 짝 맞추기(악성 피드 4)의 카드 한 장. 총에 맞으면 OnShot 으로 알리고, 앞뒤 표시는 CardMatchFeedClear 가 정한다.
    /// 같은 오브젝트의 DamageButton 은 One Shot 을 끄고(여러 번 맞을 수 있게) 콜라이더는 공격이 맞는 레이어(Obstacle)로 둔다.
    /// </summary>
    [RequireComponent(typeof(DamageButton))]
    public class FeedMatchCard : MonoBehaviour
    {
        #region Serialized Fields
        [Header("Visual")]
        [Tooltip("그림이 보이는 앞면 렌더러")]
        [SerializeField] private Renderer _front;

        [Tooltip("덮였을 때 보이는 뒷면")]
        [SerializeField] private GameObject _back;
        #endregion

        #region Private Fields
        private DamageButton _button;
        #endregion

        #region Properties
        /// <summary>같은 그림끼리 같은 번호.</summary>
        public int PairId { get; private set; }

        /// <summary>앞면이 보이는 중인지 여부.</summary>
        public bool IsFaceUp { get; private set; }
        #endregion

        #region Events
        /// <summary>총에 맞았을 때 호출된다.</summary>
        public event Action<FeedMatchCard> OnShot;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            _button = GetComponent<DamageButton>();
            _button.OnPressed.AddListener(() => OnShot?.Invoke(this));

            Debug.Assert(_front != null, $"[{name}] Front가 연결되지 않았습니다.");
            Debug.Assert(_back != null, $"[{name}] Back이 연결되지 않았습니다.");

            Conceal();
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// 그림을 정한다.
        /// </summary>
        /// <param name="pairId">같은 그림끼리 같은 번호</param>
        /// <param name="face">앞면 머티리얼</param>
        public void SetFace(int pairId, Material face)
        {
            PairId = pairId;
            _front.sharedMaterial = face;
        }

        /// <summary>앞면을 보여 준다.</summary>
        public void Reveal()
        {
            IsFaceUp = true;
            _front.gameObject.SetActive(true);
            _back.SetActive(false);
        }

        /// <summary>뒷면으로 덮는다.</summary>
        public void Conceal()
        {
            IsFaceUp = false;
            _front.gameObject.SetActive(false);
            _back.SetActive(true);
        }
        #endregion
    }
}
