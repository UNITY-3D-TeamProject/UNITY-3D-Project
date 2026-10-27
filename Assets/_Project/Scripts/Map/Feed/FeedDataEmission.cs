using UnityEngine;
using Map.Gimmicks;

namespace Map.Feed
{
    /// <summary>카드 표면에서 외형을 방출하고, 피드의 정화·배치 연출이 끝나면 추적을 허용한다.</summary>
    public class FeedDataEmission : MonoBehaviour
    {
        #region Constants
        private const float EMISSION_DURATION = 1.0f;
        #endregion

        #region Private Fields
        private Transform _visual;
        private Vector3 _startPosition;
        private Vector3 _restPosition;
        private float _elapsed;
        private HomingHazard _hazard;
        private FeedPurification _purification;
        #endregion

        #region Unity Lifecycle
        private void LateUpdate()
        {
            if (_visual == null)
            {
                Destroy(this);
                return;
            }

            _elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(_elapsed / EMISSION_DURATION);
            _visual.localPosition = Vector3.Lerp(_startPosition, _restPosition,
                Mathf.SmoothStep(0.0f, 1.0f, progress));
            bool hasPurificationFinished = (_purification == null) || _purification.IsFinished;
            if ((progress >= 1.0f) && hasPurificationFinished)
            {
                if (_hazard != null) _hazard.SetMovementSuspended(false);
                Destroy(this);
            }
        }

        private void OnDestroy()
        {
            // 연출 컴포넌트만 먼저 제거돼도 추적 정지가 남지 않게 한다.
            if (_hazard != null) _hazard.SetMovementSuspended(false);
        }
        #endregion

        #region Public Methods
        public void Begin(Transform visual, Vector3 surfacePosition, FeedPurification purification)
        {
            _purification = purification;
            _hazard = GetComponent<HomingHazard>();
            if (_hazard != null) _hazard.SetMovementSuspended(true);
            _visual = visual;
            _restPosition = visual.localPosition;
            _startPosition = visual.parent.InverseTransformPoint(surfacePosition);
            _visual.localPosition = _startPosition;
        }
        #endregion
    }
}
