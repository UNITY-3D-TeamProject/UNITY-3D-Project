using UnityEngine;

namespace Core.Scanning
{
    /// <summary>
    /// 스캔에 닿으면 위치 마커를 보여주는 반응. 상호작용 오브젝트처럼 위치를 알려야 하는 대상에 쓴다.
    /// 마커는 반응 중에만 켜지고, 사라질 때는 스프라이트 알파가 서서히 줄어든다.
    /// 마커가 항상 카메라를 보게 하려면 마커에 Map.Common.Billboard 를 붙이고,
    /// 벽 뒤에서도 보이게 하려면 깊이 테스트를 끈 머티리얼을 쓴다.
    /// </summary>
    public class MarkerReaction : ScanReactionBase
    {
        #region Serialized Fields
        [Header("References")]
        [Tooltip("스캔에 닿았을 때 켜질 마커 오브젝트. 평소에는 꺼진다.")]
        [SerializeField] private GameObject _marker;
        [Tooltip("알파를 줄일 스프라이트. 비어 있으면 마커 아래에서 찾는다. 없으면 켜고 끄기만 한다.")]
        [SerializeField] private SpriteRenderer _spriteRenderer;
        #endregion

        #region Private Fields
        private Color _baseColor = Color.white;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            Debug.Assert(_marker != null, $"[{name}] 마커 오브젝트가 연결되지 않았습니다.");
            if (!_marker) return;

            // 스프라이트를 따로 지정하지 않았으면 마커 아래에서 찾는다(꺼져 있어도 찾도록 true).
            if (!_spriteRenderer) _spriteRenderer = _marker.GetComponentInChildren<SpriteRenderer>(true);

            // 페이드 때 알파만 곱해 쓰므로, 원래 색을 미리 저장해 둔다.
            if (_spriteRenderer) _baseColor = _spriteRenderer.color;

            // 스캔에 닿기 전에는 마커가 보이지 않는다.
            _marker.SetActive(false);
        }
        #endregion

        #region Public Methods
        /// <inheritdoc />
        public override void OnScanBegin(in SScanHit hit)
        {
            if (_marker) _marker.SetActive(true);
        }

        /// <inheritdoc />
        public override void OnFade(float fade, bool isFadingOut)
        {
            // 스프라이트가 없으면 알파 조절 없이 켜고 끄기만 한다.
            if (!_spriteRenderer) return;

            // 원래 색은 유지하고 알파만 fade(0~1)에 비례해 줄인다.
            Color color = _baseColor;
            color.a = _baseColor.a * fade;
            _spriteRenderer.color = color;
        }

        /// <inheritdoc />
        public override void OnScanEnd()
        {
            // 다음 스캔을 위해 알파를 원래대로 돌려놓고 마커를 끈다.
            if (_spriteRenderer) _spriteRenderer.color = _baseColor;
            if (_marker) _marker.SetActive(false);
        }
        #endregion
    }
}
