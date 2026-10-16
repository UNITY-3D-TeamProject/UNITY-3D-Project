using System;
using UnityEngine;

namespace CharacterAnimation
{
    /// <summary>
    /// 캐릭터 Animator 파라미터를 설정하는 컴포넌트.
    /// Animator는 캐릭터 루트에 두고, 이 스크립트는 자식에 있어도 된다.
    /// </summary>
    public class PlayerAnimator : MonoBehaviour
    {
        #region Constants
        private const string UPPER_BODY_LAYER_NAME = "UpperBody";
        // 이동 방향이 정확히 옆(±90°)일 때 계산 오차로 뒤쪽으로 판정되지 않도록 앞쪽 반원을 이 각도(도)만큼 넓힌다
        private const float BOARD_SIDE_TOLERANCE = 1.0f;
        // 피격 방향 판정: 몸 정면과 이 각도(도) 이내면 앞, (180 - 이 각도) 밖이면 뒤, 나머지는 좌/우
        private const float HIT_FRONT_HALF_ANGLE = 45.0f;
        // Animator HitDirection 값
        private const int HIT_DIRECTION_FRONT = 0;
        private const int HIT_DIRECTION_BACK = 1;
        private const int HIT_DIRECTION_LEFT = 2;
        private const int HIT_DIRECTION_RIGHT = 3;
        #endregion

        #region Serialized Fields
        [Header("Locomotion")]
        [Tooltip("이동 방향 파라미터(MoveX, MoveY)가 목표값에 도달하는 데 걸리는 시간(초).")]
        [SerializeField] private float _moveDirectionDampTime = 0.1f;
        [Tooltip("이동 애니메이션이 1배속으로 재생될 때의 이동속도. 현재 이동속도 / 이 값이 Locomotion 재생 배율이 된다.")]
        [SerializeField, Min(0.01f)] private float _referenceMoveSpeed = 5.0f;

        [Header("Fire")]
        [Tooltip("마지막 사격 후 사격 상체 애니메이션을 유지하는 시간(초).")]
        [SerializeField] private float _fireHoldDuration = 3.0f;

        [Header("Aim")]
        [Tooltip("사격 상체일 때 하체 이동으로 돌아간 골반 yaw 만큼 반대로 돌려 상체를 정면으로 맞출 뼈 이름. 이 뼈의 부모를 골반으로 본다.")]
        [SerializeField] private string _twistBoneName = "spine_01";
        [Tooltip("사격 상체일 때 총신이 조준점을 향하도록 돌릴 뼈 이름.")]
        [SerializeField] private string _aimBoneName = "upperarm_r";
        [Tooltip("총구 오브젝트 이름. 총신 방향은 총구의 부모 → 총구 방향으로 본다.")]
        [SerializeField] private string _muzzleName = "FirePosition";
        [Tooltip("조준 회전이 켜지고 꺼지는 데 걸리는 시간(초). UpperBody 레이어 전환 시간과 맞춘다.")]
        [SerializeField] private float _aimBlendTime = 0.15f;
        [Tooltip("조준 뼈에서 조준점까지 이 거리(m)보다 가까우면 조준 보정을 끈다. 벽에 붙어 사격할 때 팔이 꺾이는 것을 막는다.")]
        [SerializeField, Min(0.0f)] private float _minAimDistance = 1.0f;
        [Tooltip("최소 거리부터 이 거리(m)만큼 멀어지는 구간에서 조준 보정을 서서히 켠다.")]
        [SerializeField, Min(0.0f)] private float _aimFadeDistance = 0.5f;

        [Header("Special Idle")]
        [Tooltip("Idle 상태에서 입력 없이 이 시간(초)이 지나면 특수 Idle 애니메이션을 재생한다.")]
        [SerializeField, Min(0.0f)] private float _specialIdleDelay = 10.0f;
        [Tooltip("Animator 의 특수 Idle 상태 개수. SpecialIdleIndex 를 0 ~ (개수 - 1) 중 랜덤으로 고른다.")]
        [SerializeField, Min(1)] private int _specialIdleCount = 2;

        [Header("Riding")]
        [Tooltip("탑승 자세(Riding)일 때 보드 방향 기준으로 하체(골반)를 돌릴 yaw 각도(도). 음수면 왼쪽. 상체는 정면을 유지한다.")]
        [SerializeField] private float _ridingLowerBodyYaw = -90.0f;
        [Tooltip("탑승 하체 회전이 켜지고 꺼지는 데 걸리는 시간(초).")]
        [SerializeField, Min(0.0f)] private float _ridingTurnBlendTime = 0.15f;
        [Tooltip("탑승 중 보드(와 하체)가 목표 방향으로 도는 속도(도/초).")]
        [SerializeField, Min(0.0f)] private float _boardTurnSpeed = 720.0f;
        [Tooltip("탈것 위에 서 있는 동안 몸(_bodyName)을 올릴 높이. 탈것이 생성될 때 올리고 제거될 때 되돌린다.")]
        [SerializeField] private float _ridingBodyHeight = 0.3f;
        [Tooltip("몸 정면을 읽을 Transform 이름. CharacterRotator 가 돌리는 대상이다.")]
        [SerializeField] private string _bodyName = "SK_ZMike";

