using UnityEngine;
using Map.Gimmicks;

namespace Map.Feed
{
    /// <summary>밀기와 같은 스위치로 팬 회전·횡풍 리본을 제어한다.</summary>
    public class FeedWindFanVisual : MonoBehaviour
    {
        [SerializeField] private GimmickSwitch _source;
        [SerializeField] private Transform _rotor;
        [SerializeField] private LineRenderer[] _ribbons;
        [SerializeField] private ParticleSystem _particles;

        private float _speed;
        private bool _wasOn;

        private void LateUpdate()
        {
            bool isOn = (_source != null) && _source.IsOn;
            _speed = Mathf.MoveTowards(_speed, isOn ? 700f : 0f, Time.deltaTime * 1100f);
            _rotor.Rotate(Vector3.right, _speed * Time.deltaTime, Space.Self);

            if (isOn != _wasOn)
            {
                if (_particles != null)
                {
                    if (isOn) _particles.Play();
                    else _particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                }
                _wasOn = isOn;
            }

            for (int r = 0; r < _ribbons.Length; r++)
            {
                LineRenderer line = _ribbons[r];
                line.enabled = isOn;
                if (!isOn) continue;
                for (int i = 0; i < line.positionCount; i++)
                {
                    float t = i / (float)(line.positionCount - 1);
                    // 본체의 로컬 -X 방향으로 발판을 가로지른다.
                    line.SetPosition(i, new Vector3(
                        Mathf.Lerp(4.35f, -2.4f, t),
                        0.8f + r * 0.62f + Mathf.Sin(t * 9f + Time.time * 4f + r) * 0.09f,
                        (r - 1) * 0.8f + Mathf.Sin(t * 6f - Time.time * 3f) * 0.08f));
                }
            }
        }
    }
}
