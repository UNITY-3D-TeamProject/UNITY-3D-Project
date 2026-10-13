using UnityEngine;

namespace Map.Platforms
{
    /// <summary>
    /// 평소에는 사라져 있다가 Reveal 이 불리면 일정 시간 동안만 나타나는 발판.
    /// 예: 표적(DamageButton)의 On Pressed 에 Reveal 을 연결하면, 쏴서 발판을 나타나게 할 수 있다.
    /// </summary>
    public class TimedRevealPlatform : MonoBehaviour
    {
        #region Constants
        // 사라지기 전 예고로 깜빡이는 빠르기(초당 켜고 끄는 횟수)
        private const float WARNING_BLINK_RATE = 8f;
        #endregion

        #region Serialized Fields
        [Header("Timing")]
        [Tooltip("나타나 있는 시간(초)")]
        [SerializeField, Min(0.1f)] private float _revealDuration = 5.0f;

        [Tooltip("사라지기 전에 깜빡여 예고하는 시간(초). 0이면 예고하지 않는다.")]
        [SerializeField, Min(0f)] private float _warningDuration = 1.0f;
        #endregion

        #region Private Fields
        private PlatformBody _body;
        private bool _isRevealed;
        private bool _isVisible;
        private float _remainingTime;
        #endregion

        #region Unity Lifecycle
        private void Start()
        {
            // 발판 위에 놓인 오브젝트까지 함께 사라지도록, 자식이 모두 붙은 뒤인 Start 에서 모은다
            _body = new PlatformBody(gameObject);
            _body.SetSolid(false);
        }

        private void Update()
        {
            if (!_isRevealed) return;

            _remainingTime -= Time.deltaTime;
            if (_remainingTime <= 0f)
            {
                _isRevealed = false;
                _body.SetVisible(true);
                _body.SetSolid(false);
                return;
            }

            // 사라지기 직전에는 깜빡인다
            bool isWarning = _remainingTime < _warningDuration;
            bool shouldBeVisible = !isWarning || (Mathf.FloorToInt(_remainingTime * WARNING_BLINK_RATE) % 2 == 0);
            if (shouldBeVisible == _isVisible) return;

            _isVisible = shouldBeVisible;
            _body.SetVisible(_isVisible);
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// 코드로 붙인 직후 호출해 시간을 정한다.
        /// </summary>
        /// <param name="revealDuration">나타나 있는 시간(초)</param>
        /// <param name="warningDuration">사라지기 전에 깜빡이는 시간(초)</param>
        public void Initialize(float revealDuration, float warningDuration)
        {
            _revealDuration = revealDuration;
            _warningDuration = warningDuration;
        }

        /// <summary>
        /// 발판을 나타나게 한다. 이미 나타나 있으면 남은 시간을 처음부터 다시 센다.
        /// </summary>
        public void Reveal()
        {
            if (_body == null) return;

            _remainingTime = _revealDuration;
            if (_isRevealed) return;

            _isRevealed = true;
            _isVisible = true;
            _body.SetSolid(true);
        }
        #endregion
    }
}
