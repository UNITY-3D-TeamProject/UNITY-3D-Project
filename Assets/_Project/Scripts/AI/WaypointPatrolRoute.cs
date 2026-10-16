using System.Collections.Generic;
using UnityEngine;

namespace AI
{
    /// <summary>
    /// 순찰 지점 목록을 들고 다음 지점을 골라 주는 범용 순찰 경로.
    /// AIController 와 같은 오브젝트에 붙어 있으면 AIController 가 순찰 지점을 여기서 받는다.
    /// 지점을 고르는 규칙만 맡고, 도착·정체 판단은 AIController 가 그대로 한다.
    /// </summary>
    public class WaypointPatrolRoute : MonoBehaviour
    {
        #region Serialized Fields
        [Header("Waypoints")]
        [Tooltip("임시 입력. 씬에 직접 놓은 순찰 지점들. 런타임에 SetWaypoints 로 통째로 바뀔 수 있다.")]
        [SerializeField] private Transform[] _waypointTransforms;
        #endregion

        #region Private Fields
        private readonly List<Vector3> _waypoints = new List<Vector3>();
        private readonly List<int> _bag = new List<int>();
        private int _lastIndex = -1;
        #endregion

        #region Properties
        /// <summary>현재 순찰 지점 목록 (읽기 전용).</summary>
        public IReadOnlyList<Vector3> Waypoints => _waypoints;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            if (_waypointTransforms == null) return;

            for (int i = 0; i < _waypointTransforms.Length; i++)
            {
                if (_waypointTransforms[i]) _waypoints.Add(_waypointTransforms[i].position);
            }

            _lastIndex = FindNearestIndex(transform.position);
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// 순찰 지점 목록을 통째로 바꾼다. 지금 서 있는 위치에서 가장 가까운 지점을 출발 지점으로 보고,
        /// 첫 목적지에서는 그 지점을 뺀다.
        /// </summary>
        /// <param name="waypoints">월드 좌표 목록. null 이면 빈 목록으로 본다.</param>
        public void SetWaypoints(IReadOnlyList<Vector3> waypoints)
        {
            _waypoints.Clear();
            _bag.Clear();

            if (waypoints != null)
            {
                for (int i = 0; i < waypoints.Count; i++)
                {
                    _waypoints.Add(waypoints[i]);
                }
            }

            _lastIndex = FindNearestIndex(transform.position);
        }

        /// <summary>
        /// 다음 순찰 지점을 고른다. 섞은 목록을 한 바퀴 다 돌 때까지 같은 지점이 다시 나오지 않으며,
        /// 새 바퀴의 첫 지점은 직전에 도착한 지점이 아니다.
        /// </summary>
        /// <param name="waypoint">고른 월드 좌표</param>
        /// <returns>목록이 비어 있지 않아 지점을 골랐으면 true</returns>
        public bool TryGetNextWaypoint(out Vector3 waypoint)
        {
            waypoint = Vector3.zero;

            if (_waypoints.Count == 0) return false;
            if (_bag.Count == 0) RefillBag();

            int last = _bag.Count - 1;
            int index = _bag[last];
            _bag.RemoveAt(last);

            _lastIndex = index;
            waypoint = _waypoints[index];
            return true;
        }

        /// <summary>
        /// 지정한 위치에서 직선거리가 가장 가까운 순찰 지점을 고른다.
        /// 고른 지점을 직전 지점으로 기록해, 이어지는 순찰의 첫 목적지와 겹치지 않게 한다.
        /// </summary>
        /// <param name="position">기준 월드 위치</param>
        /// <param name="waypoint">고른 월드 좌표</param>
        /// <returns>목록이 비어 있지 않아 지점을 골랐으면 true</returns>
        public bool TryGetNearestWaypoint(Vector3 position, out Vector3 waypoint)
        {
            waypoint = Vector3.zero;

            int index = FindNearestIndex(position);
            if (index < 0) return false;

            _lastIndex = index;
            waypoint = _waypoints[index];
            return true;
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// 모든 지점의 인덱스를 섞어 새 바퀴를 만든다. 꺼내는 쪽은 뒤에서부터라서,
        /// 맨 뒤가 직전 지점이면 맨 앞과 바꿔 연속 중복을 막는다.
        /// </summary>
        private void RefillBag()
        {
            int count = _waypoints.Count;

            for (int i = 0; i < count; i++)
            {
                _bag.Add(i);
            }

            for (int i = count - 1; i > 0; i--)
            {
                int swapIndex = Random.Range(0, i + 1);
                (_bag[i], _bag[swapIndex]) = (_bag[swapIndex], _bag[i]);
            }

            int last = count - 1;
            if (count > 1 && _bag[last] == _lastIndex)
            {
                (_bag[last], _bag[0]) = (_bag[0], _bag[last]);
            }
        }

        private int FindNearestIndex(Vector3 position)
        {
            int nearestIndex = -1;
            float nearestSqrDistance = float.MaxValue;

            for (int i = 0; i < _waypoints.Count; i++)
            {
                float sqrDistance = (_waypoints[i] - position).sqrMagnitude;
                if (sqrDistance >= nearestSqrDistance) continue;

                nearestSqrDistance = sqrDistance;
                nearestIndex = i;
            }

            return nearestIndex;
        }
        #endregion
    }
}
