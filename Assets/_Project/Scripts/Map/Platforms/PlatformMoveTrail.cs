using UnityEngine;

namespace Map.Platforms
{
    /// <summary>
    /// 발판이 움직이는 동안에만 뒤쪽으로 파티클을 내보내 진행 방향을 보여 준다.
    /// 누가 발판을 옮기는지와 상관없이 매 프레임 위치 변화로 이동 여부·방향을 판단한다.
    /// 멈추면 새로 내보내지 않고 이미 나온 파티클만 자연스럽게 사라진다.
    /// 튜토리얼 스캔 경보(전역 셰이더 값 _HoloAlarm)가 켜지면 파티클 색도 경보 색으로 바뀐다.
    /// </summary>
    public class PlatformMoveTrail : MonoBehaviour
    {
        #region Constants
        private static readonly int AlarmId = Shader.PropertyToID("_HoloAlarm");
        private static readonly int AlarmColorId = Shader.PropertyToID("_HoloAlarmColor");
        #endregion

        #region Serialized Fields
        [Header("References")]
        [Tooltip("진행 방향(수평)을 바라보도록 돌릴 오브젝트. 파티클을 이 아래 뒤쪽에 둔다.")]
        [SerializeField] private Transform _directionRoot;

        [SerializeField] private ParticleSystem[] _particles;

        [Header("Detection")]
        [Tooltip("이 속도(m/s)보다 빠르면 움직이는 것으로 본다.")]
        [SerializeField, Min(0.0f)] private float _minSpeed = 0.3f;

        [Tooltip("한 프레임에 이보다 멀리 옮겨지면 순간이동(풀 재사용 등)으로 보고 방출하지 않는다.")]
        [SerializeField, Min(0.1f)] private float _teleportDistance = 2.0f;

        [Tooltip("진행 방향으로 돌아가는 빠르기. 원을 돌 때 파티클 위치가 튀지 않게 한다.")]
        [SerializeField, Min(0.1f)] private float _turnSharpness = 10.0f;
        #endregion

        #region Private Fields
        private Vector3 _lastPosition;
        private bool _isEmitting = true;
        private Color[] _baseColors;
        private float _appliedAlarm = -1.0f;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            _baseColors = new Color[_particles.Length];
            for (int i = 0; i < _particles.Length; i++)
            {
                _baseColors[i] = _particles[i].main.startColor.color;
            }
        }

        private void OnEnable()
        {
            _lastPosition = transform.position;
            SetEmitting(false);
        }

        private void LateUpdate()
        {
            Vector3 delta = transform.position - _lastPosition;
            _lastPosition = transform.position;

            if (delta.magnitude > _teleportDistance)
            {
                SetEmitting(false);
                return;
            }

            Vector3 flatDelta = Vector3.ProjectOnPlane(delta, Vector3.up);
            bool isMoving = (Time.deltaTime > 0.0f) && (flatDelta.magnitude / Time.deltaTime > _minSpeed);

            if (isMoving && (_directionRoot != null))
            {
                Quaternion target = Quaternion.LookRotation(flatDelta, Vector3.up);
                float t = 1.0f - Mathf.Exp(-_turnSharpness * Time.deltaTime);
                _directionRoot.rotation = _isEmitting ? Quaternion.Slerp(_directionRoot.rotation, target, t) : target;
            }

            SetEmitting(isMoving);

            if (isMoving)
            {
                ApplyAlarmColor();
            }
        }
        #endregion

        #region Private Methods
        private void SetEmitting(bool isEmitting)
        {
            if (_isEmitting == isEmitting) return;
            _isEmitting = isEmitting;

            foreach (ParticleSystem particle in _particles)
            {
                if (isEmitting)
                {
                    particle.Play(true);
                }
                else
                {
                    particle.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                }
            }
        }

        // 새로 나오는 파티클 색만 바꾼다. 이미 나온 파티클은 원래 색으로 사라진다.
        private void ApplyAlarmColor()
        {
            float alarm = Shader.GetGlobalFloat(AlarmId);
            if (Mathf.Approximately(alarm, _appliedAlarm)) return;
            _appliedAlarm = alarm;

            Color alarmColor = Shader.GetGlobalColor(AlarmColorId);
            for (int i = 0; i < _particles.Length; i++)
            {
                ParticleSystem.MainModule main = _particles[i].main;
                main.startColor = Color.Lerp(_baseColors[i], alarmColor, alarm);
            }
        }
        #endregion
    }
}
