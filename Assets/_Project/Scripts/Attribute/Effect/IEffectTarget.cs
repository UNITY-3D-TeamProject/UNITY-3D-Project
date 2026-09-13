public interface IEffectTarget
{
    bool IsValidTarget(string targetName);
    float GetValue(string targetName);
    void SetValue(string targetName, float value);
}