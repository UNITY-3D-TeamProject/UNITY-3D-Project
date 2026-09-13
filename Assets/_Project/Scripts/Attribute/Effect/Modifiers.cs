using System;
using System.Collections.Generic;

[Serializable]
public enum EModifier
{
    Add,
    Multiply
}

public abstract class Modifier
{
    public abstract float Modify(float current, float amount);
}

public sealed class AddModifier : Modifier
{
    public override float Modify(float current, float amount) => current + amount;
}

public sealed class MultiplyModifier : Modifier
{
    public override float Modify(float current, float amount) => current * amount;
}

public static class Modifiers
{
    private static readonly Dictionary<EModifier, Modifier> _modifyDictionary = new()
    {
        { EModifier.Add,      new AddModifier() },
        { EModifier.Multiply, new MultiplyModifier() },
    };

    public static float Modify(EModifier type, float current, float amount) => _modifyDictionary[type].Modify(current, amount);
}