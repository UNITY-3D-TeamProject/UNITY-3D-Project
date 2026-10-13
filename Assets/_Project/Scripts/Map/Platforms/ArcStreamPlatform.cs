using UnityEngine;
using Movement;

namespace Map.Platforms
{
    /// <summary>
    /// 원호를 따라 한 방향으로 흘러가다, 끝에 닿으면 깜빡이며 사라지고 처음 자리에서 다시 나타나는 발판.
    /// 같은 원호에 여러 개를 진행도만 다르게 두면 계속 흘러오는 발판 줄이 된다.
    /// 탄 플레이어가 함께 움직이려면 RideablePlatform 도 붙인다.
    /// </summary>
    [DefaultExecutionOrder(-100)]   // 탑승자(PlatformRider)보다 먼저 움직여야 이번 프레임 이동량이 전달된다
    [RequireComponent(typeof(TransformMotor))]
    public class ArcStreamPlatform : MonoBehaviour
    {
        #region Constants
        // 끝에 닿기 전 이 진행도부터 깜빡여 예고한다
        private const float WARNING_PROGRESS = 0.9f;

        // 예고로 깜빡이는 빠르기(초당 켜고 끄는 횟수)
        private const float WARNING_BLINK_RATE = 8f;
        #endregion

        #region Serialized Fields
        [Header("Arc")]
        [Tooltip("원호의 중심 (월드 좌표, 수평 위치만 쓴다)")]
        [SerializeField] private Vector3 _center;

        [Tooltip("원호의 반지름")]
        [SerializeField, Min(0.1f)] private float _radius = 30.0f;

        [Tooltip("시작 각도와 끝 각도(도). 반시계 방향이 양수다.")]
        [SerializeField] private Vector2 _angleRange = new Vector2(0f, 90f);

        [Tooltip("시작 높이와 끝 높이 (월드 좌표)")]
        [SerializeField] private Vector2 _heightRange;

        [Header("Movement")]
        [Tooltip("흘러가는 속도")]
        [SerializeField, Min(0f)] private float _speed = 2.5f;

        [Tooltip("처음 진행도 (0: 시작, 1: 끝)")]
        [SerializeField, Range(0f, 1f)] private float _progress;
        #endregion

        #region Private Fields
        private TransformMotor _motor;
        private PlatformBody _body;
        private bool _isVisible = true;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            _motor = GetComponent<TransformMotor>();
        }

        private void Start()
        {
            // 발판 위에 놓인 오브젝트까지 함께 깜빡이도록, 자식이 모두 붙은 뒤인 Start 에서 모은다
            _body = new PlatformBody(gameObject);
            transform.SetPositionAndRotation(GetPosition(_progress), GetRotation(_progress));
        }

        private void Update()
        {
            float arcLength = Mathf.Abs(_angleRange.y - _angleRange.x) * Mathf.Deg2Rad * _radius;
            if (arcLength <= 0f) return;

            _progress += (_speed * Time.deltaTime) / arcLength;

            if (_progress >= 1f)
            {
                // 처음 자리로 순간 이동한다. 그 이동량이 탑승자에게 전달되지 않도록 제자리 이동으로 0을 만든다.
                _progress -= Mathf.Floor(_progress);
                transform.SetPositionAndRotation(GetPosition(_progress), GetRotation(_progress));
                _motor.MoveTo(transform.position, 0.0f);
                SetVisible(true);
                return;
            }

            // 속도 제한 없이 이번 프레임의 목표 위치로 바로 이동한다
            _motor.MoveTo(GetPosition(_progress), float.MaxValue);
            transform.rotation = GetRotation(_progress);

            // 끝에 닿기 직전에는 깜빡인다
            bool isWarning = _progress >= WARNING_PROGRESS;
            SetVisible(!isWarning || (Mathf.FloorToInt(Time.time * WARNING_BLINK_RATE) % 2 == 0));
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// 코드로 붙인 직후 호출해 흘러갈 원호와 속도를 정한다.
        /// </summary>
        /// <param name="center">원호의 중심 (월드 좌표)</param>
        /// <param name="radius">원호의 반지름</param>
        /// <param name="angleRange">시작 각도와 끝 각도(도)</param>
        /// <param name="heightRange">시작 높이와 끝 높이 (월드 좌표)</param>
        /// <param name="speed">흘러가는 속도</param>
        /// <param name="progress">처음 진행도 (0 ~ 1)</param>
        public void Initialize(Vector3 center, float radius, Vector2 angleRange, Vector2 heightRange, float speed, float progress)
        {
            _center = center;
            _radius = radius;
            _angleRange = angleRange;
            _heightRange = heightRange;
            _speed = speed;
            _progress = Mathf.Repeat(progress, 1f);
        }
        #endregion

        #region Private Methods
        private Vector3 GetPosition(float progress)
        {
            float angle = Mathf.Lerp(_angleRange.x, _angleRange.y, progress) * Mathf.Deg2Rad;

            return new Vector3(
                _center.x + (Mathf.Cos(angle) * _radius),
                Mathf.Lerp(_heightRange.x, _heightRange.y, progress),
                _center.z + (Mathf.Sin(angle) * _radius));
        }

        // 원호의 접선(진행 방향)을 forward 로 둔다
        private Quaternion GetRotation(float progress)
        {
            float angle = Mathf.Lerp(_angleRange.x, _angleRange.y, progress) * Mathf.Deg2Rad;
            var tangent = new Vector3(-Mathf.Sin(angle), 0f, Mathf.Cos(angle));

            return Quaternion.LookRotation(tangent, Vector3.up);
        }

        private void SetVisible(bool isVisible)
        {
            if (isVisible == _isVisible) return;

            _isVisible = isVisible;
            _body.SetVisible(isVisible);
        }
        #endregion
    }
}
