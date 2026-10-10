using UnityEngine;

namespace Core.Scanning
{
    /// <summary>
    /// 스캔 파동이 대상에 닿았을 때 전달되는 정보. 파동이 닿아 있는 동안 매 프레임 전달된다.
    /// </summary>
    public readonly struct SScanHit
    {
        /// <summary>스캔 1회를 구분하는 번호. 같은 번호의 반복 전달은 같은 스캔의 연속이다.</summary>
        public readonly int ScanId;

        /// <summary>파동의 중심(월드 좌표). 스캔을 시작한 시점의 위치로 고정된다.</summary>
        public readonly Vector3 Origin;

        /// <summary>파동의 현재 반경(m).</summary>
        public readonly float Radius;

        /// <summary>닿은 시점부터 대상이 활성화되어야 하는 시간(초).</summary>
        public readonly float Duration;

        public SScanHit(int scanId, Vector3 origin, float radius, float duration)
        {
            ScanId = scanId;
            Origin = origin;
            Radius = radius;
            Duration = duration;
        }
    }
}
