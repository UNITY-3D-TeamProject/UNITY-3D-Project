using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Core;

namespace Map.Tutorial
{
    /// <summary>
    /// 튜토리얼 분위기 연출: 시작할 때의 어둠과 스캔 경보.
    /// 전역 셰이더 값 _HoloDarkness · _HoloAlarm (HoloCommon.hlsl) 과 안개·비네트를 함께 바꾼다.
    /// - 어둠: 시작하면 _visibleInDark 에 넣은 오브젝트(펫까지 가는 발판 등)만 보이고 나머지 렌더러는 모두 꺼진다(하늘도 검게).
    ///   Reveal() 을 부르면 카메라 주변부터 바깥으로 물결처럼 다시 켜지며 서서히 드러난다.
    ///   나중에 생기는 것(플레이어·펫·새 발판)은 영향을 받지 않는다.
    /// - 경보: TriggerAlarm() 을 부르면 붉게 바뀌고 경고 파티클이 쏟아진다. 한 번 켜지면 계속 유지된다.
    /// 씬 설정(안개)은 꺼질 때 원래대로 되돌린다.
    /// </summary>
    public class TutorialAtmosphere : MonoBehaviour
    {
        #region Constants
        private static readonly int DarknessId = Shader.PropertyToID("_HoloDarkness");
        private static readonly int AlarmId = Shader.PropertyToID("_HoloAlarm");
        private static readonly int AlarmColorId = Shader.PropertyToID("_HoloAlarmColor");
        #endregion

        #region Serialized Fields
        [Header("Darkness")]
        [Tooltip("켜면 어두운 상태로 시작한다.")]
        [SerializeField] private bool _isDarkAtStart = true;

        [Tooltip("Reveal() 뒤 주위가 다 드러나기까지 걸리는 시간(초)")]
        [SerializeField, Min(0.1f)] private float _revealDuration = 5.0f;

        [Tooltip("어둠 속에서도 보일 오브젝트 (자식 포함). 이 밖의 씬 렌더러는 시작할 때 꺼진다.")]
        [SerializeField] private GameObject[] _visibleInDark;

        [SerializeField] private Color _fogColor = new Color(0.01f, 0.02f, 0.04f, 1.0f);

        [Tooltip("어두울 때 안개 농도 (Exponential Squared). 0.06 이면 약 25 m 너머가 안 보인다.")]
        [SerializeField, Min(0.0f)] private float _darkFogDensity = 0.06f;

        [Tooltip("다 드러난 뒤 안개 농도")]
        [SerializeField, Min(0.0f)] private float _litFogDensity = 0.004f;

        [Header("Alarm")]
        [Tooltip("경보로 바뀌는 데 걸리는 시간(초)")]
        [SerializeField, Min(0.0f)] private float _alarmRiseDuration = 1.2f;

        [SerializeField] private Color _alarmColor = new Color(1.0f, 0.16f, 0.22f, 1.0f);

        [Tooltip("경보 때 화면 가장자리를 붉게 물들일 볼륨 (선택)")]
        [SerializeField] private Volume _volume;

        [SerializeField, Range(0.0f, 1.0f)] private float _alarmVignetteIntensity = 0.42f;

        [Tooltip("경보 때 붉게 바꾸고 더 많이 띄울 경고 파티클들")]
        [SerializeField] private ParticleSystem[] _warningParticles;

        [Tooltip("경보 때 경고 파티클 하나가 1초에 띄우는 수")]
        [SerializeField, Min(0.0f)] private float _alarmEmissionRate = 0.8f;

        [SerializeField, Min(1)] private int _alarmMaxParticles = 12;
        #endregion

        #region Private Fields
        private float _darkness;
        private float _alarm;
        private bool _isRevealing;
        private bool _isAlarmOn;

        private bool _savedFog;
        private FogMode _savedFogMode;
        private Color _savedFogColor;
        private float _savedFogDensity;

        private Vignette _vignette;
        private float _baseVignetteIntensity;

        // 어둠 때 꺼 둔 렌더러 (드러날 때 다시 켠다)
        private readonly List<Renderer> _hiddenRenderers = new List<Renderer>();
        #endregion

        #region Properties
        /// <summary>경보가 켜졌는지 여부.</summary>
        public bool IsAlarmOn => _isAlarmOn;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            _savedFog = RenderSettings.fog;
            _savedFogMode = RenderSettings.fogMode;
            _savedFogColor = RenderSettings.fogColor;
            _savedFogDensity = RenderSettings.fogDensity;

            // 공유 프로필을 바꾸지 않도록 실행 중에만 쓰는 사본(profile)을 쓴다
            if ((_volume != null) && _volume.profile.TryGet(out Vignette vignette))
            {
                _vignette = vignette;
                _baseVignetteIntensity = vignette.intensity.value;
            }

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = _fogColor;

            _darkness = _isDarkAtStart ? 1.0f : 0.0f;
            _alarm = 0.0f;
            Shader.SetGlobalColor(AlarmColorId, _alarmColor);
            Apply();
        }

        private void Start()
        {
            // 다른 컴포넌트가 Awake 에서 일부러 꺼 둔 렌더러(스캔 전 숨은 발판 등)를 켜진 것으로 착각하지 않도록,
            // 모든 Awake 가 끝난 뒤 지금 켜져 있는 렌더러만 끈다. (드러날 때 이것들만 다시 켠다)
            if (_isDarkAtStart)
            {
                HideAllButVisibleInDark();
            }
        }

