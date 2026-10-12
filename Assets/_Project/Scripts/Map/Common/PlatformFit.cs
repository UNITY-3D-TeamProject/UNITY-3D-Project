using UnityEngine;

namespace Map.Common
{
    /// <summary>발판 위에 놓일 때 크기를 맞추는 방식.</summary>
    public enum EPlatformFitMode
    {
        Fill,   // 발판의 넓이에 맞춰 가로·세로를 따로 늘이거나 줄인다 (바닥에 까는 구역)
        Shrink, // 발판보다 크면 비율을 지킨 채 줄인다. 작으면 그대로 둔다 (장애물)
    }

    /// <summary>
    /// 맵 생성기가 이 오브젝트를 발판 위에 놓을 때 발판 크기에 맞추도록 알려 주는 표시.
    /// 붙어 있지 않은 프리팹은 원래 크기대로 놓인다.
    /// </summary>
    public class PlatformFit : MonoBehaviour
    {
        #region Serialized Fields
        [Tooltip("크기를 맞추는 방식")]
        [SerializeField] private EPlatformFitMode _mode = EPlatformFitMode.Fill;

        [Tooltip("이 오브젝트가 원래 크기일 때 바닥에서 차지하는 넓이 (x: 폭, y: 진행 방향 길이)")]
        [SerializeField] private Vector2 _footprint = new Vector2(6.0f, 6.0f);

        [Tooltip("Fill: 발판 넓이의 몇 배를 덮을지 / Shrink: 발판의 짧은 변의 몇 배까지 허용할지")]
        [SerializeField, Min(0.1f)] private float _ratio = 1.0f;
        #endregion

        #region Public Methods
        /// <summary>
        /// 발판 크기에 맞추려면 원래 스케일에 곱해야 하는 값을 구한다.
        /// </summary>
        /// <param name="platformSize">발판 윗면의 크기 (x: 폭, y: 진행 방향 길이)</param>
        /// <returns>축별 배율</returns>
        public Vector3 GetScaleFactor(Vector2 platformSize)
        {
            float footprintX = Mathf.Max(_footprint.x, 0.01f);
            float footprintZ = Mathf.Max(_footprint.y, 0.01f);

            if (_mode == EPlatformFitMode.Fill)
            {
                return new Vector3((platformSize.x * _ratio) / footprintX, 1.0f, (platformSize.y * _ratio) / footprintZ);
            }

            float allowed = Mathf.Min(platformSize.x, platformSize.y) * _ratio;
            float factor = Mathf.Min(1.0f, allowed / Mathf.Max(footprintX, footprintZ));
            return Vector3.one * factor;
        }
        #endregion
    }
}
