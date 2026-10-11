using UnityEngine;

namespace Core.Scanning
{
    /// <summary>
    /// 스캔 파동이 대상에 닿았을 때 전달되는 정보. 파동이 닿아 있는 동안 매 프레임 전달된다.
    /// </summary>
    public readonly struct SScanHit
    {
        /// <summary>스캔 1회를 구분하는 번호. 같은 번호의 반복 전달은 같은 스캔의 연속이다.</summary>
        // 새로운 스캔을 만났다 vs 아까 그 스캔이 계속 닿아 있다.
        // 새 스캔 => 타이머 다시 시작
        // ScanId가 같은 경우는 같은 스캔 한번이 대상에 계속 닿아있는 상태
        public readonly int ScanId;

        /// <summary>파동의 중심(월드 좌표). 스캔을 시작한 시점의 위치로 고정된다.</summary>
        // 플레이어가 움직여도 파동의 시작지점은 그대로이다.
        public readonly Vector3 Origin;

        /// <summary>파동의 현재 반경(m).</summary>
        public readonly float Radius;

        /// <summary>닿은 시점부터 대상이 활성화되어야 하는 시간(초).</summary>
        public readonly float Duration;

        public SScanHit(int scanId, Vector3 origin, float radius, float duration)
        {
            ScanId = scanId; // 새 스캔인지 구분
            Origin = origin; // 리빌 중심
            Radius = radius; // 리빌 반경
            Duration = duration; // 활성 시간
        }
    }
}
