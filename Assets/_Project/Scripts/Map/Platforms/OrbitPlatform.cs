using UnityEngine;
using Movement;

namespace Map.Platforms
{
    /// <summary>
    /// 시작 위치를 중심으로 원을 그리며 도는 발판. 위치만 움직이므로 윗면은 항상 수평이다. (관람차의 칸처럼 움직인다)
    /// </summary>
    [DefaultExecutionOrder(-100)]   // 탑승자(PlatformRider)보다 먼저 움직여야 이번 프레임 이동량이 전달된다
    [RequireComponent(typeof(TransformMotor))]
    public class OrbitPlatform : MonoBehaviour
    {
        #region Serialized Fields
        [Header("Orbit")]
        [Tooltip("원을 그리는 면의 첫째 축 (월드 방향)")]
        [SerializeField] private Vector3 _axisA = Vector3.forward;

        [Tooltip("원을 그리는 면의 둘째 축 (월드 방향)")]
        [SerializeField] private Vector3 _axisB = Vector3.up;

        [Tooltip("원의 반지름")]
        [SerializeField, Min(0f)] private float _radius = 1.2f;

        [Tooltip("도는 빠르기(초당 라디안)")]
        [SerializeField] private float _speed = 1.5f;

        [Tooltip("시작 각도(라디안). 발판마다 다르게 주면 박자가 어긋난다.")]
        [SerializeField] private float _phase;
        #endregion

        #region Private Fields
        private TransformMotor _motor;
        private Vector3 _center;
        private bool _hasCenter;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            _motor = GetComponent<TransformMotor>();
        }

        private void Start()
        {
            if (!_hasCenter)
            {
                _center = transform.position;
            }
        }

        private void Update()
        {
            float angle = (Time.time * _speed) + _phase;
            Vector3 target = _center +
                (_axisA.normalized * (Mathf.Cos(angle) * _radius)) +
                (_axisB.normalized * (Mathf.Sin(angle) * _radius));

            // 속도 제한 없이 이번 프레임의 목표 위치로 바로 이동한다
            _motor.MoveTo(target, float.MaxValue);
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// 코드로 붙인 직후 호출해 도는 방식을 정한다.
        /// </summary>
        /// <param name="axisA">원을 그리는 면의 첫째 축 (월드 방향)</param>
        /// <param name="axisB">원을 그리는 면의 둘째 축 (월드 방향)</param>
        /// <param name="radius">원의 반지름</param>
        /// <param name="speed">도는 빠르기(초당 라디안)</param>
        /// <param name="phase">시작 각도(라디안)</param>
        public void Initialize(Vector3 axisA, Vector3 axisB, float radius, float speed, float phase)
        {
            _axisA = axisA;
            _axisB = axisB;
            _radius = radius;
            _speed = speed;
            _phase = phase;
        }

        /// <summary>
        /// 원의 중심을 직접 정한다. 호출하지 않으면 시작 위치가 중심이 된다.
        /// 여러 발판이 한 중심을 함께 돌게 할 때(관람차) 쓴다.
        /// </summary>
        /// <param name="center">원의 중심 (월드 좌표)</param>
        public void SetCenter(Vector3 center)
        {
            _center = center;
            _hasCenter = true;
        }
        #endregion
    }
}