        private void OnDisable()
        {
            Shader.SetGlobalFloat(DarknessId, 0.0f);
            Shader.SetGlobalFloat(AlarmId, 0.0f);

            RenderSettings.fog = _savedFog;
            RenderSettings.fogMode = _savedFogMode;
            RenderSettings.fogColor = _savedFogColor;
            RenderSettings.fogDensity = _savedFogDensity;
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// 어둠을 걷어 주위를 서서히 드러낸다. (예: 재개 큐브·펫 대화 끝)
        /// </summary>
        public void Reveal()
        {
            if (_isRevealing || (_darkness <= 0.0f)) return;

            _isRevealing = true;
            StartCoroutine(CoReveal());
        }

        /// <summary>
        /// 악성 데이터를 인식한 경보를 켠다. 한 번 켜지면 계속 유지된다. (예: 스캔 드론 아래 통과)
        /// </summary>
        public void TriggerAlarm()
        {
            if (_isAlarmOn) return;

            _isAlarmOn = true;
            BoostWarningParticles();
            StartCoroutine(CoRaiseAlarm());
        }
        #endregion

        #region Private Methods
        private void Apply()
        {
            Shader.SetGlobalFloat(DarknessId, _darkness);
            Shader.SetGlobalFloat(AlarmId, _alarm);

            // 어두울수록 안개가 짙다. 줄어들 때는 천천히 걷히도록 제곱으로 섞는다.
            RenderSettings.fogDensity = Mathf.Lerp(_litFogDensity, _darkFogDensity, _darkness * _darkness);

            if (_vignette != null)
            {
                _vignette.color.overrideState = true;
                _vignette.intensity.overrideState = true;
                _vignette.color.value = Color.Lerp(Color.black, _alarmColor, _alarm);
                _vignette.intensity.value = Mathf.Lerp(_baseVignetteIntensity, _alarmVignetteIntensity, _alarm);
            }
        }

        private void HideAllButVisibleInDark()
        {
            Renderer[] renderers = FindObjectsByType<Renderer>(FindObjectsInactive.Exclude);
            Transform playerRoot = GameManager.Instance?.CurrentPlayerFacade?.transform.root;

            foreach (Renderer target in renderers)
            {
                if ((playerRoot != null) && target.transform.IsChildOf(playerRoot)) continue;
                if (!target.enabled || IsVisibleInDark(target.transform)) continue;

                target.enabled = false;
                _hiddenRenderers.Add(target);
            }
        }

        private bool IsVisibleInDark(Transform target)
        {
            if (_visibleInDark == null) return false;

            foreach (GameObject root in _visibleInDark)
            {
                if ((root != null) && target.IsChildOf(root.transform)) return true;
            }

            return false;
        }

        // 가까운 것부터 차례로 켠다. progress: 0 ~ 1
        private void ShowHiddenRenderers(Vector3 origin, float maxDistance, float progress)
        {
            float reach = maxDistance * progress;

            for (int i = _hiddenRenderers.Count - 1; i >= 0; i--)
            {
                Renderer target = _hiddenRenderers[i];
                if (target == null)
                {
                    _hiddenRenderers.RemoveAt(i);
                    continue;
                }

                if (Vector3.Distance(origin, target.bounds.center) > reach) continue;

                target.enabled = true;
                _hiddenRenderers.RemoveAt(i);
            }
        }

        private void BoostWarningParticles()
        {
            if (_warningParticles == null) return;

            foreach (ParticleSystem particles in _warningParticles)
            {
                if (particles == null) continue;

                ParticleSystem.MainModule main = particles.main;
                main.startColor = _alarmColor;
                main.maxParticles = Mathf.Max(main.maxParticles, _alarmMaxParticles);

                ParticleSystem.EmissionModule emission = particles.emission;
                emission.rateOverTime = _alarmEmissionRate;

                particles.Emit(2);
                if (!particles.isPlaying) particles.Play();
            }
        }
        #endregion

        #region Coroutines
        private IEnumerator CoReveal()
        {
            float start = _darkness;
            float time = 0.0f;

            // 카메라(없으면 이 오브젝트) 주변부터 바깥으로 켜진다
            Vector3 origin = (Camera.main != null) ? Camera.main.transform.position : transform.position;
            float maxDistance = 0.0f;
            foreach (Renderer target in _hiddenRenderers)
            {
                if (target != null) maxDistance = Mathf.Max(maxDistance, Vector3.Distance(origin, target.bounds.center));
            }

            while (time < _revealDuration)
            {
                time += Time.deltaTime;
                float progress = Mathf.SmoothStep(0.0f, 1.0f, time / _revealDuration);
                _darkness = Mathf.Lerp(start, 0.0f, progress);
                ShowHiddenRenderers(origin, maxDistance, progress);
                Apply();
                yield return null;
            }

            _darkness = 0.0f;
            ShowHiddenRenderers(origin, maxDistance, 1.0f);
            Apply();
            _isRevealing = false;
        }

        private IEnumerator CoRaiseAlarm()
        {
            float time = 0.0f;

            while (time < _alarmRiseDuration)
            {
                time += Time.deltaTime;
                _alarm = Mathf.Clamp01(time / Mathf.Max(_alarmRiseDuration, 0.0001f));
                Apply();
                yield return null;
            }

            _alarm = 1.0f;
            Apply();
        }
        #endregion
    }
}
