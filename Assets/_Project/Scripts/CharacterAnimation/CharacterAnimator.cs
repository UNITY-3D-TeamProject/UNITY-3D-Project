using System;
using System.Collections.Generic;
using UnityEngine;

namespace CharacterAnimation
{
    /// <summary>
    /// 캐릭터 Animator 파라미터를 설정하는 컴포넌트.
    /// Animator는 캐릭터 루트에 두고, 이 스크립트는 자식에 있어도 된다.
    /// </summary>
    public class CharacterAnimator : MonoBehaviour
    {
        #region Serialized Fields
        [Header("Locomotion")]
        [Tooltip("이동 방향 파라미터(MoveX, MoveY)가 목표값에 도달하는 데 걸리는 시간(초).")]
        [SerializeField] private float _moveDirectionDampTime = 0.1f;

        [Header("Fire")]
        [Tooltip("마지막 사격 후 사격 상체 애니메이션을 유지하는 시간(초).")]
        [SerializeField] private float _fireHoldDuration = 3.0f;

        [Header("Aim")]
        [Tooltip("사격 상체일 때 시점의 위아래 각도를 나눠 적용할 뼈 이름. 부모에서 자식 순서로 적는다.")]
        [SerializeField] private string[] _aimBoneNames = { "spine_01", "spine_02", "spine_03" };
        [Tooltip("조준 회전이 켜지고 꺼지는 데 걸리는 시간(초). UpperBody 레이어 전환 시간과 맞춘다.")]
        [SerializeField] private float _aimBlendTime = 0.15f;
        #endregion

        #region Private Fields
        private Animator _animator;
        private Vector3 _moveDirection;
        private Vector3 _viewForward = Vector3.forward;
        private Vector3 _viewDirection = Vector3.forward;
        private Transform[] _aimBones;
        private bool _isFiring;
        private float _lastFireTime;
        private float _aimWeight;
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

            CacheAimBones();
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

            float targetWeight = _isFiring ? 1.0f : 0.0f;
            float blendSpeed = (_aimBlendTime > 0.0f) ? (deltaTime / _aimBlendTime) : 1.0f;
            _aimWeight = Mathf.MoveTowards(_aimWeight, targetWeight, blendSpeed);
        }

        private void LateUpdate()
        {
            // Animator 가 포즈를 적용한 뒤 시점의 위아래 각도만큼 척추를 나눠 숙이거나 젖힌다
            if (_aimWeight <= 0.0f || _aimBones.Length == 0) return;

            Quaternion pitch = Quaternion.FromToRotation(_viewForward, _viewDirection);
            Quaternion share = Quaternion.Slerp(Quaternion.identity, pitch, _aimWeight / _aimBones.Length);

            foreach (Transform bone in _aimBones)
            {
                bone.rotation = share * bone.rotation;
            }
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
        /// 위아래 각도는 사격 상체의 조준 회전에 쓴다.
        /// </summary>
        /// <param name="viewForward">카메라가 보는 방향 (월드 기준)</param>
        public void SetViewForward(Vector3 viewForward)
        {
            Vector3 flatForward = Vector3.ProjectOnPlane(viewForward, Vector3.up);
            if (flatForward.sqrMagnitude <= Mathf.Epsilon) return;

            _viewForward = flatForward.normalized;
            _viewDirection = viewForward.normalized;
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
        #endregion

        #region Private Methods
        /// <summary>
        /// Animator 하위에서 조준 회전을 적용할 뼈를 이름으로 찾아 캐싱한다.
        /// </summary>
        private void CacheAimBones()
        {
            Transform[] children = _animator.GetComponentsInChildren<Transform>(true);
            var bones = new List<Transform>(_aimBoneNames.Length);

            foreach (string boneName in _aimBoneNames)
            {
                Transform bone = Array.Find(children, child => child.name == boneName);
                if (bone)
                    bones.Add(bone);
                else
                    Debug.LogWarning($"[{name}] 조준 뼈 '{boneName}'을(를) 찾지 못했습니다.", this);
            }

            _aimBones = bones.ToArray();
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
        }
        #endregion
    }
}
