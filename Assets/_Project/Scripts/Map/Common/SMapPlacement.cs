using System;
using UnityEngine;

namespace Map.Common
{
    /// <summary>
    /// 생성된 맵에 무작위로 놓을 오브젝트(장애물, 아이템 등) 한 종류의 설정.
    /// </summary>
    [Serializable]
    public struct SMapPlacement
    {
        [Tooltip("놓을 프리팹")]
        public GameObject Prefab;

        [Tooltip("놓을 개수. 놓을 자리가 모자라면 그만큼만 놓인다.")]
        [Min(0)] public int Count;

        [Tooltip("놓을 자리(바닥 중앙)에서 띄울 거리. 예: 낙하 블록은 (0, 8, 0)")]
        public Vector3 Offset;
    }
}
