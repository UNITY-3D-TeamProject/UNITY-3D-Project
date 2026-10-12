using UnityEngine;
using Map.Gimmicks;
using Movement;

namespace Map.Platforms
{
    /// <summary>
    /// 가만히 있다가 플레이어가 올라서면 정해진 만큼 이동하고, 잠시 뒤 제자리로 돌아오는 발판.
    /// BoxCollider 가 붙은 발판에 붙인다. 밟았는지 감지하는 트리거는 스스로 만든다.
    /// 탄 플레이어가 함께 움직이려면 RideablePlatform 도 붙인다.
    /// </summary>
    [DefaultExecutionOrder(-100)]   // 탑승자(PlatformRider)보다 먼저 움직여야 이번 프레임 이동량이 전달된다
    [RequireComponent(typeof(TransformMotor))]
    public class StepMovePlatform : MonoBehaviour
    {
        #region Nested Types
        private enum EState
        {
            Idle,       // 제자리에서 기다림
            Waiting,    // 밟힌 뒤 출발을 기다림
            MovingOut,  // 목적지로 가는 중
            Resting,    // 목적지에서 기다림
            Returning,  // 제자리로 돌아오는 중
        }
        #endregion

        #region Constants
        private const float ARRIVE_DISTANCE = 0.01f;
        #endregion

        #region Serialized Fields
        [Header("Movement")]
        [Tooltip("올라섰을 때 이동할 방향과 거리 (월드 기준)")]
        [SerializeField] private Vector3 _offset = new Vector3(0f, 0f, 3f);

        [Tooltip("이동 속도")]
        [SerializeField, Min(0.1f)] private float _speed = 3.0f;

        [Header("Timing")]
        [Tooltip("올라선 뒤 출발할 때까지의 시간(초)")]
        [SerializeField, Min(0f)] private float _startDelay = 0.3f;

        [Tooltip("목적지에 도착한 뒤 돌아오기 시작할 때까지의 시간(초)")]
        [SerializeField, Min(0f)] private float _returnDelay = 1.5f;
        #endregion

        #region Private Fields
        private TransformMotor _motor;
        private Vector3 _startPosition;
        private EState _state = EState.Idle;
        private float _stateTime;
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
        }

        private void Update()
        {
            _stateTime += Time.deltaTime;

            switch (_state)
            {
                case EState.Waiting:
                    if (_stateTime >= _startDelay) ChangeState(EState.MovingOut);
                    break;

                case EState.MovingOut:
                    if (MoveTowards(_startPosition + _offset)) ChangeState(EState.Resting);
                    return;

                case EState.Resting:
                    if (_stateTime >= _returnDelay) ChangeState(EState.Returning);
                    break;

                case EState.Returning:
                    if (MoveTowards(_startPosition)) ChangeState(EState.Idle);
                    return;
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
        /// 코드로 붙인 직후 호출해 움직임을 정한다.
        /// </summary>
        /// <param name="offset">올라섰을 때 이동할 방향과 거리 (월드 기준)</param>
        /// <param name="speed">이동 속도</param>
        /// <param name="startDelay">올라선 뒤 출발할 때까지의 시간(초)</param>
        /// <param name="returnDelay">도착한 뒤 돌아오기 시작할 때까지의 시간(초)</param>
        public void Initialize(Vector3 offset, float speed, float startDelay, float returnDelay)
        {
            _offset = offset;
            _speed = speed;
            _startDelay = startDelay;
            _returnDelay = returnDelay;
        }
        #endregion

        #region Private Methods
        // target 으로 이번 프레임만큼 움직인다. 도착했으면 true.
        private bool MoveTowards(Vector3 target)
        {
            _motor.MoveTo(target, _speed);
            return Vector3.Distance(transform.position, target) < ARRIVE_DISTANCE;
        }

        private void ChangeState(EState state)
        {
            _state = state;
            _stateTime = 0f;
        }
        #endregion
    }
}
