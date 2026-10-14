using UnityEngine;

namespace Core.Scanning
{
    /// <summary>
    /// 스캔 파동에 닿으면 닿은 지점부터 메시 전체가 차례로 드러나는 반응. 숨은 플랫폼처럼 평소엔 보이지 않는 오브젝트에 쓴다(평소에도 보이는 아이템은 SilhouetteReaction).
    /// 파동이 메시 중간에서 멈추거나 끊겨도 _revealSpeed 로 끝까지 드러나므로 반만 보인 채 남지 않는다.
    /// 렌더러에는 _ScanOrigin, _ScanRadius 프로퍼티를 쓰는 리빌 셰이더(M_ScanReveal 등) 머티리얼을 지정한다.
    /// 사라질 때는 표시 반경을 줄여 바깥쪽부터 접히며 사라진다. 콜라이더는 건드리지 않는다.
    /// 메시 전체가 다 드러난 순간의 Time.time 을 _ScanRevealTime 으로 넘긴다(아니면 -1). 완료 발광 등에 쓴다.
    /// </summary>
    public class MeshRevealReaction : ScanReactionBase
    {
        #region Static Fields
        private static readonly int ScanOriginId = Shader.PropertyToID("_ScanOrigin");
        private static readonly int ScanRadiusId = Shader.PropertyToID("_ScanRadius");
        private static readonly int ScanRevealTimeId = Shader.PropertyToID("_ScanRevealTime");
        #endregion

        #region Serialized Fields
        [Header("Settings")]
        [Tooltip("스캔에 닿기 전과 사라진 뒤에 렌더러를 꺼서 숨길지 여부. 항상 보여야 하는 몸체에는 끈다.")]
        [SerializeField] private bool _shouldHideWhenIdle = true;
        [Tooltip("닿은 뒤 메시가 드러나는 속도(m/s). 파동이 이보다 빠르면 파동을 따라간다.")]
        [SerializeField, Min(0.0f)] private float _revealSpeed = 10.0f;

        [Header("References (비워 두면 자동으로 찾음)")]
        [Tooltip("드러낼 렌더러. 비어 있으면 Awake 에서 자식의 Renderer 를 모두 찾는다.")]
        [SerializeField] private Renderer[] _renderers;
        #endregion

        #region Private Fields
        private MaterialPropertyBlock _propertyBlock;
        private Vector3 _origin;
        // 현재 드러난 반경. 파동 반경과 _revealSpeed 중 더 앞선 쪽을 따라 메시의 가장 먼 점까지 커진다.
        private float _revealRadius;
        // 마지막으로 셰이더에 넣은 표시 반경. 같은 값이면 다시 쓰지 않기 위해 쓴다. 음수면 아직 넣지 않은 상태다.
        private float _appliedRadius = -1.0f;
        // 메시 전체가 다 드러났는지 여부. 사라지기 시작하면 다시 false 가 된다.
        private bool _isFullyRevealed;
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

            // 이전 스캔에서 넣은 값과 중심이 다르므로 같은 반경이어도 다시 넣게 한다.
            _appliedRadius = -1.0f;
            SetFullyRevealed(false);

            // 렌더러를 켜고, 세기 0 에서 시작하므로 가장 가까운 점부터 드러나게 셰이더 값을 넣는다.
            GetDistanceRange(out float nearDistance, out _);
            SetRenderersEnabled(true);
            ApplyProperties(nearDistance);
        }

        /// <inheritdoc />
        public override void OnScanUpdate(in SScanHit hit)
        {
            // 파동이 드러나는 속도보다 빠르면 파동을 따라간다. 이미 더 드러났으면 줄이지 않는다.
            // 셰이더 적용은 매 프레임 호출되는 OnFade 에서 한 번만 한다.
            _revealRadius = Mathf.Max(_revealRadius, hit.Radius);
        }

        /// <inheritdoc />
        public override void OnFade(float fade, bool isFadingOut)
        {
            // 파동 중심에서 이 메시의 가장 가까운 점과 가장 먼 점까지의 거리를 구한다.
            // 움직이는 대상도 있으므로 매 프레임 다시 구한다.
            GetDistanceRange(out float nearDistance, out float farDistance);

            // 활성 중에는 파동이 멈추거나 끊겨도 가장 먼 점까지 계속 드러낸다. 이미 지났으면 줄이지 않는다.
            if (!isFadingOut && (_revealRadius < farDistance))
            {
                _revealRadius = Mathf.MoveTowards(_revealRadius, farDistance, _revealSpeed * Time.deltaTime);
            }

            // 드러난 반경(가장 먼 점까지로 제한)을 세기만큼 펼쳐 보인다.
            // 사라질 때는 세기가 1 -> 0 으로 줄며 바깥에서 가장 가까운 점 쪽으로 접히고,
            // 사라지는 중 재스캔되면 세기가 다시 올라가므로 접힌 지점부터 이어서 펼쳐진다.
            float visibleRadius = Mathf.Min(farDistance, _revealRadius);
            ApplyProperties(Mathf.Lerp(nearDistance, visibleRadius, fade));

            // 다 드러난 순간은 한 번만 기록한다(움직이는 대상의 가장 먼 점이 흔들려도 다시 기록하지 않는다).
            if (isFadingOut)
            {
                SetFullyRevealed(false);
            }
            else if ((fade >= 1.0f) && (_revealRadius >= farDistance))
            {
                SetFullyRevealed(true);
            }
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
        // 스캔의 중심과 현재 표시 반경을 각 렌더러 셰이더에 전달한다.
        private void ApplyProperties(float radius)
        {
            // 다 드러난 뒤 유지 시간 동안은 매 프레임 같은 값이 들어오므로 다시 쓰지 않는다.
            if (radius == _appliedRadius) return;
            _appliedRadius = radius;

            foreach (Renderer targetRenderer in _renderers)
            {
                // 파괴된 렌더러는 건너뛴다.
                if (!targetRenderer) continue;

                // 렌더러에 이미 있던 값을 유지한 채 스캔 값만 덮어쓴다.
                // targetRenderer에 설정된 프로퍼티 블록의 값을, 미리 만들어 둔 _propertyBlock 안으로 가져온다.
                // 그리고 값 2개를 바꿔서 targetRenderer에 설정된 프로터피 블록에다가 저장한다.
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

                if (material && material.HasFloat(ScanRevealTimeId))
                {
                    _propertyBlock.SetFloat(ScanRevealTimeId, material.GetFloat(ScanRevealTimeId));
                }

                targetRenderer.SetPropertyBlock(_propertyBlock);
            }
        }

        /// <summary>
        /// 다 드러났는지 여부가 바뀔 때만 _ScanRevealTime 을 넣는다. 다 드러났으면 지금 시각, 아니면 -1 이다.
        /// </summary>
        private void SetFullyRevealed(bool isFullyRevealed)
        {
            if (isFullyRevealed == _isFullyRevealed) return;
            _isFullyRevealed = isFullyRevealed;

            float revealTime = isFullyRevealed ? Time.time : -1.0f;
            foreach (Renderer targetRenderer in _renderers)
            {
                if (!targetRenderer) continue;

                targetRenderer.GetPropertyBlock(_propertyBlock);
                _propertyBlock.SetFloat(ScanRevealTimeId, revealTime);
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
