using UnityEngine;
using UnityEngine.Events;

namespace Map.Gimmicks
{
    /// <summary>
    /// 꺼짐 → 켜짐을 일정 주기로 되풀이하며 그때마다 연결된 동작을 실행하는 타이머. 꺼진 구간에서 시작한다.
    /// 예: 주기적으로 켜지는 전기 바닥(GimmickSwitch.TurnOn / TurnOff), 솟는 가시(StateMover.Activate / Deactivate),
    /// 일정 간격으로 쏘는 발사기(On Phase On 에 ProjectileLauncher.Fire)
    /// 컴포넌트를 끄면 멈추므로, GimmickSwitch 의 Enabled While On 에 넣어 정지 버튼을 만들 수 있다.
    /// </summary>
    public class CycleTimer : MonoBehaviour
    {
        #region Serialized Fields
        [Header("Timing")]
        [Tooltip("꺼져 있는 시간(초)")]
        [SerializeField, Min(0.05f)] private float _offDuration = 2.0f;

        [Tooltip("켜져 있는 시간(초)")]
        [SerializeField, Min(0.05f)] private float _onDuration = 2.0f;

        [Tooltip("처음 켜질 때까지 더 기다리는 시간(초). 여러 개의 박자를 어긋나게 할 때 쓴다.")]
        [SerializeField, Min(0.0f)] private float _startDelay;

        [Header("Events")]
        [Tooltip("켜지는 순간 실행할 동작")]
        [SerializeField] private UnityEvent _onPhaseOn = new UnityEvent();

        [Tooltip("꺼지는 순간 실행할 동작")]
        [SerializeField] private UnityEvent _onPhaseOff = new UnityEvent();
        #endregion

        #region Private Fields
        private bool _isOn;
        private float _remaining;
        #endregion

        #region Properties
        public bool IsOn => _isOn;
        public float Remaining => Mathf.Max(0f, _remaining);

        /// <summary>켜지는 순간 실행되는 이벤트.</summary>
        public UnityEvent OnPhaseOn => _onPhaseOn;

        /// <summary>꺼지는 순간 실행되는 이벤트.</summary>
        public UnityEvent OnPhaseOff => _onPhaseOff;
        #endregion

        #region Unity Lifecycle
        private void Start()
        {
            _remaining = _offDuration + _startDelay;
        }

        private void Update()
        {
            _remaining -= Time.deltaTime;
            if (_remaining > 0.0f) return;

            _isOn = !_isOn;
            _remaining += _isOn ? _onDuration : _offDuration;

            if (_isOn)
            {
                _onPhaseOn.Invoke();
            }
            else
            {
                _onPhaseOff.Invoke();
            }
        }
        #endregion
    }
}
