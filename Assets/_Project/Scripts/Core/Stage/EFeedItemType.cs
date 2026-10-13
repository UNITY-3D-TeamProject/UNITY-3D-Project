namespace Core.Stage
{
    /// <summary>피드 앱 1라운드에서 모으는 피드 재료 아이템의 종류. 태그 순서대로 하나씩 나온다.</summary>
    public enum EFeedItemType
    {
        Title,      // 제목
        Hashtag,    // 해시태그
        Song,       // 노래
        Caption,    // 문구
        Photo,      // 사진
    }
}
