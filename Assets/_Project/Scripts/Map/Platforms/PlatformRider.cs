using UnityEngine;
using Movement;

namespace Map.Platforms
{
    /// <summary>
    /// 캐릭터가 RideablePlatform 위에 서 있을 때 발판의 이동량만큼 캐릭터를 함께 옮긴다.
    /// 발판에서 점프해 공중에 있는 동안에는 그 발판이 수평으로 움직인 만큼 같이 움직여, 발판을 기준으로 점프하게 한다.
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
        private RideablePlatform _lastPlatform;
        private Vector3 _pendingDisplacement;
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
            // 서 있을 때만 발판을 따라간다. 점프해서 떠 있는 동안에도 따라가면, 올라가는 발판 위에서 뛸 때 발판의 상승분이 더해져 지나치게 높이 뛴다.
            RideablePlatform platform = _motor.IsGrounded ? FindPlatformBelow() : null;
            if (platform != null)
            {
                // 누적값이 아니라 발판의 이번 프레임 이동량을 읽는다.
                FollowPlatform(platform.DeltaThisFrame);
                _lastPlatform = platform;
                return;
            }

            _pendingDisplacement = Vector3.zero;

            // 발판이 아닌 땅에 내려섰으면 더는 그 발판을 따라가지 않는다.
            if (_motor.IsGrounded)
            {
                _lastPlatform = null;
                return;
            }

            // 공중: 방금 떠난 발판이 수평으로 움직인 만큼만 같이 움직인다. 제자리에서 뛰면 발판 위 같은 자리에 내린다.
            if ((_lastPlatform == null) || !_lastPlatform.isActiveAndEnabled) return;

            Vector3 airDelta = _lastPlatform.DeltaThisFrame;
            airDelta.y = 0.0f;
            _motor.AddExternalDisplacement(airDelta);
        }
        #endregion

        #region Private Methods
        // 발판이 움직인 만큼 캐릭터를 먼저 옮겨 놓는다. 그 뒤 CharacterMotor가 자기 이동(접지 유지용 하강 포함)을 따로 적용한다.
        // 발판 이동량을 CharacterMotor의 이동에 합쳐 한 번에 움직이면, 접지 유지용 하강 속도가 발판의 상승분을 지워서
        // 올라가는 발판 위에서 캐릭터가 발판에 박혔다 밀려나기를 반복하며 떨린다.
        private void FollowPlatform(Vector3 delta)
        {
            // CharacterController는 Min Move Distance보다 짧은 이동을 무시하므로, 모자란 이동량은 모아 두었다가 한 번에 옮긴다.
            _pendingDisplacement += delta;
            if (_pendingDisplacement.magnitude < _controller.minMoveDistance) return;

            // 발판은 이번 프레임에 Transform으로 이미 움직였다. 물리 쪽 위치를 맞춰야 새 위치의 발판을 기준으로 옮겨진다.
            Physics.SyncTransforms();
            _controller.Move(_pendingDisplacement);
            _pendingDisplacement = Vector3.zero;
        }

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
