using UnityEngine;

namespace Map.Feed
{
    /// <summary>악성 데이터 파편을 지정한 중심 주변에서 독립적인 위성·혜성 궤도로 움직인다.</summary>
    public class MaliciousDataOrbitingFragment : MonoBehaviour
    {
        #region Fields

        [Header("Orbit")]
        [SerializeField] private Transform _orbitCenter;
        [SerializeField] private Vector3 _orbitAxis = Vector3.up;
        [SerializeField] private float _degreesPerSecond = 60f;
        [SerializeField] private float _phaseDegrees;

        [Header("Radial Drift")]
        [SerializeField] private float _radialPulseAmplitude = 0.05f;
        [SerializeField] private float _radialPulseSpeed = 1.5f;

        [Header("Self Spin")]
        [SerializeField] private Vector3 _selfSpinAxis = Vector3.up;
        [SerializeField] private float _selfSpinDegreesPerSecond = 120f;

        private Vector3 _initialOffset;
        private float _orbitAngle;
        private float _elapsedTime;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (_orbitCenter == null)
            {
                enabled = false;
                return;
            }

            Vector3 orbitAxis = _orbitAxis.sqrMagnitude > 0f ? _orbitAxis.normalized : Vector3.up;
            Vector3 authoredOffset = transform.position - _orbitCenter.position;
            _initialOffset = Quaternion.AngleAxis(-_phaseDegrees, orbitAxis) * authoredOffset;
            _orbitAngle = _phaseDegrees;
        }

        private void Update()
        {
            _elapsedTime += Time.deltaTime;
            _orbitAngle += _degreesPerSecond * Time.deltaTime;

            Vector3 orbitAxis = _orbitAxis.sqrMagnitude > 0f ? _orbitAxis.normalized : Vector3.up;
            Vector3 orbitOffset = Quaternion.AngleAxis(_orbitAngle, orbitAxis) * _initialOffset;
            float radialScale = 1f + Mathf.Sin(_elapsedTime * _radialPulseSpeed) * _radialPulseAmplitude;

            transform.position = _orbitCenter.position + orbitOffset * radialScale;

            Vector3 spinAxis = _selfSpinAxis.sqrMagnitude > 0f ? _selfSpinAxis.normalized : Vector3.up;
            transform.Rotate(spinAxis, _selfSpinDegreesPerSecond * Time.deltaTime, Space.Self);
        }

        #endregion
    }
}
