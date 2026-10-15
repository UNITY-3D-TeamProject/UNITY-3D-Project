using System;
using UnityEngine;
using UnityEngine.Splines;
using Mediator;
using Mediator.SubMediators;

namespace AI.Crowd
{
    /// <summary>
    /// 로비 군중(보행자·차량)을 스플라인 레인을 따라 이동시키는 조종부.
    /// 닫힌 스플라인은 계속 돌고, 열린 스플라인은 끝에 닿으면 멈추고 OnLaneEndReached 를 알린다.
    /// 열린 레인에서 지금 스플라인의 Knot 에 다른 스플라인의 0번 Knot 이 Link 돼 있으면 갈림길로 보고, 직진과 갈래 중 하나를 무작위로 고른다.
    /// AIController 와 같은 방식으로 이동·회전 요청만 중재자에게 보내며, 앞이 막히면 이동을 멈춘다.
    /// 이동 속도는 CharacterMediator 의 속도 어트리뷰트가 정한다. 레인이 꺼낼 때마다 ApplySpeed 로 개체별 순항 속도를 넣는다.
    /// 앞을 막은 것이 같은 방향으로 가는 더 느린 개체이면 점프 중에만 속도를 올려 넘어간다. 점프 금지 구간(CrowdNoJumpZone)에서는 멈춰 기다린다.
    /// 중재자가 Awake 에서 GetComponentInParent 로 찾으므로 프리팹 루트에 붙인다.
    /// </summary>
    public class CrowdController : MonoBehaviour, IMoveController, IBodyRotateController
    {
        #region Constants
        /// <summary>앞쪽 통로 검사 한 번에 받는 최대 충돌체 수. 자기 자신의 콜라이더가 섞여 들어오므로 넉넉히 둔다.</summary>
        private const int DETECTION_BUFFER_SIZE = 8;

        /// <summary>수평 방향 벡터의 제곱 크기가 이 값 이하이면 방향이 없는 것으로 본다.</summary>
        private const float MIN_DIRECTION_SQR_MAGNITUDE = 0.0001f;

        /// <summary>진행 방향을 구할 때 비교하는 앞쪽 점까지의 거리(m).</summary>
        private const float TANGENT_SAMPLE_DISTANCE = 0.1f;

        /// <summary>점프 요청 직후에는 모터가 아직 떠오르기 전이라 접지로 보이므로, 이 시간(초)이 지나야 착지로 본다.</summary>
        private const float JUMP_OVER_LANDING_GRACE = 0.2f;
        #endregion

        #region Serialized Fields
        [Header("Path Following")]
        [Tooltip("진행 거리에서 이만큼(m) 앞의 스플라인 점을 향해 이동한다.")]
        [SerializeField] private float _lookAheadDistance = 1.0f;
        [Tooltip("스폰마다 -값~+값(m) 안에서 옆 오프셋을 뽑아 반납될 때까지 스플라인과 평행하게 걷는다. 0 이면 스플라인 위로 걷는다. 보행자 0.3 / 차량 0 권장.")]
        [SerializeField] private float _lateralOffsetRange = 0.0f;

        [Header("Blocking Detection")]
        [Tooltip("진행 방향으로 이만큼(m) 앞까지 막힘을 검사한다. 보행자 1.5 / 차량 4 권장.")]
        [SerializeField] private float _detectDistance = 1.5f;
        [Tooltip("검사 구의 반지름(m). 몸 폭의 절반.")]
        [SerializeField] private float _detectRadius = 0.3f;
        [Tooltip("검사 구의 중심 높이(m). 몸통 중간 높이로 둔다.")]
        [SerializeField] private float _detectHeight = 0.9f;
        [Tooltip("멈춰야 하는 대상의 레이어. 보행자·차량 모두 Player, Crowd, VehicleBody.")]
        [SerializeField] private LayerMask _detectLayers;

        [Header("Speed")]
        [Tooltip("꺼낼 때마다 속도에 (1-값)~(1+값) 사이 무작위 배율을 곱한다. 보행자 0.2 / 차량 0 권장.")]
        [Range(0.0f, 0.9f)]
        [SerializeField] private float _speedVariation = 0.0f;

        [Header("Jump Over")]
        [Tooltip("같은 방향으로 가는 더 느린 개체를 점프로 넘어간다. 보행자 true / 차량 false.")]
        [SerializeField] private bool _canJumpOver = false;
        [Tooltip("점프하는 동안 순항 속도에 곱하는 배율. 체공 중에 앞 개체를 넘어갈 거리를 확보한다.")]
        [SerializeField] private float _jumpOverSpeedMultiplier = 3.0f;
        [Tooltip("점프력 어트리뷰트 이름. MoveMediator 의 점프력 키와 같아야 한다.")]
        [SerializeField] private string _jumpPowerAttributeKey = "JumpPower";
        #endregion

