namespace SceneTransition
{
    /// <summary>
    /// 게임에 쓰이는 씬 목록. 씬 이름은 SceneLoader의 SceneNames 표에서 찾는다.
    /// 포탈·컷씬 데이터에 숫자로 저장되므로 기존 값은 바꾸지 않는다.
    /// </summary>
    public enum ESceneType
    {
        Tutorial = 0,
        Lobby = 1,
        Feed = 2,
        File = 3,
        Security = 4,
        Live = 5,
    }
}
