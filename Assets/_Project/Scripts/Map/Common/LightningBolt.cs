using UnityEngine;

namespace Map.Common
{
    /// <summary>
    /// LineRenderer 로 지직거리는 번개 줄기를 그린다. 짧은 간격으로 모양을 새로 만들어 떨리는 것처럼 보인다.
    /// 좌표는 모두 자신 기준이며, LineRenderer 의 Use World Space 는 꺼 둔다.
    /// Random Area 를 주면 새로 만들 때마다 양 끝점을 그 상자 안에서 무작위로 고른다. (전기 바닥용)
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public class LightningBolt : MonoBehaviour
    {
        #region Serialized Fields
        [Header("Shape")]
        [Tooltip("시작점 (자신 기준)")]
        [SerializeField] private Vector3 _start = Vector3.left;

        [Tooltip("끝점 (자신 기준)")]
        [SerializeField] private Vector3 _end = Vector3.right;

        [Tooltip("0이 아니면 양 끝점을 이 크기의 상자(자신 중심) 안에서 매번 무작위로 고른다.")]
        [SerializeField] private Vector3 _randomArea;

        [Tooltip("꺾이는 마디 수")]
        [SerializeField, Range(2, 40)] private int _segments = 12;

        [Tooltip("옆으로 흔들리는 폭 (줄기 길이에 대한 비율)")]
        [SerializeField, Min(0.0f)] private float _jitter = 0.12f;

        [Tooltip("가운데가 위로 휘는 높이. 바닥을 타고 튀는 전기에 쓴다.")]
        [SerializeField] private float _archHeight;

        [Header("Timing")]
        [Tooltip("모양을 새로 만드는 간격(초)의 최소 / 최대")]
        [SerializeField] private Vector2 _refreshInterval = new Vector2(0.03f, 0.09f);

        [Tooltip("새로 만들 때 보일 확률. 1보다 작으면 깜빡인다.")]
        [SerializeField, Range(0.0f, 1.0f)] private float _visibleChance = 1.0f;
        #endregion

        #region Private Fields
        private LineRenderer _line;
        private Vector3[] _points;
        private float _nextRefreshTime;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            _line = GetComponent<LineRenderer>();
            _line.useWorldSpace = false;
        }

        private void OnEnable()
        {
            _nextRefreshTime = 0.0f;
        }

        private void Update()
        {
            if (Time.time < _nextRefreshTime) return;

            _nextRefreshTime = Time.time + Random.Range(_refreshInterval.x, _refreshInterval.y);
            Rebuild();
        }
        #endregion

        #region Private Methods
        private void Rebuild()
        {
            _line.enabled = Random.value <= _visibleChance;
            if (!_line.enabled) return;

            if ((_points == null) || (_points.Length != _segments + 1))
            {
                _points = new Vector3[_segments + 1];
                _line.positionCount = _points.Length;
            }

            Vector3 start = _start;
            Vector3 end = _end;
            if (_randomArea != Vector3.zero)
            {
                start = RandomPointInArea();
                end = RandomPointInArea();
            }

            Vector3 direction = end - start;
            float length = direction.magnitude;
            if (length < 0.001f) return;

            // 줄기에 수직인 두 방향으로 흔든다
            Vector3 forward = direction / length;
            Vector3 side = Vector3.Cross(forward, Vector3.up);
            if (side.sqrMagnitude < 0.001f) side = Vector3.Cross(forward, Vector3.right);
            side.Normalize();
            Vector3 up = Vector3.Cross(side, forward);

            float amplitude = length * _jitter;
            for (int i = 0; i <= _segments; i++)
            {
                float t = (float)i / _segments;

                // 양 끝은 고정하고 가운데로 갈수록 크게 흔든다
                float envelope = Mathf.Sin(t * Mathf.PI);
                Vector3 offset = ((side * Random.Range(-1.0f, 1.0f)) + (up * Random.Range(-1.0f, 1.0f))) * (amplitude * envelope);
                _points[i] = Vector3.Lerp(start, end, t) + offset + (Vector3.up * (_archHeight * envelope));
            }

            _line.SetPositions(_points);
        }

        private Vector3 RandomPointInArea()
        {
            return new Vector3(
                Random.Range(-0.5f, 0.5f) * _randomArea.x,
                Random.Range(-0.5f, 0.5f) * _randomArea.y,
                Random.Range(-0.5f, 0.5f) * _randomArea.z);
        }
        #endregion
    }
}
