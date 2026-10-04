using UnityEngine;
using UnityEngine.AI;

namespace AI
{
    /// <summary>
    /// 대상에게서 벌어지는 이동 지점을 NavMesh 위에서 고른다. 원거리 적의 후퇴·위치 변경용.
    /// 대상 반대 방향(0°)부터 측면(±90°)까지 정해진 순서로 후보를 검사해 처음 통과한 후보를 쓴다.
    /// 상태가 없는 계산 전용 클래스이며, 이동은 호출자가 AIController.MoveTo 로 한다.
    /// </summary>
    public static class RetreatPointFinder
    {
        #region Constants
        /// <summary>후보 지점을 NavMesh 위로 끌어당길 때 허용하는 최대 거리(m).</summary>
        private const float SAMPLE_DISTANCE = 2.0f;

        /// <summary>
        /// 경로 길이가 이동 거리의 이 배수를 넘으면 버린다.
        /// 벽을 크게 돌아 대상 쪽으로 지나가는 경로를 거르기 위함이다.
        /// </summary>
        private const float MAX_PATH_LENGTH_RATIO = 2.0f;
        #endregion

        #region Private Fields
        /// <summary>대상 반대 방향 기준 후보 각도(도). 앞쪽일수록 먼저 검사한다. ±90° 는 측면 이동이다.</summary>
        private static readonly float[] CandidateAngles = { 0.0f, 30.0f, -30.0f, 60.0f, -60.0f, 90.0f, -90.0f };

        /// <summary>경로 계산 결과 재사용 버퍼. 첫 사용 시 생성한다.</summary>
        private static NavMeshPath _path;
        #endregion

        #region Public Methods
        /// <summary>
        /// 대상에게서 벌어지면서 도달 가능하고, 도착 후 대상이 트여 보이는 지점을 찾는다.
        /// </summary>
        /// <param name="origin">현재 내 위치</param>
        /// <param name="targetPosition">대상 위치</param>
        /// <param name="moveDistance">한 번에 이동할 거리(m)</param>
        /// <param name="minDistanceFromTarget">도착 지점이 대상과 최소한 벌어져야 하는 수평 거리(m)</param>
        /// <param name="point">찾은 지점 (NavMesh 위)</param>
        /// <returns>조건을 만족하는 지점을 찾았으면 true</returns>
        public static bool TryFind(
            Vector3 origin,
            Vector3 targetPosition,
            float moveDistance,
            float minDistanceFromTarget,
            out Vector3 point)
        {
            point = origin;

            // 경로 계산의 출발점도 NavMesh 위여야 한다
            if (!NavMesh.SamplePosition(origin, out NavMeshHit originHit, SAMPLE_DISTANCE, NavMesh.AllAreas)) return false;

            Vector3 away = Vector3.ProjectOnPlane(origin - targetPosition, Vector3.up);
            // 대상과 위치가 겹치면 반대 방향이 없으므로 아무 방향이나 기준으로 삼는다
            away = (away.sqrMagnitude > Mathf.Epsilon) ? away.normalized : Vector3.forward;

            if (_path == null) _path = new NavMeshPath();

            foreach (float angle in CandidateAngles)
            {
                Vector3 direction = Quaternion.AngleAxis(angle, Vector3.up) * away;
                Vector3 candidate = originHit.position + (direction * moveDistance);

                if (!NavMesh.SamplePosition(candidate, out NavMeshHit candidateHit, SAMPLE_DISTANCE, NavMesh.AllAreas)) continue;
                if (GetHorizontalDistance(candidateHit.position, targetPosition) < minDistanceFromTarget) continue;
                if (!IsReachable(originHit.position, candidateHit.position, moveDistance * MAX_PATH_LENGTH_RATIO)) continue;

                // 도착하자마자 쏠 수 있도록 대상까지 막힘이 없어야 한다 (벽은 NavMesh 에서 뚫려 있다)
                if (NavMesh.Raycast(candidateHit.position, targetPosition, out _, NavMesh.AllAreas)) continue;

                point = candidateHit.position;
                return true;
            }

            return false;
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// 두 지점 사이에 완전한 경로가 있고 그 길이가 제한 이하인지 확인한다.
        /// </summary>
        /// <param name="from">출발 지점 (NavMesh 위)</param>
        /// <param name="to">도착 지점 (NavMesh 위)</param>
        /// <param name="maxPathLength">허용하는 최대 경로 길이(m)</param>
        /// <returns>도달 가능하면 true</returns>
        private static bool IsReachable(Vector3 from, Vector3 to, float maxPathLength)
        {
            if (!NavMesh.CalculatePath(from, to, NavMesh.AllAreas, _path)) return false;
            if (_path.status != NavMeshPathStatus.PathComplete) return false;

            Vector3[] corners = _path.corners;
            float length = 0.0f;
            for (int i = 1; i < corners.Length; i++)
            {
                length += Vector3.Distance(corners[i - 1], corners[i]);
            }

            return length <= maxPathLength;
        }

        /// <summary>
        /// 높이를 무시한 두 지점 사이의 거리를 구한다.
        /// </summary>
        private static float GetHorizontalDistance(Vector3 a, Vector3 b)
        {
            return Vector3.ProjectOnPlane(a - b, Vector3.up).magnitude;
        }
        #endregion
    }
}
