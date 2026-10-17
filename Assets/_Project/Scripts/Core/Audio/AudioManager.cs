using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using SceneTransition;

namespace Core.Audio
{
    /// <summary>
    /// 씬이 바뀌어도 유지되는 소리 재생 창구. 재생과 볼륨만 담당하고, 언제 어떤 소리를 낼지는 각 기능이 정한다.
    /// </summary>
    // 사용 흐름
    // 1. 이 컴포넌트가 붙은 AudioManager 프리팹을 소리가 나는 모든 씬에 배치한다. 중복은 Singleton이 지운다.
    //    (매니저가 없는 씬에서 Instance를 부르면 호출마다 씬 전체를 찾고 에러를 낸다.)
    // 2. Awake에서 스피커(AudioSource)를 자식으로 만든다.
    //    - BGM 1개: 곡이 바뀌면 줄였다가 교체하고 다시 키운다. 같은 곡이면 이어서 재생한다.
    //    - UI 1개: PlayOneShot으로 짧은 소리를 겹쳐 낸다.
    //    - 3D N개: 발생 위치로 옮겨 단발음을 낸다. 끝나는 시각만 적어 두고, 새 요청 때 끝난 소스를 고른다.
    // 3. 씬이 로드되면 SOBgmTable에서 그 씬의 곡을 찾아 튼다. 씬마다 BGM 컴포넌트를 두지 않는다.
    // 4. 일시정지는 GameManager가 AudioListener.pause로 건다.
    //    BGM·UI 소스는 ignoreListenerPause라 계속 들리고 3D 소스만 멈춘다.
    //    멈춘 동안 Time.time도 멈추므로 멈춘 3D 소스가 끝난 것으로 잘못 처리되지 않는다.
    // 5. 볼륨은 0~1 값을 dB로 바꿔 믹서에 적용하고, SaveVolume을 부를 때만 PlayerPrefs에 저장한다.
    public class AudioManager : Singleton<AudioManager>
    {
        #region Constants
        // MainScene은 ESceneType에도 빌드 설정에도 없어서 이름으로 알아본다. (SceneLoader.LOADING_SCENE_NAME과 같은 방식)
        private const string MAIN_SCENE_NAME = "MainScene";

        // 볼륨 0을 dB로 바꿀 때 log10(0)을 피하기 위한 하한값 (= -80dB)
        private const float MIN_VOLUME = 0.0001f;
        private const float DEFAULT_VOLUME = 1f;
        #endregion

        #region Static Fields
        // (int)EAudioChannel 순서와 같아야 한다.
        // 믹서에서 Expose한 볼륨 파라미터 이름
        private static readonly string[] MixerParams = { "MasterVolume", "MusicVolume", "SfxVolume" };
        private static readonly string[] PrefsKeys = { "Volume_Master", "Volume_Music", "Volume_Sfx" };
        #endregion

        #region Serialized Fields
        [Header("References")]
        [SerializeField] private AudioMixer _mixer;
        [SerializeField] private AudioMixerGroup _musicGroup;
        [Tooltip("UI 소리와 3D 효과음이 함께 쓴다")]
        [SerializeField] private AudioMixerGroup _sfxGroup;
        [SerializeField] private SOBgmTable _bgmTable;

        [Header("BGM")]
        [Tooltip("곡을 바꿀 때 줄이는 시간과 키우는 시간 (초, 일시정지 중에도 흐른다)")]
        [SerializeField] private float _bgmFadeDuration = 1f;

        [Header("3D SFX")]
        [Tooltip("동시에 낼 수 있는 3D 효과음 수. 모두 재생 중이면 새 요청은 생략한다")]
        [SerializeField] private int _worldSourceCount = 16;
        [SerializeField] private float _worldMinDistance = 1f;
        [SerializeField] private float _worldMaxDistance = 30f;

        [Header("Throttle")]
        [Tooltip("같은 클립을 이 시간(초) 안에 다시 요청하면 건너뛴다")]
        [SerializeField] private float _sameClipInterval = 0.05f;
        #endregion

        #region Private Fields
        private AudioSource _musicSource;
        private AudioSource _uiSource;
        private AudioSource[] _worldSources;

        // _worldSources[i]가 재생을 마치는 Time.time
        private float[] _worldEndTimes;

        // 클립 → 마지막으로 재생한 Time.unscaledTime
        private readonly Dictionary<AudioClip, float> _lastPlayTimes = new();

        // (int)EAudioChannel 인덱스, 0~1
        private readonly float[] _volumes = new float[MixerParams.Length];

        // 지금 틀고 있거나 바꾸는 중인 곡. 같은 곡 재요청을 거르는 데 쓴다.
        private SOAudioData _currentBgm;
        private Coroutine _bgmRoutine;
        #endregion

