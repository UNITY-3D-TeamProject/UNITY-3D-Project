using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Map.Gimmicks
{
    /// <summary>
    /// 플레이어나 밀 수 있는 블록(PushableBlock)이 올라가 있는 동안만 눌려 있는 감압판.
    /// TriggerPlate 는 닿는 순간 한 번 실행하고, 이쪽은 눌릴 때와 떨어질 때를 따로 실행한다.
    /// 예: On Pressed 에 GimmickSwitch.TurnOn, On Released 에 GimmickSwitch.TurnOff 를 연결하면 올라가 있는 동안만 열리는 문
    /// 콜라이더의 Is Trigger를 켜야 한다.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class PressurePlate : MonoBehaviour
    {
        #region Serialized Fields
        [Header("Events")]
        [Tooltip("처음 눌리는 순간 실행할 동작")]
        [SerializeField] private UnityEvent _onPressed = new UnityEvent();

        [Tooltip("위에 아무것도 없게 된 순간 실행할 동작")]
        [SerializeField] private UnityEvent _onReleased = new UnityEvent();
        #endregion

        #region Private Fields
        private readonly List<Collider> _occupants = new List<Collider>();
        #endregion

        #region Properties
        /// <summary>지금 눌려 있는지 여부.</summary>
        public bool IsPressed => _occupants.Count > 0;

        /// <summary>처음 눌리는 순간 실행되는 이벤트.</summary>
        public UnityEvent OnPressed => _onPressed;

        /// <summary>위에 아무것도 없게 된 순간 실행되는 이벤트.</summary>
        public UnityEvent OnReleased => _onReleased;
        #endregion

        #region Unity Lifecycle
        private void Update()
        {
            if (_occupants.Count == 0) return;

            // 위에 있던 것이 파괴되거나 꺼지면(순간 이동 포함) 나갔다는 알림이 오지 않으므로 직접 걸러 낸다
            int removedCount = _occupants.RemoveAll(IsGone);
            if ((removedCount > 0) && (_occupants.Count == 0))
            {
                _onReleased.Invoke();
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!IsWeight(other) || _occupants.Contains(other)) return;

            _occupants.Add(other);
            if (_occupants.Count == 1)
            {
                _onPressed.Invoke();
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (!_occupants.Remove(other)) return;

            if (_occupants.Count == 0)
            {
                _onReleased.Invoke();
            }
        }
        #endregion

        #region Private Methods
        private static bool IsWeight(Collider other)
        {
            return GimmickUtility.TryGetPlayer(other, out _) || (other.GetComponentInParent<PushableBlock>() != null);
        }

        private static bool IsGone(Collider occupant)
        {
            return (occupant == null) || !occupant.enabled || !occupant.gameObject.activeInHierarchy;
        }
        #endregion
    }
}
