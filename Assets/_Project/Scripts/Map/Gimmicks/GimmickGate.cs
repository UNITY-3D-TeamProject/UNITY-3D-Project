using UnityEngine;
using UnityEngine.Events;

namespace Map.Gimmicks
{
    /// <summary>
    /// 연결된 GimmickSwitch 가 모두 켜졌을 때 동작을 실행하는 조건 장치.
    /// 예: 표적 두 개를 제한 시간 안에 모두 맞혀야 열리는 다리
    /// (표적마다 Auto Revert Time 이 있는 GimmickSwitch 를 두고 이 컴포넌트의 Inputs 에 넣는다)
    /// </summary>
    public class GimmickGate : MonoBehaviour
    {
        #region Serialized Fields
        [Header("Inputs")]
        [Tooltip("모두 켜져야 하는 스위치들")]
        [SerializeField] private GimmickSwitch[] _inputs;

        [Header("Events")]
        [Tooltip("모두 켜진 순간 실행할 동작")]
        [SerializeField] private UnityEvent _onAllOn = new UnityEvent();

        [Tooltip("모두 켜져 있다가 하나라도 꺼진 순간 실행할 동작")]
        [SerializeField] private UnityEvent _onAnyOff = new UnityEvent();
        #endregion

        #region Private Fields
        private bool _isAllOn;
        #endregion

        #region Properties
        /// <summary>모두 켜진 순간 실행되는 이벤트.</summary>
        public UnityEvent OnAllOn => _onAllOn;

        /// <summary>모두 켜져 있다가 하나라도 꺼진 순간 실행되는 이벤트.</summary>
        public UnityEvent OnAnyOff => _onAnyOff;
        #endregion

        #region Unity Lifecycle
        private void Update()
        {
            bool isAllOn = IsEveryInputOn();
            if (isAllOn == _isAllOn) return;

            _isAllOn = isAllOn;
            if (_isAllOn)
            {
                _onAllOn.Invoke();
            }
            else
            {
                _onAnyOff.Invoke();
            }
        }
        #endregion

        #region Private Methods
        private bool IsEveryInputOn()
        {
            if ((_inputs == null) || (_inputs.Length == 0)) return false;

            foreach (GimmickSwitch input in _inputs)
            {
                if ((input == null) || !input.IsOn) return false;
            }

            return true;
        }
        #endregion
    }
}
