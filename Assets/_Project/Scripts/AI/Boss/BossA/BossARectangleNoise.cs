using UnityEngine;
using UnityEngine.VFX;

namespace AI.Boss.BossA
{
    /// <summary>
    /// 한쪽 손에 붙인 INab Rectangles 복제본에 붉은 노이즈 설정을 전달한다.
    /// VFX는 손 본의 로컬 공간에서 재생해 팔 움직임에 따른 잔상을 줄인다.
    /// </summary>
    [ExecuteAlways]
    [DefaultExecutionOrder(1100)]
    [DisallowMultipleComponent]
    public class BossARectangleNoise : MonoBehaviour
    {
        #region Serialized Fields
        [Header("References")]
        [SerializeField] private VisualEffect _effect;

        [Header("Noise")]
        [SerializeField, ColorUsage(true, true)] private Color _color = new Color(6.0f, 0.02f, 0.006f, 1.0f);
        [SerializeField, Min(0.0f)] private float _spawnRate = 80.0f;
        [SerializeField, Min(0.001f)] private float _size = 0.45f;
        #endregion

        #region Private Fields
        private bool _needsSetup;
        #endregion

        #region Unity Lifecycle
        private void OnEnable()
        {
            _needsSetup = true;
        }

        private void OnValidate()
        {
            _needsSetup = true;
        }

        private void LateUpdate()
        {
            if (!_needsSetup || !_effect || !_effect.visualEffectAsset) return;

            ConfigureEffect();
            _needsSetup = false;
        }

        private void OnDisable()
        {
            if (_effect) _effect.Stop();
        }
        #endregion

        #region Private Methods
        private void ConfigureEffect()
        {
            _effect.SetFloat("Spawn Rate", _spawnRate);
            _effect.SetFloat("Spawn Rate Multiplier", 1.0f);
            _effect.SetFloat("Scale Multiplier", _size);
            _effect.SetFloat("Emission Power", 1.0f);
            _effect.SetVector4("Color", Vector4.one);

            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(_color, 0.0f), new GradientColorKey(_color * 0.3f, 1.0f) },
                new[] { new GradientAlphaKey(1.0f, 0.0f), new GradientAlphaKey(1.0f, 1.0f) });
            _effect.SetGradient("Color Gradient", gradient);
            _effect.Reinit();
            _effect.Play();
        }
        #endregion
    }
}
