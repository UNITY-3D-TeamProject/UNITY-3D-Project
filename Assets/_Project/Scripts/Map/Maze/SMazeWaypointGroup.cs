using System;
using UnityEngine;

namespace Map.Maze
{
    /// <summary>
    /// 미로 하나에 놓을 웨이포인트 묶음. 생성할 때마다 앵커를 그 미로의 칸 가운데로 무작위로 옮긴다.
    /// </summary>
    [Serializable]
    public struct SMazeWaypointGroup
    {
        [Tooltip("웨이포인트를 놓을 미로 번호 (0 부터)")]
        [Min(0)] public int MazeIndex;

        [Tooltip("씬에 미리 놓아 둔 웨이포인트. 그 칸에는 배치물을 놓지 않는다.")]
        public Transform[] Anchors;
    }
}
