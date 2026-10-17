using UnityEngine;

namespace Map.Lobby.SkySign
{
    /// <summary>
    /// 하늘 전광판 화면 카메라들을 매 프레임 그리지 않고, 한 프레임에 한 대씩 번갈아 그린다.
    /// 그리지 않는 동안 RenderTexture 는 마지막 화면을 그대로 보여 준다.
    /// </summary>
    public class SkySignCameraScheduler : MonoBehaviour
    {
        #region Serialized Fields
        [SerializeField] private Camera[] _cameras;

        [Tooltip("전광판 한 개가 1초에 다시 그려지는 횟수")]
        [SerializeField] private float _refreshRate = 15.0f;
        #endregion

        #region Private Fields
        private float _timer;
        private int _nextIndex;
        private bool _hasRenderedAll;
        #endregion

        #region Unity Lifecycle
        private void LateUpdate()
        {
            // 첫 프레임은 켜진 그대로 모두 그려 빈 화면이 보이지 않게 한다.
            if (!_hasRenderedAll)
            {
                _hasRenderedAll = true;
                return;
            }

            SetAllEnabled(false);

            float step = 1.0f / (_refreshRate * _cameras.Length);
            _timer += Time.unscaledDeltaTime;
            if (_timer < step) return;

            // 밀린 만큼 한꺼번에 그리지 않도록 나머지만 남긴다.
            _timer %= step;
            _cameras[_nextIndex].enabled = true;
            _nextIndex = (_nextIndex + 1) % _cameras.Length;
        }

        private void OnDisable()
        {
            SetAllEnabled(true);
            _hasRenderedAll = false;
        }
        #endregion

        #region Private Methods
        private void SetAllEnabled(bool isEnabled)
        {
            foreach (Camera screenCamera in _cameras)
            {
                screenCamera.enabled = isEnabled;
            }
        }
        #endregion
    }
}
