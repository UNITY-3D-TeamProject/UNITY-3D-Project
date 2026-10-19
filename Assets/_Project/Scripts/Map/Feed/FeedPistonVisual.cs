using UnityEngine;
using Map.Gimmicks;

namespace Map.Feed
{
    /// <summary>타격 헤드와 실린더 사이 로드를 늘리고 실제 주기의 작동 예고를 표시한다.</summary>
    public class FeedPistonVisual : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        [SerializeField] private Transform _head;
        [SerializeField] private Transform _socket;
        [SerializeField] private Transform _rod;
        [SerializeField] private Renderer _warning;
        [SerializeField] private CycleTimer _timer;

        private MaterialPropertyBlock _block;

        private void Awake()
        {
            _block = new MaterialPropertyBlock();
        }

        private void LateUpdate()
        {
            Vector3 delta = _head.position - _socket.position;
            _rod.position = (_head.position + _socket.position) * 0.5f;
            _rod.rotation = Quaternion.FromToRotation(Vector3.up, delta.normalized);
            _rod.localScale = new Vector3(0.24f, delta.magnitude * 0.5f, 0.24f);
            bool isWarning = _timer.IsOn || (_timer.Remaining < 0.4f);
            bool isLit = isWarning && (Mathf.FloorToInt(Time.time * 12f) % 2 == 0);
            _block.SetColor(BaseColorId, isLit ? new Color(1f, 0.15f, 0.24f) : new Color(0.1f, 0.75f, 0.85f));
            _warning.SetPropertyBlock(_block);
        }
    }
}
