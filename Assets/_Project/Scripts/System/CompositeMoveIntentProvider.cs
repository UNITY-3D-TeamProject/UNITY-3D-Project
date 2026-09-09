public abstract class CompositeMoveIntentProvider : IMoveIntentProvider
{
    private readonly IMoveIntentProvider[] _providers;

    protected CompositeMoveIntentProvider(IMoveIntentProvider[] providers)
    {
        _providers = providers;
    }

    public SMoveIntent GetIntent()
    {
        SMoveIntent result = default;

        for (int i = 0; i < _providers.Length; i++)
        {
            if (_providers[i] == null)
                continue;

            result = Combine(result, _providers[i].GetIntent());
        }

        return result;
    }

    // 기본 합산 규칙: 값을 반환한(default가 아닌) provider가 이전 결과를 덮어쓴다.
    // 실제 우선순위/블렌딩 규칙은 각 오브젝트 담당자가 오버라이드해서 결정한다(코어 책임 아님, spec.md 열린 질문).
    protected virtual SMoveIntent Combine(SMoveIntent accumulated, SMoveIntent next)
    {
        return next.Equals(default(SMoveIntent)) ? accumulated : next;
    }
}
