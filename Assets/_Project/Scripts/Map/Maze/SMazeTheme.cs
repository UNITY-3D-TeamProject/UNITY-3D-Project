using System;
using UnityEngine;

namespace Map.Maze
{
    /// <summary>
    /// 이어진 미로 하나의 겉모습 설정. 미로마다 포인트 색이 다른 프리팹을 넣는다.
    /// </summary>
    [Serializable]
    public struct SMazeTheme
    {
        [Tooltip("벽 한 칸 프리팹 목록. 벽마다 무작위로 하나를 고른다. 길이 방향이 X축, 바닥이 y = 0")]
        public GameObject[] WallPrefabs;

        [Tooltip("벽 끝이 만나는 꼭짓점에 세울 기둥 프리팹 목록 (선택). 기둥마다 무작위로 하나를 고른다. 원점이 꼭짓점 바닥")]
        public GameObject[] PillarPrefabs;

        [Tooltip("출구 프리팹 (MazeExitGate 필요). 출구 칸 중앙에 놓이고 +Z 쪽 벽이 문이다.")]
        public MazeExitGate GatePrefab;

        [Tooltip("다음에 뚫을 칸으로 가장 최근 칸을 고를 확률. 1 이면 긴 외길, 0 이면 짧은 곁가지와 갈림길이 많은 미로")]
        [Range(0.0f, 1.0f)] public float NewestCellRatio;

        [Tooltip("막다른 칸마다 막힌 벽 하나를 뚫어 순환을 만들 확률. 0 이면 순환이 없다.")]
        [Range(0.0f, 1.0f)] public float BraidRatio;

        [Tooltip("이 미로 안에만 무작위로 놓을 기믹 목록. 막다른 칸에는 놓지 않는다. 남은 칸은 맵 공통 배치(Placements)에 쓰인다.")]
        public SMazePlacement[] Placements;

        [Tooltip("켜면 이 미로에 NavMesh 를 만든다. 적이 돌아다니는 미로에서만 켠다.")]
        public bool ShouldBakeNavMesh;
    }
}
