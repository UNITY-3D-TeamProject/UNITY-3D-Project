using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace AI
{
    /// <summary>
    /// 위협에게서 달아날 지점을 내 주변 NavMesh 위에서 무작위로 뽑은 후보 중에서 고른다. 미로처럼 직선 후퇴가 통하지 않는 곳용.
    /// 경로 길이 제한, 위협 근처를 지나지 않음, 지금보다 멀어짐을 모두 통과한 후보 중
    /// 위협에게서 먼 상위 절반에서 무작위로 하나를 쓴다. 통과한 후보가 없으면 "멀어짐" 조건만 빼고 다시 고른다.
    /// 웨이포인트를 후보로 쓰지 않는 이유: 미로에는 웨이포인트를 많이 놓을 수 없어 경로 제한 안에 후보가 거의 없다.
    /// 상태가 없는 계산 전용 클래스이며, 이동은 호출자가 AIController.MoveTo 로 한다.
    /// 비용: 후보마다 경로 계산 1회. 도망 시작·도착·정체, 그리고 남은 경로가 위협 근처를 지나게 됐을 때만 부른다.
    /// </summary>
    public static class FleePointFinder
    {
        #region Constants
        /// <summary>후보 지점을 NavMesh 위로 끌어당길 때 허용하는 최대 거리(m).</summary>
        private const float SAMPLE_DISTANCE = 2.0f;

        /// <summary>한 번 찾을 때 뽑는 후보 수. 미로에서는 벽 너머 후보가 경로 길이 제한에 많이 걸린다.</summary>
        private const int CANDIDATE_COUNT = 32;

        /// <summary>위협이 이미 회피 반경 안에 있을 때, 경로가 위협에 이만큼(현재 거리 대비)보다 가까워지지 않아야 한다.</summary>
        private const float CURRENT_DISTANCE_RATIO = 0.9f;
        #endregion

        #region Private Fields
        /// <summary>경로 계산 결과 재사용 버퍼. 첫 사용 시 생성한다.</summary>
        private static NavMeshPath _path;

        /// <summary>모든 조건을 통과한 후보. (위협과의 수평 거리, 지점)</summary>
        private static readonly List<(float distance, Vector3 point)> StrictCandidates = new List<(float, Vector3)>();

        /// <summary>"멀어짐" 조건만 실패한 후보.</summary>
        private static readonly List<(float distance, Vector3 point)> RelaxedCandidates = new List<(float, Vector3)>();
        #endregion

        #region Public Methods
        /// <summary>
        /// 위협에게서 달아날 지점을 찾는다.
        /// </summary>
        /// <param name="origin">현재 내 위치</param>
        /// <param name="threatPosition">위협(플레이어) 위치</param>
        /// <param name="maxPathLength">허용하는 최대 경로 길이(m). 후보도 이 반경 안에서 뽑는다</param>
        /// <param name="avoidRadius">경로가 위협에 이 거리 안으로 들어가면 버린다(m)</param>
        /// <param name="point">고른 지점 (NavMesh 위)</param>
        /// <returns>달아날 지점을 찾았으면 true. 둘 다 비어 있으면 false</returns>
        public static bool TryFind(
            Vector3 origin,
            Vector3 threatPosition,
            float maxPathLength,
            float avoidRadius,
            out Vector3 point)
        {
            point = origin;

            if (!NavMesh.SamplePosition(origin, out NavMeshHit originHit, SAMPLE_DISTANCE, NavMesh.AllAreas)) return false;

            if (_path == null) _path = new NavMeshPath();

            StrictCandidates.Clear();
            RelaxedCandidates.Clear();

            float currentDistance = GetHorizontalDistance(originHit.position, threatPosition);
            float passThreshold = GetPassThreshold(currentDistance, avoidRadius);

            for (int i = 0; i < CANDIDATE_COUNT; i++)
            {
                // 직선 거리가 경로 길이 제한보다 멀면 통과할 수 없으므로 그 반경 안에서만 뽑는다.
                // 반지름을 균등하게 뽑아 가까운 쪽에 후보를 더 둔다. 미로에서는 먼 후보일수록 경로가 제한을 넘기 쉽다
                float angle = Random.value * Mathf.PI * 2.0f;
                float radius = Random.value * maxPathLength;
                Vector3 candidate = originHit.position + new Vector3(Mathf.Cos(angle) * radius, 0.0f, Mathf.Sin(angle) * radius);

                if (!NavMesh.SamplePosition(candidate, out NavMeshHit candidateHit, SAMPLE_DISTANCE, NavMesh.AllAreas)) continue;
                if (!IsPathValid(originHit.position, candidateHit.position, threatPosition, maxPathLength, passThreshold)) continue;

                float candidateDistance = GetHorizontalDistance(candidateHit.position, threatPosition);
                if (candidateDistance > currentDistance)
                {
                    StrictCandidates.Add((candidateDistance, candidateHit.position));
                }
                else
                {
                    RelaxedCandidates.Add((candidateDistance, candidateHit.position));
                }
            }

            List<(float distance, Vector3 point)> chosen = (StrictCandidates.Count > 0) ? StrictCandidates : RelaxedCandidates;
            if (chosen.Count == 0) return false;

            // 위협에게서 먼 순으로 정렬해 상위 절반에서 무작위로 고른다
            chosen.Sort((a, b) => b.distance.CompareTo(a.distance));
            int topCount = (chosen.Count + 1) / 2;
            point = chosen[Random.Range(0, topCount)].point;
            return true;
        }

        /// <summary>
        /// 내 위치에서 이어지는 남은 경로가 위협 근처를 지나는지 확인한다. 기준 거리는 TryFind 와 같다.
        /// 도망 중 위협이 경로 쪽으로 움직였는지 볼 때 쓴다.
        /// </summary>
        /// <param name="origin">현재 내 위치</param>
        /// <param name="corners">지금 따라가는 경로의 코너. 0번은 이미 지나온 위치일 수 있어 쓰지 않는다</param>
        /// <param name="threatPosition">위협(플레이어) 위치</param>
        /// <param name="avoidRadius">경로가 위협에 이 거리 안으로 들어가면 막힌 것으로 본다(m)</param>
        /// <returns>남은 경로의 어느 구간이 위협에 기준 거리 안으로 들어가면 true</returns>
        public static bool IsPathBlocked(
            Vector3 origin,
            Vector3[] corners,
            Vector3 threatPosition,
            float avoidRadius)
        {
            float passThreshold = GetPassThreshold(GetHorizontalDistance(origin, threatPosition), avoidRadius);

            Vector3 from = origin;
            for (int i = 1; i < corners.Length; i++)
            {
                if (GetSegmentDistance(from, corners[i], threatPosition) < passThreshold) return true;
                from = corners[i];
            }

            return false;
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// 경로가 위협에 이 거리보다 가까워지면 안 되는 기준 거리를 구한다.
        /// 위협이 이미 반경 안에 있으면 모든 경로의 출발점이 반경 안이므로, 기준을 "지금보다 가까워지지 않음"으로 낮춘다.
        /// </summary>
        private static float GetPassThreshold(float currentDistance, float avoidRadius)
        {
            return Mathf.Min(avoidRadius, currentDistance * CURRENT_DISTANCE_RATIO);
        }

        /// <summary>
        /// 완전한 경로가 있고, 길이가 제한 이하이며, 어느 구간도 위협에 기준 거리 안으로 들어가지 않는지 확인한다.
        /// </summary>
        private static bool IsPathValid(
            Vector3 from,
            Vector3 to,
            Vector3 threatPosition,
            float maxPathLength,
            float passThreshold)
        {
            if (!NavMesh.CalculatePath(from, to, NavMesh.AllAreas, _path)) return false;
            if (_path.status != NavMeshPathStatus.PathComplete) return false;

            Vector3[] corners = _path.corners;
            float length = 0.0f;
            for (int i = 1; i < corners.Length; i++)
            {
                length += Vector3.Distance(corners[i - 1], corners[i]);
                if (length > maxPathLength) return false;
                if (GetSegmentDistance(corners[i - 1], corners[i], threatPosition) < passThreshold) return false;
            }

            return true;
        }

        /// <summary>
        /// 높이를 무시한 선분 ab 와 점 p 사이의 최단 거리를 구한다.
        /// </summary>
        private static float GetSegmentDistance(Vector3 a, Vector3 b, Vector3 p)
        {
            Vector2 start = new Vector2(a.x, a.z);
            Vector2 segment = new Vector2(b.x, b.z) - start;
            Vector2 toPoint = new Vector2(p.x, p.z) - start;

            float sqrLength = segment.sqrMagnitude;
            float t = (sqrLength > Mathf.Epsilon) ? Mathf.Clamp01(Vector2.Dot(toPoint, segment) / sqrLength) : 0.0f;

            return (toPoint - (segment * t)).magnitude;
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
