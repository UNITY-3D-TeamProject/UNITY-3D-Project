using System;
using System.Collections;
using UnityEngine;

namespace Pet
{
    /// <summary>
    /// 지정한 대상(Transform) 위치를 부드럽게 따라가는 펫.
    /// 실제 수평 이동 속도를 매 프레임 OnMoveSpeedUpdated 로 알리고, StopFollow 이후에는 추종을 멈춘다.
    /// MoveAndHold 를 쓰면 다른 대상까지 정해진 시간 안에 정확히 이동해 잠시 머문 뒤 원래 대상 추종으로 돌아온다 (스캔 대리 사용).
    /// 이 이동은 원래 대상에서 새 대상 사이에 벽이 있으면 벽 앞까지만 간다.
    /// </summary>
    public class PetFollower : MonoBehaviour
    {
        #region Constants
        // 이 속도(m/s) 이하면 멈춘 것으로 보고 대상의 정면을 바라본다
        private const float MOVE_THRESHOLD = 0.1f;
        #endregion

        #region Serialized Fields
        [Header("Follow")]
        [Tooltip("대상 위치까지 따라가는 데 걸리는 대략적인 시간(초)")]
        [SerializeField, Min(0.01f)] private float _smoothTime = 0.3f;
        [Tooltip("대상과 이 거리(m) 이상 멀어지면 대상 위치로 즉시 이동한다")]
        [SerializeField, Min(0.0f)] private float _teleportDistance = 10.0f;
        [Tooltip("초당 회전 각도")]
        [SerializeField] private float _rotateSpeed = 720.0f;

        [Header("Block")]
        [Tooltip("대상까지 가는 길을 막는 레이어. 가림 확인 기준이 있을 때만 쓴다")]
        [SerializeField] private LayerMask _blockingLayers = (1 << 0) | (1 << 7) | (1 << 10);
        [Tooltip("벽에 막혔을 때 벽에서 띄우는 거리(m)")]
        [SerializeField, Min(0.0f)] private float _wallMargin = 0.3f;

        [Header("Move And Hold")]
        [Tooltip("MoveAndHold 대상까지 이동하는 시간(초). 이 시간이 지나면 도착 위치를 알린다")]
        [SerializeField, Min(0.0f)] private float _moveDuration = 0.3f;
        [Tooltip("MoveAndHold 도착 후 그 자리에 머무는 시간(초). 지나면 원래 대상 추종으로 돌아간다")]
        [SerializeField, Min(0.0f)] private float _holdDuration = 0.5f;
        #endregion

        #region Private Fields
        // 현재 따라가는 대상. MoveAndHold 중에는 이동 대상이거나 null(머묾)
        private Transform _target;
        // SetTarget 으로 정한 원래 추종 대상. MoveAndHold 가 끝나면 여기로 돌아온다
        private Transform _followTarget;
        // 대상까지 벽에 막히는지 확인하는 기준. 없으면 확인하지 않는다
        private Transform _blockOrigin;
        private Vector3 _velocity;
        private bool _isStopped;
        // 시간 보간 이동 시간(초). 0 이면 SmoothDamp 로 따라간다
        private float _timedMoveDuration;
        private float _timedMoveElapsed;
        private Vector3 _timedMoveStartPosition;
        private Coroutine _moveAndHoldRoutine;
        #endregion

        #region Events
        /// <summary>추종 중 매 프레임 실제 수평 이동 속도(m/s)와 함께 발생한다. 제자리에 머물게 하면 0 으로 한 번 발생한다.</summary>
        public event Action<float> OnMoveSpeedUpdated;
        #endregion

        #region Unity Lifecycle
        // 대상(플레이어)이 Update 에서 이동한 뒤에 따라간다
        private void LateUpdate()
        {
            if (_isStopped || !_target) return;

            float deltaTime = Time.deltaTime;
            if (deltaTime <= 0.0f) return;

            Vector3 targetPosition = GetTargetPosition();
            if (_timedMoveDuration > 0.0f)
            {
                MoveTimed(targetPosition, deltaTime);
            }
            else if (Vector3.Distance(transform.position, targetPosition) >= _teleportDistance)
            {
                transform.SetPositionAndRotation(targetPosition, _target.rotation);
                _velocity = Vector3.zero;
            }
            else
            {
                transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref _velocity, _smoothTime);
            }

            Vector3 horizontalVelocity = Vector3.ProjectOnPlane(_velocity, Vector3.up);
            float moveSpeed = horizontalVelocity.magnitude;

            Rotate(moveSpeed > MOVE_THRESHOLD ? horizontalVelocity : _target.forward, deltaTime);
            OnMoveSpeedUpdated?.Invoke(moveSpeed);
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// 따라갈 대상을 설정하고 그 위치로 즉시 이동한다. 진행 중인 MoveAndHold 는 취소한다.
        /// </summary>
        /// <param name="target">따라갈 대상</param>
        public void SetTarget(Transform target)
        {
            CancelMoveAndHold();
            _followTarget = target;
            _target = target;
            _blockOrigin = null;
            _timedMoveDuration = 0.0f;
            _velocity = Vector3.zero;
            if (_target) transform.SetPositionAndRotation(_target.position, _target.rotation);
        }

        /// <summary>
        /// target 까지 _moveDuration 동안 이동해 정확히 도착하면 그 위치를 onArrived 로 알리고, _holdDuration 동안 그 자리에 머문 뒤 원래 대상 추종으로 돌아간다.
        /// 이동 중 target 이 움직여도 따라가며, 원래 대상에서 target 사이에 벽이 있으면 벽 앞까지만 간다. 진행 중이던 MoveAndHold 는 버린다.
        /// </summary>
        /// <param name="target">이동할 대상</param>
        /// <param name="onArrived">도착 위치(월드 좌표)를 받을 콜백</param>
        public void MoveAndHold(Transform target, Action<Vector3> onArrived)
        {
            CancelMoveAndHold();
            _moveAndHoldRoutine = StartCoroutine(CoMoveAndHold(target, onArrived));
        }

