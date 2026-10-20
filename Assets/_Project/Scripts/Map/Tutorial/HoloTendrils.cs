using UnityEngine;

namespace Map.Tutorial
{
    /// <summary>
    /// 해파리 드론의 빛 촉수: 자식 LineRenderer 들을 원 둘레에서 아래로 늘어뜨리고, 끝으로 갈수록 크게 물결치게 한다.
    /// LineRenderer 는 에디터에서 미리 만들어 둔다(로컬 좌표, useWorldSpace 끔). 판정 없음.
    /// </summary>
    public class HoloTendrils : MonoBehaviour
    {
        #region Serialized Fields
        [Tooltip("촉수가 매달리는 원의 반지름(m)")]
        [SerializeField, Min(0.0f)] private float _radius = 1.1f;

        [Tooltip("촉수 길이(m)")]
        [SerializeField, Min(0.1f)] private float _length = 3.2f;

        [Tooltip("끝이 안쪽으로 모이는 비율 (0 = 곧게, 1 = 가운데로)")]
        [SerializeField, Range(0.0f, 1.0f)] private float _taper = 0.4f;

        [Tooltip("물결 크기(m). 끝으로 갈수록 커진다.")]
        [SerializeField, Min(0.0f)] private float _amplitude = 0.3f;

        [Tooltip("물결 속도")]
        [SerializeField] private float _speed = 2.0f;

        [Tooltip("촉수 한 가닥에 들어가는 물결 수")]
        [SerializeField] private float _waves = 1.6f;
        #endregion

        #region Private Fields
        private LineRenderer[] _lines;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            _lines = GetComponentsInChildren<LineRenderer>();
        }

        private void Update()
        {
            for (int i = 0; i < _lines.Length; i++)
            {
                LineRenderer line = _lines[i];
                float angle = (i / (float)_lines.Length) * Mathf.PI * 2.0f;
                var direction = new Vector3(Mathf.Cos(angle), 0.0f, Mathf.Sin(angle));
                var side = new Vector3(-direction.z, 0.0f, direction.x);
                int count = line.positionCount;

                for (int k = 0; k < count; k++)
                {
                    float u = k / (float)(count - 1);
                    float phase = (Time.time * _speed) - (u * _waves * Mathf.PI * 2.0f) + i;
                    float radius = _radius * (1.0f - (_taper * u));
                    Vector3 position = (direction * radius) + (Vector3.down * (u * _length));
                    position += ((side * Mathf.Sin(phase)) + (direction * Mathf.Cos(phase * 0.7f))) * (_amplitude * u);
                    line.SetPosition(k, position);
                }
            }
        }
        #endregion
    }
}
