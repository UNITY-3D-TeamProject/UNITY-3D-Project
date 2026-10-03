using UnityEngine;
using Attribute.Core;
using Attribute.Effect;
using PlayerInput;

namespace Map.Gimmicks
{
    /// <summary>
    /// 플레이어가 영역에 들어올 때 / 머무는 동안 / 나갈 때 Effect 목록을 적용하는 장애물.
    /// 데미지 지대: Tick Effects에 Subtract CurrentHp
    /// 감속 지대: Enter Effects에 Multiply MoveSpeed 0.5, Exit Effects에 Divide MoveSpeed 0.5
    /// 콜라이더의 Is Trigger를 켜야 한다.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class EffectZone : MonoBehaviour
    {
        #region Serialized Fields
        [Header("Effects")]
        [Tooltip("들어올 때 한 번 적용")]
        [SerializeField] private SOAttributeEffect[] _enterEffects;

        [Tooltip("머무는 동안 Tick Interval마다 적용")]
        [SerializeField] private SOAttributeEffect[] _tickEffects;

        [Tooltip("Tick Effects를 적용하는 간격(초). 첫 적용은 들어온 뒤 이 시간이 지나서다.")]
        [SerializeField, Min(0.05f)] private float _tickInterval = 1.0f;

        [Tooltip("나갈 때 한 번 적용. 들어올 때 바꾼 값을 되돌리는 Effect를 넣는다.")]
        [SerializeField] private SOAttributeEffect[] _exitEffects;
        #endregion

        #region Private Fields
        private PlayerInputComponent _player;
        private IEffectTarget _target;
        private float _nextTickTime;
        #endregion

        #region Unity Lifecycle
        private void Update()
        {
            if (_target == null) return;

            // 영역 안에서 플레이어가 파괴된 경우
            if (_player == null)
            {
                _target = null;
                return;
            }

            if (Time.time < _nextTickTime) return;

            _nextTickTime += _tickInterval;
            GimmickUtility.ApplyEffects(_tickEffects, _target);
        }

        private void OnDisable()
        {
            Leave();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_target != null) return;
            if (!GimmickUtility.TryGetPlayer(other, out PlayerInputComponent player)) return;

            _player = player;
            _target = player.GetComponentInChildren<IEffectTarget>();
            _nextTickTime = Time.time + _tickInterval;

            GimmickUtility.ApplyEffects(_enterEffects, _target);
        }

        private void OnTriggerExit(Collider other)
        {
            if (!GimmickUtility.TryGetPlayer(other, out PlayerInputComponent player)) return;
            if (player != _player) return;

            Leave();
        }
        #endregion

        #region Private Methods
        private void Leave()
        {
            if ((_target != null) && (_player != null))
            {
                GimmickUtility.ApplyEffects(_exitEffects, _target);
            }

            _player = null;
            _target = null;
        }
        #endregion
    }
}
