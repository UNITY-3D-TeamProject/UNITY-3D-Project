using UnityEngine;
using TMPro;

namespace Map.Feed
{
    /// <summary>
    /// 2라운드에서 피드를 올려 보내는 해시태그 벽 하나. FeedRound2 가 만들면서 크기·글자·색을 정한다.
    /// 이 오브젝트의 forward 가 경기장 안쪽을 향한다.
    /// </summary>
    public class FeedTagWall : MonoBehaviour
    {
        #region Constants
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        #endregion

        #region Serialized Fields
        [Header("References")]
        [Tooltip("벽 판. 크기 1 × 1 인 메시와 콜라이더를 가진 자식이며, 폭과 높이에 맞춰 늘린다.")]
        [SerializeField] private Transform _panel;

        [Tooltip("색을 입힐 벽 판의 렌더러")]
        [SerializeField] private Renderer _panelRenderer;

        [Tooltip("해시태그를 적는 글자")]
        [SerializeField] private TMP_Text _label;
        #endregion

        #region Public Methods
        /// <summary>
        /// 벽의 크기, 글자, 색을 정한다.
        /// </summary>
        /// <param name="width">벽의 폭</param>
        /// <param name="height">벽의 높이</param>
        /// <param name="bottom">이 오브젝트 기준 벽 아랫변의 높이</param>
        /// <param name="label">벽에 적을 글자</param>
        /// <param name="color">벽의 색</param>
        public void Setup(float width, float height, float bottom, string label, Color color)
        {
            if (_panel != null)
            {
                _panel.localScale = new Vector3(width, height, _panel.localScale.z);
                _panel.localPosition = new Vector3(0.0f, bottom + (height * 0.5f), 0.0f);
            }

            if (_panelRenderer != null)
            {
                // 투명도는 머티리얼의 값을 그대로 쓴다
                color.a = _panelRenderer.sharedMaterial.GetColor(BaseColorId).a;

                var block = new MaterialPropertyBlock();
                _panelRenderer.GetPropertyBlock(block);
                block.SetColor(BaseColorId, color);
                _panelRenderer.SetPropertyBlock(block);
            }

            if (_label != null)
            {
                _label.text = label;
            }
        }
        #endregion
    }
}
