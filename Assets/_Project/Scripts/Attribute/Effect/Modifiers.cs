using System;
using System.Collections.Generic;

namespace Attribute.Effect
{
    /// <summary>어트리뷰트 값에 적용할 연산 종류.</summary>
    [Serializable]
    public enum EModifier
    {
        Add,
        Subtract,
        Multiply,
        Divide
    }

    /// <summary>
    /// 현재 값과 amount 를 받아 새 값을 계산하는 연산 단위.
    /// </summary>
    public abstract class Modifier
    {
        /// <param name="current">현재 값</param>
        /// <param name="amount">적용할 수치</param>
        /// <returns>연산 결과</returns>
        public abstract float Modify(float current, float amount);
    }

    /// <summary>current + amount</summary>
    public sealed class AddModifier : Modifier
    {
        public override float Modify(float current, float amount) => current + amount;
    }

    /// <summary>current - amount</summary>
    public sealed class SubtractModifier : Modifier
    {
        public override float Modify(float current, float amount) => current - amount;
    }

    /// <summary>current * amount</summary>
    public sealed class MultiplyModifier : Modifier
    {
        public override float Modify(float current, float amount) => current * amount;
    }

    /// <summary>current / amount</summary>
    public sealed class DivideModifier : Modifier
    {
        public override float Modify(float current, float amount) => current / amount;
    }

    /// <summary>
    /// EModifier 를 실제 Modifier 구현으로 매핑해 연산을 수행하는 정적 진입점.
    /// </summary>
    public static class Modifiers
    {
        #region Private Fields
        private static readonly Dictionary<EModifier, Modifier> ModifyDictionary = new()
        {
            { EModifier.Add, new AddModifier() },
            { EModifier.Subtract, new SubtractModifier() },
            { EModifier.Multiply, new MultiplyModifier() },
            { EModifier.Divide, new DivideModifier() },
        };
        #endregion

        #region Public Methods
        /// <summary>
        /// type 에 해당하는 연산을 current 와 amount 에 적용한다.
        /// </summary>
        /// <param name="type">연산 종류</param>
        /// <param name="current">현재 값</param>
        /// <param name="amount">적용할 수치</param>
        /// <returns>연산 결과</returns>
        public static float Modify(EModifier type, float current, float amount) =>
            ModifyDictionary[type].Modify(current, amount);
        #endregion
    }
}
