using UnityEngine;
using Attribute.Core;
using Attribute.Effect;
using Movement;
using PlayerInput;

namespace Map.Gimmicks
{
    /// <summary>
    /// 플레이어가 닿으면 Effect(피해)를 한 번 적용하고 장애물 반대쪽으로 밀어내는 장애물.
    /// 닿아도 통과되는 피해 전용 장애물은 EffectZone 을 쓴다.
    /// 콜라이더의 Is Trigger를 켜야 한다. 움직이는 장애물이면 Rigidbody(Is Kinematic)도 붙인다.
    /// </summary>
    [DefaultExecutionOrder(-50)]   // CharacterMotor(0)보다 먼저 실행해야 이번 프레임 이동에 합쳐진다
    [RequireComponent(typeof(Collider))]
    public class KnockbackHazard : MonoBehaviour
    {
        #region Constants
        // 한 프레임에 이보다 적게 움직였으면 멈춰 있는 것으로 본다
        private const float MIN_MOTION = 0.005f;
        #endregion

        #region Serialized Fields
        [Header("Effects")]
        [Tooltip("닿을 때 한 번 적용할 Effect (ValueSource는 Float)")]
        [SerializeField] private SOAttributeEffect[] _hitEffects;

        [Header("Knockback")]
        [Tooltip("수평으로 밀어내는 거리")]
        [SerializeField, Min(0.0f)] private float _pushDistance = 3.0f;

        [Tooltip("밀어내는 데 걸리는 시간(초). 짧을수록 세게 튕긴다.")]
        [SerializeField, Min(0.05f)] private float _pushDuration = 0.25f;

        [Tooltip("다시 닿았을 때 또 맞기까지의 시간(초)")]
        [SerializeField, Min(0.0f)] private float _hitCooldown = 0.6f;
        #endregion

        #region Private Fields
        // 움직이는 방향을 구하려고 직전 두 프레임의 자세를 기억한다
        private Matrix4x4 _previousPose;
        private Matrix4x4 _currentPose;
        private Collider _collider;
        private CharacterMotor _motor;
        private Vector3 _pushDirection;
        private float _pushElapsed;
        private float _nextHitTime;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            _collider = GetComponent<Collider>();
        }

        private void OnEnable()
        {
            _previousPose = transform.localToWorldMatrix;
            _currentPose = _previousPose;
        }

        private void LateUpdate()
        {
            _previousPose = _currentPose;
            _currentPose = transform.localToWorldMatrix;
        }

        private void Update()
        {
            if (_motor == null) return;

            // 처음에 빠르고 끝으로 갈수록 느려지게, 남은 시간에 비례한 속도로 민다
            float previous = _pushElapsed;
            _pushElapsed = Mathf.Min(_pushElapsed + Time.deltaTime, _pushDuration);
            float moved = EaseOut(_pushElapsed / _pushDuration) - EaseOut(previous / _pushDuration);
            _motor.AddExternalDisplacement(_pushDirection * (moved * _pushDistance));

            if (_pushElapsed >= _pushDuration)
            {
                _motor = null;
            }
        }

        private void OnDisable()
        {
            _motor = null;
        }

        private void OnTriggerEnter(Collider other)
        {
            TryHit(other);
        }

        // 장애물에 붙은 채로 있으면 쿨다운마다 다시 맞는다
        private void OnTriggerStay(Collider other)
        {
            TryHit(other);
        }
        #endregion

        #region Private Methods
        private void TryHit(Collider other)
        {
            if (Time.time < _nextHitTime) return;
            if (!GimmickUtility.TryGetPlayer(other, out PlayerInputComponent player)) return;

            _nextHitTime = Time.time + _hitCooldown;
            GimmickUtility.ApplyEffects(_hitEffects, player.GetComponentInChildren<IEffectTarget>());

            // 꺼진 컴포넌트에도 트리거 메시지가 온다. 멈춰 있는 동안에는 피해만 주고,
            // 넉백을 예약해 두면 다시 켜질 때 늦게 발동하므로 여기서 끝낸다.
            if (!enabled) return;

            // 장애물이 움직이고 있으면 플레이어가 닿은 부분이 움직이던 방향으로 민다 (휘두르는 봉, 튀어나오는 피스톤).
            Vector3 hitPoint = other.bounds.center;
            Vector3 direction = hitPoint - _previousPose.MultiplyPoint3x4(_currentPose.inverse.MultiplyPoint3x4(hitPoint));
            direction.y = 0.0f;

            // 멈춰 있으면 장애물 중심에서 플레이어 쪽으로 민다.
            if (direction.sqrMagnitude < (MIN_MOTION * MIN_MOTION))
            {
                direction = hitPoint - _collider.bounds.center;
                direction.y = 0.0f;
            }

            // 겹쳐 있어 방향을 알 수 없으면 장애물의 정면으로 민다.
            if (direction.sqrMagnitude < 0.0001f)
            {
                direction = transform.forward;
                direction.y = 0.0f;
            }

            _pushDirection = direction.normalized;
            _pushElapsed = 0.0f;
            _motor = player.GetComponentInChildren<CharacterMotor>();
        }

        private static float EaseOut(float t)
        {
            return 1.0f - ((1.0f - t) * (1.0f - t));
        }
        #endregion
    }
}
