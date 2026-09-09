public interface IMoveMotor
{
    bool UseFixedTick { get; }

    void Tick(SMoveIntent intent, SOMovementConfig config, float deltaTime);
}
