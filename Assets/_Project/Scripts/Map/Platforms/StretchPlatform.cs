using UnityEngine;

namespace Map.Platforms
{
    /// <summary>
    /// 진행 방향(로컬 Z) 길이가 주기적으로 늘었다 줄어드는 발판. 줄어들 때는 이웃 발판과의 틈이 벌어진다.
    /// </summary>
    public class StretchPlatform : MonoBehaviour
    {
        #region Serialized Fields
        [Header("Stretch")]
        [Tooltip("가장 짧을 때의 길이 비율 (1 = 원래 길이)")]
        [SerializeField, Range(0.05f, 1f)] private float _minRatio = 0.4f;

        [Tooltip("늘었다 줄어드는 빠르기")]
        [SerializeField, Min(0f)] private float _speed = 1.5f;

        [Tooltip("시작 시점 어긋남(라디안). 발판마다 다르게 주면 박자가 어긋난다.")]
        [SerializeField] private float _phase;
        #endregion

        #region Private Fields
        private Vector3 _baseScale;
        #endregion

        #region Unity Lifecycle
        private void Start()
        {
            _baseScale = transform.localScale;
        }

        private void Update()
        {
            // 0 ~ 1 사이를 부드럽게 오간다
            float wave = (Mathf.Sin((Time.time * _speed) + _phase) + 1f) * 0.5f;
            float ratio = Mathf.Lerp(_minRatio, 1f, wave);

            transform.localScale = new Vector3(_baseScale.x, _baseScale.y, _baseScale.z * ratio);
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// 코드로 붙인 직후 호출해 움직임을 정한다.
        /// </summary>
        /// <param name="minRatio">가장 짧을 때의 길이 비율</param>
        /// <param name="speed">늘었다 줄어드는 빠르기</param>
        /// <param name="phase">시작 시점 어긋남(라디안)</param>
        public void Initialize(float minRatio, float speed, float phase)
        {
            _minRatio = minRatio;
            _speed = speed;
            _phase = phase;
        }
        #endregion
    }
}
