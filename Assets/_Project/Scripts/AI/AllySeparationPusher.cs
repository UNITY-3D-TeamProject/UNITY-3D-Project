using UnityEngine;
using Movement;

namespace AI
{
    /// <summary>
    /// 반경 안 동료에게서 매 프레임 조금씩 밀려나 적끼리 겹치지 않게 한다.
    /// 적끼리는 레이어 충돌을 꺼서(올라타기 방지) 서로 통과하므로, 겹침은 이 컴포넌트가 막는다.
    /// 밀려날 방향은 AIController.TryGetAwayFromAllies 를 그대로 쓴다(ID가 작은 동료만 피하므로 서로 밀며 진동하지 않는다).
    /// 이동은 CharacterMotor 의 외부 변위로 적용하므로 벽은 통과하지 않는다.
    /// </summary>
    public class AllySeparationPusher : MonoBehaviour
    {
        #region Serialized Fields
        [Header("References")]
        [Tooltip("동료 방향을 묻는 조종부. 비워두면 이 오브젝트에서 찾는다.")]
        [SerializeField] private AIController _controller;
        [Tooltip("밀어낼 몸통의 CharacterMotor. 비워두면 자식에서 찾는다.")]
        [SerializeField] private CharacterMotor _motor;

        [Header("Push")]
        [Tooltip("동료를 찾을 반경(m). 두 적의 CharacterController 반지름 합보다 조금 크게 둔다.")]
        [SerializeField, Min(0.1f)] private float _pushRadius = 0.9f;
        [Tooltip("밀려나는 속도(m/s).")]
        [SerializeField, Min(0.0f)] private float _pushSpeed = 1.5f;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            if (!_controller) _controller = GetComponent<AIController>();
            if (!_motor) _motor = GetComponentInChildren<CharacterMotor>();

            if (!_controller || !_motor)
            {
                Debug.LogError($"[{name}] AIController 또는 CharacterMotor 가 없어 동료 밀어내기를 끈다.", this);
                enabled = false;
            }
        }

        private void Update()
        {
            if (!_controller.TryGetAwayFromAllies(_pushRadius, out Vector3 direction)) return;

            _motor.AddExternalDisplacement(direction * (_pushSpeed * Time.deltaTime));
        }
        #endregion
    }
}
