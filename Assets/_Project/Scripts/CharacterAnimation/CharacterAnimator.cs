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
        #endregion

        #region Private Fields
        private Animator _animator;
        private Vector3 _moveDirection;
        private Vector3 _viewForward = Vector3.forward;
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
            }
        }

        private void Update()
        {
            // 몸은 시점 방향을 바라보므로 시점 기준 로컬 방향을 매 프레임 다시 계산한다
            Vector3 localDirection = Quaternion.Inverse(Quaternion.LookRotation(_viewForward)) * _moveDirection;
            float deltaTime = Time.deltaTime;

            _animator.SetFloat(AnimHash.MoveX, localDirection.x, _moveDirectionDampTime, deltaTime);
            _animator.SetFloat(AnimHash.MoveY, localDirection.z, _moveDirectionDampTime, deltaTime);
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
        #endregion

        #region Nested Types
        private static class AnimHash
        {
            public static readonly int IsMoving = Animator.StringToHash("IsMoving");
            public static readonly int MoveX = Animator.StringToHash("MoveX");
            public static readonly int MoveY = Animator.StringToHash("MoveY");
        }
        #endregion
    }
}
