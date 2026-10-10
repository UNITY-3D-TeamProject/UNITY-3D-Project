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
            // 머티리얼을 복제하지 않고 렌더러별로 셰이더 값만 덮어쓰기 위한 블록이다.
            _propertyBlock = new MaterialPropertyBlock();

            // 렌더러를 지정하지 않았으면 자식 렌더러 전부를 대상으로 한다.
            if ((_renderers == null) || (_renderers.Length == 0))
            {
                _renderers = GetComponentsInChildren<Renderer>(true);
            }

            // 스캔에 닿기 전에는 보이지 않게 렌더러를 끈다(콜라이더는 그대로라 부딪힐 수 있다).
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
            // 파동 중심과 현재 반경을 저장한다(사라지는 단계에서도 이 값을 쓴다).
            _origin = hit.Origin;
            _waveRadius = hit.Radius;

            // 렌더러를 켜고, 첫 프레임부터 반경 안쪽만 드러나게 셰이더 값을 넣는다.
            SetRenderersEnabled(true);
            ApplyProperties(_waveRadius);
        }

        /// <inheritdoc />
        public override void OnScanUpdate(in SScanHit hit)
        {
            // 파동이 커지는 동안 반경을 매 프레임 갱신해 메시가 점점 드러나게 한다.
            _origin = hit.Origin;
            _waveRadius = hit.Radius;
            ApplyProperties(_waveRadius);
        }

        /// <inheritdoc />
        public override void OnFade(float fade, bool isFadingOut)
        {
            // 드러나는 동안은 파동 반경 그대로 두고, 사라질 때만 보이는 범위를 접는다.
            if (!isFadingOut) return;

            // 파동 중심에서 이 메시의 가장 가까운 점과 가장 먼 점까지의 거리를 구한다.
            GetDistanceRange(out float nearDistance, out float farDistance);

            // 메시 전체가 드러난 뒤에는 파동이 더 커져도 의미가 없으므로 가장 먼 점까지로 제한한다.
            float visibleRadius = Mathf.Min(farDistance, _waveRadius);

            // fade 1 -> 0 에 따라 보이는 반경을 바깥에서 가장 가까운 점 쪽으로 접어 사라지게 한다.
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
                // 파괴된 렌더러는 건너뛴다.
                if (!targetRenderer) continue;

                // 렌더러에 이미 있던 값을 유지한 채 스캔 값만 덮어쓴다.
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
            // 비교를 위해 가까운 쪽은 큰 값, 먼 쪽은 0 에서 시작한다. 제곱 거리로 비교하다 마지막에 한 번만 제곱근을 구한다.
            float nearSqr = float.MaxValue;
            float far = 0.0f;

            foreach (Renderer targetRenderer in _renderers)
            {
                if (!targetRenderer) continue;

                // 렌더러가 여러 개면 전체 중 가장 가까운 점과 가장 먼 점을 찾는다.
                Bounds bounds = targetRenderer.bounds;
                nearSqr = Mathf.Min(nearSqr, bounds.SqrDistance(_origin));
                far = Mathf.Max(far, Vector3.Distance(bounds.center, _origin) + bounds.extents.magnitude);
            }

            // 유효한 렌더러가 하나도 없었다면 0 으로 돌려준다.
            nearDistance = (nearSqr == float.MaxValue) ? 0.0f : Mathf.Sqrt(nearSqr);
            farDistance = far;
        }
        #endregion
    }
}
