using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Core.Audio;
using Map.Gimmicks;
using Movement;

namespace Map.Platforms
{
    public enum EMovingPlatformType
    {
        Once,       // 지정된 웨이포인트의 끝에 도달하면 정지
        PingPong,   // 끝에 도달하면 역방향으로 돌아가며 반복 이동
        Loop        // 끝에 도달하면 처음 웨이포인트로 바로 넘어가서 반복
    }

    [DefaultExecutionOrder(-100)]   // 탑승자(PlatformRider)보다 먼저 움직여야 이번 프레임 이동량이 전달된다
    [RequireComponent(typeof(TransformMotor))]
    public class WaypointPlatform : MonoBehaviour
    {
        #region Serialized Fields
        [Header("Movement Settings")]
        [Tooltip("플랫폼의 이동 방식을 설정합니다.")]
        [SerializeField] private EMovingPlatformType _platformType = EMovingPlatformType.PingPong;
        
        [Tooltip("이동 속도")]
        [SerializeField] private float _moveSpeed = 5.0f;
        
        [Tooltip("시작 시 자동으로 이동할지 여부. 꺼져있으면 상호작용으로 시작해야 합니다.")]
        [SerializeField] private bool _autoStart = true;
        
        [Tooltip("각 웨이포인트에 도착했을 때 대기하는 시간")]
        [SerializeField] private float _delayAtWaypoint = 0.5f;

        [Header("Waypoints")]
        [Tooltip("플랫폼이 이동할 경로가 되는 Transform들")]
        [SerializeField] private List<Transform> _waypoints;

        [Header("Sound")]
        [Tooltip("MoveToNextPoint로 이동을 시작할 때 낼 소리 (비우면 무음)")]
        [SerializeField] private SOAudioData _moveSound;
        #endregion

        #region Private Fields
        private int  _currentWaypointIndex = 0;
        private bool _isMoving;
        private bool _isForward = true;
        private bool _isWaiting;
        private bool _isResting;
        private Vector3 _restLocalPosition;
        private TransformMotor _motor;
        #endregion

        #region Properties
        /// <summary>지금 웨이포인트를 향해 이동 중인지 여부. (도착 후 대기 중도 포함)</summary>
        public bool IsMoving => _isMoving;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            // 이전에 배치된 발판에는 TransformMotor가 없을 수 있다
            if (!TryGetComponent(out _motor))
            {
                _motor = gameObject.AddComponent<TransformMotor>();
            }
        }

        private void Start()
        {
            if (_waypoints == null || _waypoints.Count < 2)
            {
                Debug.LogWarning($"[{name}] WaypointPlatform requires at least 2 waypoints.");
                return;
            }

            // 첫 번째 웨이포인트로 시작 위치 초기화
            transform.position = _waypoints[0].position;
            _currentWaypointIndex = 1; // 다음 이동 목표
            
            if (_autoStart)
            {
                StartMoving();
            }
        }

