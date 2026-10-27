using UnityEngine;
using Core.Audio;
using Movement;

namespace Map.Gimmicks
{
    /// <summary>
    /// 놓인 자리(기본 상태)와 옮겨진 자리(작동 상태) 사이를 오가는 움직임. 이동과 회전을 함께 줄 수 있다.
    /// 예: 문·셔터(위로 이동), 솟아오르는 엄폐물, 옆에서 들어오는 발판, 90도 도는 벽·발판
    /// GimmickSwitch 의 이벤트나 DamageButton.OnPressed 에 Activate / Deactivate / Toggle 을 연결해서 쓴다.
    /// 회전은 오브젝트의 원점 기준이므로, 경첩처럼 돌리려면 빈 부모를 경첩 자리에 두고 부모에 붙인다.
    /// 탄 플레이어가 함께 움직이려면 RideablePlatform 도 붙인다(회전은 따라가지 않는다).
    /// </summary>
    [DefaultExecutionOrder(-100)]   // 탑승자(PlatformRider)보다 먼저 움직여야 이번 프레임 이동량이 전달된다
    [RequireComponent(typeof(TransformMotor))]
    public class StateMover : MonoBehaviour
    {
        #region Serialized Fields
        [Header("Movement")]
        [Tooltip("작동 상태에서 옮겨 갈 방향과 거리 (자신의 처음 방향 기준)")]
        [SerializeField] private Vector3 _moveOffset = new Vector3(0.0f, 3.0f, 0.0f);

        [Tooltip("작동 상태에서 돌아갈 각도 (자신의 처음 방향 기준, 도)")]
        [SerializeField] private Vector3 _rotateAngles;

        [Tooltip("한쪽 상태에서 다른 쪽 상태로 가는 데 걸리는 시간(초)")]
        [SerializeField, Min(0.05f)] private float _duration = 1.0f;

        [Header("Options")]
        [Tooltip("켜면 작동 상태로 시작한다.")]
        [SerializeField] private bool _isActivatedAtStart;

        [Header("Sound")]
        [Tooltip("작동 상태로 움직이기 시작할 때 낼 소리 (비우면 무음)")]
        [SerializeField] private SOAudioData _activateSound;

        [Tooltip("기본 상태로 돌아가기 시작할 때 낼 소리 (비우면 무음)")]
        [SerializeField] private SOAudioData _deactivateSound;

        [Tooltip("작동 상태에 도착했을 때 낼 소리 (예: 크러셔 내려찍기). 비우면 무음")]
        [SerializeField] private SOAudioData _arrivedSound;
        #endregion

        #region Private Fields
        private TransformMotor _motor;
        private Vector3 _basePosition;
        private Quaternion _baseRotation;
        private Vector3 _activePosition;
        private float _progress;
        private float _targetProgress;
        #endregion

        #region Properties
        /// <summary>작동 상태(또는 그쪽으로 가는 중)인지 여부.</summary>
        public bool IsActivated => _targetProgress > 0.5f;

        /// <summary>한쪽 상태에서 다른 쪽 상태로 가는 중인지 여부. 도착해 멈춰 있으면 false.</summary>
        public bool IsMoving => !Mathf.Approximately(_progress, _targetProgress);
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            _motor = GetComponent<TransformMotor>();
        }

        private void Start()
        {
            _basePosition = transform.position;
            _baseRotation = transform.rotation;
            _activePosition = _basePosition + (_baseRotation * _moveOffset);

            if (_isActivatedAtStart)
            {
                _progress = 1.0f;
                _targetProgress = 1.0f;
                ApplyPose();
            }
        }

        private void Update()
        {
            if (Mathf.Approximately(_progress, _targetProgress))
            {
                // TransformMotor.DeltaThisFrame은 MoveTo를 호출하지 않으면 직전 값이 남는다.
                // 멈춰 있는 동안 탑승자가 밀리지 않도록 제자리 이동으로 0을 만든다.
                _motor.MoveTo(transform.position, 0.0f);
                return;
            }

            _progress = Mathf.MoveTowards(_progress, _targetProgress, Time.deltaTime / _duration);
            ApplyPose();

            if (Mathf.Approximately(_progress, 1.0f) && Mathf.Approximately(_targetProgress, 1.0f))
            {
                GimmickUtility.PlaySfx(_arrivedSound, transform.position);
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (Application.isPlaying) return;

            Vector3 target = transform.position + (transform.rotation * _moveOffset);
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, target);
            Gizmos.DrawWireSphere(target, 0.2f);
        }
        #endregion

        #region Public Methods
        /// <summary>작동 상태로 움직인다. (UnityEvent로 연결 가능)</summary>
        public void Activate()
        {
            SetTarget(1.0f);
        }

        /// <summary>기본 상태로 돌아간다. (UnityEvent로 연결 가능)</summary>
        public void Deactivate()
        {
            SetTarget(0.0f);
        }

        /// <summary>반대 상태로 움직인다. (UnityEvent로 연결 가능)</summary>
        public void Toggle()
        {
            SetTarget(IsActivated ? 0.0f : 1.0f);
        }

        /// <summary>기본 상태로 즉시 되돌린다. 움직이는 시간 없이 바로 원위치에 놓는다.</summary>
        public void ResetImmediately()
        {
            _progress = 0.0f;
            _targetProgress = 0.0f;

            // ApplyPose 는 Time.deltaTime 으로 이동량을 정해 시간이 멈춰 있으면 움직이지 않으므로 위치를 직접 넣는다
            transform.SetPositionAndRotation(_basePosition, _baseRotation);
        }
        #endregion

        #region Private Methods
        // 목표 상태가 바뀔 때만 출발 소리를 낸다
        private void SetTarget(float targetProgress)
        {
            if (Mathf.Approximately(_targetProgress, targetProgress)) return;

            _targetProgress = targetProgress;
            GimmickUtility.PlaySfx((targetProgress > 0.5f) ? _activateSound : _deactivateSound, transform.position);
        }

        private void ApplyPose()
        {
            // 출발과 도착에서 부드럽게 속도를 줄인다
            float eased = Mathf.SmoothStep(0.0f, 1.0f, _progress);

            // 속도 제한 없이 이번 프레임의 목표 위치로 바로 이동한다
            _motor.MoveTo(Vector3.Lerp(_basePosition, _activePosition, eased), float.MaxValue);
            transform.rotation = _baseRotation * Quaternion.Euler(_rotateAngles * eased);
        }
        #endregion
    }
}
