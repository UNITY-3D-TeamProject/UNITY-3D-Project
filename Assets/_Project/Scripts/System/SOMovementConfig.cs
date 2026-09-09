using UnityEngine;

[CreateAssetMenu(fileName = "SOMovementConfig", menuName = "Movement/Movement Config")]
public class SOMovementConfig : ScriptableObject
{
    [Header("Movement")]
    [SerializeField] private float _maxSpeed = 5.0f;
    [SerializeField] private float _acceleration = 20.0f;
    [SerializeField] private float _deceleration = 25.0f;

    [Header("Jump")]
    [SerializeField] private float _gravity = -20.0f;
    [SerializeField] private float _jumpForce = 8.0f;

    public float MaxSpeed      => _maxSpeed;
    public float Acceleration  => _acceleration;
    public float Deceleration  => _deceleration;
    public float Gravity       => _gravity;
    public float JumpForce     => _jumpForce;
}
