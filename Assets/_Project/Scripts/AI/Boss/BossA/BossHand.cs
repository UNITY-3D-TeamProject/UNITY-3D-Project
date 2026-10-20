using System;
using UnityEngine;
using UnityEngine.AI;

namespace AI.Boss.BossA
{
    /// <summary>
    /// A 의 손. 위치 보간으로 떠오르고 내려치고 머물고 회수한다. 물리는 쓰지 않는다.
    /// 손 본(_bone)이 있으면 이 오브젝트를 따라 손 본만 움직인다(팔은 애니메이션 그대로). 떠올라 내려찍는 동안은 손가락이 아래를 향하게 돌린다. 쉬는 동안은 반대로 이 오브젝트가 평소 위치를 따라간다.
    /// 콜라이더는 손 프리팹 쪽에 늘 켜 두고(피격 중계·막기 겸용), 머무는 동안만 NavMesh 장애물을 켠다.
    /// </summary>
    public class BossHand : MonoBehaviour
    {
        #region Types
        private enum EHandState
        {
            Idle,
            MovingToHover,
            Hovering,
            Slamming,
            Landed,
            Staying,
            Recalling,
            Flying,
        }
        #endregion

        #region Serialized Fields
        [Tooltip("평소 위치. 움직이는 손 본이 아니라 팔 본 밑에 둔 별도 오브젝트여야 한다(손 본은 이 손을 따라 움직이므로)")]
        [SerializeField] private Transform _restPoint;
        [Tooltip("이 손을 따라 움직일 원본 모델의 손 본(Palm). 비워 두면 이 오브젝트만 움직인다")]
        [SerializeField] private Transform _bone;
        [Tooltip("손 본 원점이 이 오브젝트보다 위에 놓이는 높이(m). 손가락 끝이 이 오브젝트 위치(지면)에 닿게 한다")]
        [SerializeField, Min(0.0f)] private float _boneLift = 0.0f;
        [Tooltip("손 본 로컬 좌표에서 손가락 끝을 향하는 방향. 떠올라 내려찍는 동안 이 방향이 아래를 향하게 손을 돌린다(인형뽑기처럼)")]
        [SerializeField] private Vector3 _fingerAxis = Vector3.left;
        [Tooltip("손가락이 아래를 향할 때 손 본 원점이 이 오브젝트보다 위에 놓이는 높이(m). 손가락 끝이 이 오브젝트 위치(지면)에 닿게 한다")]
        [SerializeField, Min(0.0f)] private float _fingersDownLift = 0.0f;
        [Tooltip("내려친 순간부터 머무는 동안 손끝을 지면 아래로 박아 넣는 깊이(m). 클수록 손 전체가 낮아진다(콜라이더는 그대로). 손바닥 아래쪽을 지면에 맞추려면 손끝에서 손바닥 아래까지 길이만큼 준다")]
        [SerializeField, Min(0.0f)] private float _sinkDepth = 0.0f;
        [SerializeField, Min(0.0f)] private float _hoverHeight = 4.0f;
        [Tooltip("내려칠 지점에서 평소 위치 쪽으로 물러나 떠 있는 거리(m). 예고 동안 손이 대각선 위로 올라가 있다가 대각선으로 내려찍는다. 0이면 바로 위")]
        [SerializeField, Min(0.0f)] private float _hoverBackDistance = 4.0f;
        [SerializeField, Min(0.0f)] private float _moveSeconds = 0.4f;
        [SerializeField, Min(0.0f)] private float _slamSeconds = 0.15f;
        [Tooltip("머무는 동안만 켠다 (carve)")]
        [SerializeField] private NavMeshObstacle _stayObstacle;
        #endregion

        #region Private Fields
        private EHandState _state = EHandState.Idle;
        private Vector3 _fromPosition;
        private Vector3 _toPosition;
        private float _elapsed;
        private float _stayRemaining;
        private float _flySeconds;
        private float _fingerTilt;
        private float _fingerSink;
        private Action _onImpact;
        private Action _onFlyDone;
        private Action _onStayDone;
        private Action _onRecallDone;
        #endregion

        #region Properties
        public bool IsIdle => _state == EHandState.Idle;

        /// <summary>떠 있는 자리에서 내려쳐 닿기까지 걸리는 시간(초).</summary>
        public float SlamSeconds => _slamSeconds;

        /// <summary>내려칠 준비로 떠 있는 상태 (MoveToHover 도착 후).</summary>
        public bool IsHovering => _state == EHandState.Hovering;

