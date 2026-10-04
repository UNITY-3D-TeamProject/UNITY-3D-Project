using UnityEngine;
using Movement;

namespace Map.Platforms
{
    /// <summary>
    /// 캐릭터가 RideablePlatform 위에 서 있을 때 발판의 이동량을 CharacterMotor에 전달한다.
    /// OnControllerColliderHit을 받아야 하므로 CharacterController가 붙은 오브젝트(캐릭터 루트)에 붙인다.
    /// </summary>
    [DefaultExecutionOrder(-50)]   // 발판(-100) 다음, CharacterMotor(0) 이전
    [RequireComponent(typeof(CharacterController))]
    public class PlatformRider : MonoBehaviour
    {
        #region Constants
        private const float FLOOR_NORMAL_THRESHOLD = 0.5f;

        // CharacterMotor가 Update로 돌든 FixedUpdate로 돌든 접촉 보고가 끊기지 않을 만큼의 여유
        private const float CONTACT_GRACE_SECONDS = 0.1f;
        #endregion

        #region Private Fields
        private CharacterMotor _motor;
        private RideablePlatform _currentPlatform;
        private float _contactTime;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            _motor = GetComponentInChildren<CharacterMotor>();
            Debug.Assert(_motor != null, $"[{name}] CharacterMotor를 찾지 못했습니다.");
        }

        private void Update()
        {
            if (_currentPlatform == null) return;

            bool isOnPlatform = (Time.time - _contactTime) <= CONTACT_GRACE_SECONDS;
            if (!isOnPlatform)
            {
                _currentPlatform = null;
                return;
            }

            // 누적값이 아니라 발판의 이번 프레임 이동량을 읽는다.
            _motor.AddExternalDisplacement(_currentPlatform.DeltaThisFrame);
        }

        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            bool isFloorHit = hit.normal.y > FLOOR_NORMAL_THRESHOLD;
            if (!isFloorHit) return;

            // 여기서는 발판과 시각만 기록한다. 이동량을 여기서 쌓으면 한 프레임 늦게 적용된다.
            RideablePlatform platform = hit.collider.GetComponentInParent<RideablePlatform>();
            if (platform != null)
            {
                _currentPlatform = platform;
                _contactTime = Time.time;
            }
        }
        #endregion
    }
}
