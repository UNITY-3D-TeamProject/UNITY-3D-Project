using UnityEngine;

public class MovementMotor : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private SOMovementConfig _movementConfig;

    private IMoveIntentProvider _intentProvider;
    private IMoveMotor _motor;

    private void Awake()
    {
        _intentProvider = GetComponent<IMoveIntentProvider>();
        _motor = GetComponent<IMoveMotor>();

        Debug.Assert(_motor != null, $"[{name}] IMoveMotor 구현 컴포넌트가 없습니다.");
        Debug.Assert(_movementConfig != null, $"[{name}] SOMovementConfig가 연결되지 않았습니다.");
    }

    private void Update()
    {
        if (_motor == null || _motor.UseFixedTick)
            return;

        Tick(Time.deltaTime);
    }

    private void FixedUpdate()
    {
        if (_motor == null || !_motor.UseFixedTick)
            return;

        Tick(Time.fixedDeltaTime);
    }

    private void Tick(float deltaTime)
    {
        SMoveIntent intent = _intentProvider != null ? _intentProvider.GetIntent() : default;
        _motor.Tick(intent, _movementConfig, deltaTime);
    }
}
