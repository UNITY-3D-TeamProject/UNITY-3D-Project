using Mediator;
using UnityEngine;

namespace AI.GarbageCollector
{
    /// <summary>
    /// 생성된 순간부터 경과 시간을 재고, 곡선이 정한 속도를 이동 속도 어트리뷰트에 넣는다.
    /// 시간이 지날수록 빨라지는 추격자(시간 제한 역할)에 쓴다.
    /// 생성이 곧 시계 0이므로 리셋 API는 없다. 되돌리려면 새로 만든다.
    /// </summary>
    public class SpeedOverTime : MonoBehaviour
    {
        #region Serialized Fields
        [Header("References")]
        [Tooltip("속도를 넣을 중재자. 비워두면 이 오브젝트에서 찾는다.")]
        [SerializeField] private CharacterMediator _mediator;

        [Header("Speed")]
        [Tooltip("속도를 넣을 어트리뷰트 이름.")]
        [SerializeField] private string _speedAttributeKey = "MoveSpeed";
        [Tooltip("가로축: 경과 시간(초), 세로축: 속도. 마지막 키 뒤에는 마지막 값으로 고정된다.")]
        [SerializeField] private AnimationCurve _speedCurve = new AnimationCurve(
            new Keyframe(0.0f, 2.5f),
            new Keyframe(60.0f, 7.0f));
        #endregion

        #region Private Fields
        private float _elapsed;
        private float _lastAppliedSpeed;
        #endregion

        #region Unity Lifecycle
        private void Start()
        {
            if (!_mediator) _mediator = GetComponent<CharacterMediator>();

            if (!_mediator || !_mediator.IsValidAttribute(_speedAttributeKey))
            {
                // 잘못된 키는 AttributeSet 이 로그만 남기고 넘어가므로 여기서 먼저 알린다
                Debug.LogWarning($"[{name}] 속도를 넣을 어트리뷰트 '{_speedAttributeKey}' 를 쓸 수 없어 SpeedOverTime 을 끈다.", this);
                enabled = false;
                return;
            }

            Apply(_speedCurve.Evaluate(0.0f));
        }

        private void Update()
        {
            _elapsed += Time.deltaTime;

            float speed = _speedCurve.Evaluate(_elapsed);
            if (Mathf.Approximately(speed, _lastAppliedSpeed)) return;

            Apply(speed);
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// 속도를 어트리뷰트에 넣고 마지막으로 넣은 값을 기록한다.
        /// </summary>
        /// <param name="speed">넣을 속도</param>
        private void Apply(float speed)
        {
            _lastAppliedSpeed = speed;
            _mediator.SetAttribute(_speedAttributeKey, speed);
        }
        #endregion
    }
}