        [Header("Death")]
        [Tooltip("사망 루프(DeathLoop) 상태를 이 횟수만큼 재생하면 사망 애니메이션 종료를 알린다.")]
        [SerializeField, Min(0.0f)] private float _deathLoopCount = 2.0f;
        #endregion

        #region Private Fields
        private Animator _animator;
        private Vector3 _moveDirection;
        private Vector3 _viewForward = Vector3.forward;
        private Func<Vector3> _getAimPoint;
        private Transform _twistBone;
        private Transform _aimBone;
        private Transform _muzzle;
        // Idle 자세의 골반 로컬 회전. 이동 애니메이션이 골반을 얼마나 돌렸는지 비교하는 기준이다
        private bool _hasPelvisReference;
        private Quaternion _pelvisReferenceLocal;
        private int _upperBodyLayer = -1;
        // FireHold 자세의 총구 위치·총신 방향 (조준 뼈 부모 기준 로컬). 반동과 무관한 고정 보정의 기준이다
        private bool _hasHoldReference;
        private Vector3 _holdMuzzleLocal;
        private Vector3 _holdBarrelLocal;
        private bool _isFiring;
        private bool _isCharging;
        private bool _isAiming;
        private float _lastFireTime;
        private float _aimWeight;
        private bool _isRolling;
        private bool _isRiding;
        private float _ridingTurnWeight;
        private Vector3 _rollDirection;
        // 탑승 중 보드가 바라보는 월드 yaw. 하체도 이 방향을 기준으로 돌린다
        private float _boardYaw;
        private Transform _ridingBoard;
        private Transform _body;
        // 몸의 기준 자세(yaw 제외)의 역회전. 몸 localRotation 에서 yaw 만 뽑을 때 쓴다
        private Quaternion _bodyRestInverse = Quaternion.identity;
        // 몸의 기준 로컬 위치. 탑승 중 높이를 더하고 하차 시 되돌릴 때 쓴다
        private Vector3 _bodyRestPosition;
        private float _idleTime;
        private Vector3 _lastViewForward;
        private bool _isDead;
        private bool _hasDeathEnded;
        #endregion

        #region Events
        /// <summary>사망 애니메이션(사망 루프 정해진 횟수 재생)이 끝났을 때 한 번 발생한다.</summary>
        public event Action OnDeathAnimationEnded;
        /// <summary>특수 Idle 재생을 시작할 때 재생할 특수 Idle 인덱스(SpecialIdleIndex)를 알린다.</summary>
        public event Action<int> OnSpecialIdleStarted;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            // GetComponentInParent는 자기 자신을 먼저 검사하므로 루트/자식 배치를 모두 커버한다
            _animator = GetComponentInParent<Animator>();

            if (_animator == null)
            {
                Debug.LogError($"[{name}] Animator를 찾지 못했습니다. 캐릭터 루트에 추가하세요.", this);
                enabled = false;
                return;
            }

            CacheAimTransforms();
            CacheBody();
            _upperBodyLayer = _animator.GetLayerIndex(UPPER_BODY_LAYER_NAME);
        }

        private void Update()
        {
            // 몸은 시점 방향을 바라보므로 시점 기준 로컬 방향을 매 프레임 다시 계산한다
            Vector3 localDirection = Quaternion.Inverse(Quaternion.LookRotation(_viewForward)) * _moveDirection;
            float deltaTime = Time.deltaTime;

            _animator.SetFloat(AnimHash.MoveX, localDirection.x, _moveDirectionDampTime, deltaTime);
            _animator.SetFloat(AnimHash.MoveY, localDirection.z, _moveDirectionDampTime, deltaTime);

            // 차지 중·조준 중에는 사격 자세를 계속 유지한다
            if (_isFiring && !_isCharging && !_isAiming && (Time.time - _lastFireTime >= _fireHoldDuration))
            {
                _isFiring = false;
                _animator.SetBool(AnimHash.IsFiring, false);
            }

            // 구르는 동안에는 몸이 시점과 다른 방향을 보므로 조준 회전을 끈다
            float targetWeight = (_isFiring && !_isRolling) ? 1.0f : 0.0f;
            float blendSpeed = (_aimBlendTime > 0.0f) ? (deltaTime / _aimBlendTime) : 1.0f;
            _aimWeight = Mathf.MoveTowards(_aimWeight, targetWeight, blendSpeed);

            UpdateRidingTurnWeight(deltaTime);
            UpdateBoardYaw(deltaTime);
            UpdateSpecialIdle(deltaTime);
            UpdateDeath();
        }

