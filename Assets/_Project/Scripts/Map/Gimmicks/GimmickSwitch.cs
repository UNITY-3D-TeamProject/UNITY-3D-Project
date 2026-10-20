using UnityEngine;
using UnityEngine.Events;

namespace Map.Gimmicks
{
    /// <summary>
    /// 켜짐 / 꺼짐 상태 하나를 갖고, 상태가 바뀔 때 연결된 대상을 켜고 끄는 중계 장치.
    /// 입력(DamageButton.OnPressed, TriggerPlate.OnPlayerEntered, InteractButton.OnInteracted)에
    /// Toggle / TurnOn / TurnOff 를 연결해서 쓴다.
    /// 예: 다리 생성·제거(Active While On 에 다리), 방해 장치 일시 정지(Enabled While On 에 장애물, Auto Revert Time 5),
    /// 문 열기(On Turned On 에 StateMover.Activate)
    /// </summary>
    public class GimmickSwitch : MonoBehaviour
    {
        #region Constants
        // 처음 상태로 돌아가기 전에 표시등이 깜빡이는 시간(초)과 빠르기(초당 켜고 끄는 횟수)
        private const float REVERT_WARNING_TIME = 1.5f;
        private const float REVERT_BLINK_RATE = 6.0f;
        #endregion

        #region Serialized Fields
        [Header("State")]
        [Tooltip("시작할 때의 상태")]
        [SerializeField] private bool _isOnAtStart;

        [Tooltip("처음 상태에서 바뀐 뒤 이 시간(초)이 지나면 처음 상태로 돌아간다. 0이면 돌아가지 않는다.")]
        [SerializeField, Min(0.0f)] private float _autoRevertTime;

        [Header("Targets")]
        [Tooltip("켜져 있는 동안에만 활성화되는 오브젝트")]
        [SerializeField] private GameObject[] _activeWhileOn;

        [Tooltip("꺼져 있는 동안에만 활성화되는 오브젝트")]
        [SerializeField] private GameObject[] _activeWhileOff;

        [Tooltip("켜져 있는 동안에만 동작하는 컴포넌트 (예: SelfSpinner, EffectZone, SentryTurret)")]
        [SerializeField] private Behaviour[] _enabledWhileOn;

        [Header("Events")]
        [Tooltip("켜질 때 실행할 동작")]
        [SerializeField] private UnityEvent _onTurnedOn = new UnityEvent();

        [Tooltip("꺼질 때 실행할 동작")]
        [SerializeField] private UnityEvent _onTurnedOff = new UnityEvent();

        [Header("Indicator")]
        [Tooltip("상태를 색으로 보여 줄 표시등 (선택)")]
        [SerializeField] private Renderer _indicator;

        [SerializeField] private Color _onColor = new Color(0.2f, 1.0f, 0.4f);
        [SerializeField] private Color _offColor = new Color(1.0f, 0.25f, 0.2f);
        #endregion

        #region Private Fields
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        private MaterialPropertyBlock _indicatorBlock;
        private float _revertRemaining;
        #endregion

        #region Properties
        /// <summary>지금 켜져 있는지 여부.</summary>
        public bool IsOn { get; private set; }

        /// <summary>시작할 때의 상태. 자동으로 돌아갈 때도 이 상태로 돌아간다.</summary>
        public bool IsOnAtStart => _isOnAtStart;

        /// <summary>시각 표시가 실제 복귀 타이머와 같은 시간을 사용한다.</summary>
        public float RevertRemaining => Mathf.Max(0f, _revertRemaining);
        public float RevertRatio => (_autoRevertTime > 0f) ? Mathf.Clamp01(_revertRemaining / _autoRevertTime) : 0f;

        /// <summary>켜질 때 실행되는 이벤트.</summary>
        public UnityEvent OnTurnedOn => _onTurnedOn;

        /// <summary>꺼질 때 실행되는 이벤트.</summary>
        public UnityEvent OnTurnedOff => _onTurnedOff;
        #endregion

        #region Unity Lifecycle
        private void Start()
        {
            IsOn = _isOnAtStart;
            ApplyTargets();
            SetIndicatorColor(IsOn);
        }

        private void Update()
        {
            if (_revertRemaining <= 0.0f) return;

            _revertRemaining -= Time.deltaTime;
            if (_revertRemaining <= 0.0f)
            {
                SetState(_isOnAtStart);
                return;
            }

            // 돌아가기 직전에는 표시등을 깜빡여 알린다
            if (_revertRemaining < REVERT_WARNING_TIME)
            {
                bool isBlinkOn = (Mathf.FloorToInt(_revertRemaining * REVERT_BLINK_RATE) % 2) == 0;
                SetIndicatorColor(isBlinkOn ? IsOn : !IsOn);
            }
        }
        #endregion

        #region Public Methods
        /// <summary>상태를 반대로 바꾼다. (UnityEvent로 연결 가능)</summary>
        public void Toggle()
        {
            SetState(!IsOn);
        }

        /// <summary>켠다. (UnityEvent로 연결 가능)</summary>
        public void TurnOn()
        {
            SetState(true);
        }

        /// <summary>끈다. (UnityEvent로 연결 가능)</summary>
        public void TurnOff()
        {
            SetState(false);
        }
        #endregion

        #region Private Methods
        private void SetState(bool isOn)
        {
            // 처음 상태가 아닌 동안에는 같은 입력이 다시 와도 남은 시간을 처음부터 다시 센다
            bool isChanged = isOn != _isOnAtStart;
            _revertRemaining = (isChanged && (_autoRevertTime > 0.0f)) ? _autoRevertTime : 0.0f;

            if (isOn == IsOn)
            {
                SetIndicatorColor(IsOn);
                return;
            }

            IsOn = isOn;
            ApplyTargets();
            SetIndicatorColor(IsOn);

            if (IsOn)
            {
                _onTurnedOn.Invoke();
            }
            else
            {
                _onTurnedOff.Invoke();
            }
        }

        private void ApplyTargets()
        {
            SetActiveAll(_activeWhileOn, IsOn);
            SetActiveAll(_activeWhileOff, !IsOn);

            if (_enabledWhileOn == null) return;

            foreach (Behaviour behaviour in _enabledWhileOn)
            {
                if (behaviour != null) behaviour.enabled = IsOn;
            }
        }

        private static void SetActiveAll(GameObject[] targets, bool isActive)
        {
            if (targets == null) return;

            foreach (GameObject target in targets)
            {
                if (target != null) target.SetActive(isActive);
            }
        }

        private void SetIndicatorColor(bool isOn)
        {
            if (_indicator == null) return;

            _indicatorBlock ??= new MaterialPropertyBlock();
            _indicatorBlock.SetColor(BaseColorId, isOn ? _onColor : _offColor);
            _indicator.SetPropertyBlock(_indicatorBlock);
        }
        #endregion
    }
}
