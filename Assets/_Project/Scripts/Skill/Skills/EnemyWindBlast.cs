using UnityEngine;
using AI;
using CharacterSound;
using Core.Effect;
using Movement;
using Skill.Core;

namespace Skill.Skills
{
    /// <summary>
    /// 적의 바람 스킬. 발동하면 몸통을 제자리에 붙잡고(AIController.Hold),
    /// 램프 시간 동안 바람 방향을 매 프레임 대상 쪽으로 맞추며 점점 세지는 바람으로 부채꼴 안의 대상을 밀어낸 뒤,
    /// 끝나는 순간 범위 안이면 그 순간 방향으로 한 번 크게 넉백한다.
    /// 피해는 없다. 쿨타임은 이 스킬이 아니라 BT 가 관리한다(CooldownCost 를 붙이지 않는다).
    /// 끝나면(정상·대상 놓침·Stop·비활성화) 붙잡힘을 풀므로, BT 는 IsHolding 이 풀릴 때까지 기다리면 된다.
    /// </summary>
    [DefaultExecutionOrder(-50)]   // CharacterMotor(0)보다 먼저 실행해야 이번 프레임 이동에 합쳐진다
    public class EnemyWindBlast : SkillBase
    {
        #region Constants
        /// <summary>정상 종료는 ReleaseHold 가 맡으므로, 붙잡힘 안전 시간에 더하는 여유(초).</summary>
        private const float HOLD_MARGIN = 0.5f;
        /// <summary>기즈모 호를 나누어 그리는 선분 수.</summary>
        private const int GIZMO_ARC_SEGMENTS = 16;
        #endregion

        #region Serialized Fields
        [Header("Wind")]
        [Tooltip("램프 시작 시점의 미는 속도(m/s)")]
        [SerializeField, Min(0.0f)] private float _minSpeed = 0.5f;
        [Tooltip("램프 끝 시점의 미는 속도(m/s)")]
        [SerializeField, Min(0.0f)] private float _maxSpeed = 2.0f;
        [Tooltip("미는 속도가 최소에서 최대까지 선형으로 커지는 시간(초)")]
        [SerializeField, Min(0.1f)] private float _rampDuration = 2.0f;

        [Header("Range")]
        [Tooltip("바람이 닿는 3D 거리(m)")]
        [SerializeField, Min(0.0f)] private float _range = 6.0f;
        [Tooltip("정면 기준 좌우 반각(도)")]
        [SerializeField, Range(0.0f, 180.0f)] private float _horizontalHalfAngle = 30.0f;
        [Tooltip("수평면 기준 위아래 반각(도)")]
        [SerializeField, Range(0.0f, 90.0f)] private float _verticalHalfAngle = 30.0f;
        [Tooltip("바람을 막는 장애물 레이어. 몹과 대상 사이에 있으면 밀지 않는다.")]
        [SerializeField] private LayerMask _obstacleLayers;

        [Header("Knockback")]
        [Tooltip("램프가 끝나는 순간 범위 안이면 수평으로 밀어내는 거리(m)")]
        [SerializeField, Min(0.0f)] private float _knockbackDistance = 4.0f;
        [Tooltip("넉백에 걸리는 시간(초). 짧을수록 세게 튕긴다.")]
        [SerializeField, Min(0.05f)] private float _knockbackDuration = 0.3f;

        [Header("VFX")]
        [SerializeField] private SOEffectData _windVfx;
        [SerializeField] private SOEffectData _burstSmokeVfx;
        [SerializeField] private SOEffectData _burstLinesVfx;

        [Header("References")]
        [Tooltip("바람이 나가는 기준점. 몸 중심 가슴 높이에 둔다.")]
        [SerializeField] private Transform _windOrigin;
        [Tooltip("VFX 전용 기준점. VFX 의 위치와 회전(월드)을 이 오브젝트에 맞추고 붙인다. 비우면 바람 기준점을 쓴다.")]
        [SerializeField] private Transform _vfxAnchor;
        #endregion

        #region Private Fields
        private enum EWindPhase
        {
            Idle,
            Ramp,
            Knockback,
        }

        private SEffectHandle _windHandle;
        private AIController _controller;
        private SoundController _soundController;
        private EWindPhase _phase;
        private Transform _target;
        private CharacterMotor _motor;
        private Vector3 _forward;
        private Vector3 _knockbackDirection;
        private float _elapsed;
        #endregion

        #region Unity Lifecycle
        protected override void Awake()
        {
            base.Awake();
            _controller = GetComponentInParent<AIController>();
            if (_controller) _soundController = _controller.GetComponentInChildren<SoundController>();

            Debug.Assert(_controller != null, $"[{name}] AIController 를 찾을 수 없습니다.");
            Debug.Assert(_windOrigin != null, $"[{name}] 바람 기준점이 연결되지 않았습니다.");
        }

