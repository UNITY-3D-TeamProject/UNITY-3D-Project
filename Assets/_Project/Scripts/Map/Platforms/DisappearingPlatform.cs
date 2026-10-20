using UnityEngine;

namespace Map.Platforms
{
    public class DisappearingPlatform : MonoBehaviour
    {
        // 사라지기 전 예고로 깜빡이는 빠르기(초당 켜고 끄는 횟수)
        private const float WARNING_BLINK_RATE = 8f;

        public float activeDuration = 2f;
        public float inactiveDuration = 1.5f;
        public float timeOffset = 0f;

        [Tooltip("사라지기 전에 깜빡여 예고하는 시간(초). 0이면 예고하지 않는다.")]
        public float warningDuration = 0f;

        private PlatformBody body;
        private bool isSolid = true;
        private bool isVisible = true;

        public bool IsSolid => isSolid;
        public float VisibleRatio
        {
            get
            {
                float t = Mathf.Repeat(Time.time + timeOffset, Mathf.Max(activeDuration + inactiveDuration, 0.001f));
                return (t < activeDuration) ? 1f - (t / Mathf.Max(activeDuration, 0.001f)) : 0f;
            }
        }

        void Start()
        {
            // 발판 위에 놓인 오브젝트까지 함께 사라지도록, 자식이 모두 붙은 뒤인 Start 에서 모은다
            body = new PlatformBody(gameObject);
        }

        void Update()
        {
            float cycle = activeDuration + inactiveDuration;

            // 시간 어긋남이 음수여도 0 ~ cycle 사이로 맞춘다
            float t = Mathf.Repeat(Time.time + timeOffset, cycle);

            bool isActive = t < activeDuration;
            SetPlatformActive(isActive);

            if (!isActive) return;

            // 사라지기 직전에는 깜빡인다
            bool isWarning = (activeDuration - t) < warningDuration;
            bool shouldBeVisible = !isWarning || (Mathf.FloorToInt(t * WARNING_BLINK_RATE) % 2 == 0);
            if (shouldBeVisible == isVisible) return;

            isVisible = shouldBeVisible;
            body.SetVisible(isVisible);
        }

        private void SetPlatformActive(bool isActive)
        {
            if (isActive == isSolid) return;

            isSolid = isActive;
            isVisible = isActive;
            body.SetSolid(isActive);
        }
    }
}