        #region Private Fields
        private readonly RaycastHit[] _hitBuffer = new RaycastHit[DETECTION_BUFFER_SIZE];

        private CharacterMediator _mediator;
        private CharacterController _characterController;
        private string _speedAttributeKey;
        private float _cruiseSpeed;
        private bool _isJumpingOver;
        private float _jumpOverStartTime;

        private SplineContainer _lane;
        private int _splineIndex;
        private float _laneLength; // 지금 타고 있는 스플라인의 길이
        private int _nextForkKnot;
        private float _nextForkDistance;
        private bool _isLaneClosed;
        private float _progress;
        private float _lateralOffset;
        private Vector3 _lastPosition;
        private bool _hasBaseSpeed;
        private float _baseSpeed;
        #endregion

        #region Properties
        /// <summary>멈춰야 하는 대상의 레이어. 레인이 시작점 막힘 검사에도 같은 값을 쓴다.</summary>
        public LayerMask DetectLayers => _detectLayers;

        /// <summary>옆 오프셋 범위(m). 레인이 스폰할 때 이 범위에서 개체의 오프셋을 뽑는다.</summary>
        public float LateralOffsetRange => _lateralOffsetRange;

        /// <summary>점프 중이 아닐 때의 이동 속도. 다른 개체가 추월할지 판단할 때 읽는다.</summary>
        public float CruiseSpeed => _cruiseSpeed;

        /// <summary>앞 개체를 넘어가는 점프 중인지 여부.</summary>
        public bool IsJumpingOver => _isJumpingOver;
        #endregion

        #region Events
        private event Action<Vector2> _onMoveRequested;
        private event Action _onJumpRequested;
        private event Action<Vector2> _onBodyRotateRequested;

        /// <summary>열린 레인의 끝에 닿았을 때 매 프레임 알린다. 레인이 받아서 개체를 회수한다.</summary>
        public event Action<CrowdController> OnLaneEndReached;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            _mediator = GetComponent<CharacterMediator>();
            _characterController = GetComponent<CharacterController>();
        }

