using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Serialization;

namespace CameraControl
{
    /// <summary>
    /// Look 입력으로 카메라 피벗을 yaw/pitch 회전시키는 컴포넌트.
    /// ApplyLook 으로 전달된 값을 감도만큼 누적해 즉시 피벗 회전에 반영한다.
    /// 입력이 없으면 회전을 유지하며, 각도는 설정된 범위로 클램프된다.
    /// 조준 시 조준 전용 시네머신 카메라를 활성화하며, 전환 보간은 CinemachineBrain 의 Blend 가 담당한다.
    /// </summary>
    public class PlayerBaseCamera : MonoBehaviour
    {
        #region Serialized Fields
        [Header("References")]
        [FormerlySerializedAs("cameraPivot")]
        [SerializeField] private Transform _cameraPivot;
        [Tooltip("조준 중에만 활성화되는 시네머신 카메라. 기본 카메라와 같은 피벗을 따라가도록 설정한다.")]
        [SerializeField] private CinemachineCamera _aimCamera;

        [Header("Look")]
        [FormerlySerializedAs("sensitivity")]
        [SerializeField, Range(0.01f, 1f)] private float _sensitivity;
        [FormerlySerializedAs("yawMin")]
        //[SerializeField] private float _yawMin = -90f;
        [FormerlySerializedAs("yawMax")]
        //[SerializeField] private float _yawMax = 90f;
        [FormerlySerializedAs("pitchMin")]
        [SerializeField] private float _pitchMin = -50f;
        [FormerlySerializedAs("pitchMax")]
        [SerializeField] private float _pitchMax = 50f;
        [Tooltip("true 면 Y 입력 방향을 반전한다.")]
        [FormerlySerializedAs("invertY")]
        [SerializeField] private bool _shouldInvertY = false;

        [Header("Fire Feedback")]
        [Tooltip("사격 시 카메라 쉐이크를 보내는 Impulse Source. 차지샷 기준 세기(DefaultVelocity)로 설정한다.")]
        [SerializeField] private CinemachineImpulseSource _fireImpulseSource;
        [Tooltip("일반 사격의 쉐이크·반동 배율 (차지샷 = 1)")]
        [SerializeField, Range(0.0f, 1.0f)] private float _normalShotRatio = 0.2f;
        [Tooltip("조준 중 쉐이크·반동 배율")]
        [SerializeField, Range(0.0f, 1.0f)] private float _aimShotRatio = 0.5f;
        [Tooltip("차지샷 반동으로 피벗이 위로 튀는 각도 (도)")]
        [SerializeField, Min(0.0f)] private float _recoilPitch = 3.0f;
        [Tooltip("반동이 원래 시점으로 돌아오는 속도 (도/초)")]
        [SerializeField, Min(0.0f)] private float _recoilRecoverySpeed = 20.0f;
        #endregion

        #region Private Fields
        private float _pitch;
        private float _yaw;
        // 반동으로 위로 올라간 각도. 시점 입력 누적값(_pitch)과 따로 두고 시간이 지나면 0 으로 돌아온다
        private float _recoilOffset;
        #endregion

        #region Properties
        /// <summary>회전이 적용되는 카메라 피벗. 이동 기준 프레임으로도 사용된다.</summary>
        public Transform Pivot => _cameraPivot;
        #endregion

        #region Unity Lifecycle
        private void Start()
        {
            // 조준하지 않은 상태로 시작
            SetAim(false);

            if (_cameraPivot == null) return;

            // 피벗의 초기 회전을 yaw/pitch 누적값의 시작점으로 사용
            Vector3 eulerAngle = _cameraPivot.rotation.eulerAngles;
            _yaw = eulerAngle.y;
            _pitch = Mathf.DeltaAngle(0f, eulerAngle.x);
        }

        private void Update()
        {
            if (_recoilOffset <= 0.0f) return;

            _recoilOffset = Mathf.MoveTowards(_recoilOffset, 0.0f, _recoilRecoverySpeed * Time.deltaTime);
            ApplyPivotRotation();
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Look 입력을 감도만큼 누적해 즉시 피벗 회전에 반영한다.
        /// 호출 직후 Pivot.forward 가 이번 입력이 적용된 방향이 된다.
        /// </summary>
        /// <param name="look">시점 입력 (x: yaw, y: pitch)</param>
        public void ApplyLook(Vector2 look)
        {
            if (!_cameraPivot || look.sqrMagnitude < 0.0001f) return;

            Vector2 delta = look * _sensitivity;

            _yaw += delta.x;
            //_yaw = Mathf.Clamp(_yaw, _yawMin, _yawMax);
            _pitch += _shouldInvertY ? delta.y : -delta.y;
            _pitch = Mathf.Clamp(_pitch, _pitchMin, _pitchMax);

            ApplyPivotRotation();
        }

        /// <summary>
        /// 사격 피드백(카메라 쉐이크 + 반동)을 재생한다. 일반 사격과 조준 중에는 각 배율만큼 약하게 한다.
        /// </summary>
        /// <param name="isCharged">차지샷이면 true</param>
        public void PlayFireFeedback(bool isCharged)
        {
            float ratio = isCharged ? 1.0f : _normalShotRatio;
            if (_aimCamera && _aimCamera.enabled) ratio *= _aimShotRatio;

            if (_fireImpulseSource) _fireImpulseSource.GenerateImpulseWithForce(ratio);

            if (!_cameraPivot) return;

            _recoilOffset += _recoilPitch * ratio;
            ApplyPivotRotation();
        }

        /// <summary>
        /// 조준 전용 카메라를 켜고 끈다. 켜지면 CinemachineBrain 이 기본 카메라에서 조준 카메라로 블렌드한다.
        /// </summary>
        /// <param name="isAiming">true 면 조준 카메라 활성화, false 면 비활성화</param>
        public void SetAim(bool isAiming)
        {
            if (_aimCamera == null) return;

            _aimCamera.enabled = isAiming;
        }

        /// <summary>
        /// 화면 중앙(조준점)을 지나는 레이를 반환한다. 메인 카메라 기준이며, 메인 카메라가 없으면 피벗 기준 레이를 반환한다.
        /// </summary>
        /// <returns>조준 레이 (월드 기준)</returns>
        public Ray GetAimRay()
        {
            Camera mainCamera = Camera.main;
            if (mainCamera) return new Ray(mainCamera.transform.position, mainCamera.transform.forward);

            Transform origin = _cameraPivot ? _cameraPivot : transform;
            return new Ray(origin.position, origin.forward);
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// 시점 입력 누적값에 반동을 더해 피벗 회전에 반영한다. 반동은 위쪽(pitch 감소)으로 적용한다.
        /// </summary>
        private void ApplyPivotRotation()
        {
            if (!_cameraPivot) return;

            float pitch = Mathf.Clamp(_pitch - _recoilOffset, _pitchMin, _pitchMax);
            _cameraPivot.rotation = Quaternion.Euler(pitch, _yaw, 0f);
        }
        #endregion
    }
}
