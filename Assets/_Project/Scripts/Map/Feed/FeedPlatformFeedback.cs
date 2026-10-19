using UnityEngine;
using Map.Platforms;

namespace Map.Feed
{
    /// <summary>사라지는 발판의 형제에 남는 윤곽과 데이터 소멸 효과.</summary>
    [DefaultExecutionOrder(100)]
    public class FeedPlatformFeedback : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        [Header("Platform (one source)")]
        [SerializeField] private JumpSwapPlatform _jump;
        [SerializeField] private FallingPlatform _fall;
        [SerializeField] private DisappearingPlatform _blink;
        [SerializeField] private CrumblePlatform _crumble;

        [Header("Visuals")]
        [SerializeField] private Renderer[] _outline;
        [SerializeField] private Renderer[] _cracks;
        [SerializeField] private ParticleSystem _dissolve;
        [SerializeField] private Color _color = Color.cyan;

        private MaterialPropertyBlock _block;
        private bool _wasSolid = true;

        private void Awake()
        {
            _block = new MaterialPropertyBlock();
        }

        private void LateUpdate()
        {
            bool isSolid = true;
            float warning = 0f;
            if (_jump != null) isSolid = _jump.IsSolid;
            if (_fall != null)
            {
                isSolid = _fall.IsSolid && !_fall.IsFalling;
                warning = _fall.WarningRatio;
            }
            if (_blink != null)
            {
                isSolid = _blink.IsSolid;
            }
            if (_crumble != null)
            {
                isSolid = _crumble.IsSolid;
                warning = _crumble.WarningRatio;
            }

            if (_dissolve != null && _wasSolid && !isSolid) _dissolve.Play();
            _wasSolid = isSolid;
            Color color = (warning > 0f) ? Color.Lerp(_color, new Color(1f, 0.3f, 0.1f), warning) : _color;
            foreach (Renderer renderer in _outline)
            {
                _block.SetColor(BaseColorId, color * (isSolid ? 0.35f : 0.85f));
                renderer.SetPropertyBlock(_block);
            }
            for (int i = 0; i < _cracks.Length; i++)
            {
                // 경고 단계에서만 문양을 밝힌다. 몸체의 깜빡임 여부는 기존 컴포넌트가 담당한다.
                _cracks[i].enabled = isSolid && (warning > i / (float)Mathf.Max(1, _cracks.Length));
            }
        }
    }
}
