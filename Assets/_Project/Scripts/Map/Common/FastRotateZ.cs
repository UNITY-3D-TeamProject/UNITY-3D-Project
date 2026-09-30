using UnityEngine;

namespace Map.Common
{
    public class FastRotateZ : MonoBehaviour
    {
        #region Serialized Fields
        [Header("Settings")]
        [Tooltip("Z축 회전 속도 (초당 회전 각도)")]
        [SerializeField] private float _rotationSpeed = 720f;
        #endregion

        #region Private Fields
        private Transform _cachedTransform;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            _cachedTransform = transform;
        }

        private void Update()
        {
            // Z축을 기준으로 설정한 속도만큼 매 프레임 회전합니다.
            _cachedTransform.Rotate(0f, 0f, _rotationSpeed * Time.deltaTime);
        }
        #endregion
    }
}
