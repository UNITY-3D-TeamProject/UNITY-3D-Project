namespace Core.Audio
{
    /// <summary>
    /// 3D 효과음 재생 번호. default는 없음이며, 다른 매니저(이전 씬 등)의 핸들로는 끌 수 없다.
    /// </summary>
    // AudioManager.PlaySfx·PlaySfxLoop가 돌려주고, StopSfx에 다시 넘겨 그 소리를 끊는다.
    // int 대신 전용 타입을 쓰므로 이펙트 핸들(SEffectHandle)이나 다른 숫자를 잘못 넘기면 컴파일 에러가 난다.
    // readonly struct라 만든 뒤 값이 바뀌지 않고, 필드에 보관하거나 복사해도 가비지가 생기지 않는다.
    public readonly struct SAudioHandle
    {
        #region Private Fields
        // 매니저마다 별도로 만든 토큰. 씬이 바뀌어 번호가 1부터 다시 시작해도 다른 매니저의 소리를 끄지 않게 한다.
        // 내용 없는 object라 매니저(GameObject)를 붙잡지 않는다. 핸들이 남아 있어도 파괴된 매니저를 참조하지 않는다.
        private readonly object _owner;
        #endregion

        #region Properties
        // 매니저 안에서 재생할 때마다 1씩 늘어나는 번호. 0은 "발급되지 않음"으로 쓴다.
        public int Id { get; }

        /// <summary>발급된 핸들인지 여부. 현재도 재생 중이라는 의미는 아니다.</summary>
        // default(Id 0)나 재생에 실패해 돌려받은 값이면 false. 소리가 이미 끝났어도 true일 수 있다.
        public bool IsValid => Id != 0;
        #endregion

        #region Internal Methods
        // AudioManager만 핸들을 만든다. 다른 코드는 받은 핸들을 보관했다가 StopSfx에 넘기기만 한다.
        // internal은 같은 어셈블리 안에서만 보인다는 뜻이다. 지금은 asmdef가 없어 프로젝트 스크립트 전체가 같은 어셈블리이므로
        // 다른 스크립트에서도 호출은 되지만, 직접 만들지 않는다.
        internal SAudioHandle(int id, object owner)
        {
            Id = id;
            _owner = owner;
        }

        // [호출] AudioManager.StopSfx. 이 핸들을 발급한 매니저가 owner인지 확인한다.
        // 값 비교가 아니라 참조(같은 object 인스턴스) 비교라, 번호가 같아도 다른 매니저의 핸들이면 false다.
        internal bool IsOwnedBy(object owner) => ReferenceEquals(_owner, owner);
        #endregion
    }
}
