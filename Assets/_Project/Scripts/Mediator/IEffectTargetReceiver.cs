using Attribute.Core;

namespace Mediator
{
    /// <summary>
    /// 외부(Mediator)로부터 IEffectTarget 을 주입받는 대상.
    /// Mediator 는 구체 타입 대신 이 인터페이스로 주입 대상을 수집한다.
    /// </summary>
    public interface IEffectTargetReceiver
    {
        /// <summary>
        /// 사용할 IEffectTarget 을 설정한다.
        /// </summary>
        /// <param name="target">주입할 IEffectTarget</param>
        void SetEffectTarget(IEffectTarget target);

        /// <summary>
        /// 설정된 IEffectTarget 을 해제한다.
        /// </summary>
        void ClearEffectTarget();
    }
}
