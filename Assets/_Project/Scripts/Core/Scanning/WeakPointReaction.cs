using UnityEngine;

namespace Core.Scanning
{
    /// <summary>
    /// 스캔에 닿으면 적의 약점 표시를 드러내는 반응. 표시는 반응 중에만 켜지고,
    /// 나타날 때 커지고 사라질 때 작아지며 서서히 사라진다.
    /// 약점의 피격 판정과 데미지 배율은 WeakPoint 가 맡으며, 이 반응은 보이는 것만 다룬다.
    /// </summary>
    public class WeakPointReaction : ScanReactionBase
    {
        #region Serialized Fields
        [Header("References")]
        [Tooltip("약점 위치에 둔 표시 오브젝트(발광 메시 등). 평소에는 꺼진다. 벽 뒤에서도 보이게 하려면 깊이 테스트를 끈 머티리얼을 쓴다.")]
        [SerializeField] private Transform[] _markers;
        #endregion

        #region Private Fields
        private Vector3[] _baseScales;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            Debug.Assert((_markers != null) && (_markers.Length > 0), $"[{name}] 약점 표시 오브젝트가 연결되지 않았습니다.");
            if ((_markers == null) || (_markers.Length == 0)) return;

            // 페이드 때 원래 크기에 fade 를 곱해 쓰므로, 표시마다 기본 크기를 저장해 둔다.
            _baseScales = new Vector3[_markers.Length];
            for (int i = 0; i < _markers.Length; i++)
            {
                if (!_markers[i]) continue;

                _baseScales[i] = _markers[i].localScale;

                // 스캔에 닿기 전에는 약점 표시가 보이지 않는다.
                _markers[i].gameObject.SetActive(false);
            }
        }
        #endregion

        #region Public Methods
        /// <inheritdoc />
        public override void OnScanBegin(in SScanHit hit)
        {
            SetMarkersActive(true);
        }

        /// <inheritdoc />
        public override void OnFade(float fade, bool isFadingOut)
        {
            // Awake 에서 표시가 연결되지 않아 초기화되지 않았다면 할 일이 없다.
            if (_baseScales == null) return;

            // fade 0~1 에 맞춰 기본 크기의 0~100% 로 키우거나 줄인다(나타날 때 커지고 사라질 때 작아짐).
            for (int i = 0; i < _markers.Length; i++)
            {
                if (_markers[i]) _markers[i].localScale = _baseScales[i] * fade;
            }
        }

        /// <inheritdoc />
        public override void OnScanEnd()
        {
            if (_baseScales == null) return;

            // 크기가 0으로 남아 있으면 다음 스캔 때 보이지 않으므로 기본 크기로 되돌린 뒤 끈다.
            for (int i = 0; i < _markers.Length; i++)
            {
                if (_markers[i]) _markers[i].localScale = _baseScales[i];
            }
            SetMarkersActive(false);
        }
        #endregion

        #region Private Methods
        private void SetMarkersActive(bool isActive)
        {
            if (_markers == null) return;

            foreach (Transform marker in _markers)
            {
                if (marker) marker.gameObject.SetActive(isActive);
            }
        }
        #endregion
    }
}
