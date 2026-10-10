using UnityEngine;

namespace Core.Scanning
{
    /// <summary>
    /// 스캔 파동이 지나간 만큼 메시가 드러나는 반응. 플랫폼, 아이템처럼 평소엔 보이지 않는 오브젝트에 쓴다.
    /// 렌더러에는 _ScanOrigin, _ScanRadius 프로퍼티를 쓰는 리빌 셰이더(M_ScanReveal 등) 머티리얼을 지정한다.
    /// 사라질 때는 표시 반경을 줄여 바깥쪽부터 접히며 사라진다. 콜라이더는 건드리지 않는다.
    /// </summary>
    public class MeshRevealReaction : ScanReactionBase
    {
        #region Static Fields
        private static readonly int ScanOriginId = Shader.PropertyToID("_ScanOrigin");
        private static readonly int ScanRadiusId = Shader.PropertyToID("_ScanRadius");
        #endregion

        #region Serialized Fields
        [Header("Settings")]
        [Tooltip("스캔에 닿기 전과 사라진 뒤에 렌더러를 꺼서 숨길지 여부. 항상 보여야 하는 몸체에는 끈다.")]
        [SerializeField] private bool _shouldHideWhenIdle = true;

        [Header("References")]
        [Tooltip("드러낼 렌더러. 비어 있으면 Awake 에서 자식의 Renderer 를 모두 찾는다.")]
        [SerializeField] private Renderer[] _renderers;
        #endregion

        #region Private Fields
        private MaterialPropertyBlock _propertyBlock;
        private Vector3 _origin;
        private float _waveRadius;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            _propertyBlock = new MaterialPropertyBlock();

            if ((_renderers == null) || (_renderers.Length == 0))
            {
                _renderers = GetComponentsInChildren<Renderer>(true);
            }

            if (_shouldHideWhenIdle)
            {
                SetRenderersEnabled(false);
            }
        }
        #endregion

        #region Public Methods
        /// <inheritdoc />
        public override void OnScanBegin(in SScanHit hit)
        {
            _origin = hit.Origin;
            _waveRadius = hit.Radius;
            SetRenderersEnabled(true);
            ApplyProperties(_waveRadius);
        }

        /// <inheritdoc />
        public override void OnScanUpdate(in SScanHit hit)
        {
            _origin = hit.Origin;
            _waveRadius = hit.Radius;
            ApplyProperties(_waveRadius);
        }

        /// <inheritdoc />
        public override void OnFade(float fade, bool isFadingOut)
        {
            // 드러나는 동안은 파동 반경 그대로 두고, 사라질 때만 보이는 범위를 접는다.
            if (!isFadingOut) return;

            GetDistanceRange(out float nearDistance, out float farDistance);
            float visibleRadius = Mathf.Min(farDistance, _waveRadius);
            ApplyProperties(Mathf.Lerp(nearDistance, visibleRadius, fade));
        }

        /// <inheritdoc />
        public override void OnScanEnd()
        {
            if (_shouldHideWhenIdle)
            {
                SetRenderersEnabled(false);
                return;
            }

            // 숨기지 않는 대상은 리빌 값을 지워 원래 모습으로 돌린다.
            foreach (Renderer targetRenderer in _renderers)
            {
                if (targetRenderer) targetRenderer.SetPropertyBlock(null);
            }
        }
        #endregion

        #region Private Methods
        private void ApplyProperties(float radius)
        {
            foreach (Renderer targetRenderer in _renderers)
            {
                if (!targetRenderer) continue;

                targetRenderer.GetPropertyBlock(_propertyBlock);
                _propertyBlock.SetVector(ScanOriginId, _origin);
                _propertyBlock.SetFloat(ScanRadiusId, radius);
                targetRenderer.SetPropertyBlock(_propertyBlock);
            }
        }

        private void SetRenderersEnabled(bool isEnabled)
        {
            foreach (Renderer targetRenderer in _renderers)
            {
                if (targetRenderer) targetRenderer.enabled = isEnabled;
            }
        }

        /// <summary>
        /// 파동 중심에서 렌더러들까지의 가장 가까운 거리와 가장 먼 거리를 구한다.
        /// </summary>
        /// <param name="nearDistance">가장 가까운 지점까지의 거리</param>
        /// <param name="farDistance">가장 먼 지점까지의 거리(바운드 중심 거리 + 반 대각선)</param>
        private void GetDistanceRange(out float nearDistance, out float farDistance)
        {
            float nearSqr = float.MaxValue;
            float far = 0.0f;

            foreach (Renderer targetRenderer in _renderers)
            {
                if (!targetRenderer) continue;

                Bounds bounds = targetRenderer.bounds;
                nearSqr = Mathf.Min(nearSqr, bounds.SqrDistance(_origin));
                far = Mathf.Max(far, Vector3.Distance(bounds.center, _origin) + bounds.extents.magnitude);
            }

            nearDistance = (nearSqr == float.MaxValue) ? 0.0f : Mathf.Sqrt(nearSqr);
            farDistance = far;
        }
        #endregion
    }
}
