using System;
using UnityEngine;
using UnityEngine.AI;

namespace AI.Boss.BossA
{
    /// <summary>
    /// A 의 손. 위치 보간으로 떠오르고 내려치고 머물고 회수한다. 물리는 쓰지 않는다.
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
        }
        #endregion

        #region Serialized Fields
        [Tooltip("평소 위치 (A 어깨 앞)")]
        [SerializeField] private Transform _restPoint;
        [SerializeField, Min(0.0f)] private float _hoverHeight = 4.0f;
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
        private Action _onImpact;
        private Action _onStayDone;
        private Action _onRecallDone;
        #endregion

        #region Properties
        public bool IsIdle => _state == EHandState.Idle;

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
                    if (_restPoint) _toPosition = _restPoint.position;
                    if (Advance(_moveSeconds, deltaTime)) FinishRecall();
                    break;
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// 지면 위 한 점의 위로 떠오른다. 도착하면 IsHovering 이 된다.
        /// </summary>
        /// <param name="point">내려칠 지면 위치</param>
        public void MoveToHover(Vector3 point)
        {
            ImpactPoint = point;
            BeginMove(point + (Vector3.up * _hoverHeight), EHandState.MovingToHover);
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

        /// <returns>도착했으면 true</returns>
        private bool Advance(float duration, float deltaTime)
        {
            _elapsed += deltaTime;
            float ratio = (duration > 0.0f) ? Mathf.Clamp01(_elapsed / duration) : 1.0f;
            transform.position = Vector3.Lerp(_fromPosition, _toPosition, Mathf.SmoothStep(0.0f, 1.0f, ratio));
            return ratio >= 1.0f;
        }

        private void StartRecall()
        {
            if (_stayObstacle) _stayObstacle.enabled = false;

            Vector3 target = _restPoint ? _restPoint.position : transform.position;
            BeginMove(target, EHandState.Recalling);
        }

        private void FinishSlam()
        {
            _state = EHandState.Landed;
            Action onImpact = _onImpact;
            _onImpact = null;
            onImpact?.Invoke();
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
            _onStayDone = null;
            _onRecallDone = null;
        }
        #endregion
    }
}
