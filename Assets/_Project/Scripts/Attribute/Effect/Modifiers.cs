using System;
using System.Collections.Generic;

namespace Attribute.Effect
{
    [Serializable]
    public enum EModifier
    {
        Add,
        Subtract,
        Multiply,
        Divide
    }

    public abstract class Modifier
    {
        public abstract float Modify(float current, float amount);
    }

    public sealed class AddModifier : Modifier
    {
        public override float Modify(float current, float amount) => current + amount;
    }

    public sealed class SubtractModifier : Modifier
    {
        public override float Modify(float current, float amount) => current - amount;
    }
    
    public sealed class MultiplyModifier : Modifier
    {
        public override float Modify(float current, float amount) => current * amount;
    }

    public sealed class DivideModifier : Modifier
    {
        public override float Modify(float current, float amount) => current / amount;
    }
    
    public static class Modifiers
    {
        private static readonly Dictionary<EModifier, Modifier> _modifyDictionary = new()
        {
            { EModifier.Add, new AddModifier() },
            { EModifier.Subtract, new SubtractModifier() },
            { EModifier.Multiply, new MultiplyModifier() },
            { EModifier.Divide, new DivideModifier() },
        };

        public static float Modify(EModifier type, float current, float amount) =>
            _modifyDictionary[type].Modify(current, amount);
    }
}
