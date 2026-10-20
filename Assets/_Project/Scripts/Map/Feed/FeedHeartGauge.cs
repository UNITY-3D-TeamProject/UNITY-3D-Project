using UnityEngine;

namespace Map.Feed
{
    /// <summary>
    /// 악성 피드 미니게임의 남은 기회(하트) 표시. 하트를 잃으면 오른쪽 끝부터 빈 하트 머티리얼로 바꾼다.
    /// </summary>
    public class FeedHeartGauge : MonoBehaviour
    {
        #region Serialized Fields
        [Header("Hearts")]
        [Tooltip("왼쪽부터 순서대로 놓은 하트 렌더러. 이 수가 기회 수다.")]
        [SerializeField] private Renderer[] _hearts;

        [Tooltip("남은 하트 머티리얼")]
        [SerializeField] private Material _fullMaterial;

        [Tooltip("잃은 하트 머티리얼")]
        [SerializeField] private Material _emptyMaterial;
        #endregion

        #region Private Fields
        private int _remaining;
        #endregion

        #region Properties
        /// <summary>남은 하트 수.</summary>
        public int Remaining => _remaining;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            _remaining = _hearts.Length;

            foreach (Renderer heart in _hearts)
            {
                heart.sharedMaterial = _fullMaterial;
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// 하트 하나를 잃는다.
        /// </summary>
        /// <returns>하트가 하나도 남지 않았으면 true</returns>
        public bool LoseHeart()
        {
            if (_remaining <= 0) return true;

            _remaining--;
            _hearts[_remaining].sharedMaterial = _emptyMaterial;
            return _remaining <= 0;
        }
        #endregion
    }
}
