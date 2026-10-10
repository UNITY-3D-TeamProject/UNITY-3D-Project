using System;
using UnityEngine;
using UnityEngine.Splines;
using Mediator.SubMediators;

namespace AI.Crowd
{
    /// <summary>
    /// 로비 군중(보행자·차량)을 닫힌 스플라인 레인을 따라 순환시키는 조종부.
    /// AIController 와 같은 방식으로 이동·회전 요청만 중재자에게 보내며, 앞이 막히면 이동을 멈춘다.
    /// 이동 속도는 이 컴포넌트가 아니라 CharacterMediator 의 속도 어트리뷰트(레인이 생성 직후 넣는 값)가 정한다.
    /// 중재자가 Awake 에서 GetComponentInParent 로 찾으므로 프리팹 루트에 붙인다.
    /// </summary>
    public class CrowdController : MonoBehaviour, IMoveController, IBodyRotateController
    {
        #region Constants
        /// <summary>앞쪽 통로 검사 한 번에 받는 최대 충돌체 수. 자기 자신의 콜라이더가 섞여 들어오므로 넉넉히 둔다.</summary>
        private const int DETECTION_BUFFER_SIZE = 8;

        /// <summary>수평 방향 벡터의 제곱 크기가 이 값 이하이면 방향이 없는 것으로 본다.</summary>
        private const float MIN_DIRECTION_SQR_MAGNITUDE = 0.0001f;
        #endregion

        #region Serialized Fields
        [Header("Path Following")]
        [Tooltip("진행 거리에서 이만큼(m) 앞의 스플라인 점을 향해 이동한다.")]
        [SerializeField] private float _lookAheadDistance = 1.0f;

        [Header("Blocking Detection")]
        [Tooltip("진행 방향으로 이만큼(m) 앞까지 막힘을 검사한다. 보행자 1.5 / 차량 4 권장.")]
        [SerializeField] private float _detectDistance = 1.5f;
        [Tooltip("검사 구의 반지름(m). 몸 폭의 절반.")]
        [SerializeField] private float _detectRadius = 0.3f;
        [Tooltip("검사 구의 중심 높이(m). 몸통 중간 높이로 둔다.")]
        [SerializeField] private float _detectHeight = 0.9f;
        [Tooltip("멈춰야 하는 대상의 레이어. 보행자·차량 모두 Player, Crowd, VehicleBody.")]
        [SerializeField] private LayerMask _detectLayers;
        #endregion

        #region Private Fields
        private readonly RaycastHit[] _hitBuffer = new RaycastHit[DETECTION_BUFFER_SIZE];

        private SplineContainer _lane;
        private float _laneLength;
        private float _progress;
        private Vector3 _lastPosition;
        #endregion

        #region Events
        private event Action<Vector2> _onMoveRequested;
        private event Action _onJumpRequested;
        private event Action<Vector2> _onBodyRotateRequested;
        #endregion

        #region Unity Lifecycle
        private void Update()
        {
            if (!_lane)
            {
                RequestMove(Vector2.zero);
                return;
            }

            AdvanceProgress();

            Vector3 direction = GetDirectionToTarget();
            if (direction.sqrMagnitude <= MIN_DIRECTION_SQR_MAGNITUDE)
            {
                RequestMove(Vector2.zero);
                return;
            }

            direction.Normalize();

            // 막히면 이동 0 만 요청한다. 회전은 보내지 않아 직전 방향을 유지한다.
            if (IsBlocked(direction))
            {
                RequestMove(Vector2.zero);
                return;
            }

            // 기준 프레임이 없는 MoveMediator 는 (x, y) 를 월드 (x, 0, y) 로 해석한다
            Vector2 direction2D = new Vector2(direction.x, direction.z);
            RequestMove(direction2D);
            RequestRotate(direction2D);
        }

        private void OnDisable()
        {
            // 비활성화 시 마지막 이동 명령이 남아 계속 미끄러지지 않도록 정지시킨다
            RequestMove(Vector2.zero);
        }
        #endregion

        #region IMoveController
        /// <inheritdoc />
        public void SetMoveRequest(Action<Vector2> callback)
        {
            if (callback == null) return;
            _onMoveRequested = callback;
        }

        /// <inheritdoc />
        public void SetJumpRequest(Action callback)
        {
            if (callback == null) return;
            _onJumpRequested = callback;
        }

        /// <inheritdoc />
        public void ClearMoveRequest()
        {
            _onMoveRequested = null;
        }

        /// <inheritdoc />
        public void ClearJumpRequest()
        {
            _onJumpRequested = null;
        }
        #endregion

