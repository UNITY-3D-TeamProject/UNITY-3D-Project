using System;
using UnityEngine;
using AI.Boss.Logic;

namespace AI.Boss.Arena
{
    /// <summary>
    /// 보스 아레나를 바꾸는 창구. A 패턴과 진행(BossEncounter)이 쓴다.
    /// </summary>
    public interface IBossArena : IArenaQuery
    {
        /// <summary>칸 상태·타이머 원본. 선택기 스냅샷이 읽는다.</summary>
        ArenaGrid Grid { get; }

        /// <summary>무너진 칸이 복구됐을 때 칸 인덱스를 알린다.</summary>
        event Action<int> OnTileRestored;

        /// <param name="fillSeconds">예고가 켜질 때 칸 중앙에서 가득 차기까지의 시간(초). 0 이하면 처음부터 가득 찬다</param>
        /// <param name="color">예고 색(알파는 머티리얼 값 유지). null 이면 머티리얼 색</param>
        void SetTelegraph(int index, bool isOn, float fillSeconds, Color? color = null);

        void Collapse(int index);

        /// <summary>월드 좌표에 원형 예고를 켠다.</summary>
        /// <returns>끌 때 쓸 핸들. 빈 원판이 없으면 -1</returns>
        int ShowCircle(Vector3 center, float radius);

        /// <param name="handle">ShowCircle 이 돌려준 핸들. 유효하지 않으면 무시한다</param>
        void HideCircle(int handle);

        /// <summary>월드 좌표 두 점을 잇는 직선 예고를 켠다. 높이 차는 무시하고 from 의 높이에 놓는다.</summary>
        /// <param name="width">직선의 폭(m)</param>
        /// <returns>끌 때 쓸 핸들. 빈 직선 판이 없으면 -1</returns>
        int ShowLine(Vector3 from, Vector3 to, float width);

        /// <param name="handle">ShowLine 이 돌려준 핸들. 유효하지 않으면 무시한다</param>
        void HideLine(int handle);

        /// <summary>모든 칸·원판을 처음 상태로 되돌린다.</summary>
        void ResetAll();
    }
}
