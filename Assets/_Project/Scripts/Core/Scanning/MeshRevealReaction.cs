using UnityEngine;

namespace Core.Scanning
{
    /// <summary>
    /// 스캔 파동에 닿으면 닿은 지점부터 메시 전체가 차례로 드러나는 반응. 플랫폼, 아이템처럼 평소엔 보이지 않는 오브젝트에 쓴다.
    /// 파동이 메시 중간에서 멈추거나 끊겨도 _revealSpeed 로 끝까지 드러나므로 반만 보인 채 남지 않는다.
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
        [Tooltip("닿은 뒤 메시가 드러나는 속도(m/s). 파동이 이보다 빠르면 파동을 따라간다.")]
        [SerializeField, Min(0.0f)] private float _revealSpeed = 10.0f;

        [Header("References")]
        [Tooltip("드러낼 렌더러. 비어 있으면 Awake 에서 자식의 Renderer 를 모두 찾는다.")]
        [SerializeField] private Renderer[] _renderers;
        #endregion

        #region Private Fields
        private MaterialPropertyBlock _propertyBlock;
        private Vector3 _origin;
        // 현재 드러난 반경. 파동 반경과 _revealSpeed 중 더 앞선 쪽을 따라 메시의 가장 먼 점까지 커진다.
        private float _revealRadius;
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
            // 중심은 파동 중심을 그대로 써서 드러나는 앞면이 파동 앞면과 이어지게 한다.
            // 닿은 순간의 파동 반경이 곧 메시에서 가장 가까운 지점이므로 그곳부터 드러나기 시작한다.
            _origin = hit.Origin;
            _revealRadius = hit.Radius;

            // 렌더러를 켜고, 첫 프레임부터 반경 안쪽만 드러나게 셰이더 값을 넣는다.
            SetRenderersEnabled(true);
            ApplyProperties(_revealRadius);
        }

        /// <inheritdoc />
        public override void OnScanUpdate(in SScanHit hit)
        {
            // 파동이 드러나는 속도보다 빠르면 파동을 따라간다. 이미 더 드러났으면 줄이지 않는다.
            _origin = hit.Origin;
            _revealRadius = Mathf.Max(_revealRadius, hit.Radius);
            ApplyProperties(_revealRadius);
        }

        /// <inheritdoc />
        public override void OnFade(float fade, bool isFadingOut)
        {
            // 파동 중심에서 이 메시의 가장 가까운 점과 가장 먼 점까지의 거리를 구한다.
            GetDistanceRange(out float nearDistance, out float farDistance);

            // 활성 중에는 파동이 멈추거나 끊겨도 가장 먼 점까지 계속 드러낸다(매 프레임 호출된다).
            // 사라지는 중 재스캔으로 활성 상태에 돌아와도 마지막 드러난 반경에서 다시 이어진다.
            if (!isFadingOut)
            {
                // 파동이 이미 가장 먼 점을 지났으면 줄이지 않는다.
                if (_revealRadius < farDistance)
                {
                    _revealRadius = Mathf.MoveTowards(_revealRadius, farDistance, _revealSpeed * Time.deltaTime);
                }

                ApplyProperties(_revealRadius);
                return;
            }

            // 사라질 때만 보이는 범위를 접는다.
            // 메시 전체가 드러난 뒤에는 반경이 더 커도 의미가 없으므로 가장 먼 점까지로 제한한다.
            float visibleRadius = Mathf.Min(farDistance, _revealRadius);

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

            // 숨기지 않는 대상은 스캔이 쓴 값만 머티리얼 기본값으로 되돌린다(다른 효과의 값은 건드리지 않는다).
            RestoreProperties();
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

        /// <summary>
        /// 스캔이 쓰는 두 값(_ScanOrigin, _ScanRadius)만 렌더러 머티리얼의 기본값으로 되돌린다.
        /// 렌더러의 나머지 PropertyBlock 값은 유지한다.
        /// </summary>
        private void RestoreProperties()
        {
            foreach (Renderer targetRenderer in _renderers)
            {
                if (!targetRenderer) continue;

                Material material = targetRenderer.sharedMaterial;
                targetRenderer.GetPropertyBlock(_propertyBlock);

                // 머티리얼에 해당 프로퍼티가 없으면 스캔 셰이더가 아니므로 건드리지 않는다.
                if (material && material.HasVector(ScanOriginId))
                {
                    _propertyBlock.SetVector(ScanOriginId, material.GetVector(ScanOriginId));
                }

                if (material && material.HasFloat(ScanRadiusId))
                {
                    _propertyBlock.SetFloat(ScanRadiusId, material.GetFloat(ScanRadiusId));
                }

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
