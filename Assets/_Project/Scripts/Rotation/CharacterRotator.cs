using UnityEngine;

namespace Rotation
{
    /// <summary>
    /// 몸통 회전을 실제로 실행하는 컴포넌트. RotateMediator 로부터 명령만 받는다.
    /// CharacterMotor 와 같은 계약 형태다 — 값을 설정만 하면 유지되고, 실행은 이 컴포넌트가 매 프레임 대신 처리한다.
    /// MovePrefab 처럼 캐릭터 루트의 자식에 놓일 수 있으므로, 회전 대상은 자기 transform 이 아니라
    /// _body(보통 캐릭터 루트)로 명시한다. 자기 transform 을 돌리면 자식만 제자리에서 돌고 루트는 그대로 남는다.
    /// </summary>
    public class CharacterRotator : MonoBehaviour
    {
        #region Serialized Fields
        [Header("References")]
        [Tooltip("실제로 회전할 몸통(보통 캐릭터 루트). 비워두면 CharacterController 가 붙은 부모를 자동으로 찾는다.")]
        [SerializeField] private Transform _body;

        [Header("Rotation")]
        [Tooltip("회전 속도(도/초).")]
        [SerializeField] private float _rotateSpeed = 720.0f;
        #endregion

        #region Private Fields
        private Vector3 _lookDirection;
        private bool _hasLookDirection;
        // 회전 대상의 기준 자세(yaw 제외 X/Z). 예: SK_ZMike 의 로컬 X(-90).
        // 여기 저장해 두고 매 프레임 yaw 만 갈아끼운다 — Quaternion.LookRotation 을 그대로 대입하면
        // 기준 자세가 사라지고 몸통이 눕거나 뒤집힌다.
        private Quaternion _restRotation;
        private float _yaw;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            if (!_body)
            {
                // GetComponentInParent 는 자기 자신부터 검사하므로 루트/자식 배치를 모두 커버한다.
                // CharacterMotor.Awake (Movement/CharacterMotor.cs) 와 같은 탐색 순서를 맞춰,
                // 이동 대상과 회전 대상이 항상 같은 오브젝트를 가리키게 한다.
                CharacterController controller = GetComponentInParent<CharacterController>();
                if (controller)
                {
                    _body = controller.transform;
                }
                else
                {
                    _body = transform;
                    Debug.LogWarning(
                        $"[{name}] 회전 대상(_body)을 찾지 못해 자기 transform 을 사용한다. " +
                        "자식에 붙어 있다면 몸통이 아니라 이 오브젝트만 돈다.",
                        this);
                }
            }

            Vector3 restEuler = _body.localRotation.eulerAngles;
            _restRotation = Quaternion.Euler(restEuler.x, 0.0f, restEuler.z);
            _yaw = restEuler.y;
        }

        private void Update()
        {
            if (!_hasLookDirection) return;

            // 목표 yaw 는 _body 의 부모 공간 기준으로 계산한다 — 부모가 이미 회전해 있어도
            // (예: 이동 중 기울어진 루트) localRotation 에 맞는 각도가 나온다.
            Vector3 localDirection = _body.parent
                ? _body.parent.InverseTransformDirection(_lookDirection)
                : _lookDirection;
            float targetYaw = Mathf.Atan2(localDirection.x, localDirection.z) * Mathf.Rad2Deg;

            _yaw = Mathf.MoveTowardsAngle(_yaw, targetYaw, _rotateSpeed * Time.deltaTime);
            _body.localRotation = Quaternion.AngleAxis(_yaw, Vector3.up) * _restRotation;
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// 바라볼 월드 방향을 지정한다. 수평 성분만 쓰며, zero 면 직전 방향을 유지한다.
        /// </summary>
        /// <param name="worldDirection">목표 월드 방향</param>
        public void SetLookDirection(Vector3 worldDirection)
        {
            Vector3 flatDirection = Vector3.ProjectOnPlane(worldDirection, Vector3.up);
            if (flatDirection.sqrMagnitude <= Mathf.Epsilon) return;

            _lookDirection = flatDirection;
            _hasLookDirection = true;
        }
        #endregion
    }
}