        private void Update()
        {
            if (_phase == EWindPhase.Idle) return;

            // 대상이나 모터가 파괴되었으면(플레이어 사망·재생성) 그 프레임에 끝낸다
            if (!_target || !_motor)
            {
                End();
                return;
            }

            if (_phase == EWindPhase.Ramp)
            {
                UpdateRamp();
            }
            else
            {
                UpdateKnockback();
            }
        }

        private void OnDisable()
        {
            Stop();
        }

        private void OnDrawGizmosSelected()
        {
            if (!_windOrigin) return;

            // 발동 중이면 현재 바람 방향, 아니면 기준점 정면을 쓴다
            Vector3 forward = (_phase != EWindPhase.Idle)
                ? _forward
                : Vector3.ProjectOnPlane(_windOrigin.forward, Vector3.up).normalized;
            if (forward.sqrMagnitude <= Mathf.Epsilon) return;

            Vector3 origin = _windOrigin.position;
            Gizmos.color = Color.green;

            // 수평 부채꼴
            Vector3 left = Quaternion.AngleAxis(-_horizontalHalfAngle, Vector3.up) * forward;
            Vector3 right = Quaternion.AngleAxis(_horizontalHalfAngle, Vector3.up) * forward;
            Gizmos.DrawRay(origin, left * _range);
            Gizmos.DrawRay(origin, right * _range);
            DrawGizmoArc(origin, Vector3.up, left, _horizontalHalfAngle * 2.0f, _range);

            // 수직 부채꼴
            Vector3 side = Vector3.Cross(Vector3.up, forward);
            Vector3 up = Quaternion.AngleAxis(-_verticalHalfAngle, side) * forward;
            Vector3 down = Quaternion.AngleAxis(_verticalHalfAngle, side) * forward;
            Gizmos.DrawRay(origin, up * _range);
            Gizmos.DrawRay(origin, down * _range);
            DrawGizmoArc(origin, side, up, _verticalHalfAngle * 2.0f, _range);
        }
        #endregion

        #region Public Methods
        public override void Stop()
        {
            End();
        }
        #endregion

        #region Protected Methods
        protected override void Execute()
        {
            // 쿨타임 코스트가 없어 CanExecute 가 계속 true 이므로 진행 중 재발동은 여기서 무시한다
            if (_phase != EWindPhase.Idle) return;
            if (!_controller || !_windOrigin) return;

            Transform target = _controller.VisibleTarget;
            if (!target) return;

            // 모터는 플레이어 루트가 아니라 자식에 있다
            CharacterMotor motor = target.GetComponentInChildren<CharacterMotor>();
            if (!motor) return;

            _target = target;
            _motor = motor;

            // 몹→대상 수평 방향으로 시작한다. 판정 부채꼴도 이 방향 기준이며 램프 동안 매 프레임 갱신한다
            Vector3 toTarget = Vector3.ProjectOnPlane(GetTargetPoint() - _windOrigin.position, Vector3.up);
            _forward = (toTarget.sqrMagnitude > Mathf.Epsilon) ? toTarget.normalized : _windOrigin.forward;

            _elapsed = 0.0f;
            _phase = EWindPhase.Ramp;

            _controller.Hold(_forward, _rampDuration + _knockbackDuration + HOLD_MARGIN);
            Transform vfxAnchor = GetVfxAnchor();
            if (_windVfx && _windVfx.Prefab && EffectManager.Instance)
                _windHandle = EffectManager.Instance.PlayLoop(_windVfx, vfxAnchor.position, GetVfxRotation(_windVfx), vfxAnchor);
            if (_soundController) _soundController.SetSkillLoop(SkillName, ESoundPhase.Move, true);

            base.Execute();
        }
        #endregion

        #region Private Methods
        private void UpdateRamp()
        {
            // 대상을 놓쳤으면 바로 끝낸다
            if (!_controller || !_controller.HasVisibleTarget)
            {
                End();
                return;
            }

            _elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(_elapsed / _rampDuration);
            float speed = Mathf.Lerp(_minSpeed, _maxSpeed, t);

            // 램프 동안 바람 방향과 몸 방향을 매 프레임 대상 쪽으로 다시 맞춰, 움직이는 대상을 따라간다
            Vector3 direction = GetPushDirection();
            _forward = direction;
            _controller.Hold(_forward, (_rampDuration - _elapsed) + _knockbackDuration + HOLD_MARGIN);

            bool isInWind = IsTargetInWind();
            if (isInWind)
            {
                _motor.AddExternalDisplacement(direction * (speed * Time.deltaTime));
            }

            if (t < 1.0f) return;

            EffectManager.Instance?.Stop(_windHandle);
            _windHandle = default;
            if (_soundController) _soundController.PlaySkillSound(SkillName, ESoundPhase.Impact, _windOrigin.position);
            Transform vfxAnchor = GetVfxAnchor();
            if (_burstSmokeVfx && _burstSmokeVfx.Prefab)
                EffectManager.Instance?.Play(
                    _burstSmokeVfx, vfxAnchor.position, GetVfxRotation(_burstSmokeVfx), vfxAnchor);
            if (_burstLinesVfx && _burstLinesVfx.Prefab)
                EffectManager.Instance?.Play(
                    _burstLinesVfx, vfxAnchor.position, GetVfxRotation(_burstLinesVfx), vfxAnchor);

            // 램프가 끝나는 순간 범위 안이면 넉백, 아니면 그대로 끝
            if (isInWind)
            {
                _knockbackDirection = direction;
                _elapsed = 0.0f;
                _phase = EWindPhase.Knockback;
            }
            else
            {
                End();
            }
        }

