namespace Core.Audio
{
    /// <summary>
    /// 설정창 볼륨 슬라이더 하나와 믹서 볼륨 파라미터 하나를 묶는 채널.
    /// </summary>
    // AudioManager가 (int)채널을 배열 인덱스로 쓰므로 순서를 바꾸면 그쪽 배열도 같이 바꾼다.
    public enum EAudioChannel
    {
        Master,
        Bgm,
        Sfx,
    }
}
