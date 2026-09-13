
namespace Attribute.Core
{
    public interface IEffectTarget
    {
        /// <summary>
        /// targetName의 유효성 검사를 위한 함수
        /// </summary>
        /// <param name="targetName">유효성 검사를 진행할 targetName</param>
        /// <returns>유효성 검사 결과</returns>
        bool IsValidTarget(string targetName);
        /// <summary>
        /// targetName의 값을 구하기 위한 함수
        /// </summary>
        /// <param name="targetName">값을 원하는 targetName</param>
        /// <returns>targetName의 값</returns>
        float GetValue(string targetName);
        /// <summary>
        /// targetName의 값을 수정하기 위한 함수
        /// </summary>
        /// <param name="targetName">수정을 원하는 targetName</param>
        /// <param name="value">수정될 값</param>
        void SetValue(string targetName, float value);
    }
}