        private void LateUpdate()
        {
            // Animator 가 포즈를 적용한 뒤 상체를 정면으로 맞추고, 총신이 조준점을 향하도록 조준 뼈를 돌린다
            CapturePelvisReference();
            ApplyUpperBodyTwist();
            ApplyRidingTurn();
            ApplyBoardYaw();

            if (!_aimBone || !_muzzle || !_aimBone.parent) return;

            Transform reference = _aimBone.parent;
            CaptureHoldReference(reference);

            if ((_aimWeight <= 0.0f) || (_getAimPoint == null)) return;

            // 기준 자세(FireHold)로 보정값을 구해 현재 포즈에 더한다 — 재생 중 반동은 그대로 남는다.
            // 기준 자세를 아직 본 적이 없으면 현재 포즈로 구한다
            Vector3 muzzlePosition = _hasHoldReference ? reference.TransformPoint(_holdMuzzleLocal) : _muzzle.position;
            Vector3 barrelDirection = _hasHoldReference ? reference.TransformDirection(_holdBarrelLocal) : GetBarrelDirection();

            Vector3 aimPoint = _getAimPoint.Invoke();

            // 조준점이 너무 가까우면(벽에 붙은 상태) 보정을 줄여 애니메이션 자세로 돌린다
            float distanceWeight = CalculateAimDistanceWeight(_aimBone.position, aimPoint);
            if (distanceWeight <= 0.0f) return;

            bool hasCorrection = TryCalculateAimCorrection(
                _aimBone.position,
                muzzlePosition,
                barrelDirection,
                aimPoint,
                out Quaternion correction);
            if (!hasCorrection) return;

            _aimBone.rotation = Quaternion.Slerp(Quaternion.identity, correction, _aimWeight * distanceWeight) * _aimBone.rotation;
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// 이동 중 여부를 설정한다. true 면 Run, false 면 Idle 로 전환된다.
        /// </summary>
        /// <param name="isMoving">이동 중이면 true</param>
        public void SetMoving(bool isMoving)
        {
            if (!enabled || _isDead) return;

            if (isMoving) ResetSpecialIdle();
            _animator.SetBool(AnimHash.IsMoving, isMoving);
        }

        /// <summary>
        /// 이동 방향을 설정한다. 시점 기준 로컬 방향으로 바꿔 MoveX, MoveY 에 매 프레임 반영된다.
        /// </summary>
        /// <param name="worldDirection">월드 기준 이동 방향 (수평 성분만 사용)</param>
        public void SetMoveDirection(Vector3 worldDirection)
        {
            _moveDirection = Vector3.ProjectOnPlane(worldDirection, Vector3.up);
        }

        /// <summary>
        /// 현재 이동속도를 설정한다. 기준 속도 대비 비율을 MoveAnimSpeed 에 넣어 Locomotion 재생 속도를 맞춘다.
        /// </summary>
        /// <param name="moveSpeed">현재 이동속도</param>
        public void SetMoveSpeed(float moveSpeed)
        {
            if (!enabled) return;

            _animator.SetFloat(AnimHash.MoveAnimSpeed, moveSpeed / _referenceMoveSpeed);
        }

        /// <summary>
        /// 몸이 바라보는 기준인 시점 방향을 설정한다. 수평 성분이 없으면 직전 값을 유지한다.
        /// 직전 값과 달라졌으면(카메라 회전) 특수 Idle 을 해제한다.
        /// </summary>
        /// <param name="viewForward">카메라가 보는 방향 (월드 기준)</param>
        public void SetViewForward(Vector3 viewForward)
        {
            // pitch 만 바뀌어도 카메라 회전으로 본다
            if (enabled && (viewForward != _lastViewForward)) ResetSpecialIdle();
            _lastViewForward = viewForward;

            Vector3 flatForward = Vector3.ProjectOnPlane(viewForward, Vector3.up);
            if (flatForward.sqrMagnitude <= Mathf.Epsilon) return;

            _viewForward = flatForward.normalized;
        }

        /// <summary>
        /// 조준점(월드 위치)을 얻기 위한 델리게이트 주입. 사격 상체일 때 총신이 조준점을 향하도록 조준 뼈를 돌린다.
        /// </summary>
        /// <param name="getAimPoint">조준점을 반환하는 함수</param>
        public void SetGetAimPoint(Func<Vector3> getAimPoint)
        {
            if (getAimPoint == null) return;
            _getAimPoint = getAimPoint;
        }

        /// <summary>
        /// 조준점 델리게이트 제거. 이후에는 조준 보정을 하지 않는다.
        /// </summary>
        public void ClearGetAimPoint()
        {
            _getAimPoint = null;
        }

        /// <summary>
        /// 사격을 알린다. 사격 상체 애니메이션을 처음부터 1회 재생하고, 마지막 사격 후 일정 시간 동안 사격 자세를 유지한다.
        /// </summary>
        public void PlayFire()
        {
            if (!enabled || _isDead) return;

            ResetSpecialIdle();
            _isFiring = true;
            _lastFireTime = Time.time;
            _animator.SetBool(AnimHash.IsFiring, true);
            _animator.SetTrigger(AnimHash.Fire);
        }

        /// <summary>
        /// 사격 차지 상태를 설정한다. 차지 중에는 Fire 재생 없이 사격 자세(FireHold)를 유지하고,
        /// 차지가 끝나면 그 시점부터 사격 자세 유지 시간이 흐른다.
        /// </summary>
        /// <param name="isCharging">차지 중이면 true</param>
        public void SetCharging(bool isCharging)
        {
            if (!enabled || _isDead) return;

            _isCharging = isCharging;
            _lastFireTime = Time.time;
            if (!isCharging) return;

            ResetSpecialIdle();
            _isFiring = true;
            _animator.SetBool(AnimHash.IsFiring, true);
        }

        /// <summary>
        /// 조준 상태를 설정한다. 조준 중에는 Fire 재생 없이 사격 자세(FireHold)를 유지한다.
        /// 조준이 끝나면 바로 사격 자세를 내리되, 마지막 사격·차지 후 유지 시간이 남아 있으면 그때까지 유지한다.
        /// </summary>
        /// <param name="isAiming">조준 중이면 true</param>
        public void SetAiming(bool isAiming)
        {
            if (!enabled || _isDead) return;

            _isAiming = isAiming;
            if (!isAiming) return;

            ResetSpecialIdle();
            _isFiring = true;
            _animator.SetBool(AnimHash.IsFiring, true);
        }

        /// <summary>
        /// 점프 시작을 알린다. 점프 시작 → 상승 → 공중 순서로 재생된다. 공중에서 다시 점프하면 처음부터 재생한다.
        /// 탑승 중에는 탑승 자세를 유지하고 공중 여부만 기록한다 (공중에서 하차하면 공중 애니메이션으로 간다).
        /// </summary>
        public void PlayJump()
        {
            if (!enabled || _isDead) return;

            ResetSpecialIdle();
            _animator.SetBool(AnimHash.IsJumping, true);
            if (_isRiding) return;

            _animator.SetTrigger(AnimHash.Jump);
        }

        /// <summary>
        /// 착지를 알린다. 어느 점프 단계에 있든 바로 착지 애니메이션으로 전환된다.
        /// </summary>
        public void PlayLand()
        {
            if (!enabled) return;

            _animator.SetBool(AnimHash.IsJumping, false);
            // 착지 직전 프레임에 들어온 점프 Trigger 가 남아 착지 후 다시 점프 애니메이션이 재생되지 않게 한다
            _animator.ResetTrigger(AnimHash.Jump);
        }

        /// <summary>
        /// 구르기 시작을 알린다. 구르는 동안 구르기 애니메이션을 재생한다. 탑승 중에는 탑승 자세를 유지한다.
        /// </summary>
        public void PlayRoll()
        {
            if (!enabled || _isDead) return;

            ResetSpecialIdle();
            _isRolling = true;
            _animator.SetBool(AnimHash.IsRolling, true);
            if (_isRiding) return;

            _animator.SetTrigger(AnimHash.Roll);
        }

        /// <summary>
        /// 구르기 시작을 구르기 방향과 함께 알린다. 탑승 중이면 구르는 동안 보드가 구르기 방향을 본다.
        /// </summary>
        /// <param name="direction">구르기 방향 (월드 기준)</param>
        public void PlayRoll(Vector3 direction)
        {
            _rollDirection = Vector3.ProjectOnPlane(direction, Vector3.up);
            PlayRoll();
        }

        /// <summary>
        /// 구르기 종료를 알린다. 이동 중이면 Locomotion, 아니면 Idle 로 돌아간다. 공중이면 공중 애니메이션으로 돌아간다.
        /// </summary>
        public void StopRoll()
        {
            if (!enabled) return;

            _isRolling = false;
            _animator.SetBool(AnimHash.IsRolling, false);
            _animator.ResetTrigger(AnimHash.Roll);
        }

        /// <summary>
        /// 탈것 탑승 상태를 설정한다. 탑승하면 탑승 점프 후 탑승 자세를 유지하고,
        /// 하차하면 공중이면 공중 애니메이션, 아니면 Idle/Locomotion 으로 돌아간다.
        /// </summary>
        /// <param name="isRiding">탑승 중이면 true</param>
        public void SetRiding(bool isRiding)
        {
            if (!enabled) return;

            _isRiding = isRiding;
            _animator.SetBool(AnimHash.IsRiding, isRiding);
            if (!isRiding || _isDead)
            {
                _animator.ResetTrigger(AnimHash.Mount);
                return;
            }

            ResetSpecialIdle();
            // 보드는 몸 정면에서 출발해 이동 방향으로 돈다
            _boardYaw = GetBodyYaw();
            // 탑승 직전에 들어온 점프·구르기 Trigger 가 탑승 점프를 덮지 않게 한다
            _animator.ResetTrigger(AnimHash.Jump);
            _animator.ResetTrigger(AnimHash.Roll);
            _animator.SetTrigger(AnimHash.Mount);
        }

        /// <summary>
        /// 탑승 중 이동 방향으로 돌릴 보드(탈것)를 설정한다. 위치는 그대로 두고 회전만 덮어쓴다.
        /// 보드가 있으면 몸을 탈것 위 높이로 올리고, 없으면 원래 높이로 되돌린다.
        /// </summary>
        /// <param name="board">탈것 Transform. 없으면 null</param>
        public void SetRidingBoard(Transform board)
        {
            _ridingBoard = board;
            if (_body) _body.localPosition = _bodyRestPosition + (board ? Vector3.up * _ridingBodyHeight : Vector3.zero);
        }

        /// <summary>
        /// 피격을 알린다. 기준 위치가 몸 정면 기준 어느 쪽(앞/뒤/좌/우)인지 판정해 상체 피격 애니메이션을 재생한다.
        /// </summary>
        /// <param name="hitSource">피격 방향 기준 월드 위치(맞은 지점 또는 공격자 위치). null 이면 앞쪽 피격</param>
        public void PlayHit(Vector3? hitSource)
        {
            if (!enabled || _isDead) return;

            ResetSpecialIdle();
            _animator.SetInteger(AnimHash.HitDirection, CalculateHitDirection(hitSource));
            _animator.SetTrigger(AnimHash.Hit);
        }

        /// <summary>
        /// 기준 위치가 몸 정면 기준 어느 쪽인지 판정해 피격 방향(HitDirection)을 반환한다.
        /// </summary>
        /// <param name="hitSource">피격 방향 기준 월드 위치(맞은 지점 또는 공격자 위치). null 이면 앞쪽 피격</param>
        /// <returns>0: 앞, 1: 뒤, 2: 왼쪽, 3: 오른쪽</returns>
        public int GetHitDirection(Vector3? hitSource)
        {
            return CalculateHitDirection(hitSource);
        }

        /// <summary>
        /// 사망을 알린다. 사격 자세를 내리고 사망 애니메이션(Death → DeathLoop 루프)을 재생한다.
        /// 이후에는 이동·사격·조준·점프·구르기 알림을 무시한다.
        /// </summary>
        public void PlayDeath()
        {
            if (!enabled || _isDead) return;

            _isDead = true;
            ResetSpecialIdle();

            // 사격 상체가 사망 모션을 덮지 않게 내린다. 조준 보정은 Update 에서 서서히 꺼진다
            _isFiring = false;
            _isCharging = false;
            _isAiming = false;
            _isRolling = false;
            _animator.SetBool(AnimHash.IsFiring, false);
            _animator.SetBool(AnimHash.IsRolling, false);
            _animator.SetBool(AnimHash.IsJumping, false);
            _animator.ResetTrigger(AnimHash.Fire);
            _animator.ResetTrigger(AnimHash.Jump);
            _animator.ResetTrigger(AnimHash.Roll);
            _animator.ResetTrigger(AnimHash.Mount);
            _animator.ResetTrigger(AnimHash.Hit);
            _animator.SetTrigger(AnimHash.Death);
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// 사망 루프 상태(전환 중 아님)가 정해진 횟수만큼 재생되면 사망 애니메이션 종료를 한 번 알린다.
        /// </summary>
        private void UpdateDeath()
        {
            if (!_isDead || _hasDeathEnded) return;
            if (_animator.IsInTransition(0)) return;

            AnimatorStateInfo stateInfo = _animator.GetCurrentAnimatorStateInfo(0);
            if (stateInfo.shortNameHash != AnimHash.DeathLoopState) return;
            if (stateInfo.normalizedTime < _deathLoopCount) return;

            _hasDeathEnded = true;
            OnDeathAnimationEnded?.Invoke();
        }

        /// <summary>
        /// 하체가 Idle(전환 중 아님)이고 사격 상체가 꺼져 있는 동안 무입력 시간을 센다.
        /// 대기 시간이 지나면 특수 Idle 중 하나를 랜덤으로 재생하고 시간을 다시 센다.
        /// </summary>
        /// <param name="deltaTime">이번 프레임 경과 시간</param>
        private void UpdateSpecialIdle(float deltaTime)
        {
            bool isIdle = !_animator.IsInTransition(0) &&
                (_animator.GetCurrentAnimatorStateInfo(0).shortNameHash == AnimHash.IdleState);
            if (!isIdle || _isFiring) return;

            _idleTime += deltaTime;
            if (_idleTime < _specialIdleDelay) return;

            _idleTime = 0.0f;
            int specialIdleIndex = UnityEngine.Random.Range(0, _specialIdleCount);
            _animator.SetInteger(AnimHash.SpecialIdleIndex, specialIdleIndex);
            _animator.SetBool(AnimHash.IsSpecialIdle, true);
            _animator.SetTrigger(AnimHash.SpecialIdle);
            OnSpecialIdleStarted?.Invoke(specialIdleIndex);
        }

        /// <summary>
        /// 입력이 들어왔을 때 무입력 시간을 초기화하고, 재생 중인 특수 Idle 을 끊는다.
        /// </summary>
        private void ResetSpecialIdle()
        {
            _idleTime = 0.0f;
            // 초기 시점 방향 전달이 Awake 보다 먼저 올 수 있다
            if (!_animator) return;

            _animator.SetBool(AnimHash.IsSpecialIdle, false);
            _animator.ResetTrigger(AnimHash.SpecialIdle);
        }

        /// <summary>
        /// Animator 하위에서 조준 뼈와 총구를 이름으로 찾아 캐싱한다.
        /// </summary>
        private void CacheAimTransforms()
        {
            Transform[] children = _animator.GetComponentsInChildren<Transform>(true);

            _twistBone = Array.Find(children, child => child.name == _twistBoneName);
            _aimBone = Array.Find(children, child => child.name == _aimBoneName);
            _muzzle = Array.Find(children, child => child.name == _muzzleName);

            if (!_twistBone) Debug.LogWarning($"[{name}] 상체 비틀림 뼈 '{_twistBoneName}'을(를) 찾지 못했습니다.", this);
            if (!_aimBone) Debug.LogWarning($"[{name}] 조준 뼈 '{_aimBoneName}'을(를) 찾지 못했습니다.", this);
            if (!_muzzle) Debug.LogWarning($"[{name}] 총구 '{_muzzleName}'을(를) 찾지 못했습니다.", this);
        }

        /// <summary>
        /// Animator 하위에서 몸(CharacterRotator 회전 대상)을 이름으로 찾아 캐싱하고, yaw 를 뺀 기준 자세를 저장한다.
        /// CharacterRotator 와 같은 방식(현재 localRotation 의 x, z)으로 기준 자세를 잡는다.
        /// </summary>
        private void CacheBody()
        {
            Transform[] children = _animator.GetComponentsInChildren<Transform>(true);
            _body = Array.Find(children, child => child.name == _bodyName);
            if (!_body)
            {
                Debug.LogWarning($"[{name}] 몸 '{_bodyName}'을(를) 찾지 못했습니다. 탑승 하체 방향은 시점 기준으로 계산합니다.", this);
                return;
            }

            Vector3 restEuler = _body.localRotation.eulerAngles;
            _bodyRestInverse = Quaternion.Inverse(Quaternion.Euler(restEuler.x, 0.0f, restEuler.z));
            _bodyRestPosition = _body.localPosition;
        }

        /// <summary>
        /// 하체가 Idle(전환 중 아님)이면 골반 로컬 회전을 기준으로 저장한다.
        /// Animator 가 만든 포즈에서, 상체 보정을 적용하기 전에 호출해야 한다.
        /// </summary>
        private void CapturePelvisReference()
        {
            if (!_twistBone || !_twistBone.parent) return;
            if (_animator.IsInTransition(0)) return;
            if (_animator.GetCurrentAnimatorStateInfo(0).shortNameHash != AnimHash.IdleState) return;

            _pelvisReferenceLocal = _twistBone.parent.localRotation;
            _hasPelvisReference = true;
        }

        /// <summary>
        /// 사격 상체일 때, 이동 애니메이션이 골반을 기준 자세보다 돌린 yaw 만큼 비틀림 뼈를 반대로 돌린다.
        /// 골반의 기울기·출렁임은 그대로 두고 수평 회전만 되돌린다.
        /// </summary>
        private void ApplyUpperBodyTwist()
        {
            if ((_aimWeight <= 0.0f) || !_hasPelvisReference || !_twistBone || !_twistBone.parent) return;

            Transform pelvis = _twistBone.parent;
            Quaternion pelvisParentRotation = pelvis.parent ? pelvis.parent.rotation : Quaternion.identity;

            // 기준 자세 대비 골반 회전을 월드 공간으로 옮긴 뒤 수평 성분(yaw)만 뽑는다
            Quaternion localDelta = pelvis.localRotation * Quaternion.Inverse(_pelvisReferenceLocal);
            Quaternion worldDelta = pelvisParentRotation * localDelta * Quaternion.Inverse(pelvisParentRotation);
            Vector3 turnedForward = Vector3.ProjectOnPlane(worldDelta * Vector3.forward, Vector3.up);
            if (turnedForward.sqrMagnitude <= Mathf.Epsilon) return;

            float pelvisYaw = Vector3.SignedAngle(Vector3.forward, turnedForward, Vector3.up);
            _twistBone.rotation = Quaternion.AngleAxis(-pelvisYaw * _aimWeight, Vector3.up) * _twistBone.rotation;
        }

        /// <summary>
        /// 탑승 중이고 하체가 탑승 자세(Riding)로 들어가는 중이거나 들어가 있으면 탑승 하체 회전과 탑승 상체(IsRidingPose)를 켜고, 아니면 끈다.
        /// </summary>
        /// <param name="deltaTime">이번 프레임 경과 시간</param>
        private void UpdateRidingTurnWeight(float deltaTime)
        {
            bool isRidingState = _animator.IsInTransition(0)
                ? (_animator.GetNextAnimatorStateInfo(0).shortNameHash == AnimHash.RidingState)
                : (_animator.GetCurrentAnimatorStateInfo(0).shortNameHash == AnimHash.RidingState);
            bool isRidingPose = _isRiding && !_isDead && isRidingState;
            _animator.SetBool(AnimHash.IsRidingPose, isRidingPose);

            float targetWeight = isRidingPose ? 1.0f : 0.0f;
            float blendSpeed = (_ridingTurnBlendTime > 0.0f) ? (deltaTime / _ridingTurnBlendTime) : 1.0f;
            _ridingTurnWeight = Mathf.MoveTowards(_ridingTurnWeight, targetWeight, blendSpeed);
        }

        /// <summary>
        /// 탑승 중 보드 방향(월드 yaw)을 목표 방향으로 회전 속도에 맞춰 돌린다.
        /// 구르는 중이면 구르기 방향, 이동 입력이 있으면 이동 방향(뒤쪽이면 반대 방향), 입력이 없으면 그대로 둔다.
        /// </summary>
        /// <param name="deltaTime">이번 프레임 경과 시간</param>
        private void UpdateBoardYaw(float deltaTime)
        {
            if (!_isRiding || _isDead) return;

            float targetYaw;
            if (_isRolling && (_rollDirection.sqrMagnitude > Mathf.Epsilon))
            {
                targetYaw = GetYaw(_rollDirection);
            }
            else if (_moveDirection.sqrMagnitude > Mathf.Epsilon)
            {
                targetYaw = CalculateBoardTargetYaw(GetYaw(_moveDirection));
            }
            else
            {
                return;
            }

            _boardYaw = Mathf.MoveTowardsAngle(_boardYaw, targetYaw, _boardTurnSpeed * deltaTime);
        }

        /// <summary>
        /// 이동 방향이 시점 앞쪽 반원이면 이동 방향을, 뒤쪽 반원이면 반대 방향을 보드 목표 방향으로 고른다.
        /// 뒤로 갈 때는 보드가 앞을 본 채 미끄러진다. 정확히 옆이면 이동 방향을 본다.
        /// </summary>
        /// <param name="moveYaw">이동 방향 월드 yaw</param>
        /// <returns>보드 목표 월드 yaw</returns>
        private float CalculateBoardTargetYaw(float moveYaw)
        {
            float relativeYaw = Mathf.DeltaAngle(GetYaw(_viewForward), moveYaw);
            bool isForward = Mathf.Abs(relativeYaw) <= (90.0f + BOARD_SIDE_TOLERANCE);
            return isForward ? moveYaw : moveYaw + 180.0f;
        }

        /// <summary>
        /// 탑승 자세일 때 골반을 수평으로 돌려 하체가 보드 방향 기준 스탠스 각도를 보게 한다.
        /// 비틀림 뼈(상체)는 돌리기 전 월드 회전으로 되돌려 정면을 유지한다. 상체 비틀림 보정 뒤, 조준 보정 전에 호출한다.
        /// </summary>
        private void ApplyRidingTurn()
        {
            if ((_ridingTurnWeight <= 0.0f) || !_twistBone || !_twistBone.parent) return;

            Transform pelvis = _twistBone.parent;
            Quaternion upperBodyRotation = _twistBone.rotation;

            // 애니메이션 골반은 몸 정면 기준이므로, 몸 정면 → 보드 방향 차이에 스탠스 각도를 더해 돌린다
            float turnYaw = Mathf.DeltaAngle(GetBodyYaw(), _boardYaw) + _ridingLowerBodyYaw;
            pelvis.rotation = Quaternion.AngleAxis(turnYaw * _ridingTurnWeight, Vector3.up) * pelvis.rotation;
            _twistBone.rotation = upperBodyRotation;
        }

        /// <summary>
        /// 탑승 중 보드의 회전을 보드 방향(월드 yaw)으로 덮어쓴다.
        /// </summary>
        private void ApplyBoardYaw()
        {
            if (!_ridingBoard || !_isRiding) return;

            _ridingBoard.rotation = Quaternion.AngleAxis(_boardYaw, Vector3.up);
        }

        /// <summary>
        /// 몸(CharacterRotator 가 돌리는 대상)이 바라보는 월드 yaw 를 반환한다. 몸을 찾지 못했으면 시점 yaw 를 쓴다.
        /// </summary>
        /// <returns>몸 정면 월드 yaw (도)</returns>
        private float GetBodyYaw()
        {
            if (!_body) return GetYaw(_viewForward);

            Quaternion parentRotation = _body.parent ? _body.parent.rotation : Quaternion.identity;
            Vector3 bodyForward = parentRotation * (_body.localRotation * _bodyRestInverse) * Vector3.forward;
            return GetYaw(bodyForward);
        }

        /// <summary>
        /// 기준 위치가 몸 정면 기준 어느 방향인지 Animator HitDirection 값으로 반환한다.
        /// 기준 위치가 없거나 몸과 수평 위치가 같으면 앞쪽으로 본다.
        /// </summary>
        /// <param name="hitSource">피격 방향 기준 월드 위치</param>
        /// <returns>HitDirection 값 (0 앞, 1 뒤, 2 왼쪽, 3 오른쪽)</returns>
        private int CalculateHitDirection(Vector3? hitSource)
        {
            if (!hitSource.HasValue) return HIT_DIRECTION_FRONT;

            Vector3 origin = _body ? _body.position : _animator.transform.position;
            Vector3 toSource = Vector3.ProjectOnPlane(hitSource.Value - origin, Vector3.up);
            if (toSource.sqrMagnitude <= Mathf.Epsilon) return HIT_DIRECTION_FRONT;

            float relativeYaw = Mathf.DeltaAngle(GetBodyYaw(), GetYaw(toSource));
            float absYaw = Mathf.Abs(relativeYaw);
            if (absYaw <= HIT_FRONT_HALF_ANGLE) return HIT_DIRECTION_FRONT;
            if (absYaw >= 180.0f - HIT_FRONT_HALF_ANGLE) return HIT_DIRECTION_BACK;

            return (relativeYaw > 0.0f) ? HIT_DIRECTION_RIGHT : HIT_DIRECTION_LEFT;
        }

        /// <summary>
        /// 월드 방향의 수평 yaw(도)를 반환한다. +z 가 0, +x 가 90 이다.
        /// </summary>
        /// <param name="direction">월드 방향</param>
        /// <returns>yaw (도)</returns>
        private static float GetYaw(Vector3 direction)
        {
            return Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
        }

        /// <summary>
        /// 상체가 FireHold 자세(전환 중 아님)이면 총구 위치와 총신 방향을 기준 자세로 저장한다.
        /// 조준 보정을 적용하기 전, Animator 가 만든 포즈에서 호출해야 한다.
        /// </summary>
        /// <param name="reference">기준 공간 (조준 뼈의 부모)</param>
        private void CaptureHoldReference(Transform reference)
        {
            if ((_upperBodyLayer < 0) || _animator.IsInTransition(_upperBodyLayer)) return;
            if (_animator.GetCurrentAnimatorStateInfo(_upperBodyLayer).shortNameHash != AnimHash.FireHoldState) return;

            _holdMuzzleLocal = reference.InverseTransformPoint(_muzzle.position);
            _holdBarrelLocal = reference.InverseTransformDirection(GetBarrelDirection());
            _hasHoldReference = true;
        }

        /// <summary>
        /// 현재 포즈의 총신 방향(총구의 부모 → 총구)을 반환한다.
        /// </summary>
        /// <returns>정규화된 총신 방향 (월드 기준)</returns>
        private Vector3 GetBarrelDirection()
        {
            Transform barrelBase = _muzzle.parent ? _muzzle.parent : _aimBone;
            return (_muzzle.position - barrelBase.position).normalized;
        }

        /// <summary>
        /// 조준 뼈에서 조준점까지의 거리로 조준 보정 강도 계수를 구한다.
        /// 최소 거리 이하면 0, 최소 거리 + 페이드 거리 이상이면 1 이다.
        /// </summary>
        /// <param name="pivot">조준 뼈 위치</param>
        /// <param name="target">조준점</param>
        /// <returns>0~1 사이의 계수</returns>
        private float CalculateAimDistanceWeight(Vector3 pivot, Vector3 target)
        {
            float distance = Vector3.Distance(pivot, target);
            if (_aimFadeDistance <= 0.0f) return (distance >= _minAimDistance) ? 1.0f : 0.0f;

            return Mathf.Clamp01((distance - _minAimDistance) / _aimFadeDistance);
        }

        /// <summary>
        /// pivot 을 축으로 돌려 총신(muzzle 에서 barrel 방향)이 target 을 지나게 하는 회전을 구한다.
        /// 총신 직선 위에서 pivot 과 target 사이 거리만큼 떨어진 점을 찾아, 그 점을 target 쪽으로 돌린다.
        /// 그런 점이 없으면(조준점이 너무 가깝거나 총구 뒤) 실패로 보고 보정하지 않는다.
        /// </summary>
        /// <param name="pivot">회전 중심 (조준 뼈 위치)</param>
        /// <param name="muzzle">총구 위치</param>
        /// <param name="barrel">총신 방향 (정규화)</param>
        /// <param name="target">조준점</param>
        /// <param name="correction">조준 뼈에 월드 기준으로 곱할 회전. 실패하면 identity</param>
        /// <returns>회전을 구했으면 true</returns>
        private static bool TryCalculateAimCorrection(
            Vector3 pivot,
            Vector3 muzzle,
            Vector3 barrel,
            Vector3 target,
            out Quaternion correction)
        {
            correction = Quaternion.identity;

            Vector3 offset = muzzle - pivot;
            Vector3 pivotToTarget = target - pivot;

            // |offset + barrel * s| = |pivotToTarget| 를 s 에 대해 푼다 (barrel 은 정규화되어 있음)
            float projection = Vector3.Dot(offset, barrel);
            float discriminant = (projection * projection) - (offset.sqrMagnitude - pivotToTarget.sqrMagnitude);
            if (discriminant < 0.0f) return false;

            float distanceOnBarrel = -projection + Mathf.Sqrt(discriminant);
            if (distanceOnBarrel <= 0.0f) return false;

            correction = Quaternion.FromToRotation(offset + (barrel * distanceOnBarrel), pivotToTarget);
            return true;
        }
        #endregion

        #region Nested Types
        private static class AnimHash
        {
            public static readonly int IsMoving = Animator.StringToHash("IsMoving");
            public static readonly int MoveX = Animator.StringToHash("MoveX");
            public static readonly int MoveY = Animator.StringToHash("MoveY");
            public static readonly int MoveAnimSpeed = Animator.StringToHash("MoveAnimSpeed");
            public static readonly int IsFiring = Animator.StringToHash("IsFiring");
            public static readonly int Fire = Animator.StringToHash("Fire");
            public static readonly int IsJumping = Animator.StringToHash("IsJumping");
            public static readonly int Jump = Animator.StringToHash("Jump");
            public static readonly int IsRolling = Animator.StringToHash("IsRolling");
            public static readonly int Roll = Animator.StringToHash("Roll");
            public static readonly int SpecialIdle = Animator.StringToHash("SpecialIdle");
            public static readonly int IsSpecialIdle = Animator.StringToHash("IsSpecialIdle");
            public static readonly int SpecialIdleIndex = Animator.StringToHash("SpecialIdleIndex");
            public static readonly int Death = Animator.StringToHash("Death");
            public static readonly int IsRiding = Animator.StringToHash("IsRiding");
            public static readonly int Mount = Animator.StringToHash("Mount");
            public static readonly int IsRidingPose = Animator.StringToHash("IsRidingPose");
            public static readonly int Hit = Animator.StringToHash("Hit");
            public static readonly int HitDirection = Animator.StringToHash("HitDirection");
            public static readonly int FireHoldState = Animator.StringToHash("FireHold");
            public static readonly int IdleState = Animator.StringToHash("Idle");
            public static readonly int DeathLoopState = Animator.StringToHash("DeathLoop");
            public static readonly int RidingState = Animator.StringToHash("Riding");
        }
        #endregion
    }
}
