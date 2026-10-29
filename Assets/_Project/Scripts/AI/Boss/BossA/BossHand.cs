using System;
using UnityEngine;
using UnityEngine.AI;

namespace AI.Boss.BossA
{
    /// <summary>
    /// A 의 손. 위치 보간으로 떠오르고 내려치고 머물고 회수한다. 물리는 쓰지 않는다.
    /// 손 본(_bone)이 있으면 이 오브젝트를 따라 손 본만 움직인다(팔은 애니메이션 그대로). 떠올라 내려찍는 동안은 손가락이 아래를 향하게 돌린다(주먹으로 칠 때는 손가락을 오므려 판 쪽으로 눕힌다). 박수 자세에서는 손가락을 위로, 손바닥을 맞은편 손 쪽으로 세운다. 그랩 자세에서는 집게를 날아가는 방향으로 돌리고, 잡으면 오므린다. 쉬는 동안은 반대로 이 오브젝트가 평소 위치를 따라간다.
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
            Sweeping,
            Clapping,
        }

        [Serializable]
        private struct SFingerCurl
        {
            [Tooltip("주먹을 쥘 때 오므릴 손가락 본")]
            public Transform Bone;
            [Tooltip("주먹을 쥘 때 _curlAxis 둘레로 돌리는 각도(도). 손 안쪽으로 오므리는 부호를 준다")]
            public float FistAngle;
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
        [Tooltip("손 본 로컬 좌표에서 손바닥이 향하는 방향. 박수 자세에서 손가락은 위로, 이 방향은 맞은편 손을 향하게 손을 세운다")]
        [SerializeField] private Vector3 _palmAxis = Vector3.down;
        [Tooltip("박수 자세(손가락 위)에서 손 본 원점이 이 오브젝트(지면)보다 위에 놓이는 높이(m). 손목 쪽 손 아랫면이 지면에 닿게 한다")]
        [SerializeField, Min(0.0f)] private float _clapLift = 0.0f;
        [Tooltip("손가락이 아래를 향할 때 손 본 원점이 이 오브젝트보다 위에 놓이는 높이(m). 손가락 끝이 이 오브젝트 위치(지면)에 닿게 한다")]
        [SerializeField, Min(0.0f)] private float _fingersDownLift = 0.0f;
        [Tooltip("내려친 순간부터 머무는 동안 손끝을 지면 아래로 박아 넣는 깊이(m). 클수록 손 전체가 낮아진다(콜라이더는 그대로). 손바닥 아래쪽을 지면에 맞추려면 손끝에서 손바닥 아래까지 길이만큼 준다")]
        [SerializeField, Min(0.0f)] private float _sinkDepth = 0.0f;
        [Tooltip("예고 동안 내려칠 지점 위로 떠 있는 높이(m)")]
        [SerializeField, Min(0.0f)] private float _hoverHeight = 4.0f;
        [Tooltip("내려칠 지점에서 평소 위치 쪽으로 물러나 떠 있는 거리(m). 예고 동안 손이 대각선 위로 올라가 있다가 대각선으로 내려찍는다. 0이면 바로 위")]
        [SerializeField, Min(0.0f)] private float _hoverBackDistance = 4.0f;
        [Tooltip("떠 있을 자리로 가는 시간이자 평소 위치로 돌아오는 시간(초)")]
        [SerializeField, Min(0.0f)] private float _moveSeconds = 0.4f;
        [Tooltip("떠 있던 자리에서 지면까지 내려찍는 데 걸리는 시간(초)")]
        [SerializeField, Min(0.0f)] private float _slamSeconds = 0.15f;
        [Tooltip("머무는 동안만 켠다 (carve)")]
        [SerializeField] private NavMeshObstacle _stayObstacle;

        [Header("Fist")]
        [Tooltip("주먹을 쥘 때 오므릴 손가락 본들. 애니메이션이 매 프레임 회전을 쓰는 본이어야 한다(오므림을 그 위에 더한다)")]
        [SerializeField] private SFingerCurl[] _fingerCurls;
        [Tooltip("손가락 본 로컬 좌표에서 오므리는 회전축")]
        [SerializeField] private Vector3 _curlAxis = Vector3.up;
        [Tooltip("주먹 자세에서 지면을 향하게 할 손 본 로컬 방향. 손가락은 판 쪽으로 눕히고 이 면으로 망치처럼 내려친다")]
        [SerializeField] private Vector3 _fistDownAxis = Vector3.forward;
        [Tooltip("임시값. 주먹 자세에서 손 본 원점이 이 오브젝트(지면)보다 위에 놓이는 높이(m). 주먹 아랫면이 지면에 닿게 맞춘다")]
        [SerializeField, Min(0.0f)] private float _fistLift = 4.58f;

        [Header("Grab")]
        [Tooltip("손 본 밑에 둔 집게 턱 사이 지점. 그랩에 잡힌 플레이어의 발을 여기에 맞춘다. 비워 두면 이 오브젝트 위치")]
        [SerializeField] private Transform _grabPoint;
        [Tooltip("임시값. 잡았을 때 집게를 오므리는 정도. 주먹의 _fingerCurls 각도에 곱한다")]
        [SerializeField, Range(0.0f, 1.0f)] private float _gripCurl = 1.0f;
        #endregion

        #region Private Fields
        private EHandState _state = EHandState.Idle;
        private Vector3 _fromPosition;
        private Vector3 _toPosition;
        private float _elapsed;
        private float _stayRemaining;
        private float _flySeconds;
        private float _sweepSeconds;
        private float _clapSeconds;
        private float _fingerTilt;
        private float _fingerSink;
        private float _clapWeight;
        private float _fistWeight;
        private float _grabWeight;
        private float _gripWeight;
        private bool _isClapPose;
        private bool _isFist;
        private bool _isGrabPose;
        private bool _isGripping;
        private Vector3 _clapDirection;
        private Vector3 _grabDirection;
        private Action _onImpact;
        private Action _onFlyDone;
        private Action _onSweepDone;
        private Action _onClapDone;
        private Action _onStayDone;
        private Action _onRecallDone;
        #endregion

        #region Properties
        public bool IsIdle => _state == EHandState.Idle;

        /// <summary>박수 자세에서 손 본이 놓이는 위치.</summary>
        public Vector3 ClapPoint => transform.position + (Vector3.up * _clapLift);

        /// <summary>그랩에 잡힌 플레이어의 발을 맞출 집게 턱 사이 위치.</summary>
        public Vector3 GrabPoint => _grabPoint ? _grabPoint.position : transform.position;

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
            _isClapPose = false;
            _isFist = false;
            _isGrabPose = false;
            _isGripping = false;
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

                case EHandState.Sweeping:
                    if (Advance(_sweepSeconds, deltaTime, true)) FinishSweep();
                    break;

                case EHandState.Clapping:
                    if (Advance(_clapSeconds, deltaTime, true)) FinishClap();
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
                _clapWeight = 0.0f;
                _fistWeight = 0.0f;
                _grabWeight = 0.0f;
                _gripWeight = 0.0f;
                transform.position = GetRestPosition();
                return;
            }

            // 회수 중과 날아가는 중, 박수·주먹 자세에서는 펴지고, 떠오르는 순간부터 머무는 동안은 손가락이 아래를 향한다
            bool isOpen = (_state == EHandState.Recalling) || (_state == EHandState.Flying) || _isClapPose || _isFist;
            float tiltTarget = isOpen ? 0.0f : 1.0f;
            float tiltStep = (_moveSeconds > 0.0f) ? (Time.deltaTime / _moveSeconds) : 1.0f;
            _fingerTilt = Mathf.MoveTowards(_fingerTilt, tiltTarget, tiltStep);
            float tilt = Mathf.SmoothStep(0.0f, 1.0f, _fingerTilt);

            // 주먹 자세는 회수하거나 박수로 넘어갈 때 풀린다
            _fistWeight = Mathf.MoveTowards(_fistWeight, _isFist ? 1.0f : 0.0f, tiltStep);
            float fist = Mathf.SmoothStep(0.0f, 1.0f, _fistWeight);
            _clapWeight = Mathf.MoveTowards(_clapWeight, _isClapPose ? 1.0f : 0.0f, tiltStep);
            float clap = Mathf.SmoothStep(0.0f, 1.0f, _clapWeight);

            // 내려친 순간부터 머무는 동안은 손끝이 지면 아래로 박힌다. 박수·주먹 자세는 손가락을 세우거나 눕히므로 박지 않는다
            bool isSunk = !_isClapPose && !_isFist &&
                          ((_state == EHandState.Slamming) ||
                           (_state == EHandState.Landed) ||
                           (_state == EHandState.Sweeping) ||
                           (_state == EHandState.Staying));
            float sinkSeconds = isSunk ? _slamSeconds : _moveSeconds;
            float sinkStep = (sinkSeconds > 0.0f) ? (Time.deltaTime / sinkSeconds) : 1.0f;
            _fingerSink = Mathf.MoveTowards(_fingerSink, isSunk ? 1.0f : 0.0f, sinkStep);

            float lift = Mathf.Lerp(_boneLift, _fingersDownLift, tilt) - (_sinkDepth * _fingerSink);
            lift = Mathf.Lerp(lift, _fistLift, fist);
            lift = Mathf.Lerp(lift, _clapLift, clap);
            _bone.position = transform.position + (Vector3.up * lift);

            // 애니메이션이 쓴 회전에서 손가락 방향이 아래를 향하는 쪽으로 기울인다
            Vector3 fingerDirection = _bone.TransformDirection(_fingerAxis);
            Quaternion fingersDown = Quaternion.FromToRotation(fingerDirection, Vector3.down) * _bone.rotation;
            _bone.rotation = Quaternion.Slerp(_bone.rotation, fingersDown, tilt);

            // 주먹 자세: 손가락은 판 쪽으로 눕히고 _fistDownAxis 면이 지면을 향하게 해 망치처럼 내려친다
            if (fist > 0.0f)
            {
                Vector3 forward = GetFistForward();
                Quaternion fingersForward = Quaternion.FromToRotation(_bone.TransformDirection(_fingerAxis), forward) * _bone.rotation;
                Vector3 down = Vector3.ProjectOnPlane(fingersForward * _fistDownAxis, forward);
                Quaternion fistPose = Quaternion.AngleAxis(Vector3.SignedAngle(down, Vector3.down, forward), forward) * fingersForward;
                _bone.rotation = Quaternion.Slerp(_bone.rotation, fistPose, fist);
                CurlFingers(fist);
            }

            // 그랩 자세: 집게(손가락)를 날아가는 수평 방향으로 돌리고, 잡으면 집게를 오므린다. 회수할 때 풀린다
            _grabWeight = Mathf.MoveTowards(_grabWeight, _isGrabPose ? 1.0f : 0.0f, tiltStep);
            float grab = Mathf.SmoothStep(0.0f, 1.0f, _grabWeight);
            if (grab > 0.0f)
            {
                Vector3 fingers = _bone.TransformDirection(_fingerAxis);
                fingers.y = 0.0f;
                Quaternion grabFacing = Quaternion.AngleAxis(Vector3.SignedAngle(fingers, _grabDirection, Vector3.up), Vector3.up) * _bone.rotation;
                _bone.rotation = Quaternion.Slerp(_bone.rotation, grabFacing, grab);
            }

            _gripWeight = Mathf.MoveTowards(_gripWeight, _isGripping ? 1.0f : 0.0f, tiltStep);
            if (_gripWeight > 0.0f) CurlFingers(Mathf.SmoothStep(0.0f, 1.0f, _gripWeight) * _gripCurl);

            // 박수 자세: 손가락은 위로, 손바닥은 맞은편 손을 향하게 세운다
            if (clap <= 0.0f) return;

            Quaternion fingersUp = Quaternion.FromToRotation(_bone.TransformDirection(_fingerAxis), Vector3.up) * _bone.rotation;
            Vector3 palm = fingersUp * _palmAxis;
            palm.y = 0.0f;
            Quaternion palmFacing = Quaternion.AngleAxis(Vector3.SignedAngle(palm, _clapDirection, Vector3.up), Vector3.up) * fingersUp;
            _bone.rotation = Quaternion.Slerp(_bone.rotation, palmFacing, clap);
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// 지면 위 한 점의 대각선 위(평소 위치 쪽으로 물러난 높은 곳)로 떠오른다. 도착하면 IsHovering 이 된다.
        /// </summary>
        /// <param name="point">내려칠 지면 위치</param>
        /// <param name="isFist">true 면 손가락 아래 대신 주먹을 쥐고 망치처럼 내려친다. 회수하거나 박수로 넘어갈 때 풀린다</param>
        public void MoveToHover(Vector3 point, bool isFist = false)
        {
            ImpactPoint = point;
            _isFist = isFist;
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
        /// 그랩 자세로 바꾼다. 집게(손가락)를 direction 수평 방향으로 돌린다. 회수할 때 풀린다.
        /// </summary>
        public void SetGrabPose(Vector3 direction)
        {
            direction.y = 0.0f;
            _grabDirection = direction.normalized;
            _isGrabPose = true;
        }

        /// <summary>
        /// 그랩 자세에서 집게를 오므린다. 회수할 때 풀린다.
        /// </summary>
        public void Grip()
        {
            _isGripping = true;
        }

        /// <summary>
        /// 내려친 상태에서 바닥에 붙은 채 지면 위 한 점까지 일정한 속도로 쓸고 간다. 도착하면 onDone 을 부른다.
        /// </summary>
        /// <param name="point">쓸고 갈 지면 위치</param>
        /// <param name="seconds">걸리는 시간(초)</param>
        public void Sweep(Vector3 point, float seconds, Action onDone)
        {
            if (_state != EHandState.Landed) return;

            ImpactPoint = point;
            _sweepSeconds = seconds;
            _onSweepDone = onDone;
            BeginMove(point, EHandState.Sweeping);
        }

        /// <summary>
        /// 내려친 상태에서 한 점까지 일정한 속도로 움직이며 박수 자세(손가락 위, 손바닥이 palmDirection)로 손을 세운다.
        /// 도착하면 onDone 을 부르고, 회수할 때까지 박수 자세로 그 자리에 있는다.
        /// </summary>
        /// <param name="position">모일 월드 위치(높이 포함)</param>
        /// <param name="palmDirection">손바닥이 향할 수평 방향(맞은편 손 쪽)</param>
        /// <param name="seconds">걸리는 시간(초)</param>
        public void Clap(Vector3 position, Vector3 palmDirection, float seconds, Action onDone)
        {
            if (_state != EHandState.Landed) return;

            palmDirection.y = 0.0f;
            _clapDirection = palmDirection.normalized;
            _isClapPose = true;
            _isFist = false;
            _clapSeconds = seconds;
            _onClapDone = onDone;
            BeginMove(position, EHandState.Clapping);
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

            _isClapPose = false;
            _isFist = false;
            _isGrabPose = false;
            _isGripping = false;
            BeginMove(GetRestPosition(), EHandState.Recalling);
        }

        /// <summary>
        /// 주먹 자세에서 손가락이 향할 수평 방향. 평소 위치에서 내려칠 지점 쪽(판 쪽)이다.
        /// </summary>
        private Vector3 GetFistForward()
        {
            Vector3 forward = ImpactPoint - (_restPoint ? _restPoint.position : transform.position);
            forward.y = 0.0f;
            return (forward.sqrMagnitude > 0.0001f) ? forward.normalized : transform.forward;
        }

        /// <summary>
        /// 애니메이션이 쓴 손가락 회전 위에 주먹 오므림을 weight 만큼 더한다.
        /// </summary>
        private void CurlFingers(float weight)
        {
            if (_fingerCurls == null) return;

            foreach (SFingerCurl curl in _fingerCurls)
            {
                if (!curl.Bone) continue;

                curl.Bone.localRotation *= Quaternion.AngleAxis(curl.FistAngle * weight, _curlAxis);
            }
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

        private void FinishSweep()
        {
            _state = EHandState.Landed;
            Action onDone = _onSweepDone;
            _onSweepDone = null;
            onDone?.Invoke();
        }

        private void FinishClap()
        {
            _state = EHandState.Landed;
            Action onDone = _onClapDone;
            _onClapDone = null;
            onDone?.Invoke();
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
            _onSweepDone = null;
            _onClapDone = null;
            _onStayDone = null;
            _onRecallDone = null;
        }
        #endregion
    }
}
