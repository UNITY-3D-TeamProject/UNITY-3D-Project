using UnityEngine;

namespace Map.Feed
{
    /// <summary>실제 이동 축의 중앙에 경로 선 하나만 표시한다. 충돌 판정은 없다.</summary>
    public class FeedRouteGuide : MonoBehaviour
    {
        [SerializeField] private Transform _start;
        [SerializeField] private Transform _end;
        [SerializeField] private LineRenderer _line;

        private void Start()
        {
            _line.SetPosition(0, _line.transform.InverseTransformPoint(_start.position));
            _line.SetPosition(1, _line.transform.InverseTransformPoint(_end.position));
        }
    }
}
