using System.Collections.Generic;
using UnityEngine;
using Map.Gimmicks;

namespace Map.Platforms
{
    /// <summary>
    /// WaypointPlatform 을 "플레이어가 올라타면 반대편으로 데려다 주는 셔틀"로 쓰게 한다.
    /// 플레이어가 올라서면 잠시 뒤 다음 웨이포인트로 한 번 이동하고, 아무도 없이 집(첫 웨이포인트)이 아닌 곳에 서 있으면
    /// 잠시 뒤 혼자 집으로 돌아온다. (Is One Way 를 켜면 첫 운행 후 도착 위치에 고정)
    /// WaypointPlatform 의 Auto Start 는 꺼 둔다.
    /// 밟았는지 감지하는 트리거는 스스로 만든다. (발판에 BoxCollider 필요)
    /// </summary>
    [RequireComponent(typeof(WaypointPlatform))]
    public class RideTriggeredWaypoint : MonoBehaviour
    {
        #region Constants
        private const float ARRIVE_DISTANCE = 0.05f;
        #endregion

        #region Serialized Fields
        [Tooltip("셔틀이 기다리는 집 위치 (보통 첫 웨이포인트)")]
        [SerializeField] private Transform _homePoint;

        [Tooltip("올라선 뒤 출발할 때까지의 시간(초)")]
        [SerializeField, Min(0.0f)] private float _startDelay = 0.4f;

        [Tooltip("아무도 없이 집이 아닌 곳에 서 있을 때 집으로 돌아가기까지의 시간(초)")]
        [SerializeField, Min(0.0f)] private float _returnDelay = 4.0f;

        [Tooltip("켜면 플레이어가 탑승해 출발한 첫 운행의 도착 위치에 고정된다.")]
        [SerializeField] private bool _isOneWay;

        [Header("Visuals (선택)")]
        [Tooltip("기다리는 동안 켜 둘 오브젝트 (예: 올라타라는 안내)")]
        [SerializeField] private GameObject _showWhileIdle;

        [Tooltip("움직이는 동안 켜 둘 오브젝트 (예: 진행 방향 화살표)")]
        [SerializeField] private GameObject _showWhileMoving;
        #endregion

        #region Private Fields
        private readonly HashSet<Collider> _riders = new HashSet<Collider>();
        private WaypointPlatform _platform;
        private BoxCollider _stepTrigger;
        private float _boardTime = -1.0f;
        private float _emptyTime;
        private bool _hasDeparted;
        private bool _isLocked;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            _platform = GetComponent<WaypointPlatform>();
            if (PlatformBody.TryAddStepTrigger(gameObject))
            {
                // 방금 붙은 트리거 (마지막 BoxCollider)
                BoxCollider[] boxes = GetComponents<BoxCollider>();
                _stepTrigger = boxes[boxes.Length - 1];
            }
        }

        private void Update()
        {
            // 순간이동(리스폰 등)으로 벗어나면 OnTriggerExit 가 오지 않으므로 겹침을 직접 확인한다
            _riders.RemoveWhere(rider => (rider == null) ||
                                         !rider.enabled ||
                                         ((_stepTrigger != null) && !rider.bounds.Intersects(_stepTrigger.bounds)));

            // 편도: 탑승 후 출발했다면 도착 시 플레이어가 내려도 그 자리에 고정한다.
            if (_isOneWay && _hasDeparted && !_platform.IsMoving)
            {
                _isLocked = true;
                _hasDeparted = false;
            }

            if (_isLocked)
            {
                // 고정됨
            }
            else if (!_platform.IsMoving)
            {
                if (_riders.Count > 0)
                {
                    // 올라선 뒤 잠깐 기다렸다 출발한다. 음수면 도착한 뒤에도 계속 타고 있는 중이라 다시 출발하지 않는다.
                    if (_boardTime >= 0.0f)
                    {
                        _boardTime += Time.deltaTime;
                        if (_boardTime >= _startDelay)
                        {
                            _platform.MoveToNextPoint();
                            _boardTime = -1.0f;
                            _hasDeparted = true;
                        }
                    }
                }
                else if (!IsAtHome())
                {
                    _emptyTime += Time.deltaTime;
                    if (_emptyTime >= _returnDelay)
                    {
                        _platform.MoveToNextPoint();
                        _emptyTime = 0.0f;
                    }
                }
            }
            else
            {
                _emptyTime = 0.0f;
            }

            RefreshVisuals();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!GimmickUtility.TryGetPlayer(other, out _)) return;

            // 도착해서 서 있는 동안 계속 타고 있으면 다시 출발하지 않도록, 새로 올라섰을 때만 시간을 잰다
            if (_riders.Count == 0)
            {
                _boardTime = 0.0f;
            }
            _riders.Add(other);
        }

        private void OnTriggerExit(Collider other)
        {
            _riders.Remove(other);
            if (_riders.Count == 0)
            {
                _boardTime = -1.0f;
            }
        }
        #endregion

        #region Private Methods
        private bool IsAtHome()
        {
            return (_homePoint == null) || (Vector3.Distance(transform.position, _homePoint.position) < ARRIVE_DISTANCE);
        }

        private void RefreshVisuals()
        {
            bool isMoving = _platform.IsMoving;
            bool shouldShowIdle = !isMoving && !_isLocked;

            if ((_showWhileIdle != null) && (_showWhileIdle.activeSelf != shouldShowIdle))
            {
                _showWhileIdle.SetActive(shouldShowIdle);
            }

            if ((_showWhileMoving != null) && (_showWhileMoving.activeSelf != isMoving))
            {
                _showWhileMoving.SetActive(isMoving);
            }
        }
        #endregion
    }
}