        /// <summary>내려친 지점 (MoveToHover 에 넘긴 지면 위치).</summary>
        public Vector3 ImpactPoint { get; private set; }
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            if (_stayObstacle) _stayObstacle.enabled = false;
        }

        private void OnDisable()
        {
            ClearCallbacks();
            _state = EHandState.Idle;
            if (_stayObstacle) _stayObstacle.enabled = false;
        }

        private void Update()
        {
            float deltaTime = Time.deltaTime;

            switch (_state)
            {
                case EHandState.MovingToHover:
                    if (Advance(_moveSeconds, deltaTime)) _state = EHandState.Hovering;
                    break;

                case EHandState.Slamming:
                    if (Advance(_slamSeconds, deltaTime)) FinishSlam();
                    break;

                case EHandState.Staying:
                    _stayRemaining -= deltaTime;
                    if (_stayRemaining <= 0.0f) FinishStay();
                    break;

                case EHandState.Recalling:
                    // 평소 위치가 몸과 함께 움직일 수 있으므로 매 프레임 목적지를 갱신한다
                    if (_restPoint) _toPosition = GetRestPosition();
                    if (Advance(_moveSeconds, deltaTime)) FinishRecall();
                    break;

                case EHandState.Flying:
                    if (Advance(_flySeconds, deltaTime, true)) FinishFly();
                    break;
            }
        }

