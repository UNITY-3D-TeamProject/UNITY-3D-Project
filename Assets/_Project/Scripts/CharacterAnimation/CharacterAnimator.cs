using System;
using UnityEngine;

namespace CharacterAnimation
{
    /// <summary>
    /// 캐릭터 Animator 파라미터를 설정하는 컴포넌트.
    /// Animator는 캐릭터 루트에 두고, 이 스크립트는 자식에 있어도 된다.
    /// </summary>
    public class CharacterAnimator : MonoBehaviour
    {
        #region Constants
        private const string UPPER_BODY_LAYER_NAME = "UpperBody";
        #endregion

        #region Serialized Fields
        [Header("Locomotion")]
        [Tooltip("이동 방향 파라미터(MoveX, MoveY)가 목표값에 도달하는 데 걸리는 시간(초).")]
        [SerializeField] private float _moveDirectionDampTime = 0.1f;

        [Header("Fire")]
        [Tooltip("마지막 사격 후 사격 상체 애니메이션을 유지하는 시간(초).")]
        [SerializeField] private float _fireHoldDuration = 3.0f;

        [Header("Aim")]
        [Tooltip("사격 상체일 때 하체 이동으로 돌아간 골반 yaw 만큼 반대로 돌려 상체를 정면으로 맞출 뼈 이름. 이 뼈의 부모를 골반으로 본다.")]
        [SerializeField] private string _twistBoneName = "spine_01";
        [Tooltip("사격 상체일 때 총신이 조준점을 향하도록 돌릴 뼈 이름.")]
        [SerializeField] private string _aimBoneName = "upperarm_r";
        [Tooltip("총구 오브젝트 이름. 총신 방향은 총구의 부모 → 총구 방향으로 본다.")]
        [SerializeField] private string _muzzleName = "FirePosition";
        [Tooltip("조준 회전이 켜지고 꺼지는 데 걸리는 시간(초). UpperBody 레이어 전환 시간과 맞춘다.")]
        [SerializeField] private float _aimBlendTime = 0.15f;
        [Tooltip("조준 뼈에서 조준점까지 이 거리(m)보다 가까우면 조준 보정을 끈다. 벽에 붙어 사격할 때 팔이 꺾이는 것을 막는다.")]
        [SerializeField, Min(0.0f)] private float _minAimDistance = 1.0f;
        [Tooltip("최소 거리부터 이 거리(m)만큼 멀어지는 구간에서 조준 보정을 서서히 켠다.")]
        [SerializeField, Min(0.0f)] private float _aimFadeDistance = 0.5f;
        #endregion

        #region Private Fields
        private Animator _animator;
        private Vector3 _moveDirection;
        private Vector3 _viewForward = Vector3.forward;
        private Func<Vector3> _getAimPoint;
        private Transform _twistBone;
        private Transform _aimBone;
        private Transform _muzzle;
        // Idle 자세의 골반 로컬 회전. 이동 애니메이션이 골반을 얼마나 돌렸는지 비교하는 기준이다
        private bool _hasPelvisReference;
        private Quaternion _pelvisReferenceLocal;
        private int _upperBodyLayer = -1;
        // FireHold 자세의 총구 위치·총신 방향 (조준 뼈 부모 기준 로컬). 반동과 무관한 고정 보정의 기준이다
        private bool _hasHoldReference;
        private Vector3 _holdMuzzleLocal;
        private Vector3 _holdBarrelLocal;
        private bool _isFiring;
        private float _lastFireTime;
        private float _aimWeight;
        private bool _isRolling;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            // GetComponentInParent는 자기 자신을 먼저 검사하므로 루트/자식 배치를 모두 커버한다
            _animator = GetComponentInParent<Animator>();

            if (_animator == null)
            {
                Debug.LogError($"[{name}] Animator를 찾지 못했습니다. 캐릭터 루트에 추가하세요.", this);
                enabled = false;
                return;
            }

            CacheAimTransforms();
            _upperBodyLayer = _animator.GetLayerIndex(UPPER_BODY_LAYER_NAME);
        }

        private void Update()
        {
            // 몸은 시점 방향을 바라보므로 시점 기준 로컬 방향을 매 프레임 다시 계산한다
            Vector3 localDirection = Quaternion.Inverse(Quaternion.LookRotation(_viewForward)) * _moveDirection;
            float deltaTime = Time.deltaTime;

            _animator.SetFloat(AnimHash.MoveX, localDirection.x, _moveDirectionDampTime, deltaTime);
            _animator.SetFloat(AnimHash.MoveY, localDirection.z, _moveDirectionDampTime, deltaTime);

            if (_isFiring && (Time.time - _lastFireTime >= _fireHoldDuration))
            {
                _isFiring = false;
                _animator.SetBool(AnimHash.IsFiring, false);
            }

            // 구르는 동안에는 몸이 시점과 다른 방향을 보므로 조준 회전을 끈다
            float targetWeight = (_isFiring && !_isRolling) ? 1.0f : 0.0f;
            float blendSpeed = (_aimBlendTime > 0.0f) ? (deltaTime / _aimBlendTime) : 1.0f;
            _aimWeight = Mathf.MoveTowards(_aimWeight, targetWeight, blendSpeed);
        }

