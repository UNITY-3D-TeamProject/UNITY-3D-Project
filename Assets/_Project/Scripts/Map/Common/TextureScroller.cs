using UnityEngine;

namespace Map.Common
{
    /// <summary>
    /// 렌더러의 바탕 텍스처를 일정 속도로 흘려보낸다. (컨베이어 벨트의 무늬용)
    /// 무늬가 반복되는 텍스처와 Wrap Mode = Repeat 이 필요하다.
    /// </summary>
    [RequireComponent(typeof(Renderer))]
    public class TextureScroller : MonoBehaviour
    {
        #region Serialized Fields
        [Tooltip("텍스처가 가로·세로로 반복되는 횟수")]
        [SerializeField] private Vector2 _tiling = Vector2.one;

        [Tooltip("초당 흘러가는 양 (1 = 무늬 한 칸). 음수면 반대 방향")]
        [SerializeField] private Vector2 _scrollPerSecond = new Vector2(0.0f, 1.0f);
        #endregion

        #region Private Fields
        private static readonly int BaseMapStId = Shader.PropertyToID("_BaseMap_ST");

        private Renderer _renderer;
        private MaterialPropertyBlock _block;
        private Vector2 _offset;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            _renderer = GetComponent<Renderer>();
            _block = new MaterialPropertyBlock();
        }

        private void Update()
        {
            // 값이 계속 커지지 않게 0 ~ 1 안에서 돈다
            _offset.x = Mathf.Repeat(_offset.x + (_scrollPerSecond.x * Time.deltaTime), 1.0f);
            _offset.y = Mathf.Repeat(_offset.y + (_scrollPerSecond.y * Time.deltaTime), 1.0f);

            _renderer.GetPropertyBlock(_block);
            _block.SetVector(BaseMapStId, new Vector4(_tiling.x, _tiling.y, _offset.x, _offset.y));
            _renderer.SetPropertyBlock(_block);
        }
        #endregion
    }
}