        /// <summary>
        /// 추종을 멈추고 그 자리에 머문다. 진행 중인 MoveAndHold 는 취소한다.
        /// </summary>
        public void StopFollow()
        {
            if (_moveAndHoldRoutine != null) StopCoroutine(_moveAndHoldRoutine);
            _moveAndHoldRoutine = null;
            _isStopped = true;
            _velocity = Vector3.zero;
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// 따라갈 대상을 바꾼다. SetTarget 과 달리 순간이동하지 않고 현재 위치에서 부드럽게 따라간다.
        /// </summary>
        /// <param name="target">따라갈 대상</param>
        private void ChangeTarget(Transform target)
        {
            _target = target;
            _blockOrigin = null;
            _timedMoveDuration = 0.0f;
        }

        /// <summary>
        /// 현재 위치에서 대상까지 duration 동안 이동해 정확히 도착하고, 이후에는 대상 위치에 붙어 있는다.
        /// 대상이 움직여도 매 프레임 현재 대상 위치를 향해 보간하며, 시작과 끝은 SmoothStep 으로 부드럽게 한다.
        /// </summary>
        /// <param name="target">이동할 대상</param>
        /// <param name="blockOrigin">대상까지 벽에 막히는지 확인하는 기준. 막히면 벽 앞까지만 간다. null 이면 확인하지 않는다</param>
        /// <param name="duration">도착까지 걸리는 시간(초)</param>
        private void MoveToTarget(Transform target, Transform blockOrigin, float duration)
        {
            _target = target;
            _blockOrigin = blockOrigin;
            // 0 이면 SmoothDamp 로 처리되므로 아주 작은 값으로 바꿔 다음 프레임에 바로 도착하게 한다
            _timedMoveDuration = Mathf.Max(duration, Mathf.Epsilon);
            _timedMoveElapsed = 0.0f;
            _timedMoveStartPosition = transform.position;
        }

        /// <summary>
        /// 대상을 비우고 그 자리에 머문다. ChangeTarget 으로 다시 따라가게 할 수 있다.
        /// </summary>
        private void HoldPosition()
        {
            _target = null;
            _blockOrigin = null;
            _timedMoveDuration = 0.0f;
            _velocity = Vector3.zero;
            OnMoveSpeedUpdated?.Invoke(0.0f);
        }

        /// <summary>
        /// 진행 중인 MoveAndHold 를 멈추고 원래 대상 추종으로 되돌린다.
        /// </summary>
        private void CancelMoveAndHold()
        {
            if (_moveAndHoldRoutine == null) return;

            StopCoroutine(_moveAndHoldRoutine);
            _moveAndHoldRoutine = null;
            ChangeTarget(_followTarget);
        }

        /// <summary>
        /// 시간 보간 이동 한 프레임. 시작 위치에서 현재 대상 위치까지 경과 비율(SmoothStep)만큼 보간하고, 애니메이션용 속도를 갱신한다.
        /// </summary>
        /// <param name="targetPosition">현재 대상 위치</param>
        /// <param name="deltaTime">프레임 시간</param>
        private void MoveTimed(Vector3 targetPosition, float deltaTime)
        {
            _timedMoveElapsed += deltaTime;
            float progress = Mathf.SmoothStep(0.0f, 1.0f, _timedMoveElapsed / _timedMoveDuration);
            Vector3 nextPosition = Vector3.Lerp(_timedMoveStartPosition, targetPosition, progress);

            _velocity = (nextPosition - transform.position) / deltaTime;
            transform.position = nextPosition;
        }

        /// <summary>
        /// 따라갈 위치를 반환한다. 가림 확인 기준에서 대상 사이에 벽이 있으면 벽 앞 위치를 반환한다.
        /// </summary>
        /// <returns>따라갈 위치(월드 좌표)</returns>
        private Vector3 GetTargetPosition()
        {
            Vector3 targetPosition = _target.position;
            if (!_blockOrigin) return targetPosition;

            Vector3 origin = _blockOrigin.position;
            Vector3 toTarget = targetPosition - origin;
            float distance = toTarget.magnitude;
            if (distance <= Mathf.Epsilon) return targetPosition;

            Vector3 direction = toTarget / distance;
            bool isBlocked = Physics.Raycast(origin, direction, out RaycastHit hit, distance, _blockingLayers, QueryTriggerInteraction.Ignore);
            if (!isBlocked) return targetPosition;

            return origin + direction * Mathf.Max(0.0f, hit.distance - _wallMargin);
        }

        private void Rotate(Vector3 direction, float deltaTime)
        {
            direction = Vector3.ProjectOnPlane(direction, Vector3.up);
            if (direction.sqrMagnitude <= Mathf.Epsilon) return;

            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, _rotateSpeed * deltaTime);
        }
        #endregion

        #region Coroutines
        /// <summary>
        /// target 까지 이동해 도착 위치를 알리고, 그 자리에 머물렀다가 원래 대상 추종으로 돌아간다.
        /// </summary>
        private IEnumerator CoMoveAndHold(Transform target, Action<Vector3> onArrived)
        {
            MoveToTarget(target, _followTarget, _moveDuration);
            yield return new WaitForSeconds(_moveDuration);

            HoldPosition();
            onArrived?.Invoke(transform.position);
            yield return new WaitForSeconds(_holdDuration);

            _moveAndHoldRoutine = null;
            ChangeTarget(_followTarget);
        }
        #endregion
    }
}
