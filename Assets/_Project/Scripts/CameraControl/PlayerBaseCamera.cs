using System;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Serialization;
using Rendering;

namespace CameraControl
{
    /// <summary>구르는 동안의 카메라 추종 설정.</summary>
    public struct SRollFollowSettings
    {
        /// <summary>구르는 동안 적용할 Damping (x, y, z).</summary>
        public Vector3 Damping;
        /// <summary>구르기가 끝난 뒤 Damping 이 원래 값으로 돌아오는 시간(초). 0 이면 즉시 복귀.</summary>
        public float RecoverTime;
    }

    /// <summary>탑승 중의 카메라 시야·가장자리 블러 설정.</summary>
    public struct SRidingViewSettings
    {
        /// <summary>기본 시야각에 더하는 값 (도).</summary>
        public float FovOffset;
        /// <summary>탑승·하차 시 전환 시간(초). 0 이면 즉시 전환.</summary>
        public float TransitionTime;
        /// <summary>가장자리 블러 설정.</summary>
        public SSpeedEdgeBlurSettings EdgeBlur;
    }

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

        [Header("Speed Feedback References")]
        [Tooltip("기본 시점 시네머신 카메라. 탑승 시 시야각(FOV)을 조절한다.")]
        [SerializeField] private CinemachineCamera _baseCamera;
        [Tooltip("기본 시점 카메라의 Third Person Follow. 구르는 동안 지정한 Damping 으로 추종을 완화한다.")]
        [SerializeField] private CinemachineThirdPersonFollow _baseFollow;
        #endregion

        #region Private Fields
        private float _pitch;
        private float _yaw;
        // 반동으로 위로 올라간 각도. 시점 입력 누적값(_pitch)과 따로 두고 시간이 지나면 0 으로 돌아온다
        private float _recoilOffset;

        // 구르기·탑승 상태. 컴포넌트가 꺼져 있어도 갱신되며, 켜질 때 즉시 반영한다
        private bool _isRolling;
        private bool _isRiding;
        // 구르기 후 Damping 복귀 진행도 (0~1, 1 = 원본)
        private float _rollRecoverT = 1.0f;
        // 탑승 전환 진행도 (0~1, 1 = 탑승 중)
        private float _ridingWeight;
        private bool _hasOriginals;
        private Vector3 _originalDamping;
        private float _originalFov;
        // 시네머신 카메라에 효과 값을 써 둔 상태인지 여부
        private bool _isEffectApplied;
        // 기본 카메라를 실제로 출력하는 카메라. 블러 대상 지정에 쓴다
        private Camera _outputCamera;

        // 구르기·탑승 설정 제공자. 해제해도 마지막 설정은 유지해 퇴장 전환이 끊기지 않게 한다
        private Func<SRollFollowSettings> _getRollSettings;
        private Func<SRidingViewSettings> _getRidingSettings;
        private SRollFollowSettings _rollSettings;
        private SRidingViewSettings _ridingSettings;
        private bool _hasRollSettings;
        private bool _hasRidingSettings;
        #endregion

