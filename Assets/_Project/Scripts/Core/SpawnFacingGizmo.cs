using UnityEngine;

namespace Core
{
    // 플레이어가 생성·복귀하는 지점의 위치와 바라볼 방향을 씬 뷰에 화살표로 그린다.
    public static class SpawnFacingGizmo
    {
        private const float ARROW_LENGTH = 1.5f;
        private const float ARROW_HEAD_LENGTH = 0.4f;
        private const float ARROW_HEAD_ANGLE = 30f;
        private const float POINT_RADIUS = 0.15f;

        // point 위치에 작은 구를, point의 수평 정면(파란 Z축)으로 화살표를 그린다. 플레이어는 이 방향을 보고 생성된다.
        // [호출] CheckpointTrigger.OnDrawGizmos, PlayerSpawner.OnDrawGizmos에서 호출한다.
        public static void Draw(Transform point, Color color)
        {
            if (point == null)
            {
                return;
            }

            Vector3 forward = Vector3.ProjectOnPlane(point.forward, Vector3.up).normalized;
            if (forward == Vector3.zero)
            {
                return;
            }

            Vector3 start = point.position;
            Vector3 end = start + (forward * ARROW_LENGTH);
            Vector3 left = Quaternion.AngleAxis(180f - ARROW_HEAD_ANGLE, Vector3.up) * forward;
            Vector3 right = Quaternion.AngleAxis(180f + ARROW_HEAD_ANGLE, Vector3.up) * forward;

            Gizmos.color = color;
            Gizmos.DrawWireSphere(start, POINT_RADIUS);
            Gizmos.DrawLine(start, end);
            Gizmos.DrawLine(end, end + (left * ARROW_HEAD_LENGTH));
            Gizmos.DrawLine(end, end + (right * ARROW_HEAD_LENGTH));
        }
    }
}