        private void LateUpdate()
        {
            // Animator 가 포즈를 적용한 뒤 상체를 정면으로 맞추고, 총신이 조준점을 향하도록 조준 뼈를 돌린다
            CapturePelvisReference();
            ApplyUpperBodyTwist();

            if (!_aimBone || !_muzzle || !_aimBone.parent) return;

            Transform reference = _aimBone.parent;
            CaptureHoldReference(reference);

            if ((_aimWeight <= 0.0f) || (_getAimPoint == null)) return;

            // 기준 자세(FireHold)로 보정값을 구해 현재 포즈에 더한다 — 재생 중 반동은 그대로 남는다.
            // 기준 자세를 아직 본 적이 없으면 현재 포즈로 구한다
            Vector3 muzzlePosition = _hasHoldReference ? reference.TransformPoint(_holdMuzzleLocal) : _muzzle.position;
            Vector3 barrelDirection = _hasHoldReference ? reference.TransformDirection(_holdBarrelLocal) : GetBarrelDirection();

            Vector3 aimPoint = _getAimPoint.Invoke();

            // 조준점이 너무 가까우면(벽에 붙은 상태) 보정을 줄여 애니메이션 자세로 돌린다
            float distanceWeight = CalculateAimDistanceWeight(_aimBone.position, aimPoint);
            if (distanceWeight <= 0.0f) return;

            bool hasCorrection = TryCalculateAimCorrection(
                _aimBone.position,
                muzzlePosition,
                barrelDirection,
                aimPoint,
                out Quaternion correction);
            if (!hasCorrection) return;

            _aimBone.rotation = Quaternion.Slerp(Quaternion.identity, correction, _aimWeight * distanceWeight) * _aimBone.rotation;
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// 이동 중 여부를 설정한다. true 면 Run, false 면 Idle 로 전환된다.
        /// </summary>
        /// <param name="isMoving">이동 중이면 true</param>
        public void SetMoving(bool isMoving)
        {
            if (!enabled) return;

            _animator.SetBool(AnimHash.IsMoving, isMoving);
        }

        /// <summary>
        /// 이동 방향을 설정한다. 시점 기준 로컬 방향으로 바꿔 MoveX, MoveY 에 매 프레임 반영된다.
        /// </summary>
        /// <param name="worldDirection">월드 기준 이동 방향 (수평 성분만 사용)</param>
        public void SetMoveDirection(Vector3 worldDirection)
        {
            _moveDirection = Vector3.ProjectOnPlane(worldDirection, Vector3.up);
        }

        /// <summary>
        /// 몸이 바라보는 기준인 시점 방향을 설정한다. 수평 성분이 없으면 직전 값을 유지한다.
        /// </summary>
        /// <param name="viewForward">카메라가 보는 방향 (월드 기준)</param>
        public void SetViewForward(Vector3 viewForward)
        {
            Vector3 flatForward = Vector3.ProjectOnPlane(viewForward, Vector3.up);
            if (flatForward.sqrMagnitude <= Mathf.Epsilon) return;

            _viewForward = flatForward.normalized;
        }

        /// <summary>
        /// 조준점(월드 위치)을 얻기 위한 델리게이트 주입. 사격 상체일 때 총신이 조준점을 향하도록 조준 뼈를 돌린다.
        /// </summary>
        /// <param name="getAimPoint">조준점을 반환하는 함수</param>
        public void SetGetAimPoint(Func<Vector3> getAimPoint)
        {
            if (getAimPoint == null) return;
            _getAimPoint = getAimPoint;
        }

        /// <summary>
        /// 조준점 델리게이트 제거. 이후에는 조준 보정을 하지 않는다.
        /// </summary>
        public void ClearGetAimPoint()
        {
            _getAimPoint = null;
        }

        /// <summary>
        /// 사격을 알린다. 사격 상체 애니메이션을 처음부터 1회 재생하고, 마지막 사격 후 일정 시간 동안 사격 자세를 유지한다.
        /// </summary>
        public void PlayFire()
        {
            if (!enabled) return;

            _isFiring = true;
            _lastFireTime = Time.time;
            _animator.SetBool(AnimHash.IsFiring, true);
            _animator.SetTrigger(AnimHash.Fire);
        }

        /// <summary>
        /// 점프 시작을 알린다. 점프 시작 → 상승 → 공중 순서로 재생된다. 공중에서 다시 점프하면 처음부터 재생한다.
        /// </summary>
        public void PlayJump()
        {
            if (!enabled) return;

            _animator.SetBool(AnimHash.IsJumping, true);
            _animator.SetTrigger(AnimHash.Jump);
        }

        /// <summary>
        /// 착지를 알린다. 어느 점프 단계에 있든 바로 착지 애니메이션으로 전환된다.
        /// </summary>
        public void PlayLand()
        {
            if (!enabled) return;

            _animator.SetBool(AnimHash.IsJumping, false);
            // 착지 직전 프레임에 들어온 점프 Trigger 가 남아 착지 후 다시 점프 애니메이션이 재생되지 않게 한다
            _animator.ResetTrigger(AnimHash.Jump);
        }

        /// <summary>
        /// 구르기 시작을 알린다. 구르는 동안 구르기 애니메이션을 재생한다.
        /// </summary>
        public void PlayRoll()
        {
            if (!enabled) return;

            _isRolling = true;
            _animator.SetBool(AnimHash.IsRolling, true);
            _animator.SetTrigger(AnimHash.Roll);
        }

        /// <summary>
        /// 구르기 종료를 알린다. 이동 중이면 Locomotion, 아니면 Idle 로 돌아간다. 공중이면 공중 애니메이션으로 돌아간다.
        /// </summary>
        public void StopRoll()
        {
            if (!enabled) return;

            _isRolling = false;
            _animator.SetBool(AnimHash.IsRolling, false);
            _animator.ResetTrigger(AnimHash.Roll);
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Animator 하위에서 조준 뼈와 총구를 이름으로 찾아 캐싱한다.
        /// </summary>
        private void CacheAimTransforms()
        {
            Transform[] children = _animator.GetComponentsInChildren<Transform>(true);

            _twistBone = Array.Find(children, child => child.name == _twistBoneName);
            _aimBone = Array.Find(children, child => child.name == _aimBoneName);
            _muzzle = Array.Find(children, child => child.name == _muzzleName);

            if (!_twistBone) Debug.LogWarning($"[{name}] 상체 비틀림 뼈 '{_twistBoneName}'을(를) 찾지 못했습니다.", this);
            if (!_aimBone) Debug.LogWarning($"[{name}] 조준 뼈 '{_aimBoneName}'을(를) 찾지 못했습니다.", this);
            if (!_muzzle) Debug.LogWarning($"[{name}] 총구 '{_muzzleName}'을(를) 찾지 못했습니다.", this);
        }

        /// <summary>
        /// 하체가 Idle(전환 중 아님)이면 골반 로컬 회전을 기준으로 저장한다.
        /// Animator 가 만든 포즈에서, 상체 보정을 적용하기 전에 호출해야 한다.
        /// </summary>
        private void CapturePelvisReference()
        {
            if (!_twistBone || !_twistBone.parent) return;
            if (_animator.IsInTransition(0)) return;
            if (_animator.GetCurrentAnimatorStateInfo(0).shortNameHash != AnimHash.IdleState) return;

            _pelvisReferenceLocal = _twistBone.parent.localRotation;
            _hasPelvisReference = true;
        }

        /// <summary>
        /// 사격 상체일 때, 이동 애니메이션이 골반을 기준 자세보다 돌린 yaw 만큼 비틀림 뼈를 반대로 돌린다.
        /// 골반의 기울기·출렁임은 그대로 두고 수평 회전만 되돌린다.
        /// </summary>
        private void ApplyUpperBodyTwist()
        {
            if ((_aimWeight <= 0.0f) || !_hasPelvisReference || !_twistBone || !_twistBone.parent) return;

            Transform pelvis = _twistBone.parent;
            Quaternion pelvisParentRotation = pelvis.parent ? pelvis.parent.rotation : Quaternion.identity;

            // 기준 자세 대비 골반 회전을 월드 공간으로 옮긴 뒤 수평 성분(yaw)만 뽑는다
            Quaternion localDelta = pelvis.localRotation * Quaternion.Inverse(_pelvisReferenceLocal);
            Quaternion worldDelta = pelvisParentRotation * localDelta * Quaternion.Inverse(pelvisParentRotation);
            Vector3 turnedForward = Vector3.ProjectOnPlane(worldDelta * Vector3.forward, Vector3.up);
            if (turnedForward.sqrMagnitude <= Mathf.Epsilon) return;

            float pelvisYaw = Vector3.SignedAngle(Vector3.forward, turnedForward, Vector3.up);
            _twistBone.rotation = Quaternion.AngleAxis(-pelvisYaw * _aimWeight, Vector3.up) * _twistBone.rotation;
        }

        /// <summary>
        /// 상체가 FireHold 자세(전환 중 아님)이면 총구 위치와 총신 방향을 기준 자세로 저장한다.
        /// 조준 보정을 적용하기 전, Animator 가 만든 포즈에서 호출해야 한다.
        /// </summary>
        /// <param name="reference">기준 공간 (조준 뼈의 부모)</param>
        private void CaptureHoldReference(Transform reference)
        {
            if ((_upperBodyLayer < 0) || _animator.IsInTransition(_upperBodyLayer)) return;
            if (_animator.GetCurrentAnimatorStateInfo(_upperBodyLayer).shortNameHash != AnimHash.FireHoldState) return;

            _holdMuzzleLocal = reference.InverseTransformPoint(_muzzle.position);
            _holdBarrelLocal = reference.InverseTransformDirection(GetBarrelDirection());
            _hasHoldReference = true;
        }

        /// <summary>
        /// 현재 포즈의 총신 방향(총구의 부모 → 총구)을 반환한다.
        /// </summary>
        /// <returns>정규화된 총신 방향 (월드 기준)</returns>
        private Vector3 GetBarrelDirection()
        {
            Transform barrelBase = _muzzle.parent ? _muzzle.parent : _aimBone;
            return (_muzzle.position - barrelBase.position).normalized;
        }

        /// <summary>
        /// 조준 뼈에서 조준점까지의 거리로 조준 보정 강도 계수를 구한다.
        /// 최소 거리 이하면 0, 최소 거리 + 페이드 거리 이상이면 1 이다.
        /// </summary>
        /// <param name="pivot">조준 뼈 위치</param>
        /// <param name="target">조준점</param>
        /// <returns>0~1 사이의 계수</returns>
        private float CalculateAimDistanceWeight(Vector3 pivot, Vector3 target)
        {
            float distance = Vector3.Distance(pivot, target);
            if (_aimFadeDistance <= 0.0f) return (distance >= _minAimDistance) ? 1.0f : 0.0f;

            return Mathf.Clamp01((distance - _minAimDistance) / _aimFadeDistance);
        }

        /// <summary>
        /// pivot 을 축으로 돌려 총신(muzzle 에서 barrel 방향)이 target 을 지나게 하는 회전을 구한다.
        /// 총신 직선 위에서 pivot 과 target 사이 거리만큼 떨어진 점을 찾아, 그 점을 target 쪽으로 돌린다.
        /// 그런 점이 없으면(조준점이 너무 가깝거나 총구 뒤) 실패로 보고 보정하지 않는다.
        /// </summary>
        /// <param name="pivot">회전 중심 (조준 뼈 위치)</param>
        /// <param name="muzzle">총구 위치</param>
        /// <param name="barrel">총신 방향 (정규화)</param>
        /// <param name="target">조준점</param>
        /// <param name="correction">조준 뼈에 월드 기준으로 곱할 회전. 실패하면 identity</param>
        /// <returns>회전을 구했으면 true</returns>
        private static bool TryCalculateAimCorrection(
            Vector3 pivot,
            Vector3 muzzle,
            Vector3 barrel,
            Vector3 target,
            out Quaternion correction)
        {
            correction = Quaternion.identity;

            Vector3 offset = muzzle - pivot;
            Vector3 pivotToTarget = target - pivot;

            // |offset + barrel * s| = |pivotToTarget| 를 s 에 대해 푼다 (barrel 은 정규화되어 있음)
            float projection = Vector3.Dot(offset, barrel);
            float discriminant = (projection * projection) - (offset.sqrMagnitude - pivotToTarget.sqrMagnitude);
            if (discriminant < 0.0f) return false;

            float distanceOnBarrel = -projection + Mathf.Sqrt(discriminant);
            if (distanceOnBarrel <= 0.0f) return false;

            correction = Quaternion.FromToRotation(offset + (barrel * distanceOnBarrel), pivotToTarget);
            return true;
        }
        #endregion

        #region Nested Types
        private static class AnimHash
        {
            public static readonly int IsMoving = Animator.StringToHash("IsMoving");
            public static readonly int MoveX = Animator.StringToHash("MoveX");
            public static readonly int MoveY = Animator.StringToHash("MoveY");
            public static readonly int IsFiring = Animator.StringToHash("IsFiring");
            public static readonly int Fire = Animator.StringToHash("Fire");
            public static readonly int IsJumping = Animator.StringToHash("IsJumping");
            public static readonly int Jump = Animator.StringToHash("Jump");
            public static readonly int IsRolling = Animator.StringToHash("IsRolling");
            public static readonly int Roll = Animator.StringToHash("Roll");
            public static readonly int FireHoldState = Animator.StringToHash("FireHold");
            public static readonly int IdleState = Animator.StringToHash("Idle");
        }
        #endregion
    }
}
