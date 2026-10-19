using UnityEngine;
using Map.Gimmicks;

namespace Map.Feed
{
    /// <summary>지지 콜라이더를 움직이지 않고 패드 윗판만 눌렀다가 복귀시킨다.</summary>
    public class FeedPressurePadVisual : MonoBehaviour
    {
        [SerializeField] private GimmickSwitch _source;
        [SerializeField] private Transform _pad;
        [SerializeField] private float _travel = 0.12f;

        private Vector3 _restPosition;
        private float _press;

        private void Awake()
        {
            _restPosition = _pad.localPosition;
        }

        private void LateUpdate()
        {
            bool isPressed = (_source != null) && _source.IsOn;
            float duration = isPressed ? 0.1f : 0.2f;
            _press = Mathf.MoveTowards(_press, isPressed ? 1f : 0f, Time.deltaTime / duration);
            _pad.localPosition = _restPosition + Vector3.down * (_travel * Mathf.SmoothStep(0f, 1f, _press));
        }
    }
}
