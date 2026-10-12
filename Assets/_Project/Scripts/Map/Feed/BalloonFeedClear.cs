using UnityEngine;
using Map.Gimmicks;

namespace Map.Feed
{
    /// <summary>
    /// 악성 피드 미니게임 2: 악성 피드를 매달고 있는 풍선을 모두 쏴서 터뜨리면 제거된다.
    /// 풍선은 DamageButton(1회용)으로 만들고, HP가 0이 되면 터진 것으로 본다.
    /// </summary>
    [RequireComponent(typeof(MaliciousFeed))]
    public class BalloonFeedClear : MonoBehaviour
    {
        #region Serialized Fields
        [Header("Balloons")]
        [Tooltip("터뜨려야 하는 풍선들")]
        [SerializeField] private DamageButton[] _balloons;
        #endregion

        #region Private Fields
        private MaliciousFeed _feed;
        private int _remainingCount;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            _feed = GetComponent<MaliciousFeed>();

            foreach (DamageButton balloon in _balloons)
            {
                if (balloon == null) continue;

                _remainingCount++;

                // 어느 풍선이 터졌는지 알 수 있도록 풍선마다 따로 연결한다
                DamageButton poppedBalloon = balloon;
                balloon.OnPressed.AddListener(() => HandleBalloonPopped(poppedBalloon));
            }
        }
        #endregion

        #region Private Methods
        private void HandleBalloonPopped(DamageButton balloon)
        {
            Destroy(balloon.gameObject);

            _remainingCount--;
            if (_remainingCount > 0) return;

            _feed.Clear();
        }
        #endregion
    }
}
