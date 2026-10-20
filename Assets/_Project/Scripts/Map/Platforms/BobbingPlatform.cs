using UnityEngine;
using Map.Gimmicks;
using Movement;
using PlayerInput;

namespace Map.Platforms
{
    /// <summary>
    /// 플레이어가 올라서면 아래로 출렁였다가 다시 올라오고, 조금씩 작게 오르내리다 제자리에 멈추는 발판.
    /// 하늘에 떠 있는 발판이 무게를 받는 느낌을 준다. 움직임은 감쇠 스프링으로 계산한다.
    /// 출렁임은 플레이어 발이 실제로 닿은 뒤 시작하고, 내려가는 속도는 _pressDuration 동안 서서히 붙는다.
    /// 발판 루트에 붙이고, 밟는 판정 BoxCollider 는 자식(Body)에 있어도 된다. 올라섰는지 감지하는 트리거는 스스로 만든다.
    /// </summary>
    [DefaultExecutionOrder(-100)]   // 탑승자(PlatformRider)보다 먼저 움직여야 이번 프레임 이동량이 전달된다
    [RequireComponent(typeof(RideablePlatform))]
    public class BobbingPlatform : MonoBehaviour
    {
        #region Constants
        // 이보다 작게 움직이면 멈춘 것으로 보고 제자리에 맞춘다
        private const float REST_OFFSET = 0.002f;
        private const float REST_SPEED = 0.02f;

        // 누르는 힘을 줬을 때의 최저점을 미리 계산하는 시간 간격(초)
        private const float SIMULATION_STEP = 0.001f;
        #endregion

        #region Serialized Fields
        [Header("Bob")]
        [Tooltip("올라섰을 때 처음 내려가는 깊이(m)")]
        [SerializeField, Min(0f)] private float _dipDepth = 0.35f;

        [Tooltip("오르내리는 빠르기(초당 횟수)")]
        [SerializeField, Min(0.1f)] private float _frequency = 1.3f;

        [Tooltip("출렁임이 줄어드는 정도. 작을수록 오래 출렁인다. (0 ~ 1)")]
        [SerializeField, Range(0.05f, 0.95f)] private float _dampingRatio = 0.2f;

        [Tooltip("올라섰을 때 내려가는 속도가 붙는 시간(초). 짧을수록 덜컹인다. 0 이면 한 번에 붙는다.")]
        [SerializeField, Min(0f)] private float _pressDuration = 0.12f;

        [Header("References")]
        [Tooltip("밟는 판정 BoxCollider. 비우면 자식에서 찾는다.")]
        [SerializeField] private BoxCollider _body;
        #endregion

        #region Private Fields
        private TransformMotor _motor;
        private Vector3 _startPosition;
        private float _offset;
        private float _velocity;
        private float _landingSpeed;
        private float _pressTimeLeft;
        private CharacterController _arrivingPlayer;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            _motor = GetComponent<TransformMotor>();
            if (_body == null) _body = GetComponentInChildren<BoxCollider>();

            // 트리거가 자식에 있어도 이 오브젝트의 Rigidbody 를 통해 OnTriggerEnter 가 전달된다
            if (_body != null) PlatformBody.TryAddStepTrigger(_body.gameObject);

            _landingSpeed = CalculateLandingSpeed();
        }

        private void Start()
        {
            _startPosition = transform.position;
        }

        private void Update()
        {
            // 트리거는 발판 윗면보다 위까지 있으므로, 발이 실제로 닿은 뒤에 출렁이기 시작한다
            if ((_arrivingPlayer != null) && _arrivingPlayer.isGrounded)
            {
                _arrivingPlayer = null;
                StartPress();
            }

            float omega = 2f * Mathf.PI * _frequency;
            float step = Time.deltaTime;

            // 감쇠 스프링: 제자리로 끌어당기는 힘 + 속도에 비례한 저항 + 올라선 직후 누르는 힘
            float acceleration = (-omega * omega * _offset) - (2f * _dampingRatio * omega * _velocity) + CalculatePressAcceleration(step);
            _velocity += acceleration * step;
            _offset += _velocity * step;

            bool isPressing = _pressTimeLeft > 0f;
            if (!isPressing && (Mathf.Abs(_offset) < REST_OFFSET) && (Mathf.Abs(_velocity) < REST_SPEED))
            {
                _offset = 0f;
                _velocity = 0f;
            }

            // 멈춰 있어도 호출해야 이번 프레임 이동량이 0 으로 갱신되어 탑승자가 밀리지 않는다
            _motor.MoveTo(_startPosition + (Vector3.up * _offset), float.MaxValue);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!GimmickUtility.TryGetPlayer(other, out PlayerInputComponent player)) return;

            _arrivingPlayer = player.GetComponent<CharacterController>();
        }

        private void OnTriggerExit(Collider other)
        {
            if (!GimmickUtility.TryGetPlayer(other, out _)) return;

            // 닿지 않고 지나갔으면 출렁이지 않는다
            _arrivingPlayer = null;
        }
        #endregion

        #region Private Methods
        private void StartPress()
        {
            if (_pressDuration <= 0f)
            {
                _velocity = Mathf.Min(_velocity, -_landingSpeed);
                return;
            }

            _pressTimeLeft = _pressDuration;
        }

        // 속도를 한 번에 바꾸면 발판과 카메라가 덜컹이므로, 같은 크기의 충격을 _pressDuration 동안 나눠 준다
        private float CalculatePressAcceleration(float step)
        {
            if ((_pressTimeLeft <= 0f) || (step <= 0f)) return 0f;

            float pressStep = Mathf.Min(step, _pressTimeLeft);
            _pressTimeLeft -= step;
            return -(_landingSpeed / _pressDuration) * (pressStep / step);
        }

        // 정지 상태에서 이만큼의 속도 변화(충격)를 받으면 첫 최저점이 _dipDepth 가 된다
        private float CalculateLandingSpeed()
        {
            float omega = 2f * Mathf.PI * _frequency;
            float dampedOmega = omega * Mathf.Sqrt(1f - (_dampingRatio * _dampingRatio));
            float peakTime = Mathf.Atan2(dampedOmega, _dampingRatio * omega) / dampedOmega;
            float peakScale = Mathf.Exp(-_dampingRatio * omega * peakTime) * Mathf.Sin(dampedOmega * peakTime) / dampedOmega;
            if (_pressDuration <= 0f) return _dipDepth / peakScale;

            // 충격을 나눠 주면 최저점이 얕아지므로, 속도 1 로 눌렀을 때의 최저점을 직접 계산해 맞춘다
            return _dipDepth / CalculatePressedPeakScale(omega, peakTime + _pressDuration);
        }

        private float CalculatePressedPeakScale(float omega, float duration)
        {
            float offset = 0f;
            float velocity = 0f;
            float deepest = 0f;
            for (float time = 0f; time < duration; time += SIMULATION_STEP)
            {
                float press = (time < _pressDuration) ? (-1f / _pressDuration) : 0f;
                float acceleration = (-omega * omega * offset) - (2f * _dampingRatio * omega * velocity) + press;
                velocity += acceleration * SIMULATION_STEP;
                offset += velocity * SIMULATION_STEP;
                deepest = Mathf.Min(deepest, offset);
            }

            return -deepest;
        }
        #endregion
    }
}
