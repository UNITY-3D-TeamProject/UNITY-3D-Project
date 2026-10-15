using System;
using UnityEngine;
using Map.Common;

namespace Map.Maze
{
    /// <summary>
    /// 이어진 미로 하나의 겉모습 설정. 미로마다 포인트 색이 다른 프리팹을 넣는다.
    /// </summary>
    [Serializable]
    public struct SMazeTheme
    {
        [Tooltip("벽 한 칸 프리팹. 길이 방향이 X축, 바닥이 y = 0")]
        public GameObject WallPrefab;

        [Tooltip("출구 프리팹 (MazeExitGate 필요). 출구 칸 중앙에 놓이고 +Z 쪽 벽이 문이다.")]
        public MazeExitGate GatePrefab;

        [Tooltip("이 미로 안에만 무작위로 놓을 장애물·아이템 목록. 남은 칸은 맵 공통 배치(Placements)에 쓰인다.")]
        public SMapPlacement[] Placements;

        [Tooltip("켜면 이 미로에 NavMesh 를 만든다. 적이 돌아다니는 미로에서만 켠다.")]
        public bool ShouldBakeNavMesh;
    }
}
