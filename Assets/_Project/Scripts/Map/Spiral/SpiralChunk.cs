using UnityEngine;

namespace Map.Spiral
{
    /// <summary>
    /// 나선 구간에 끼워 넣는 기믹 묶음. 곧고 평평한 기믹 한 덩어리의 프리팹 루트에 붙인다.
    /// 원점의 높이는 발판 중심 높이, +Z 가 진행 방향, +X 가 나선의 바깥쪽이다.
    /// SpiralMapGenerator 가 Entry Z ~ Exit Z 구간을 길 위에 맞춰 놓고, 앞뒤에 고정 발판을 둔다.
    /// 나선이 휘어 있으므로 길이는 14 이하로 만든다.
    /// </summary>
    public class SpiralChunk : MonoBehaviour
    {
        #region Serialized Fields
        [Header("Range")]
        [Tooltip("플레이어가 올라서는 쪽 끝의 Z 위치 (자신 기준)")]
        [SerializeField] private float _entryZ = -4.0f;

        [Tooltip("플레이어가 내리는 쪽 끝의 Z 위치 (자신 기준)")]
        [SerializeField] private float _exitZ = 4.0f;

        [Header("Appearance")]
        [Tooltip("생성할 때 그 태그의 기본 발판 머티리얼로 바꿀 렌더러들 (움직이지 않는 발판)")]
        [SerializeField] private Renderer[] _platformRenderers;

        [Tooltip("생성할 때 그 태그의 움직이는 발판 머티리얼로 바꿀 렌더러들 (옮겨 가거나 도는 발판)")]
        [SerializeField] private Renderer[] _movingRenderers;

        [Tooltip("생성할 때 그 태그의 사라지는 발판 머티리얼로 바꿀 렌더러들 (생겼다 사라지는 다리·길)")]
        [SerializeField] private Renderer[] _unstableRenderers;
        #endregion

        #region Properties
        /// <summary>진행 방향 길이.</summary>
        public float Length => Mathf.Max(_exitZ - _entryZ, 0.0f);

        /// <summary>올라서는 끝과 내리는 끝의 가운데 Z 위치 (자신 기준).</summary>
        public float CenterZ => (_entryZ + _exitZ) * 0.5f;
        #endregion

        #region Unity Lifecycle
        private void OnDrawGizmosSelected()
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = Color.green;
            Gizmos.DrawLine(new Vector3(-2.0f, 0.0f, _entryZ), new Vector3(2.0f, 0.0f, _entryZ));
            Gizmos.color = Color.red;
            Gizmos.DrawLine(new Vector3(-2.0f, 0.0f, _exitZ), new Vector3(2.0f, 0.0f, _exitZ));
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// 발판 모양 부분의 머티리얼을 그 태그의 것으로 바꾼다. null 인 머티리얼의 부분은 그대로 둔다.
        /// </summary>
        /// <param name="basic">움직이지 않는 발판의 머티리얼</param>
        /// <param name="moving">움직이는 발판의 머티리얼</param>
        /// <param name="unstable">생겼다 사라지는 발판의 머티리얼</param>
        public void ApplyPlatformMaterials(Material basic, Material moving, Material unstable)
        {
            SetMaterial(_platformRenderers, basic);
            SetMaterial(_movingRenderers, moving);
            SetMaterial(_unstableRenderers, unstable);
        }
        #endregion

        #region Private Methods
        private static void SetMaterial(Renderer[] renderers, Material material)
        {
            if ((material == null) || (renderers == null)) return;

            foreach (Renderer platformRenderer in renderers)
            {
                if (platformRenderer != null) platformRenderer.sharedMaterial = material;
            }
        }
        #endregion
    }
}
