using UnityEngine;

namespace Map.Common
{
    public class FastRotateX : MonoBehaviour
    {
        #region Serialized Fields
        [Header("Settings")]
        [Tooltip("X축 회전 속도 (초당 회전 각도)")]
        [SerializeField] private float _rotationSpeed = 720f;

        
        [Tooltip("고정할 Y축 각도")]
        [SerializeField] private float _rotationY = 90f;

        [Tooltip("고정할 Z축 각도")]
        [SerializeField] private float _rotationZ = -90f;
        #endregion

        #region Private Fields
        private Transform _cachedTransform;
        private float _currentRotationX;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            _cachedTransform = transform;
            // 시작 시점의 X축 각도를 초기값으로 설정합니다.
            _currentRotationX = _cachedTransform.localEulerAngles.x;
        }

        private void Update()
        {
            // X축 회전값을 누적하여 증가시킵니다.
            _currentRotationX += _rotationSpeed * Time.deltaTime;

            // Y와 Z는 설정한 값으로 고정하고, X축만 회전된 값을 직접 대입합니다.
            _cachedTransform.localEulerAngles = new Vector3(_currentRotationX, _rotationY, _rotationZ);
        }
        #endregion
    }
}
