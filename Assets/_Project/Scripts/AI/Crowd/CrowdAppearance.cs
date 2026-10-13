using UnityEngine;

namespace AI.Crowd
{
    /// <summary>
    /// 군중 개체가 활성화될 때마다 몸통 렌더러의 머티리얼을 목록에서 무작위로 골라 바꾼다.
    /// 머티리얼 복사본을 만들지 않고 sharedMaterial 참조만 교체하므로 같은 머티리얼끼리 배칭이 유지된다.
    /// </summary>
    public class CrowdAppearance : MonoBehaviour
    {
        #region Serialized Fields
        [Header("Appearance")]
        [Tooltip("색을 바꿀 몸통 렌더러. 눈 등 다른 렌더러는 넣지 않는다.")]
        [SerializeField] private Renderer _bodyRenderer;
        [Tooltip("무작위로 고를 색 머티리얼 목록.")]
        [SerializeField] private Material[] _materials;
        #endregion

        #region Unity Lifecycle
        private void OnEnable()
        {
            if (!_bodyRenderer || _materials == null || _materials.Length == 0) return;

            // 목록의 빈 칸이 뽑히면 기존 머티리얼을 유지한다
            Material material = _materials[Random.Range(0, _materials.Length)];
            if (!material) return;

            // 원본 에셋을 수정하는 것이 아니라 이 렌더러가 참조하는 머티리얼만 바꾼다
            _bodyRenderer.sharedMaterial = material;
        }
        #endregion
    }
}