        private void Update()
        {
            if (!_lane)
            {
                RequestMove(Vector2.zero);
                return;
            }

            AdvanceProgress();

            // 앞쪽 목표점이 갈림길을 넘기 전에 길을 골라야 직진 쪽으로 쏠리지 않고, 마지막 Knot 갈림길에서 끝 판정보다 먼저 갈래로 넘어간다
            if (HasReachedFork()) ChooseForkPath();

            if (HasReachedLaneEnd())
            {
                RequestMove(Vector2.zero);
                OnLaneEndReached?.Invoke(this);
                return;
            }

            Vector3 direction = GetDirectionToTarget();
            if (direction.sqrMagnitude <= MIN_DIRECTION_SQR_MAGNITUDE)
            {
                RequestMove(Vector2.zero);
                return;
            }

            direction.Normalize();

            // 점프로 넘어가는 중에는 앞 개체 위를 지나가므로 막힘 검사를 하지 않는다
            if (_isJumpingOver)
            {
                if (HasLandedFromJumpOver()) EndJumpOver();
            }
            else if (!TryPassAhead(direction))
            {
                // 막히면 이동 0 만 요청한다. 회전은 보내지 않아 직전 방향을 유지한다.
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

            // 점프 중 반납돼도 다음에 꺼낼 때 ApplySpeed 가 순항 속도를 다시 넣는다
            _isJumpingOver = false;
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
        /// <param name="lane">스플라인을 가진 레인. 닫혀 있으면 순환하고, 열려 있으면 끝에서 멈춘다.</param>
        /// <param name="startDistance">스플라인 시작점으로부터의 진행 거리(m)</param>
        /// <param name="lateralOffset">진행 방향 오른쪽(+) 기준 옆 오프셋(m). 반납될 때까지 유지한다.</param>
        public void SetLane(SplineContainer lane, float startDistance, float lateralOffset)
        {
            _lane = lane;
            _splineIndex = 0;
            _lateralOffset = lateralOffset;
            _laneLength = lane ? lane.CalculateLength() : 0.0f;

            if (_laneLength <= Mathf.Epsilon)
            {
                _lane = null;
                return;
            }

            _isLaneClosed = lane.Splines.Count > 0 && lane.Splines[0].Closed;
            _progress = WrapDistance(startDistance);
            _lastPosition = transform.position;
            FindNextFork(0);
        }

        /// <summary>
        /// 순항 속도를 기본 속도 × 레인 배율 × 개체별 무작위 배율로 정해 속도 어트리뷰트에 넣는다.
        /// 기본 속도는 처음 호출될 때의 어트리뷰트 값(프리팹 SO 값)을 기억해 쓴다. 공용 풀이라 다른 배율의 레인에서 쓰던 개체가 와도 배율이 누적되지 않는다.
        /// 점프로 넘어갈 수 있는 개체는 점프력 어트리뷰트도 다시 넣는다.
        /// </summary>
        /// <param name="speedKey">속도 어트리뷰트 이름</param>
        /// <param name="laneMultiplier">레인의 속도 배율</param>
        public void ApplySpeed(string speedKey, float laneMultiplier)
        {
            // 잘못된 키는 AttributeSet 이 Debug.Log 만 찍어 묻히므로 먼저 검사한다
            if (!_mediator || !_mediator.IsValidAttribute(speedKey))
            {
                Debug.LogWarning($"[{name}] 어트리뷰트 '{speedKey}' 가 개체에 없습니다. 속도가 적용되지 않습니다.", this);
                return;
            }

            if (!_hasBaseSpeed)
            {
                _baseSpeed = _mediator.GetAttribute(speedKey);
                _hasBaseSpeed = true;
            }

            float variation = UnityEngine.Random.Range(1.0f - _speedVariation, 1.0f + _speedVariation);
            _speedAttributeKey = speedKey;
            _cruiseSpeed = _baseSpeed * laneMultiplier * variation;

            // SO 초기값은 모터에 전달되지 않으므로(MediatorBase.InitValue 미호출) 값이 같아도 다시 넣어 전달한다
            _mediator.SetAttribute(_speedAttributeKey, _cruiseSpeed);

            if (!_canJumpOver) return;

            if (!_mediator.IsValidAttribute(_jumpPowerAttributeKey))
            {
                Debug.LogWarning($"[{name}] 어트리뷰트 '{_jumpPowerAttributeKey}' 가 개체에 없어 점프하지 못합니다.", this);
                return;
            }

            _mediator.SetAttribute(_jumpPowerAttributeKey, _mediator.GetAttribute(_jumpPowerAttributeKey));
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
        /// 멈춰 있으면 움직임이 0이라 진행 거리도 늘지 않는다. 길이를 넘으면 닫힌 레인은 0으로 감고, 열린 레인은 끝에 멈춘다.
        /// </summary>
        private void AdvanceProgress()
        {
            Vector3 position = transform.position;
            Vector3 moved = position - _lastPosition;
            _lastPosition = position;

            Vector3 tangent = GetHorizontalTangent(_progress);
            if (tangent.sqrMagnitude <= MIN_DIRECTION_SQR_MAGNITUDE) return;

            float advance = Mathf.Max(0.0f, Vector3.Dot(moved, tangent.normalized));
            _progress = WrapDistance(_progress + advance);
        }

        /// <summary>
        /// 열린 레인에서 남은 거리가 앞쪽 목표점 거리 이하이면 끝에 닿은 것으로 본다.
        /// </summary>
        /// <returns>열린 레인의 끝에 닿았으면 true</returns>
        private bool HasReachedLaneEnd()
        {
            return !_isLaneClosed && (_laneLength - _progress <= _lookAheadDistance);
        }

        /// <summary>
        /// 앞쪽 목표점이 다음 갈림길에 닿았는지 확인한다.
        /// </summary>
        /// <returns>갈림길에서 길을 골라야 하면 true</returns>
        private bool HasReachedFork()
        {
            return (_nextForkKnot >= 0) && (_progress + _lookAheadDistance >= _nextForkDistance);
        }

        /// <summary>
        /// 지금 스플라인에서 지정한 Knot 다음부터 갈림길 Knot 을 찾아 그 Knot 과 거리를 저장한다. 없거나 닫힌 레인이면 -1 로 둔다.
        /// </summary>
        /// <param name="afterKnot">이 Knot 다음부터 찾는다</param>
        private void FindNextFork(int afterKnot)
        {
            _nextForkKnot = -1;
            if (_isLaneClosed) return;

            Spline spline = _lane.Splines[_splineIndex];
            float distance = 0.0f;

            for (int knot = 1; knot < spline.Count; knot++)
            {
                distance += spline.GetCurveLength(knot - 1);
                if (knot <= afterKnot) continue;
                if (CountBranches(knot) == 0) continue;

                _nextForkKnot = knot;
                _nextForkDistance = distance;
                return;
            }
        }

        /// <summary>
        /// 지금 스플라인의 Knot 에서 시작하는 갈래(다른 스플라인의 0번 Knot 이 Link 된 것) 수를 센다.
        /// 갈래 끝이 Link 된 것은 합류이므로 세지 않는다.
        /// </summary>
        /// <param name="knot">지금 스플라인의 Knot 인덱스</param>
        /// <returns>갈래 수</returns>
        private int CountBranches(int knot)
        {
            SplineKnotIndex knotIndex = new SplineKnotIndex(_splineIndex, knot);
            if (!_lane.KnotLinkCollection.TryGetKnotLinks(knotIndex, out var links)) return 0;

            int count = 0;
            for (int i = 0; i < links.Count; i++)
            {
                if (IsBranchStart(links[i])) count++;
            }

            return count;
        }

        /// <summary>
        /// 지금 스플라인의 Knot 에서 시작하는 갈래 중 order 번째 갈래의 스플라인 인덱스를 찾는다.
        /// </summary>
        /// <param name="knot">지금 스플라인의 Knot 인덱스</param>
        /// <param name="order">갈래 순번 (0 부터)</param>
        /// <returns>갈래의 스플라인 인덱스. 못 찾으면 -1.</returns>
        private int GetBranchSpline(int knot, int order)
        {
            SplineKnotIndex knotIndex = new SplineKnotIndex(_splineIndex, knot);
            if (!_lane.KnotLinkCollection.TryGetKnotLinks(knotIndex, out var links)) return -1;

            int count = 0;
            for (int i = 0; i < links.Count; i++)
            {
                if (!IsBranchStart(links[i])) continue;
                if (count == order) return links[i].Spline;

                count++;
            }

            return -1;
        }

        /// <summary>
        /// Link 된 Knot 이 지금 스플라인이 아닌 다른 스플라인의 시작점(0번 Knot)인지 확인한다. 자기 자신도 Link 목록에 들어 있다.
        /// </summary>
        /// <param name="link">Link 된 Knot</param>
        /// <returns>갈래의 시작점이면 true</returns>
        private bool IsBranchStart(SplineKnotIndex link)
        {
            return (link.Spline != _splineIndex) &&
                   (link.Knot == 0) &&
                   (_lane.Splines[link.Spline].Count >= 2);
        }

        /// <summary>
        /// 갈림길에서 직진과 갈래 중 하나를 균등하게 고른다. 마지막 Knot 갈림길이면 갈래 중에서만 고른다.
        /// </summary>
        private void ChooseForkPath()
        {
            int forkKnot = _nextForkKnot;
            int branchCount = CountBranches(forkKnot);
            bool canGoStraight = forkKnot < _lane.Splines[_splineIndex].Count - 1;

            int choice = UnityEngine.Random.Range(0, branchCount + (canGoStraight ? 1 : 0));
            if (choice >= branchCount)
            {
                FindNextFork(forkKnot);
                return;
            }

            EnterSpline(GetBranchSpline(forkKnot, choice));
        }

        /// <summary>
        /// 갈래 스플라인으로 갈아타고 시작점부터 진행 거리를 다시 센다.
        /// </summary>
        /// <param name="splineIndex">갈아탈 스플라인 인덱스</param>
        private void EnterSpline(int splineIndex)
        {
            _splineIndex = splineIndex;
            _laneLength = _lane.CalculateLength(splineIndex);
            _progress = 0.0f;
            FindNextFork(0);
        }

        /// <summary>
        /// 진행 거리를 레인 범위 안으로 맞춘다. 닫힌 레인은 길이로 감고, 열린 레인은 0~길이로 자른다.
        /// </summary>
        /// <param name="distance">진행 거리(m)</param>
        /// <returns>레인 범위 안의 진행 거리(m)</returns>
        private float WrapDistance(float distance)
        {
            return _isLaneClosed
                ? Mathf.Repeat(distance, _laneLength)
                : Mathf.Clamp(distance, 0.0f, _laneLength);
        }

        /// <summary>
        /// 현재 위치에서 진행 거리 앞쪽 목표점까지의 수평 방향(정규화되지 않음)을 구한다.
        /// 목표점은 옆 오프셋만큼 진행 방향 오른쪽으로 옮긴 점이다.
        /// </summary>
        /// <returns>수평 방향 벡터</returns>
        private Vector3 GetDirectionToTarget()
        {
            float targetDistance = WrapDistance(_progress + _lookAheadDistance);
            Vector3 target = _lane.EvaluatePosition(_splineIndex, DistanceToNormalized(targetDistance));

            // 스플라인 위의 점 대신 옆으로 옮긴 점을 따라가야 가운데로 모이지 않고 평행하게 걷는다
            Vector3 right = Vector3.Cross(Vector3.up, GetHorizontalTangent(targetDistance).normalized);
            target += right * _lateralOffset;

            return Vector3.ProjectOnPlane(target - transform.position, Vector3.up);
        }

        /// <summary>
        /// 진행 거리 지점의 스플라인 접선을 수평면에 투영해 반환한다.
        /// </summary>
        /// <param name="distance">진행 거리(m)</param>
        /// <returns>수평 접선 벡터(정규화되지 않음)</returns>
        private Vector3 GetHorizontalTangent(float distance)
        {
            // Linear Knot 은 그 지점의 접선이 0 이라 EvaluateTangent 대신 가까운 두 점의 위치 차이로 구한다
            Vector3 current = _lane.EvaluatePosition(_splineIndex, DistanceToNormalized(distance));
            Vector3 ahead = _lane.EvaluatePosition(
                _splineIndex,
                DistanceToNormalized(WrapDistance(distance + TANGENT_SAMPLE_DISTANCE)));

            return Vector3.ProjectOnPlane(ahead - current, Vector3.up);
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
        /// 진행 방향 앞쪽을 검사해 지나갈 수 있는지 판단한다. 자기 자신의 콜라이더는 제외한다.
        /// 앞의 대상이 모두 점프로 넘을 수 있는 개체이면 점프를 시작하고 지나간다. 하나라도 넘을 수 없으면 막힌다.
        /// </summary>
        /// <param name="direction">정규화된 수평 진행 방향</param>
        /// <returns>계속 이동해도 되면 true, 멈춰야 하면 false</returns>
        private bool TryPassAhead(Vector3 direction)
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

            bool hasJumpTarget = false;
            for (int i = 0; i < hitCount; i++)
            {
                Collider hitCollider = _hitBuffer[i].collider;
                if (hitCollider.transform.IsChildOf(transform)) continue;
                if (!CanJumpOver(hitCollider, direction)) return false;

                hasJumpTarget = true;
            }

            if (hasJumpTarget) StartJumpOver();

            return true;
        }

        /// <summary>
        /// 앞을 막은 콜라이더를 점프로 넘어갈 수 있는지 확인한다.
        /// 같은 방향으로 가는 더 느린 개체만 넘고, 플레이어·마주 오는 개체·이미 점프 중인 개체 앞이나 점프 금지 구간에서는 넘지 않는다.
        /// </summary>
        /// <param name="blocker">앞을 막은 콜라이더</param>
        /// <param name="direction">정규화된 수평 진행 방향</param>
        /// <returns>넘어갈 수 있으면 true</returns>
        private bool CanJumpOver(Collider blocker, Vector3 direction)
        {
            if (!_canJumpOver || !_characterController || !_characterController.isGrounded) return false;

            CrowdController other = blocker.GetComponentInParent<CrowdController>();
            if (!other || other.IsJumpingOver) return false;

            // 더 느린 개체만 넘어야 착지한 뒤 넘겨진 개체가 다시 넘어오는 왕복이 생기지 않는다
            bool isSameDirection = Vector3.Dot(other.transform.forward, direction) > 0.0f;
            bool isSlower = other.CruiseSpeed < _cruiseSpeed;
            if (!isSameDirection || !isSlower) return false;

            return !CrowdNoJumpZone.Contains(transform.position) &&
                   !CrowdNoJumpZone.Contains(other.transform.position);
        }

        /// <summary>
        /// 점프 중에만 속도를 올리고 점프를 요청한다. 접지 상태에서 같은 프레임에 요청하므로 올린 수평 속도가 바로 반영된다.
        /// </summary>
        private void StartJumpOver()
        {
            _isJumpingOver = true;
            _jumpOverStartTime = Time.time;

            _mediator.SetAttribute(_speedAttributeKey, _cruiseSpeed * _jumpOverSpeedMultiplier);
            _onJumpRequested?.Invoke();
        }

        /// <summary>
        /// 점프를 시작하고 유예 시간이 지난 뒤 다시 지면에 닿았는지 확인한다.
        /// </summary>
        /// <returns>착지했으면 true</returns>
        private bool HasLandedFromJumpOver()
        {
            bool isGraceOver = Time.time - _jumpOverStartTime >= JUMP_OVER_LANDING_GRACE;
            return isGraceOver && _characterController.isGrounded;
        }

        /// <summary>
        /// 점프를 끝내고 속도를 순항 속도로 되돌린다.
        /// </summary>
        private void EndJumpOver()
        {
            _isJumpingOver = false;
            _mediator.SetAttribute(_speedAttributeKey, _cruiseSpeed);
        }
        #endregion
    }
}
