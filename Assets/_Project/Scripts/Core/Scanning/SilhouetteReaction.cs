using UnityEngine;

namespace Core.Scanning
{
    /// <summary>
    /// 스캔 중에만 벽 뒤에 가려진 대상의 실루엣을 보여주는 반응. 아이템처럼 평소에도 보이는 오브젝트에 쓴다.
    /// 대상의 원래 렌더러는 건드리지 않고, 같은 메시에 실루엣 머티리얼(M_ScanSilhouette)을 지정한 별도 렌더러를 켜고 끈다.
    /// 실루엣 머티리얼은 가려진 부분만 그리므로 보이는 부분의 외형은 그대로이다.
    /// 사라질 때는 실루엣 알파가 서서히 줄어든다.
    /// </summary>
    public class SilhouetteReaction : ScanReactionBase
    {
        #region Static Fields
        // 셰이더 프로퍼티 이름 "_Color"(SG_ScanSilhouette 셰이더 그래프의 실루엣 색)를 정수 ID 로 바꿔 둔 값.
        // 문자열로 넘기면 호출할 때마다 이름을 ID 로 변환하므로, 매 프레임 쓰는 값은 한 번만 변환해 재사용한다.
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        #endregion

        #region Serialized Fields
        [Header("References")]
        [Tooltip("스캔 중에만 켜질 실루엣 렌더러. 대상과 같은 메시에 M_ScanSilhouette 를 지정한 자식 오브젝트를 연결한다. 평소에는 꺼진다.")]
        // 아이템이 여러 메시 조각(예: 상자 몸통 + 뚜껑)으로 이루어지면 조각마다 실루엣 렌더러가 하나씩 필요하므로 배열로 받는다.
        // 메시가 하나뿐인 아이템은 1칸만 채우면 된다.
        [SerializeField] private Renderer[] _silhouetteRenderers;
        #endregion

        #region Private Fields
        // 머티리얼을 복제하지 않고 렌더러별로 셰이더 값만 덮어쓰기 위한 블록이다.
        // renderer.material 로 색을 바꾸면 머티리얼 인스턴스가 새로 생기므로 이 방식을 쓴다.
        // 셰이더 프로퍼티 값들을 담아두는 컨테이너Z
        private MaterialPropertyBlock _propertyBlock;
        // 렌더러별 머티리얼 원래 색. _silhouetteRenderers 와 같은 순서로 저장하고, 페이드 때 알파만 곱해 쓴다.
        private Color[] _baseColors;
        // 마지막으로 적용한 세기. 같은 값이면 다시 쓰지 않기 위해 쓴다. 음수면 아직 적용하지 않은 상태다.
        private float _appliedFade = -1.0f;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            // 대상의 원래 렌더러까지 잡히지 않도록 자동 탐색하지 않고 직접 연결받는다.
            Debug.Assert(
                (_silhouetteRenderers != null) && (_silhouetteRenderers.Length > 0),
                $"[{name}] 실루엣 렌더러가 연결되지 않았습니다.");

            // 연결이 비어 있어도 아래 반복문들이 null 예외 없이 아무것도 안 하도록 빈 배열로 둔다.
            if (_silhouetteRenderers == null) _silhouetteRenderers = new Renderer[0];

            _propertyBlock = new MaterialPropertyBlock();

            // 페이드 때마다 머티리얼을 다시 읽지 않도록 원래 색을 미리 저장해 둔다.
            _baseColors = new Color[_silhouetteRenderers.Length];
            for (int i = 0; i < _silhouetteRenderers.Length; i++)
            {
                Renderer targetRenderer = _silhouetteRenderers[i];
                Material material = targetRenderer ? targetRenderer.sharedMaterial : null;

                // 실루엣 셰이더가 아니라 _Color 가 없는 머티리얼이면 흰색을 기준으로 삼는다.
                _baseColors[i] = (material && material.HasColor(ColorId)) ? material.GetColor(ColorId) : Color.white;
            }

            // 스캔에 닿기 전에는 실루엣이 보이지 않는다.
            SetRenderersEnabled(false);
        }
        #endregion

        #region Public Methods
        /// <inheritdoc />
        public override void OnScanBegin(in SScanHit hit)
        {
            // 세기 0 에서 시작하므로 첫 프레임에 튀지 않게 투명한 상태로 켠다.
            // 이후 ScanTarget 이 매 프레임 OnFade 로 세기를 올려 주면 서서히 진해진다.
            ApplyAlpha(0.0f);
            SetRenderersEnabled(true);
        }

        /// <inheritdoc />
        public override void OnFade(float fade, bool isFadingOut)
        {
            // 나타날 때(0 -> 1)와 사라질 때(1 -> 0) 모두 세기를 그대로 알파에 반영하므로 isFadingOut 은 구분하지 않는다.
            ApplyAlpha(fade);
        }

        /// <inheritdoc />
        public override void OnScanEnd()
        {
            // 완전히 사라졌거나 중단되면 렌더러를 꺼서 다음 스캔 전까지 그리기 비용도 쓰지 않는다.
            SetRenderersEnabled(false);
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// 모든 실루엣 렌더러의 색을 원래 색에서 알파만 fade 비율로 줄여 적용한다.
        /// </summary>
        /// <param name="fade">반응의 세기(0~1). 0 이면 완전히 투명하다.</param>
        private void ApplyAlpha(float fade)
        {
            // 다 나타난 뒤 유지 시간 동안은 매 프레임 같은 세기가 들어오므로 다시 쓰지 않는다.
            if (fade == _appliedFade) return;
            _appliedFade = fade;

            for (int i = 0; i < _silhouetteRenderers.Length; i++)
            {
                Renderer targetRenderer = _silhouetteRenderers[i];

                // 파괴된 렌더러는 건너뛴다.
                if (!targetRenderer) continue;

                // 원래 색은 유지하고 알파만 fade(0~1)에 비례해 줄인다.
                Color color = _baseColors[i];
                color.a *= fade;

                // 렌더러에 이미 있던 블록 값을 유지한 채 색만 덮어쓴다.
                targetRenderer.GetPropertyBlock(_propertyBlock);
                _propertyBlock.SetColor(ColorId, color);
                targetRenderer.SetPropertyBlock(_propertyBlock);
            }
        }

        /// <summary>
        /// 모든 실루엣 렌더러를 켜거나 끈다. 오브젝트가 아니라 렌더러만 끄므로 자식에 붙은 다른 컴포넌트는 그대로 동작한다.
        /// </summary>
        /// <param name="isEnabled">켤지 여부</param>
        private void SetRenderersEnabled(bool isEnabled)
        {
            foreach (Renderer targetRenderer in _silhouetteRenderers)
            {
                if (targetRenderer) targetRenderer.enabled = isEnabled;
            }
        }
        #endregion
    }
}
