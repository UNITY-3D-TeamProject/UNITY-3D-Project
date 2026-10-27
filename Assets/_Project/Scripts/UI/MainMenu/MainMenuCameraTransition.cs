using System.Collections;
using UnityEngine;

namespace UI.MainMenu
{
    [DisallowMultipleComponent]
    public class MainMenuCameraTransition : MonoBehaviour
    {
        #region Serialized Fields
        [Header("References")]
        [SerializeField] private Camera _camera;
        [SerializeField] private Transform _menuPose;
        [Tooltip("비워 두면 직선 이동. 지정하면 이 지점 방향으로 휘어지는 경로를 사용한다.")]
        [SerializeField] private Transform _approachPoint;
        [SerializeField] private CanvasGroup _menuGroup;
        [SerializeField] private CanvasGroup _sceneRevealGroup;

        [Header("Transition")]
        [SerializeField, Min(0.0f)] private float _duration = 2.2f;
        [SerializeField, Range(1.0f, 179.0f)] private float _menuFieldOfView = 45.0f;
        [SerializeField, Min(0.0f)] private float _overviewHoldDuration = 0.4f;
        [SerializeField, Min(0.0f)] private float _menuRevealDuration = 0.25f;
        [SerializeField, Min(0.0f)] private float _sceneRevealDuration = 0.75f;
        #endregion

        #region Private Fields
        private Coroutine _transition;
        #endregion

        #region Unity Lifecycle
        private void OnEnable()
        {
            if ((_camera == null) || (_menuPose == null)) return;

            if (_menuGroup != null)
            {
                _menuGroup.alpha = 0.0f;
                _menuGroup.interactable = false;
                _menuGroup.blocksRaycasts = false;
            }

            if (_sceneRevealGroup != null)
                _sceneRevealGroup.alpha = 1.0f;

            _transition = StartCoroutine(CoMoveToMenu());
        }

        private void OnDisable()
        {
            if (_transition == null) return;

            StopCoroutine(_transition);
            _transition = null;
        }
        #endregion

        #region Coroutines
        private IEnumerator CoMoveToMenu()
        {
            Transform cameraTransform = _camera.transform;
            Vector3 startPosition = cameraTransform.position;
            Quaternion startRotation = cameraTransform.rotation;
            float startFieldOfView = _camera.fieldOfView;
            float elapsed = 0.0f;

            if (_overviewHoldDuration > 0.0f)
                yield return new WaitForSecondsRealtime(_overviewHoldDuration);

            while (elapsed < _duration)
            {
                if (_sceneRevealGroup != null)
                {
                    float revealProgress = _sceneRevealDuration > 0.0f
                        ? Mathf.Clamp01(elapsed / _sceneRevealDuration)
                        : 1.0f;
                    _sceneRevealGroup.alpha = 1.0f - Mathf.SmoothStep(0.0f, 1.0f, revealProgress);
                }

                float progress = Mathf.SmoothStep(0.0f, 1.0f, elapsed / _duration);
                Vector3 position = Vector3.Lerp(startPosition, _menuPose.position, progress);
                if (_approachPoint != null)
                {
                    Vector3 firstLeg = Vector3.Lerp(startPosition, _approachPoint.position, progress);
                    Vector3 secondLeg = Vector3.Lerp(_approachPoint.position, _menuPose.position, progress);
                    position = Vector3.Lerp(firstLeg, secondLeg, progress);
                }

                cameraTransform.SetPositionAndRotation(
                    position,
                    Quaternion.Slerp(startRotation, _menuPose.rotation, progress));
                _camera.fieldOfView = Mathf.Lerp(startFieldOfView, _menuFieldOfView, progress);

                yield return null;
                elapsed += Time.unscaledDeltaTime;
            }

            cameraTransform.SetPositionAndRotation(_menuPose.position, _menuPose.rotation);
            _camera.fieldOfView = _menuFieldOfView;

            if (_sceneRevealGroup != null)
                _sceneRevealGroup.alpha = 0.0f;

            if (_menuGroup != null)
            {
                elapsed = 0.0f;
                while (elapsed < _menuRevealDuration)
                {
                    _menuGroup.alpha = Mathf.SmoothStep(0.0f, 1.0f, elapsed / _menuRevealDuration);
                    yield return null;
                    elapsed += Time.unscaledDeltaTime;
                }

                _menuGroup.alpha = 1.0f;
                _menuGroup.interactable = true;
                _menuGroup.blocksRaycasts = true;
            }

            _transition = null;
        }
        #endregion
    }
}
