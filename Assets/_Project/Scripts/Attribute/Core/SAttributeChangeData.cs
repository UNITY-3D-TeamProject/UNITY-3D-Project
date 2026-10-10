namespace Attribute.Core
{
    /// <summary>
    /// 어트리뷰트 값 변경 알림 데이터. AttributeSet 의 변경 콜백 인자를 하나로 묶는다.
    /// 출처 정보가 필요 없는 구독자는 Context 를 무시하면 된다.
    /// </summary>
    public readonly struct SAttributeChangeData
    {
        /// <summary>변경된 어트리뷰트 이름.</summary>
        public readonly string AttributeName;

        /// <summary>변경 후 값.</summary>
        public readonly float NewValue;

        /// <summary>변경 전 값.</summary>
        public readonly float OldValue;

        /// <summary>값 변경의 출처 정보. default 는 출처 없음.</summary>
        public readonly SEffectContext Context;

        /// <param name="attributeName">변경된 어트리뷰트 이름</param>
        /// <param name="newValue">변경 후 값</param>
        /// <param name="oldValue">변경 전 값</param>
        /// <param name="context">값 변경의 출처 정보</param>
        public SAttributeChangeData(string attributeName, float newValue, float oldValue, SEffectContext context)
        {
            AttributeName = attributeName;
            NewValue = newValue;
            OldValue = oldValue;
            Context = context;
        }
    }
}
