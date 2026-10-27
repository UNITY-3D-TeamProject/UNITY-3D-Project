using UnityEngine;
using Core.Audio;

namespace Map.Gimmicks
{
    /// <summary>
    /// CycleTimer 상태에 맞춰 위험 장치의 표시색을 안전, 예고, 위험 순서로 바꾼다.
    /// 타이머의 기능에는 관여하지 않고 Renderer의 MaterialPropertyBlock만 변경한다.
    /// </summary>
    public class CycleHazardIndicator : MonoBehaviour
    {
        #region Serialized Fields
        [Header("References")]
        [SerializeField] private CycleTimer _timer;
        [SerializeField] private Renderer[] _indicators;

        [Header("Colors")]
        [SerializeField] private Color _safeColor = new Color(0.15f, 0.9f, 1.0f, 1.0f);
        [SerializeField] private Color _warningColor = new Color(1.0f, 0.55f, 0.08f, 1.0f);
        [SerializeField] private Color _dangerColor = new Color(1.0f, 0.08f, 0.12f, 1.0f);

        [Header("Warning")]
        [Tooltip("꺼진 구간이 끝나기 전, 예고색으로 바뀌기 시작하는 시간(초)")]
        [SerializeField, Min(0.0f)] private float _warningTime = 0.75f;

        [Tooltip("예고 구간에서 초당 깜빡이는 횟수. 0이면 깜빡이지 않는다.")]
        [SerializeField, Min(0.0f)] private float _warningBlinkRate = 6.0f;

        [Header("Sound")]
        [Tooltip("예고 구간에 들어설 때 낼 소리 (비우면 무음)")]
        [SerializeField] private SOAudioData _warningSound;
        #endregion

        #region Private Fields
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        private MaterialPropertyBlock _propertyBlock;
        private Color _lastColor;
        private bool _hasLastColor;
        private bool _isWarning;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            Debug.Assert(_timer != null, $"[{name}] CycleTimer가 연결되지 않았습니다.");
            Debug.Assert((_indicators != null) && (_indicators.Length > 0),
                $"[{name}] 상태 표시 Renderer가 연결되지 않았습니다.");

            _propertyBlock = new MaterialPropertyBlock();
        }

        private void LateUpdate()
        {
            if (_timer == null) return;

            UpdateWarningSound();

            Color color = GetCurrentColor();
            if (_hasLastColor && (color == _lastColor)) return;

            ApplyColor(color);
            _lastColor = color;
            _hasLastColor = true;
        }
        #endregion

        #region Private Methods
        private Color GetCurrentColor()
        {
            if (_timer.IsOn) return _dangerColor;
            if ((_warningTime <= 0.0f) || (_timer.Remaining > _warningTime)) return _safeColor;
            if (_warningBlinkRate <= 0.0f) return _warningColor;

            bool isWarningVisible = (Mathf.FloorToInt(Time.time * _warningBlinkRate * 2.0f) % 2) == 0;
            return isWarningVisible ? _warningColor : _safeColor;
        }

        // 예고 구간에 들어서는 순간 한 번만 소리를 낸다
        private void UpdateWarningSound()
        {
            bool isWarning = (!_timer.IsOn) && (_warningTime > 0.0f) && (_timer.Remaining <= _warningTime);
            if (isWarning && !_isWarning)
            {
                GimmickUtility.PlaySfx(_warningSound, transform.position);
            }

            _isWarning = isWarning;
        }

        private void ApplyColor(Color color)
        {
            if (_indicators == null) return;

            foreach (Renderer indicator in _indicators)
            {
                if (indicator == null) continue;

                indicator.GetPropertyBlock(_propertyBlock);
                _propertyBlock.SetColor(BaseColorId, color);
                _propertyBlock.SetColor(ColorId, color);
                indicator.SetPropertyBlock(_propertyBlock);
            }
        }
        #endregion
    }
}

