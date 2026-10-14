using UnityEngine;
using Map.Platforms;

namespace Map.Gimmicks
{
    /// <summary>
    /// 사격 표적이 맞아서 기믹이 작동하는 동안 표적 모양을 천천히 돌린다.
    /// 부모 쪽에 GimmickSwitch 가 있으면 스위치가 처음 상태가 아닌 동안 돌고, 처음 상태로 돌아오면 감속해 앞면 각도에서 멈춘다.
    /// 스위치가 없고 맞았을 때 이동 발판(WaypointPlatform)을 움직이면, 발판이 이동하는 동안만 돈다.
    /// 둘 다 없으면 맞은 뒤 Fallback Spin Time 동안 돈다.
    /// DamageButton 이 있는 표적의 루트에 붙인다.
    /// </summary>
    [RequireComponent(typeof(DamageButton))]
    public class ShootTargetSpinner : MonoBehaviour
    {
        #region Constants
        // 멈출 때 맞출 각도의 간격. 표적이 앞뒤 대칭이라 180도마다 앞면이 보인다.
        private const float STOP_ANGLE_STEP = 180.0f;

        // 멈추는 각도까지 남은 거리가 이보다 작으면 그 각도에 딱 맞춘다
        private const float SNAP_ANGLE = 0.5f;

        // 멈추는 각도까지 갈 때 최소한 유지하는 빠르기 (초당 도)
        private const float MIN_SETTLE_SPEED = 15.0f;
        #endregion

        #region Serialized Fields
        [Header("References")]
        [Tooltip("돌릴 모양 (표적 모델). 콜라이더는 돌리지 않는다.")]
        [SerializeField] private Transform _spinTarget;

        [Header("Spin")]
        [Tooltip("최고 회전 빠르기 (초당 도)")]
        [SerializeField, Min(0.0f)] private float _maxSpeed = 120.0f;

        [Tooltip("빨라지고 느려지는 정도 (초당 도/초)")]
        [SerializeField, Min(0.01f)] private float _acceleration = 180.0f;

        [Tooltip("스위치도 이동 발판도 없는 표적이 맞은 뒤 도는 시간(초)")]
        [SerializeField, Min(0.0f)] private float _fallbackSpinTime = 5.0f;
        #endregion

        #region Private Fields
        private DamageButton _button;
        private GimmickSwitch _gimmickSwitch;
        private WaypointPlatform _waypointPlatform;
        private Quaternion _baseRotation;
        private float _angle;
        private float _speed;
        private float _spinRemaining;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            _button = GetComponent<DamageButton>();
            _gimmickSwitch = GetComponentInParent<GimmickSwitch>(true);

            if (_gimmickSwitch == null)
            {
                _waypointPlatform = FindPressedWaypointPlatform();
            }

            Debug.Assert(_spinTarget != null, $"[{name}] 돌릴 모양(Spin Target)이 연결되지 않았습니다.");
            _baseRotation = _spinTarget.localRotation;
        }

        private void OnEnable()
        {
            _button.OnPressed.AddListener(HandlePressed);
        }

        private void OnDisable()
        {
            _button.OnPressed.RemoveListener(HandlePressed);
        }

        private void Update()
        {
            if (_spinTarget == null) return;

            float deltaTime = Time.deltaTime;
            _spinRemaining = Mathf.Max(_spinRemaining - deltaTime, 0.0f);

            if (ShouldSpin())
            {
                _speed = Mathf.MoveTowards(_speed, _maxSpeed, _acceleration * deltaTime);
                _angle = Mathf.Repeat(_angle + (_speed * deltaTime), 360.0f);
            }
            else
            {
                Settle(deltaTime);
            }

            _spinTarget.localRotation = _baseRotation * Quaternion.Euler(0.0f, _angle, 0.0f);
        }
        #endregion

        #region Private Methods
        private void HandlePressed()
        {
            _spinRemaining = _fallbackSpinTime;
        }

        // 스위치가 있으면 처음 상태가 아닌 동안, 이동 발판이면 이동하는 동안, 둘 다 없으면 맞은 뒤 정해진 시간 동안 돈다
        private bool ShouldSpin()
        {
            if (_gimmickSwitch != null) return _gimmickSwitch.IsOn != _gimmickSwitch.IsOnAtStart;
            if (_waypointPlatform != null) return _waypointPlatform.IsMoving;

            return _spinRemaining > 0.0f;
        }

        // 맞았을 때 실행하도록 연결된 대상 가운데 이동 발판을 찾는다. 없으면 null.
        private WaypointPlatform FindPressedWaypointPlatform()
        {
            for (int i = 0; i < _button.OnPressed.GetPersistentEventCount(); i++)
            {
                if ((_button.OnPressed.GetPersistentTarget(i) is Component target) &&
                    target.TryGetComponent(out WaypointPlatform platform))
                {
                    return platform;
                }
            }

            return null;
        }

        // 다음 앞면 각도까지 감속하며 가서 멈춘다
        private void Settle(float deltaTime)
        {
            float stopAngle = Mathf.Ceil(_angle / STOP_ANGLE_STEP) * STOP_ANGLE_STEP;
            float remaining = stopAngle - _angle;

            if (remaining < SNAP_ANGLE)
            {
                _angle = Mathf.Repeat(stopAngle, 360.0f);
                _speed = 0.0f;
                return;
            }

            // 남은 거리 안에서 멈출 수 있는 빠르기를 넘지 않게 줄인다
            float stoppableSpeed = Mathf.Sqrt(2.0f * _acceleration * remaining);
            _speed = Mathf.Max(Mathf.Min(_speed, stoppableSpeed), MIN_SETTLE_SPEED);
            _angle = Mathf.Min(_angle + (_speed * deltaTime), stopAngle);
        }
        #endregion
    }
}
