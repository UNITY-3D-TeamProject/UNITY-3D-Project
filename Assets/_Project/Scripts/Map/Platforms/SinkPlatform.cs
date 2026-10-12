using UnityEngine;
using Map.Gimmicks;
using Movement;

namespace Map.Platforms
{
    /// <summary>
    /// 플레이어가 서 있는 동안 천천히 가라앉고, 내리면 제자리로 올라오는 발판.
    /// 짝(Counterpart)을 연결하면 저울처럼 움직인다: 한쪽에 서면 그쪽은 내려가고 반대쪽은 그만큼 올라간다.
    /// BoxCollider 가 붙은 발판에 붙인다. 올라섰는지 감지하는 트리거는 스스로 만든다.
    /// 탄 플레이어가 함께 움직이려면 RideablePlatform 도 붙인다.
    /// </summary>
    [DefaultExecutionOrder(-100)]   // 탑승자(PlatformRider)보다 먼저 움직여야 이번 프레임 이동량이 전달된다
    [RequireComponent(typeof(TransformMotor))]
    public class SinkPlatform : MonoBehaviour
    {
        #region Serialized Fields
        [Header("Movement")]
        [Tooltip("제자리에서 위아래로 움직일 수 있는 최대 거리")]
        [SerializeField, Min(0f)] private float _maxOffset = 2.0f;

        [Tooltip("가라앉는(또는 짝이 눌려 올라가는) 속도")]
        [SerializeField, Min(0f)] private float _sinkSpeed = 1.2f;

        [Tooltip("아무도 없을 때 제자리로 돌아오는 속도")]
        [SerializeField, Min(0f)] private float _returnSpeed = 1.5f;

        [Header("Scale Pair")]
        [Tooltip("저울처럼 함께 움직일 짝 (선택). 짝에 플레이어가 서 있으면 이 발판이 올라간다.")]
        [SerializeField] private SinkPlatform _counterpart;
        #endregion

        #region Private Fields
        private TransformMotor _motor;
        private Vector3 _startPosition;
        private float _offset;
        private Transform _occupant;
        #endregion

        #region Properties
        /// <summary>플레이어가 이 발판 위에 서 있는지 여부.</summary>
        public bool IsOccupied => _occupant != null;
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
            float step = Time.deltaTime;

            if (IsOccupied)
            {
                _offset = Mathf.Max(_offset - (_sinkSpeed * step), -_maxOffset);
            }
            else if ((_counterpart != null) && _counterpart.IsOccupied)
            {
                _offset = Mathf.Min(_offset + (_sinkSpeed * step), _maxOffset);
            }
            else
            {
                _offset = Mathf.MoveTowards(_offset, 0f, _returnSpeed * step);
            }

            // 속도 제한 없이 이번 프레임의 목표 위치로 바로 이동한다
            _motor.MoveTo(_startPosition + (Vector3.up * _offset), float.MaxValue);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!GimmickUtility.TryGetPlayer(other, out var player)) return;

            _occupant = player.transform;
        }

        private void OnTriggerExit(Collider other)
        {
            if (!GimmickUtility.TryGetPlayer(other, out var player)) return;
            if (player.transform != _occupant) return;

            _occupant = null;
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// 코드로 붙인 직후 호출해 움직임을 정한다.
        /// </summary>
        /// <param name="maxOffset">제자리에서 위아래로 움직일 수 있는 최대 거리</param>
        /// <param name="sinkSpeed">가라앉는 속도</param>
        public void Initialize(float maxOffset, float sinkSpeed)
        {
            _maxOffset = maxOffset;
            _sinkSpeed = sinkSpeed;
        }

        /// <summary>
        /// 저울처럼 함께 움직일 짝을 정한다. 양쪽 발판에 서로를 지정해야 한다.
        /// </summary>
        /// <param name="counterpart">짝이 되는 발판</param>
        public void SetCounterpart(SinkPlatform counterpart)
        {
            _counterpart = counterpart;
        }
        #endregion
    }
}
