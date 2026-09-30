using UnityEngine;

namespace Map.Common
{
    public class FastRotateY : MonoBehaviour
    {
        #region Serialized Fields
        [Header("Settings")]
        [Tooltip("Y축 회전 속도 (초당 회전 각도)")]
        [SerializeField] private float _rotationSpeed = 720f;

        
        [Tooltip("고정할 X축 각도")]
        [SerializeField] private float _rotationX = 90f;

        [Tooltip("고정할 Z축 각도")]
        [SerializeField] private float _rotationZ = -90f;
        #endregion

        #region Private Fields
        private Transform _cachedTransform;
        private float _currentRotationY;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            _cachedTransform = transform;
            // 시작 시점의 Y축 각도를 초기값으로 설정합니다.
            _currentRotationY = _cachedTransform.localEulerAngles.y;
        }

        private void Update()
        {
            // Y축 회전값을 누적하여 증가시킵니다.
            _currentRotationY += _rotationSpeed * Time.deltaTime;

            // X와 Z는 설정한 값으로 고정하고, Y축만 회전된 값을 직접 대입합니다.
            _cachedTransform.localEulerAngles = new Vector3(_rotationX, _currentRotationY, _rotationZ);
        }
        #endregion
    }
}
