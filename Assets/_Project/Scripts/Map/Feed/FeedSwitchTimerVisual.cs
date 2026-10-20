using UnityEngine;
using Map.Gimmicks;

namespace Map.Feed
{
    /// <summary>스위치의 실제 남은 시간을 장치의 분할 표시등에 표시한다.</summary>
    public class FeedSwitchTimerVisual : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        [SerializeField] private GimmickSwitch _source;
        [SerializeField] private Renderer[] _segments;
        [SerializeField] private Color _color = Color.cyan;

        private MaterialPropertyBlock _block;

        private void Awake()
        {
            _block = new MaterialPropertyBlock();
        }

        private void LateUpdate()
        {
            if (_source == null) return;

            float ratio = _source.RevertRatio;
            bool isWarning = (_source.RevertRemaining > 0f) && (_source.RevertRemaining < 1.5f);
            bool isFlashOn = !isWarning || (Mathf.FloorToInt(Time.time * 6f) % 2 == 0);
            for (int i = 0; i < _segments.Length; i++)
            {
                if (_segments[i] == null) continue;
                bool isLit = (i < Mathf.CeilToInt(ratio * _segments.Length)) && isFlashOn;
                Color color = isLit ? _color : new Color(0.06f, 0.09f, 0.12f);
                _segments[i].GetPropertyBlock(_block);
                _block.SetColor(BaseColorId, color);
                _block.SetColor(EmissionColorId, isLit ? color * 1.5f : Color.black);
                _segments[i].SetPropertyBlock(_block);
            }
        }
    }
}
