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
        [Tooltip("벽 한 칸 프리팹. 길이 방향이 X축, 바닥이 y = 0")]
        public GameObject WallPrefab;

        [Tooltip("출구 프리팹 (MazeExitGate 필요). 출구 칸 중앙에 놓이고 +Z 쪽 벽이 문이다.")]
        public MazeExitGate GatePrefab;
    }
}
