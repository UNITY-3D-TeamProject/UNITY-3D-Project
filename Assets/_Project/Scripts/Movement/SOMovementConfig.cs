using UnityEngine;

namespace Movement
{
    /// <summary>
    /// 이동 관련 수치를 담는 설정 에셋. Player/Enemy 등 캐릭터 타입별로 별도 에셋을 만들어 CharacterMotor에 할당한다. 
    /// MaxSpeed는 CharacterMotor가 직접 사용하지 않는다 — 이동 주체
    /// 스크립트가 이 값을 읽어 CharacterMotor.Move()의 speed 인자로 넘겨야 한다.
    /// </summary>
    [CreateAssetMenu(fileName = "SOMovementConfig", menuName = "Movement/Movement Config")]
    public class SOMovementConfig : ScriptableObject
    {
        #region Serialized Fields
        [Header("Speed")]
        [SerializeField] private float _maxSpeed = 5.0f;

        [Header("Gravity")]
        [SerializeField] private float _gravity = -20.0f;

        [Header("Rotation")]
        [SerializeField] private float _rotationSpeed = 10.0f;
        #endregion

        #region Properties
        public float MaxSpeed => _maxSpeed;
        public float Gravity => _gravity;
        public float RotationSpeed => _rotationSpeed;
        #endregion
    }
}
