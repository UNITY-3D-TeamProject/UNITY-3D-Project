using UnityEngine;

namespace Map.Gimmicks
{
    /// <summary>
    /// 바닥에 픽셀이 번졌다가 깨지듯 사라지는 연출(Project/VirusPixelSpread 셰이더)의 진행도를 올린다.
    /// 켜지면 처음부터 재생하고, 다 재생하면 스스로 파괴된다. 보기만 하는 연출이라 판정은 없다.
    /// </summary>
    public class PixelSpreadEffect : MonoBehaviour
    {
        #region Serialized Fields
        [Tooltip("번지기 시작해서 다 사라질 때까지 걸리는 시간(초)")]
        [SerializeField, Min(0.1f)] private float _duration = 1.2f;

        [Tooltip("Project/VirusPixelSpread 셰이더를 쓰는 렌더러")]
        [SerializeField] private Renderer _renderer;
        #endregion

        #region Private Fields
        private static readonly int ProgressId = Shader.PropertyToID("_Progress");
        private static readonly int SeedId = Shader.PropertyToID("_Seed");

        private MaterialPropertyBlock _block;
        private float _elapsed;
        #endregion

        #region Properties
        /// <summary>번지기 시작해서 다 사라질 때까지 걸리는 시간(초).</summary>
        public float Duration => _duration;
        #endregion

        #region Unity Lifecycle
        private void OnEnable()
        {
            _elapsed = 0.0f;
            _block ??= new MaterialPropertyBlock();

            // 매번 다른 모양으로 번지게 한다
            _block.SetFloat(SeedId, Random.Range(0.0f, 100.0f));
            ApplyProgress(0.0f);
        }

        private void Update()
        {
            _elapsed += Time.deltaTime;
            ApplyProgress(_elapsed / _duration);

            if (_elapsed >= _duration)
            {
                Destroy(gameObject);
            }
        }
        #endregion

        #region Private Methods
        private void ApplyProgress(float progress)
        {
            if (_renderer == null) return;

            _block.SetFloat(ProgressId, Mathf.Clamp01(progress));
            _renderer.SetPropertyBlock(_block);
        }
        #endregion
    }
}
