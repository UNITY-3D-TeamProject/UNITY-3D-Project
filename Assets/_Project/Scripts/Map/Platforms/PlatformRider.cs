using UnityEngine;
using Movement;

namespace Map.Platforms
{
    /// <summary>
    /// 캐릭터가 RideablePlatform 위에 서 있을 때 발판의 이동량을 CharacterMotor에 전달한다.
    /// 발판에서 점프해 공중에 있는 동안에는 발판의 수평 속도를 그대로 이어받아, 발판이 발밑에서 빠져나가지 않게 한다.
    /// CharacterController가 붙은 오브젝트(캐릭터 루트)에 붙인다.
    /// </summary>
    [DefaultExecutionOrder(-50)]   // 발판(-100) 다음, CharacterMotor(0) 이전
    [RequireComponent(typeof(CharacterController))]
    public class PlatformRider : MonoBehaviour
    {
        #region Constants
        // 발밑으로 이 거리 안에 발판이 있으면 타고 있는 것으로 본다. (CharacterController의 Skin Width에 더한다)
        private const float PROBE_DISTANCE = 0.2f;

        // 캡슐 옆면에 닿은 벽을 발밑으로 오인하지 않도록 탐색 구의 반지름을 조금 줄인다.
        private const float PROBE_RADIUS_RATIO = 0.9f;

        private const int PROBE_BUFFER_SIZE = 8;
        #endregion

        #region Private Fields
        private readonly RaycastHit[] _probeHits = new RaycastHit[PROBE_BUFFER_SIZE];
        private CharacterController _controller;
        private CharacterMotor _motor;
        private Vector3 _carriedVelocity;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            _motor = GetComponentInChildren<CharacterMotor>();
            Debug.Assert(_motor != null, $"[{name}] CharacterMotor를 찾지 못했습니다.");
        }

        private void Update()
        {
            RideablePlatform platform = FindPlatformBelow();
            if (platform != null)
            {
                // 누적값이 아니라 발판의 이번 프레임 이동량을 읽는다.
                Vector3 delta = platform.DeltaThisFrame;
                _motor.AddExternalDisplacement(delta);

                if (Time.deltaTime > 0.0f)
                {
                    _carriedVelocity = delta / Time.deltaTime;
                    _carriedVelocity.y = 0.0f;
                }

                return;
            }

            // 발판이 아닌 땅에 내려섰으면 이어받은 속도를 버린다.
            if (_motor.IsGrounded)
            {
                _carriedVelocity = Vector3.zero;
                return;
            }

            _motor.AddExternalDisplacement(_carriedVelocity * Time.deltaTime);
        }
        #endregion

        #region Private Methods
        // CharacterController의 충돌 보고는 발판이 빠르게 움직이거나 오르내릴 때 끊길 수 있어, 발밑을 직접 탐색한다.
        private RideablePlatform FindPlatformBelow()
        {
            float radius = _controller.radius * PROBE_RADIUS_RATIO;
            float bottomOffset = (_controller.height * 0.5f) - _controller.radius;
            Vector3 origin = transform.TransformPoint(_controller.center) + (Vector3.down * bottomOffset);
            float distance = (_controller.radius - radius) + _controller.skinWidth + PROBE_DISTANCE;

            int hitCount = Physics.SphereCastNonAlloc(
                origin,
                radius,
                Vector3.down,
                _probeHits,
                distance,
                Physics.AllLayers,
                QueryTriggerInteraction.Ignore);

            for (int i = 0; i < hitCount; i++)
            {
                RideablePlatform platform = _probeHits[i].collider.GetComponentInParent<RideablePlatform>();
                if (platform != null) return platform;
            }

            return null;
        }
        #endregion
    }
}