        #region Properties
        /// <summary>회전이 적용되는 카메라 피벗. 이동 기준 프레임으로도 사용된다.</summary>
        public Transform Pivot => _cameraPivot;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            CacheOriginals();
        }

        private void OnEnable()
        {
            // CharacterMediator 가 먼저 켜져 상태를 보냈을 수 있으므로 원본이 없으면 지금 읽는다
            CacheOriginals();
            _rollRecoverT = _isRolling ? 0.0f : 1.0f;
            _ridingWeight = _isRiding ? 1.0f : 0.0f;
            ApplySpeedFeedback();
        }

        private void OnDisable()
        {
            if (_isEffectApplied) RestoreOriginals();

            _isEffectApplied = false;
            SpeedEdgeBlurRendererFeature.ClearTarget(_outputCamera);
            _outputCamera = null;
        }

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
            UpdateRecoil();
            UpdateSpeedFeedback();
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
        /// 구르는 동안 지정한 Damping 으로 카메라 추종을 완화할지 정한다. 끄면 Damping 이 복귀 시간 동안 원래 값으로 돌아온다.
        /// 컴포넌트가 꺼져 있으면 상태만 기억하고, 켜질 때 반영한다.
        /// </summary>
        /// <param name="isRolling">구르는 중이면 true</param>
        public void SetRollFollow(bool isRolling)
        {
            _isRolling = isRolling;
            if (isRolling) _rollRecoverT = 0.0f;
        }

        /// <summary>
        /// 탑승 상태를 정한다. 켜면 시야각을 넓히고 화면 가장자리를 흐리게 하며, 전환 시간 동안 보간한다.
        /// 컴포넌트가 꺼져 있으면 상태만 기억하고, 켜질 때 반영한다.
        /// </summary>
        /// <param name="isRiding">탑승 중이면 true</param>
        public void SetRiding(bool isRiding)
        {
            _isRiding = isRiding;
        }

        /// <summary>
        /// 구르기·탑승 상태를 보간 없이 즉시 반영한다.
        /// </summary>
        /// <param name="isRolling">구르는 중이면 true</param>
        /// <param name="isRiding">탑승 중이면 true</param>
        public void SyncSpeedFeedback(bool isRolling, bool isRiding)
        {
            _isRolling = isRolling;
            _isRiding = isRiding;
            _rollRecoverT = isRolling ? 0.0f : 1.0f;
            _ridingWeight = isRiding ? 1.0f : 0.0f;

            if (isActiveAndEnabled) ApplySpeedFeedback();
        }

        /// <summary>
        /// 구르기 카메라 설정을 읽어 올 함수를 등록한다. 효과가 진행되는 동안 매 프레임 호출해 런타임 변경을 반영한다.
        /// </summary>
        /// <param name="getter">구르기 카메라 설정 제공 함수</param>
        public void SetGetRollFollowSettings(Func<SRollFollowSettings> getter)
        {
            _getRollSettings = getter;
        }

        /// <summary>
        /// 등록된 구르기 설정 제공 함수를 해제한다. 마지막으로 읽은 설정은 유지한다.
        /// </summary>
        public void ClearGetRollFollowSettings()
        {
            _getRollSettings = null;
        }

        /// <summary>
        /// 탑승 시야·블러 설정을 읽어 올 함수를 등록한다. 효과가 진행되는 동안 매 프레임 호출해 런타임 변경을 반영한다.
        /// </summary>
        /// <param name="getter">탑승 시야·블러 설정 제공 함수</param>
        public void SetGetRidingViewSettings(Func<SRidingViewSettings> getter)
        {
            _getRidingSettings = getter;
        }

        /// <summary>
        /// 등록된 탑승 설정 제공 함수를 해제한다. 마지막으로 읽은 설정은 유지한다.
        /// </summary>
        public void ClearGetRidingViewSettings()
        {
            _getRidingSettings = null;
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
        /// 사격 반동을 원래 시점으로 되돌린다.
        /// </summary>
        private void UpdateRecoil()
        {
            if (_recoilOffset <= 0.0f) return;

            _recoilOffset = Mathf.MoveTowards(_recoilOffset, 0.0f, _recoilRecoverySpeed * Time.deltaTime);
            ApplyPivotRotation();
        }

        /// <summary>
        /// 기본 카메라의 원래 Damping·FOV 를 읽어 둔다. 이미 읽었으면 다시 읽지 않는다.
        /// </summary>
        private void CacheOriginals()
        {
            if (_hasOriginals || !_baseCamera) return;

            ReadOriginals();
            _hasOriginals = true;
        }

        private void ReadOriginals()
        {
            _originalFov = _baseCamera.Lens.FieldOfView;
            if (_baseFollow) _originalDamping = _baseFollow.Damping;
        }

        private bool IsSpeedFeedbackIdle()
        {
            return !_isRolling && _rollRecoverT >= 1.0f && !_isRiding && _ridingWeight <= 0.0f;
        }

        /// <summary>
        /// 구르기 Damping 복귀와 탑승 전환을 시간만큼 진행하고 결과를 반영한다.
        /// 효과가 없는 동안에는 시네머신에 쓰지 않고 원본 값을 다시 읽어 Inspector 의 런타임 변경을 따라간다.
        /// </summary>
        private void UpdateSpeedFeedback()
        {
            if (!_baseCamera) return;

            if (IsSpeedFeedbackIdle() && !_isEffectApplied)
            {
                ReadOriginals();
                return;
            }

            // 진행 속도도 설정값을 쓰므로 계산 전에 최신 설정을 읽는다
            RefreshSettings();

            if (!_isRolling)
            {
                float recoverTime = _rollSettings.RecoverTime;
                _rollRecoverT = recoverTime <= 0.0f
                    ? 1.0f
                    : Mathf.MoveTowards(_rollRecoverT, 1.0f, Time.deltaTime / recoverTime);
            }

            float targetWeight = _isRiding ? 1.0f : 0.0f;
            float transitionTime = _ridingSettings.TransitionTime;
            _ridingWeight = transitionTime <= 0.0f
                ? targetWeight
                : Mathf.MoveTowards(_ridingWeight, targetWeight, Time.deltaTime / transitionTime);

            WriteSpeedFeedback();
        }

        /// <summary>
        /// 설정 제공자가 있으면 최신 설정을 읽어 캐시한다. 제공자가 해제되어 있으면 마지막 설정을 그대로 쓴다.
        /// </summary>
        private void RefreshSettings()
        {
            if (_getRollSettings != null)
            {
                _rollSettings = _getRollSettings();
                _hasRollSettings = true;
            }

            if (_getRidingSettings != null)
            {
                _ridingSettings = _getRidingSettings();
                _hasRidingSettings = true;
            }
        }

        /// <summary>
        /// 최신 설정을 읽은 뒤 현재 진행도를 반영한다. 상태를 즉시 맞출 때 쓴다.
        /// </summary>
        private void ApplySpeedFeedback()
        {
            RefreshSettings();
            WriteSpeedFeedback();
        }

        /// <summary>
        /// 현재 진행도를 Damping·FOV·가장자리 블러에 반영한다. 효과가 모두 끝났으면 원본으로 두고 블러 대상을 해제한다.
        /// </summary>
        private void WriteSpeedFeedback()
        {
            if (!_baseCamera) return;

            if (IsSpeedFeedbackIdle())
            {
                if (_isEffectApplied) RestoreOriginals();

                _isEffectApplied = false;
                SpeedEdgeBlurRendererFeature.ClearTarget(_outputCamera);
                return;
            }

            CacheOriginals();
            _isEffectApplied = true;

            if (_baseFollow && _hasRollSettings)
            {
                _baseFollow.Damping = _isRolling
                    ? _rollSettings.Damping
                    : Vector3.Lerp(_rollSettings.Damping, _originalDamping, _rollRecoverT);
            }

            _baseCamera.Lens.FieldOfView = _originalFov + (_ridingSettings.FovOffset * _ridingWeight);

            // TODO: 공통 FullScreenFX 표시 함수가 생기면 (_isRolling || _isRiding) 동안 유지하고, 둘 다 끝났을 때만 끈다.
            if (!_outputCamera)
            {
                CinemachineBrain brain = CinemachineCore.FindPotentialTargetBrain(_baseCamera);
                if (brain) _outputCamera = brain.OutputCamera;
            }

            // 블러 설정이 없으면 머티리얼 기본값으로 번지지 않도록 대상을 지정하지 않는다
            if (_outputCamera && _hasRidingSettings)
            {
                SpeedEdgeBlurRendererFeature.SetTarget(_outputCamera, _ridingWeight, _ridingSettings.EdgeBlur);
            }
        }

        /// <summary>
        /// 기본 카메라의 Damping·FOV 를 원래 값으로 되돌린다.
        /// </summary>
        private void RestoreOriginals()
        {
            if (!_baseCamera || !_hasOriginals) return;

            _baseCamera.Lens.FieldOfView = _originalFov;
            if (_baseFollow) _baseFollow.Damping = _originalDamping;
        }

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
