using UnityEngine;
using Movement;
using PlayerInput;

namespace Map.Gimmicks
{
    /// <summary>
    /// 영역 안의 플레이어를 한 방향으로 계속 미는 구역. 컨베이어 벨트, 송풍기에 쓴다.
    /// 수평으로 미는 용도다. 위로 밀면 중력이 계속 쌓여 벗어나는 순간 빠르게 떨어진다.
    /// 콜라이더의 Is Trigger를 켜야 한다. 컴포넌트를 끄면 밀지 않는다.
    /// </summary>
    [DefaultExecutionOrder(-50)]   // CharacterMotor(0)보다 먼저 실행해야 이번 프레임 이동에 합쳐진다
    [RequireComponent(typeof(Collider))]
    public class PushZone : MonoBehaviour
    {
        #region Serialized Fields
        [Header("Push")]
        [Tooltip("미는 방향 (자신의 방향 기준)")]
        [SerializeField] private Vector3 _direction = Vector3.forward;

        [Tooltip("미는 속도")]
        [SerializeField, Min(0.0f)] private float _speed = 3.0f;
        #endregion

        #region Private Fields
        private PlayerInputComponent _player;
        private CharacterMotor _motor;
        private float _lastStayTime = float.NegativeInfinity;
        #endregion

        #region Unity Lifecycle
        private void Update()
        {
            if (_motor == null) return;

            // 순간 이동(낙하 복귀 등)으로 빠져나가면 나갔다는 알림이 오지 않는다.
            // 그래서 직전 물리 스텝에도 영역 안에 있었을 때만 민다.
            bool isInside = (Time.fixedTime - _lastStayTime) <= (Time.fixedDeltaTime * 1.5f);
            if (!isInside)
            {
                _player = null;
                _motor = null;
                return;
            }

            Vector3 worldDirection = transform.TransformDirection(_direction).normalized;
            _motor.AddExternalDisplacement(worldDirection * (_speed * Time.deltaTime));
        }

        private void OnTriggerStay(Collider other)
        {
            if (!GimmickUtility.TryGetPlayer(other, out PlayerInputComponent player)) return;

            if (player != _player)
            {
                _player = player;
                _motor = player.GetComponentInChildren<CharacterMotor>();
            }

            _lastStayTime = Time.fixedTime;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(transform.position, transform.TransformDirection(_direction).normalized * 2.0f);
        }
        #endregion
    }
}
