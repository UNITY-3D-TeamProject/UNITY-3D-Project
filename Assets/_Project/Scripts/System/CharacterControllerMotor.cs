using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class CharacterControllerMotor : MonoBehaviour, IMoveMotor
{
    private const float GROUNDED_STICK_VELOCITY = -2.0f;

    private CharacterController _characterController;
    private Vector3 _horizontalVelocity;
    private float   _verticalVelocity;

    public bool UseFixedTick => false;

    private void Awake()
    {
        _characterController = GetComponent<CharacterController>();
    }

    public void Tick(SMoveIntent intent, SOMovementConfig config, float deltaTime)
    {
        Vector3 flatDirection = new Vector3(intent.Direction.x, 0f, intent.Direction.z);
        Vector3 targetVelocity = flatDirection.normalized * Mathf.Min(intent.Speed, config.MaxSpeed);

        float rate = targetVelocity.sqrMagnitude > _horizontalVelocity.sqrMagnitude
            ? config.Acceleration
            : config.Deceleration;
        _horizontalVelocity = Vector3.MoveTowards(_horizontalVelocity, targetVelocity, rate * deltaTime);

        if (_characterController.isGrounded && _verticalVelocity < 0f)
            _verticalVelocity = GROUNDED_STICK_VELOCITY;

        if (intent.IsJumping && _characterController.isGrounded)
            _verticalVelocity = config.JumpForce;

        _verticalVelocity += config.Gravity * deltaTime;

        Vector3 motion = _horizontalVelocity;
        motion.y = _verticalVelocity;
        _characterController.Move(motion * deltaTime);
    }
}