        #region Unity Lifecycle
        public override void Awake()
        {
            base.Awake();
            if (Instance != this)
            {
                return;
            }

            if (_mixer == null)
                Debug.LogWarning("[AudioManager] AudioMixer가 연결되지 않아 볼륨 설정이 적용되지 않습니다.", this);

            CreateSources();
            LoadVolumes();
        }

        // 첫 씬의 sceneLoaded는 OnEnable 다음, Start 전에 오므로 여기서 구독해야 첫 씬 곡도 튼다.
        private void OnEnable()
        {
            if (Instance != this)
            {
                return;
            }

            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
        }

        // AudioMixer.SetFloat은 Awake/OnEnable에서 호출하면 적용되지 않으므로 Start에서 적용한다.
        private void Start()
        {
            if (Instance != this)
            {
                return;
            }

            foreach (EAudioChannel channel in Enum.GetValues(typeof(EAudioChannel)))
                ApplyVolume(channel);
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// BGM을 바꾼다. 지금 곡을 줄였다가 새 곡으로 바꾸고 다시 키운다. 같은 곡이면 이어서 재생한다.
        /// </summary>
        /// <param name="data">틀 곡</param>
        // [호출] 씬 로드 시 HandleSceneLoaded, 씬 안에서 곡을 바꿔야 하는 기능(보스전 등)
        public void PlayBgm(SOAudioData data)
        {
            if (data == null)
            {
                Debug.LogWarning("[AudioManager] PlayBgm에 곡이 없습니다. 끄려면 StopBgm을 호출하세요.", this);
                return;
            }

            if (data == _currentBgm)
                return;

            _currentBgm = data;
            RestartBgmRoutine(CoSwitchBgm(data));
        }

        /// <summary>
        /// 지금 곡을 줄인 뒤 끈다.
        /// </summary>
        public void StopBgm()
        {
            if ((_currentBgm == null) && !_musicSource.isPlaying)
                return;

            _currentBgm = null;
            RestartBgmRoutine(CoSwitchBgm(null));
        }

        /// <summary>
        /// UI 소리(버튼 클릭 등)를 낸다. 일시정지 중에도 들린다.
        /// </summary>
        /// <param name="data">낼 소리</param>
        public void PlayUi(SOAudioData data)
        {
            if (!TryPickClip(data, out AudioClip clip))
                return;

            // PlayOneShot은 소리마다 피치를 줄 수 없어 UI 소리는 피치를 쓰지 않는다.
            _uiSource.PlayOneShot(clip, data.Volume);
        }

        /// <summary>
        /// 3D 효과음을 지정 위치에서 한 번 낸다.
        /// </summary>
        /// <param name="data">낼 소리</param>
        /// <param name="position">소리가 나는 월드 위치</param>
        // 3D 소스가 모두 재생 중이면 생략한다.
        public void PlaySfx(SOAudioData data, Vector3 position)
        {
            if (!TryPickClip(data, out AudioClip clip))
                return;

            int index = FindFreeWorldSource();
            if (index < 0)
                return;

            AudioSource source = _worldSources[index];
            float pitch = data.PickPitch();

            source.transform.position = position;
            source.clip = clip;
            source.volume = data.Volume;
            source.pitch = pitch;
            source.Play();

            _worldEndTimes[index] = Time.time + (clip.length / pitch);
        }

        /// <summary>
        /// 채널 볼륨을 바꿔 바로 믹서에 적용한다. 디스크 저장은 SaveVolume에서 한다.
        /// </summary>
        /// <param name="channel">바꿀 채널</param>
        /// <param name="value">0~1 볼륨</param>
        // [호출] SettingsPresenter가 슬라이더를 움직일 때
        public void SetVolume(EAudioChannel channel, float value)
        {
            _volumes[(int)channel] = Mathf.Clamp01(value);
            ApplyVolume(channel);
        }

        public float GetVolume(EAudioChannel channel) => _volumes[(int)channel];

        /// <summary>
        /// 지금 볼륨을 PlayerPrefs에 저장한다.
        /// </summary>
        // [호출] SettingsPresenter.Close. 슬라이더를 움직일 때마다 디스크에 쓰지 않도록 닫을 때만 저장한다.
        public void SaveVolume()
        {
            for (int i = 0; i < _volumes.Length; i++)
                PlayerPrefs.SetFloat(PrefsKeys[i], _volumes[i]);

            PlayerPrefs.Save();
        }
        #endregion

        #region Private Methods
        // [호출] Awake에서 한 번
        private void CreateSources()
        {
            _musicSource = CreateSource("MusicSource", _musicGroup);
            _musicSource.loop = true;
            _musicSource.volume = 0f;
            _musicSource.ignoreListenerPause = true;

            _uiSource = CreateSource("UiSource", _sfxGroup);
            _uiSource.ignoreListenerPause = true;

            _worldSources = new AudioSource[_worldSourceCount];
            _worldEndTimes = new float[_worldSourceCount];

            for (int i = 0; i < _worldSourceCount; i++)
            {
                AudioSource source = CreateSource($"WorldSource_{i:00}", _sfxGroup);
                source.spatialBlend = 1f;
                source.minDistance = _worldMinDistance;
                source.maxDistance = _worldMaxDistance;
                _worldSources[i] = source;
            }
        }

        private AudioSource CreateSource(string sourceName, AudioMixerGroup group)
        {
            GameObject sourceObject = new GameObject(sourceName);
            sourceObject.transform.SetParent(transform, false);

            AudioSource source = sourceObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.outputAudioMixerGroup = group;
            return source;
        }

        // 끝나는 시각이 지난 3D 소스를 찾는다. 모두 재생 중이면 -1
        private int FindFreeWorldSource()
        {
            for (int i = 0; i < _worldEndTimes.Length; i++)
            {
                if (_worldEndTimes[i] <= Time.time)
                    return i;
            }

            return -1;
        }

        // 클립을 고르고, 같은 클립이 너무 짧은 간격으로 다시 요청됐으면 거른다.
        private bool TryPickClip(SOAudioData data, out AudioClip clip)
        {
            clip = (data != null) ? data.PickClip() : null;
            if (clip == null)
            {
                Debug.LogWarning($"[AudioManager] 재생할 클립이 없습니다: '{(data != null ? data.name : "null")}'", this);
                return false;
            }

            // 일시정지 중 UI 소리도 걸러야 하므로 unscaledTime을 쓴다.
            float now = Time.unscaledTime;
            if (_lastPlayTimes.TryGetValue(clip, out float lastTime) && (now - lastTime < _sameClipInterval))
                return false;

            _lastPlayTimes[clip] = now;
            return true;
        }

        private void LoadVolumes()
        {
            for (int i = 0; i < _volumes.Length; i++)
                _volumes[i] = PlayerPrefs.GetFloat(PrefsKeys[i], DEFAULT_VOLUME);
        }

        private void ApplyVolume(EAudioChannel channel)
        {
            if (_mixer == null)
                return;

            int index = (int)channel;
            float decibel = Mathf.Log10(Mathf.Max(_volumes[index], MIN_VOLUME)) * 20f;
            _mixer.SetFloat(MixerParams[index], decibel);
        }

        // 새 요청이 오면 진행 중인 곡 교체를 멈추고 마지막 요청만 적용한다.
        private void RestartBgmRoutine(IEnumerator routine)
        {
            if (_bgmRoutine != null)
                StopCoroutine(_bgmRoutine);

            _bgmRoutine = StartCoroutine(routine);
        }

        // Scene => 방금 로드된 씬, LoadSceneMode => Single(새 씬만) / Additive(기존 씬 유지)
        // [이벤트] SceneManager.sceneLoaded (OnEnable에서 구독)
        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // 로딩씬은 Additive로 겹쳐 열리므로 무시한다 → 로딩 중에는 이전 곡이 이어진다.
            if ((mode == LoadSceneMode.Additive) || (_bgmTable == null))
                return;

            SOAudioData bgm;
            if (scene.name == MAIN_SCENE_NAME)
            {
                bgm = _bgmTable.MainMenuBgm;
            }
            else
            {
                // SceneLoader와 같이 빌드 인덱스 = (int)ESceneType 규칙을 따른다.
                // 표에 없는 씬(테스트 씬 등)은 지금 곡을 유지한다.
                bool isGameScene = Enum.IsDefined(typeof(ESceneType), scene.buildIndex);
                if (!isGameScene || !_bgmTable.TryGetBgm((ESceneType)scene.buildIndex, out bgm))
                    return;
            }

            if (bgm != null)
                PlayBgm(bgm);
            else
                StopBgm();
        }
        #endregion

        #region Coroutines
        // 지금 곡을 줄여 끄고, 다음 곡이 있으면 바꿔서 키운다.
        private IEnumerator CoSwitchBgm(SOAudioData next)
        {
            if (_musicSource.isPlaying)
                yield return CoFadeMusic(0f);

            _musicSource.Stop();

            if (next == null)
                yield break;

            AudioClip clip = next.PickClip();
            if (clip == null)
            {
                Debug.LogWarning($"[AudioManager] BGM에 클립이 없습니다: '{next.name}'", this);
                yield break;
            }

            _musicSource.clip = clip;
            _musicSource.pitch = next.PickPitch();
            _musicSource.volume = 0f;
            _musicSource.Play();

            yield return CoFadeMusic(next.Volume);
        }

        // 일시정지(timeScale 0) 중에도 흐르도록 unscaledDeltaTime을 쓴다.
        private IEnumerator CoFadeMusic(float targetVolume)
        {
            float startVolume = _musicSource.volume;
            float elapsed = 0f;

            while (elapsed < _bgmFadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                _musicSource.volume = Mathf.Lerp(startVolume, targetVolume, elapsed / _bgmFadeDuration);
                yield return null;
            }

            _musicSource.volume = targetVolume;
        }
        #endregion
    }
}
