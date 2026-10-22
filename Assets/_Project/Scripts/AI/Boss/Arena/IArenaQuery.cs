using System;
using UnityEngine;

namespace AI.Boss.Arena
{
    /// <summary>
    /// 보스 아레나를 읽기만 하는 질의 창구. B 가 발밑 칸·앞 칸·착지 칸을 판단할 때 쓴다.
    /// 칸 상태를 바꾸지 않는 착지 예약(SetReservedTile)만 예외로 쓸 수 있다. 좌표는 모두 월드 기준이다.
    /// </summary>
    public interface IArenaQuery
    {
        /// <summary>칸 한 변의 길이(m).</summary>
        float TileSize { get; }

        /// <summary>칸이 무너졌을 때 칸 인덱스를 알린다. 이미 칸의 겉모습이 바뀐 뒤에 호출된다.</summary>
        event Action<int> OnTileCollapsed;

        /// <returns>월드 좌표가 판 안이면 true. 판 밖이면 index = -1</returns>
        bool TryGetTile(Vector3 world, out int index);

        bool IsHoleOrTelegraphed(int index);

        /// <returns>월드 좌표가 판 안의 멀쩡한 칸 위면 true</returns>
        bool IsSafeAt(Vector3 world);

        /// <returns>칸 중심의 월드 좌표(아레나 바닥 높이)</returns>
        Vector3 GetTileCenter(int index);

        /// <returns>가장 가까운 멀쩡한 칸 인덱스(착지 예약 칸 제외). 멀쩡한 칸이 없으면 -1</returns>
        int FindNearestSafe(Vector3 world);

        /// <returns>두 점 사이에 구멍(붕괴·예고 칸)이나 판 밖이 있으면 true. 두 끝점의 칸은 검사하지 않는다</returns>
        bool HasHoleBetween(Vector3 a, Vector3 b);

        /// <returns>칸 중심에서 target 까지 직선에 구멍이 없는 멀쩡한 칸 중 from 에서 가장 가까운 칸(from·target 칸, 착지 예약 칸 제외). 없으면 -1</returns>
        int FindClearLineTile(Vector3 from, Vector3 target);

        /// <summary>B 가 착지할 칸을 예약한다. A 는 예약 칸을 노리지 않는다. 칸 상태는 바꾸지 않는다.</summary>
        /// <param name="index">예약할 칸. -1 이면 예약을 푼다</param>
        void SetReservedTile(int index);
    }
}
