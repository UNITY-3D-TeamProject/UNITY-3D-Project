using UnityEngine;

namespace Map.Feed
{
    /// <summary>
    /// 주변 피드(FeedPost)의 사진. 같은 태그 피드가 같은 부모 아래 여러 개 만들어지므로,
    /// 부모 안에서 몇 번째 피드인지에 따라 사진과 그 사진에 맞는 틀(계정명·글)을 순서대로 붙인다. 이 오브젝트는 FeedPost 의 자식이다.
    /// </summary>
    [RequireComponent(typeof(Renderer))]
    public class FeedPostPhoto : MonoBehaviour
    {
        #region Serialized Fields
        [Header("Photos")]
        [Tooltip("이 태그에서 쓸 사진 머티리얼. 같은 부모의 첫 번째 피드부터 순서대로 하나씩 쓴다.")]
        [SerializeField] private Material[] _photos;

        [Header("Frame")]
        [Tooltip("사진과 짝을 이루는 틀(머리줄·글)을 그리는 렌더러")]
        [SerializeField] private Renderer _frame;

        [Tooltip("사진과 같은 순서의 틀 머티리얼")]
        [SerializeField] private Material[] _frames;

        [Tooltip("콘텐츠 순서. -1이면 기존처럼 부모 안의 순서로 고른다.")]
        [SerializeField] private int _contentIndex = -1;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            SetContent(_contentIndex >= 0 ? _contentIndex : GetOrderInParent());
        }
        #endregion

        #region Public Methods
        /// <summary>사진과 대응하는 계정·본문을 같은 순서로 선택한다.</summary>
        public void SetContent(int contentIndex)
        {
            _contentIndex = contentIndex;
            if ((_photos == null) || (_photos.Length == 0)) return;

            int index = Mathf.Max(0, contentIndex) % _photos.Length;
            GetComponent<Renderer>().sharedMaterial = _photos[index];

            if ((_frame != null) && (_frames != null) && (index < _frames.Length))
            {
                _frame.sharedMaterial = _frames[index];
            }
        }
        #endregion

        #region Private Methods
        // 부모 안에서 이 피드보다 앞에 있는 피드 수. 피드는 생성 순서대로 부모 끝에 붙으므로 생성 순서와 같다.
        private int GetOrderInParent()
        {
            Transform post = transform.parent;
            if ((post == null) || (post.parent == null)) return 0;

            int order = 0;
            for (int i = 0; i < post.GetSiblingIndex(); i++)
            {
                if (post.parent.GetChild(i).GetComponent<FeedPost>() != null)
                {
                    order++;
                }
            }
            return order;
        }
        #endregion
    }
}
