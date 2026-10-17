using UnityEngine;
using Map.Gimmicks;
using Movement;

namespace Map.Platforms
{
    /// <summary>
    /// 플레이어가 올라서면 아래로 출렁였다가 다시 올라오고, 조금씩 작게 오르내리다 제자리에 멈추는 발판.
    /// 하늘에 떠 있는 발판이 무게를 받는 느낌을 준다. 움직임은 감쇠 스프링으로 계산한다.
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
        #endregion

        #region Serialized Fields
        [Header("Bob")]
        [Tooltip("올라섰을 때 처음 내려가는 깊이(m)")]
        [SerializeField, Min(0f)] private float _dipDepth = 0.35f;

        [Tooltip("오르내리는 빠르기(초당 횟수)")]
        [SerializeField, Min(0.1f)] private float _frequency = 1.3f;

        [Tooltip("출렁임이 줄어드는 정도. 작을수록 오래 출렁인다. (0 ~ 1)")]
        [SerializeField, Range(0.05f, 0.95f)] private float _dampingRatio = 0.2f;

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
            float omega = 2f * Mathf.PI * _frequency;
            float step = Time.deltaTime;

            // 감쇠 스프링: 제자리로 끌어당기는 힘 + 속도에 비례한 저항
            float acceleration = (-omega * omega * _offset) - (2f * _dampingRatio * omega * _velocity);
            _velocity += acceleration * step;
            _offset += _velocity * step;

            if ((Mathf.Abs(_offset) < REST_OFFSET) && (Mathf.Abs(_velocity) < REST_SPEED))
            {
                _offset = 0f;
                _velocity = 0f;
            }

            // 멈춰 있어도 호출해야 이번 프레임 이동량이 0 으로 갱신되어 탑승자가 밀리지 않는다
            _motor.MoveTo(_startPosition + (Vector3.up * _offset), float.MaxValue);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!GimmickUtility.TryGetPlayer(other, out _)) return;

            _velocity = Mathf.Min(_velocity, -_landingSpeed);
        }
        #endregion

        #region Private Methods
        // 정지 상태에서 이 속도로 내려가기 시작하면 첫 최저점이 _dipDepth 가 된다
        private float CalculateLandingSpeed()
        {
            float omega = 2f * Mathf.PI * _frequency;
            float dampedOmega = omega * Mathf.Sqrt(1f - (_dampingRatio * _dampingRatio));
            float peakTime = Mathf.Atan2(dampedOmega, _dampingRatio * omega) / dampedOmega;
            float peakScale = Mathf.Exp(-_dampingRatio * omega * peakTime) * Mathf.Sin(dampedOmega * peakTime) / dampedOmega;
            return _dipDepth / peakScale;
        }
        #endregion
    }
}