        // Animator 가 본을 쓴 뒤에 덮어써야 해서 LateUpdate 에서 한다
        private void LateUpdate()
        {
            if (!_bone) return;

            if (_state == EHandState.Idle)
            {
                // 쉬는 동안은 애니메이션이 본을 움직이고, 이 오브젝트(콜라이더)가 그 자리를 따라간다
                _fingerTilt = 0.0f;
                _fingerSink = 0.0f;
                transform.position = GetRestPosition();
                return;
            }

            // 회수 중과 날아가는 중에만 펴지고, 떠오르는 순간부터 머무는 동안은 손가락이 아래를 향한다
            bool isOpen = (_state == EHandState.Recalling) || (_state == EHandState.Flying);
            float tiltTarget = isOpen ? 0.0f : 1.0f;
            float tiltStep = (_moveSeconds > 0.0f) ? (Time.deltaTime / _moveSeconds) : 1.0f;
            _fingerTilt = Mathf.MoveTowards(_fingerTilt, tiltTarget, tiltStep);
            float tilt = Mathf.SmoothStep(0.0f, 1.0f, _fingerTilt);

            // 내려친 순간부터 머무는 동안은 손끝이 지면 아래로 박힌다
            bool isSunk = (_state == EHandState.Slamming) ||
                          (_state == EHandState.Landed) ||
                          (_state == EHandState.Staying);
            float sinkSeconds = isSunk ? _slamSeconds : _moveSeconds;
            float sinkStep = (sinkSeconds > 0.0f) ? (Time.deltaTime / sinkSeconds) : 1.0f;
            _fingerSink = Mathf.MoveTowards(_fingerSink, isSunk ? 1.0f : 0.0f, sinkStep);

            float lift = Mathf.Lerp(_boneLift, _fingersDownLift, tilt) - (_sinkDepth * _fingerSink);
            _bone.position = transform.position + (Vector3.up * lift);

            // 애니메이션이 쓴 회전에서 손가락 방향이 아래를 향하는 쪽으로 기울인다
            Vector3 fingerDirection = _bone.TransformDirection(_fingerAxis);
            Quaternion fingersDown = Quaternion.FromToRotation(fingerDirection, Vector3.down) * _bone.rotation;
            _bone.rotation = Quaternion.Slerp(_bone.rotation, fingersDown, tilt);
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// 지면 위 한 점의 대각선 위(평소 위치 쪽으로 물러난 높은 곳)로 떠오른다. 도착하면 IsHovering 이 된다.
        /// </summary>
        /// <param name="point">내려칠 지면 위치</param>
        public void MoveToHover(Vector3 point)
        {
            ImpactPoint = point;
            BeginMove(point + GetHoverOffset(point), EHandState.MovingToHover);
        }

        /// <summary>
        /// 떠 있는 상태에서 ImpactPoint 로 내려친다. 도착하는 순간 onImpact 를 부른다.
        /// </summary>
        public void Slam(Action onImpact)
        {
            if (_state != EHandState.Hovering) return;

            _onImpact = onImpact;
            BeginMove(ImpactPoint, EHandState.Slamming);
        }

        /// <summary>
        /// 지금 자리에서 월드 한 점까지 일정한 속도로 곧장 날아간다. 도착하면 onArrived 를 부른다.
        /// 날아가는 도중 Recall 하면 그 자리에서 평소 위치로 돌아오며 onArrived 는 부르지 않는다.
        /// </summary>
        /// <param name="position">날아갈 월드 위치(높이 포함)</param>
        /// <param name="seconds">걸리는 시간(초)</param>
        public void Fly(Vector3 position, float seconds, Action onArrived)
        {
            _flySeconds = seconds;
            _onFlyDone = onArrived;
            BeginMove(position, EHandState.Flying);
        }

        /// <summary>
        /// 내려친 자리에 머문다. 머무는 동안 NavMesh 장애물이 켜진다. seconds 가 0 이하면 바로 onDone 을 부른다.
        /// </summary>
        public void Stay(float seconds, Action onDone)
        {
            if (_state != EHandState.Landed) return;

            if (seconds <= 0.0f)
            {
                onDone?.Invoke();
                return;
            }

            _stayRemaining = seconds;
            _onStayDone = onDone;
            _state = EHandState.Staying;
            if (_stayObstacle) _stayObstacle.enabled = true;
        }

        /// <summary>
        /// 평소 위치로 회수한다. 머물던 중이면 머묾을 끝낸다. 도착하면 onDone 을 부른다.
        /// </summary>
        public void Recall(Action onDone)
        {
            ClearCallbacks();
            _onRecallDone = onDone;
            StartRecall();
        }

        /// <summary>
        /// 대기 중인 모든 콜백을 버리고 평소 위치로 회수한다.
        /// </summary>
        public void CancelAndRecall()
        {
            ClearCallbacks();
            if (_state == EHandState.Idle) return;

            StartRecall();
        }
        #endregion

        #region Private Methods
        private void BeginMove(Vector3 target, EHandState nextState)
        {
            _fromPosition = transform.position;
            _toPosition = target;
            _elapsed = 0.0f;
            _state = nextState;
        }

        /// <param name="isLinear">true 면 일정한 속도로, false 면 처음과 끝이 느리게 움직인다</param>
        /// <returns>도착했으면 true</returns>
        private bool Advance(float duration, float deltaTime, bool isLinear = false)
        {
            _elapsed += deltaTime;
            float ratio = (duration > 0.0f) ? Mathf.Clamp01(_elapsed / duration) : 1.0f;
            float eased = isLinear ? ratio : Mathf.SmoothStep(0.0f, 1.0f, ratio);
            transform.position = Vector3.Lerp(_fromPosition, _toPosition, eased);
            return ratio >= 1.0f;
        }

        /// <summary>
        /// 내려칠 지점 기준으로 떠 있을 자리의 상대 위치. 위로 _hoverHeight, 평소 위치 쪽 수평으로 _hoverBackDistance 다.
        /// </summary>
        private Vector3 GetHoverOffset(Vector3 point)
        {
            Vector3 offset = Vector3.up * _hoverHeight;
            if (!_restPoint) return offset;

            Vector3 backward = _restPoint.position - point;
            backward.y = 0.0f;
            if (backward.sqrMagnitude < 0.0001f) return offset;

            return offset + (backward.normalized * _hoverBackDistance);
        }

        private void StartRecall()
        {
            if (_stayObstacle) _stayObstacle.enabled = false;

            BeginMove(GetRestPosition(), EHandState.Recalling);
        }

        /// <summary>
        /// 평소 위치에 해당하는 이 오브젝트의 위치. 손 본이 평소 위치에 오도록 본 높이만큼 아래다.
        /// </summary>
        private Vector3 GetRestPosition()
        {
            if (!_restPoint) return transform.position;

            Vector3 lift = _bone ? (Vector3.up * _boneLift) : Vector3.zero;
            return _restPoint.position - lift;
        }

        private void FinishSlam()
        {
            _state = EHandState.Landed;
            Action onImpact = _onImpact;
            _onImpact = null;
            onImpact?.Invoke();
        }

        private void FinishFly()
        {
            _state = EHandState.Landed;
            Action onArrived = _onFlyDone;
            _onFlyDone = null;
            onArrived?.Invoke();
        }

        private void FinishStay()
        {
            if (_stayObstacle) _stayObstacle.enabled = false;

            _state = EHandState.Landed;
            Action onDone = _onStayDone;
            _onStayDone = null;
            onDone?.Invoke();
        }

        private void FinishRecall()
        {
            _state = EHandState.Idle;
            Action onDone = _onRecallDone;
            _onRecallDone = null;
            onDone?.Invoke();
        }

        private void ClearCallbacks()
        {
            _onImpact = null;
            _onFlyDone = null;
            _onStayDone = null;
            _onRecallDone = null;
        }
        #endregion
    }
}
