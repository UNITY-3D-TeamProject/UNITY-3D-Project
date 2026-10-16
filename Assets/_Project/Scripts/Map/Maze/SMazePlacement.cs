using System;
using UnityEngine;

namespace Map.Maze
{
    /// <summary>
    /// 미로 하나에만 놓을 배치물 한 종류의 설정. 칸 고르는 규칙과 방향을 함께 정한다.
    /// 프리팹은 +Z 가 통로 방향이고, 원점이 칸 바닥 중앙이 되도록 만든다.
    /// </summary>
    [Serializable]
    public struct SMazePlacement
    {
        [Tooltip("놓을 프리팹")]
        public GameObject Prefab;

        [Tooltip("놓을 개수. 규칙에 맞는 칸이 모자라면 그만큼만 놓인다.")]
        [Min(0)] public int Count;

        [Tooltip("놓을 자리(바닥 중앙)에서 띄울 거리. 예: 낙하 블록은 (0, 8, 0)")]
        public Vector3 Offset;

        [Tooltip("놓을 칸을 고르는 규칙")]
        public EMazeCellRule Rule;

        [Tooltip("Solution Path 규칙에서, 놓인 칸 뒤(입구 쪽)로 함께 차지할 칸 수. 같은 직선 위여야 한다. 예: 감압판 문은 2")]
        [Min(0)] public int CellsBehind;

        [Tooltip("Solution Path 규칙에서, 놓인 칸 앞(출구 쪽)으로 함께 차지할 칸 수. 같은 직선 위여야 한다. 예: 일방통행 문은 1")]
        [Min(0)] public int CellsAhead;
    }
}
