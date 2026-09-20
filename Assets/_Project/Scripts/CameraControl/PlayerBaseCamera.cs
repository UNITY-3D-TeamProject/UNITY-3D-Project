using UnityEngine;
using UnityEngine.Serialization;

namespace CameraControl
{
    /// <summary>
    /// Look 입력으로 카메라 피벗을 yaw/pitch 회전시키는 컴포넌트.
    /// 매 프레임 Look 에 설정된 값을 감도만큼 누적해 LateUpdate 에서 피벗 회전에 반영한다.
    /// 입력이 없으면 회전을 유지하며, 각도는 설정된 범위로 클램프된다.
    /// </summary>
    public class PlayerBaseCamera : MonoBehaviour
    {
        #region Serialized Fields
        [Header("References")]
        [FormerlySerializedAs("cameraPivot")]
        [SerializeField] private Transform _cameraPivot;

        [Header("Look")]
        [FormerlySerializedAs("sensitivity")]
        [SerializeField, Range(0.01f, 1f)] private float _sensitivity;
        [FormerlySerializedAs("yawMin")]
        [SerializeField] private float _yawMin = -90f;
        [FormerlySerializedAs("yawMax")]
        [SerializeField] private float _yawMax = 90f;
        [FormerlySerializedAs("pitchMin")]
        [SerializeField] private float _pitchMin = -50f;
        [FormerlySerializedAs("pitchMax")]
        [SerializeField] private float _pitchMax = 50f;
        [Tooltip("true 면 Y 입력 방향을 반전한다.")]
        [FormerlySerializedAs("invertY")]
        [SerializeField] private bool _shouldInvertY = false;
        #endregion

        #region Private Fields
        private Vector2 _look;
        private float _pitch;
        private float _yaw;
        #endregion

        #region Properties
        /// <summary>이번 프레임에 적용할 Look 입력 (x: yaw, y: pitch).</summary>
        public Vector2 Look
        {
            set => _look = value;
        }

        /// <summary>회전이 적용되는 카메라 피벗. 이동 기준 프레임으로도 사용된다.</summary>
        public Transform Pivot => _cameraPivot;
        #endregion

        #region Unity Lifecycle
        private void Start()
        {
            if (_cameraPivot == null) return;

            // 피벗의 초기 회전을 yaw/pitch 누적값의 시작점으로 사용
            Vector3 eulerAngle = _cameraPivot.rotation.eulerAngles;
            _yaw = eulerAngle.y;
            _pitch = Mathf.DeltaAngle(0f, eulerAngle.x);
        }

        private void LateUpdate()
        {
            if (!_cameraPivot || _look.sqrMagnitude < 0.0001f) return;

            Vector2 delta = _look * _sensitivity;

            _yaw += delta.x;
            _yaw = Mathf.Clamp(_yaw, _yawMin, _yawMax);
            _pitch += _shouldInvertY ? delta.y : -delta.y;
            _pitch = Mathf.Clamp(_pitch, _pitchMin, _pitchMax);

            _cameraPivot.rotation = Quaternion.Euler(_pitch, _yaw, 0f);
        }
        #endregion
    }
}
