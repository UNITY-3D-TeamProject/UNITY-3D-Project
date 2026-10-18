namespace Core.Effect
{
    /// <summary>
    /// VFX 재생 번호. default는 없음이며, 반납된 인스턴스를 재사용해도 이전 핸들로 끌 수 없다.
    /// </summary>
    public readonly struct SEffectHandle
    {
        #region Private Fields
        // 매니저마다 별도로 만든 토큰. GameObject를 참조하지 않고 매니저 교체 후 ID 충돌을 막는다.
        private readonly object _owner;
        #endregion

        #region Properties
        public int Id { get; }

        /// <summary>발급된 핸들인지 여부. 현재도 재생 중이라는 의미는 아니다.</summary>
        public bool IsValid => Id != 0;
        #endregion

        #region Internal Methods
        internal SEffectHandle(int id, object owner)
        {
            Id = id;
            _owner = owner;
        }

        internal bool IsOwnedBy(object owner) => ReferenceEquals(_owner, owner);
        #endregion
    }
}