        private void UpdateKnockback()
        {
            // 처음에 빠르고 끝으로 갈수록 느려지게 미는 증분 방식 (KnockbackHazard 와 같다)
            float previous = _elapsed;
            _elapsed = Mathf.Min(_elapsed + Time.deltaTime, _knockbackDuration);
            float moved = EaseOut(_elapsed / _knockbackDuration) - EaseOut(previous / _knockbackDuration);
            _motor.AddExternalDisplacement(_knockbackDirection * (moved * _knockbackDistance));

            if (_elapsed >= _knockbackDuration) End();
        }

        /// <summary>
        /// 대상이 고정 정면 기준 부채꼴(수평·수직 각도, 3D 거리) 안에 있고 사이에 장애물이 없는지 판정한다.
        /// </summary>
        private bool IsTargetInWind()
        {
            Vector3 origin = _windOrigin.position;
            Vector3 targetPoint = GetTargetPoint();
            Vector3 toTarget = targetPoint - origin;

            if (toTarget.magnitude > _range) return false;

            Vector3 flat = Vector3.ProjectOnPlane(toTarget, Vector3.up);
            if (Vector3.Angle(_forward, flat) > _horizontalHalfAngle) return false;

            float verticalAngle = Mathf.Atan2(Mathf.Abs(toTarget.y), flat.magnitude) * Mathf.Rad2Deg;
            if (verticalAngle > _verticalHalfAngle) return false;

            bool isBlocked = Physics.Linecast(origin, targetPoint, _obstacleLayers, QueryTriggerInteraction.Ignore);
            return !isBlocked;
        }

        /// <summary>
        /// 몹에서 대상 쪽으로 미는 수평 방향. 겹쳐서 알 수 없으면 고정 정면을 쓴다.
        /// </summary>
        private Vector3 GetPushDirection()
        {
            Vector3 flat = Vector3.ProjectOnPlane(GetTargetPoint() - _windOrigin.position, Vector3.up);
            return (flat.sqrMagnitude > Mathf.Epsilon) ? flat.normalized : _forward;
        }

        /// <summary>
        /// 발밑이 아니라 몸통 중심을 판정 대상점으로 쓴다.
        /// </summary>
        private Vector3 GetTargetPoint()
        {
            return _target.TryGetComponent(out Collider targetCollider)
                ? targetCollider.bounds.center
                : _target.position;
        }

        /// <summary>
        /// 상태를 초기화하고 붙잡힘을 푼다. 진행 중이 아니면 아무것도 하지 않는다.
        /// </summary>
        private void End()
        {
            if (_phase == EWindPhase.Idle) return;

            EffectManager.Instance?.Stop(_windHandle);
            _windHandle = default;
            if (_soundController) _soundController.SetSkillLoop(SkillName, ESoundPhase.Move, false);
            _phase = EWindPhase.Idle;
            _elapsed = 0.0f;
            _target = null;
            _motor = null;

            if (_controller) _controller.ReleaseHold();
        }

        /// <summary>
        /// VFX 를 놓고 붙일 기준점. VFX 전용 기준점이 없으면 바람 기준점을 쓴다.
        /// </summary>
        private Transform GetVfxAnchor()
        {
            return _vfxAnchor ? _vfxAnchor : _windOrigin;
        }

        /// <summary>
        /// VFX 기준점의 월드 회전에 VFX 프리팹 루트의 localRotation 을 곱한 회전.
        /// 기준점은 몸통과 함께 도므로, 몸통이 대상 쪽으로 도는 중에 놓아도 VFX 가 함께 돌아 끝에 대상 방향이 된다.
        /// </summary>
        private Quaternion GetVfxRotation(SOEffectData data)
        {
            return GetVfxAnchor().rotation * data.Prefab.transform.localRotation;
        }

        private static float EaseOut(float t)
        {
            return 1.0f - ((1.0f - t) * (1.0f - t));
        }

        /// <summary>
        /// from 방향에서 axis 를 축으로 angle 만큼 도는 반지름 radius 의 호를 기즈모 선분으로 그린다.
        /// </summary>
        private static void DrawGizmoArc(Vector3 center, Vector3 axis, Vector3 from, float angle, float radius)
        {
            Vector3 previous = center + (from * radius);
            for (int i = 1; i <= GIZMO_ARC_SEGMENTS; i++)
            {
                Vector3 direction = Quaternion.AngleAxis(angle * i / GIZMO_ARC_SEGMENTS, axis) * from;
                Vector3 point = center + (direction * radius);
                Gizmos.DrawLine(previous, point);
                previous = point;
            }
        }
        #endregion
    }
}