        private void Update()
        {
            if (!_isMoving || _isWaiting || _waypoints.Count < 2)
            {
                HoldRestPosition();
                return;
            }

            _isResting = false;
            MoveTowardsWaypoint();
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// 플랫폼 이동을 시작합니다. (상호작용 버튼 등에서 UnityEvent로 연결하여 호출 가능)
        /// </summary>
        public void StartMoving()
        {
            if (_waypoints == null || _waypoints.Count < 2) return;
            _isMoving = true;
        }

        /// <summary>
        /// 플랫폼 이동을 일시 정지합니다.
        /// </summary>
        public void StopMoving()
        {
            _isMoving = false;
        }

        /// <summary>
        /// 한 웨이포인트씩 이동하게 하는 수동 이동 트리거입니다. (토글 버튼 등에 활용)
        /// </summary>
        public void MoveToNextPoint()
        {
            if (_isWaiting || _waypoints.Count < 2) return;

            _isMoving = true;
            GimmickUtility.PlaySfx(_moveSound, transform.position);
            
            // Once 타입인데 이미 마지막 지점이라면 초기 위치로 리셋 후 다시 이동 가능하게 처리
            if (_platformType == EMovingPlatformType.Once && _currentWaypointIndex >= _waypoints.Count - 1)
            {
                if (Vector3.Distance(transform.position, _waypoints[_waypoints.Count - 1].position) < 0.01f)
                {
                    // 끝에 도달한 상태면 인덱스 초기화하고 처음부터 이동
                    _currentWaypointIndex = 0;
                }
            }
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// 멈춘 순간의 로컬 위치를 기억해 두고, 멈춰 있는 동안 매 프레임 그 자리로 되돌린다.
        /// TransformMotor.DeltaThisFrame은 MoveTo를 호출하지 않으면 직전 값이 남으므로 멈춰 있어도 호출해 0을 만든다.
        /// 현재 위치를 그대로 다시 쓰면 회전·멀리 떨어진 부모 아래에서 변환 오차가 쌓여 발판이 밀리므로, 기억한 위치를 기준으로 쓴다.
        /// </summary>
        private void HoldRestPosition()
        {
            if (!_isResting)
            {
                _restLocalPosition = transform.localPosition;
                _isResting = true;
            }

            Vector3 restPosition = (transform.parent != null) ? transform.parent.TransformPoint(_restLocalPosition) : _restLocalPosition;
            _motor.MoveTo(restPosition, float.MaxValue);
        }

        private void MoveTowardsWaypoint()
        {
            Transform targetWaypoint = _waypoints[_currentWaypointIndex];
            _motor.MoveTo(targetWaypoint.position, _moveSpeed);

            if (Vector3.Distance(transform.position, targetWaypoint.position) < 0.01f)
            {
                StartCoroutine(CoWaitAtWaypoint());
            }
        }
        #endregion

        #region Coroutines
        private IEnumerator CoWaitAtWaypoint()
        {
            _isWaiting = true;

            if (_delayAtWaypoint > 0f)
            {
                yield return new WaitForSeconds(_delayAtWaypoint);
            }

            UpdateNextWaypointIndex();

            _isWaiting = false;

            // 자동 시작 모드가 아니거나, Once 모드에서 특정 포인트 도달 시 멈춰야 하는 경우
            if (!_autoStart)
            {
                // 수동 모드인 경우 1칸 이동 후 멈춤 (MoveToNextPoint 활용 시 유용)
                _isMoving = false;
            }
            else if (_platformType == EMovingPlatformType.Once && _currentWaypointIndex >= _waypoints.Count)
            {
                // Once 모드인데 끝까지 도달했으면 정지
                _isMoving = false;
                _currentWaypointIndex = _waypoints.Count - 1; // 인덱스 초과 방지
            }
        }
        
        private void UpdateNextWaypointIndex()
        {
            if (_platformType == EMovingPlatformType.Once)
            {
                if (_currentWaypointIndex < _waypoints.Count - 1)
                {
                    _currentWaypointIndex++;
                }
                else
                {
                    _currentWaypointIndex = _waypoints.Count; // 종료 상태 표기용
                }
            }
            else if (_platformType == EMovingPlatformType.PingPong)
            {
                if (_isForward)
                {
                    if (_currentWaypointIndex < _waypoints.Count - 1)
                        _currentWaypointIndex++;
                    else
                    {
                        _isForward = false;
                        _currentWaypointIndex--;
                    }
                }
                else
                {
                    if (_currentWaypointIndex > 0)
                        _currentWaypointIndex--;
                    else
                    {
                        _isForward = true;
                        _currentWaypointIndex++;
                    }
                }
            }
            else if (_platformType == EMovingPlatformType.Loop)
            {
                _currentWaypointIndex = (_currentWaypointIndex + 1) % _waypoints.Count;
            }
        }
        #endregion
    }
}