        #region IBodyRotateController
        /// <inheritdoc />
        public void SetBodyRotateRequest(Action<Vector2> callback)
        {
            if (callback == null) return;
            _onBodyRotateRequested = callback;
        }

        /// <inheritdoc />
        public void ClearBodyRotateRequest()
        {
            _onBodyRotateRequested = null;
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// 따라갈 레인과 시작 진행 거리를 지정한다. 레인 오브젝트의 스케일은 1 이어야 한다.
        /// </summary>
        /// <param name="lane">닫힌 스플라인을 가진 레인</param>
        /// <param name="startDistance">스플라인 시작점으로부터의 진행 거리(m)</param>
        public void SetLane(SplineContainer lane, float startDistance)
        {
            _lane = lane;
            _laneLength = lane ? lane.CalculateLength() : 0.0f;

            if (_laneLength <= Mathf.Epsilon)
            {
                _lane = null;
                return;
            }

            _progress = Mathf.Repeat(startDistance, _laneLength);
            _lastPosition = transform.position;
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// 중재자에 이동을 요청한다.
        /// </summary>
        /// <param name="input">이동 입력 (x: 좌우, y: 전후)</param>
        private void RequestMove(Vector2 input)
        {
            _onMoveRequested?.Invoke(input);
        }

        /// <summary>
        /// 중재자에 몸통 회전을 요청한다.
        /// </summary>
        /// <param name="direction">회전 방향 입력 (x, y — 월드 기준)</param>
        private void RequestRotate(Vector2 direction)
        {
            _onBodyRotateRequested?.Invoke(direction);
        }

        /// <summary>
        /// 지난 프레임 이후 실제로 움직인 거리를 스플라인 접선에 투영해 진행 거리를 늘린다.
        /// 멈춰 있으면 움직임이 0이라 진행 거리도 늘지 않는다. 길이를 넘으면 0으로 감는다.
        /// </summary>
        private void AdvanceProgress()
        {
            Vector3 position = transform.position;
            Vector3 moved = position - _lastPosition;
            _lastPosition = position;

            Vector3 tangent = GetHorizontalTangent(_progress);
            if (tangent.sqrMagnitude <= MIN_DIRECTION_SQR_MAGNITUDE) return;

            float advance = Mathf.Max(0.0f, Vector3.Dot(moved, tangent.normalized));
            _progress = Mathf.Repeat(_progress + advance, _laneLength);
        }

        /// <summary>
        /// 현재 위치에서 진행 거리 앞쪽 목표점까지의 수평 방향(정규화되지 않음)을 구한다.
        /// </summary>
        /// <returns>수평 방향 벡터</returns>
        private Vector3 GetDirectionToTarget()
        {
            float targetDistance = Mathf.Repeat(_progress + _lookAheadDistance, _laneLength);
            Vector3 target = _lane.EvaluatePosition(DistanceToNormalized(targetDistance));

            return Vector3.ProjectOnPlane(target - transform.position, Vector3.up);
        }

        /// <summary>
        /// 진행 거리 지점의 스플라인 접선을 수평면에 투영해 반환한다.
        /// </summary>
        /// <param name="distance">진행 거리(m)</param>
        /// <returns>수평 접선 벡터(정규화되지 않음)</returns>
        private Vector3 GetHorizontalTangent(float distance)
        {
            Vector3 tangent = _lane.EvaluateTangent(DistanceToNormalized(distance));

            return Vector3.ProjectOnPlane(tangent, Vector3.up);
        }

        /// <summary>
        /// 미터 단위 진행 거리를 스플라인의 0~1 정규화 t 로 바꾼다.
        /// </summary>
        /// <param name="distance">진행 거리(m)</param>
        /// <returns>정규화된 t</returns>
        private float DistanceToNormalized(float distance)
        {
            return distance / _laneLength;
        }

        /// <summary>
        /// 진행 방향 앞쪽에 멈춰야 할 대상이 있는지 검사한다. 자기 자신의 콜라이더는 제외한다.
        /// </summary>
        /// <param name="direction">정규화된 수평 진행 방향</param>
        /// <returns>막혀 있으면 true</returns>
        private bool IsBlocked(Vector3 direction)
        {
            Vector3 origin = transform.position + Vector3.up * _detectHeight;

            int hitCount = Physics.SphereCastNonAlloc(
                origin,
                _detectRadius,
                direction,
                _hitBuffer,
                _detectDistance,
                _detectLayers,
                QueryTriggerInteraction.Ignore);

            for (int i = 0; i < hitCount; i++)
            {
                bool isSelf = _hitBuffer[i].collider.transform.IsChildOf(transform);
                if (!isSelf) return true;
            }

            return false;
        }
        #endregion
    }
}
