namespace Core.Scanning
{
    /// <summary>
    /// 스캔 파동에 닿을 수 있는 대상. 닿았을 때 어떻게 반응할지는 대상이 결정한다.
    /// 파동이 닿아 있는 동안 대상별로 프레임당 한 번씩 호출되며(콜라이더가 여러 개여도 한 번), 같은 스캔이 반복해서 들어오므로 안전해야 한다.
    /// 보통은 ScanTarget 과 ScanReactionBase 조합을 쓰고, 특수한 대상만 직접 구현한다.
    /// </summary>
    public interface IScannable
    {
        /// <summary>
        /// 스캔 파동에 닿았음을 알린다.
        /// </summary>
        /// <param name="hit">닿은 스캔의 정보</param>
        void OnScanned(in SScanHit hit);
    }
}
