using UnityEngine;
using Map.Gimmicks;
using Movement;

namespace Map.Platforms
{
    /// <summary>
    /// 플레이어가 올라서면 잠시 뒤 아래로 떨어지고, 사라졌다가 원래 자리에 다시 생기는 발판.
    /// BoxCollider 가 붙은 발판에 붙인다. 밟았는지 감지하는 트리거는 스스로 만든다.
    /// 떨어지는 동안 탄 플레이어가 함께 내려가려면 RideablePlatform 도 붙인다.
    /// </summary>
    [DefaultExecutionOrder(-100)]   // 탑승자(PlatformRider)보다 먼저 움직여야 이번 프레임 이동량이 전달된다
    [RequireComponent(typeof(TransformMotor))]
    public class FallingPlatform : MonoBehaviour
    {
        #region Nested Types
        private enum EState
        {
            Idle,       // 제자리에서 기다림
            Waiting,    // 밟힌 뒤 떨어지기를 기다림
            Falling,    // 떨어지는 중
            Hidden,     // 사라져서 다시 생기기를 기다림
        }
        #endregion

        #region Serialized Fields
        [Header("Timing")]
        [Tooltip("올라선 뒤 떨어지기 시작할 때까지의 시간(초)")]
        [SerializeField, Min(0f)] private float _fallDelay = 0.6f;

        [Tooltip("사라진 뒤 다시 생기기까지의 시간(초)")]
        [SerializeField, Min(0f)] private float _respawnDelay = 2.5f;

        [Header("Fall")]
        [Tooltip("떨어지는 가속도")]
        [SerializeField, Min(0.1f)] private float _gravity = 12.0f;

        [Tooltip("이만큼 떨어지면 사라진다.")]
        [SerializeField, Min(0.1f)] private float _fallDistance = 8.0f;
        #endregion

        #region Private Fields
        private TransformMotor _motor;
        private PlatformBody _body;
        private Vector3 _startPosition;
        private EState _state = EState.Idle;
        private float _stateTime;
        private float _fallSpeed;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            _motor = GetComponent<TransformMotor>();
            PlatformBody.TryAddStepTrigger(gameObject);
        }

        private void Start()
        {
            _startPosition = transform.position;

            // 발판 위에 놓인 오브젝트까지 함께 사라지도록, 자식이 모두 붙은 뒤인 Start 에서 모은다
            _body = new PlatformBody(gameObject);
        }

        private void Update()
        {
            _stateTime += Time.deltaTime;

            switch (_state)
            {
                case EState.Waiting:
                    if (_stateTime >= _fallDelay)
                    {
                        _fallSpeed = 0f;
                        ChangeState(EState.Falling);
                    }
                    break;

                case EState.Falling:
                    _fallSpeed += _gravity * Time.deltaTime;
                    _motor.MoveTo(_startPosition + (Vector3.down * _fallDistance), _fallSpeed);

                    if ((_startPosition.y - transform.position.y) >= _fallDistance)
                    {
                        _body.SetSolid(false);
                        ChangeState(EState.Hidden);
                    }
                    return;

                case EState.Hidden:
                    if (_stateTime >= _respawnDelay)
                    {
                        transform.position = _startPosition;
                        _body.SetSolid(true);
                        ChangeState(EState.Idle);
                    }
                    break;
            }

            // TransformMotor.DeltaThisFrame은 MoveTo를 호출하지 않으면 직전 값이 남는다.
            // 멈춰 있는 동안 탑승자가 밀리지 않도록 제자리 이동으로 0을 만든다.
            _motor.MoveTo(transform.position, 0.0f);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_state != EState.Idle) return;
            if (!GimmickUtility.TryGetPlayer(other, out _)) return;

            ChangeState(EState.Waiting);
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// 코드로 붙인 직후 호출해 시간을 정한다.
        /// </summary>
        /// <param name="fallDelay">올라선 뒤 떨어지기 시작할 때까지의 시간(초)</param>
        /// <param name="respawnDelay">사라진 뒤 다시 생기기까지의 시간(초)</param>
        public void Initialize(float fallDelay, float respawnDelay)
        {
            _fallDelay = fallDelay;
            _respawnDelay = respawnDelay;
        }
        #endregion

        #region Private Methods
        private void ChangeState(EState state)
        {
            _state = state;
            _stateTime = 0f;
        }
        #endregion
    }
}
