using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Serialization;
using UnityEngine.SceneManagement;

namespace Core.Audio
{
    /// <summary>
    /// 씬 단위 소리 재생 창구. 소리가 필요한 씬에 하나씩 배치한다.
    /// 어디서든 AudioManager.Instance로 접근하지만, DontDestroyOnLoad가 아니라 씬이 내려가면 함께 사라진다.
    /// 재생과 볼륨만 담당하고, 언제 어떤 소리를 낼지는 각 기능이 정한다.
    /// </summary>
    // 사용 흐름 (ObjectPoolManager와 같은 씬 단위 방식)
    // 1. 소리가 필요한 씬에 AudioManager 프리팹을 배치하고, 인스펙터에서 이 씬의 BGM을 넣는다.
    //    BGM 클립이 여러 개면 플레이리스트가 된다. 비우면 무음. 매니저가 없는 씬에서는 Instance가 null이다.
    // 2. 효과음은 소리를 내는 쪽이 [SerializeField] SOAudioData 필드로 들고 있다가 넘긴다.
    //    AudioManager.Instance.PlaySfx(_shootSound, 위치);   AudioManager.Instance.PlayUi(_clickSound);
    //    매니저에 미리 등록할 필요가 없다. 씬에 있는 오브젝트가 참조하는 소리만 그 씬과 함께 메모리에 올라온다.
    //    BGM은 밖에서 직접 바꾸지 않고, SkipBgmTrack으로 이 씬 플레이리스트 안에서만 다음 곡으로 넘긴다.
    // 3. Awake에서 스피커(AudioSource)를 자식으로 만든다.
    //    - BGM 1개: Start에서 페이드인으로 튼다. 플레이리스트는 곡이 끝나기 페이드아웃 시간 전부터 줄이고 다음 곡을 0에서 키운다.
    //    - UI 1개: PlayOneShot으로 짧은 소리를 겹쳐 낸다.
    //    - 3D N개: 발생 위치로 옮겨 단발음을 낸다. 끝나는 시각만 적어 두고, 새 요청 때 끝난 소스를 고른다.
    // 4. 다른 씬으로 갈 때 로딩씬이 Additive로 열리면 BGM을 줄인다. 그 뒤 이 씬과 함께 매니저도 사라지고,
    //    새 씬의 매니저가 자기 곡을 페이드인한다. (로딩 중에는 무음)
    // 5. 일시정지는 GameManager가 AudioListener.pause로 건다.
    //    BGM·UI 소스는 ignoreListenerPause라 계속 들리고 3D 소스만 멈춘다.
    //    멈춘 동안 Time.time도 멈추므로 멈춘 3D 소스가 끝난 것으로 잘못 처리되지 않는다.
    // 6. 볼륨은 매니저가 생길 때 PlayerPrefs에서 읽어 믹서에 적용하고, SaveVolume을 부를 때만 저장한다.
    public class AudioManager : MonoBehaviour
    {
        #region Constants
        // 볼륨 0을 dB로 바꿀 때 log10(0)을 피하기 위한 하한값 (= -80dB)
        private const float MIN_VOLUME = 0.0001f;
        private const float DEFAULT_VOLUME = 1f;

        // PlaySfx의 pitchScale이 0 이하로 들어와도 끝나는 시각 계산이 깨지지 않게 하는 하한값
        private const float MIN_PITCH = 0.1f;
        #endregion

        #region Static Fields
        // (int)EAudioChannel 순서와 같아야 한다.
        // 믹서에서 Expose한 볼륨 파라미터 이름
        private static readonly string[] MixerParams = { "MasterVolume", "BgmVolume", "SfxVolume" };
        private static readonly string[] PrefsKeys = { "Volume_Master", "Volume_Bgm", "Volume_Sfx" };
        #endregion

        #region Serialized Fields
        [Header("References")]
        [SerializeField] private AudioMixer _mixer;
        [FormerlySerializedAs("_musicGroup")]
        [SerializeField] private AudioMixerGroup _bgmGroup;
        [Tooltip("UI 소리와 3D 효과음이 함께 쓴다")]
        [SerializeField] private AudioMixerGroup _sfxGroup;

        [Header("BGM")]
        [Tooltip("이 씬에서 틀 곡. 클립이 여러 개면 플레이리스트가 된다. 비워 두면 무음")]
        [SerializeField] private SOAudioData _bgm;

        // LoadingScreen은 화면 페이드(_fadeDuration, 0.3초)가 끝나면 바로 이 씬을 내린다. 이보다 길면 줄어들다 끊긴다.
        [Tooltip("다른 씬으로 갈 때 BGM을 줄이는 시간 (초). LoadingScreen의 Fade Duration 이하로 맞춘다")]
        [Min(0f)]
        [SerializeField] private float _sceneExitFadeDuration = 0.3f;

        [Header("3D SFX")]
        [Tooltip("동시에 낼 수 있는 3D 효과음 수. 모두 재생 중이면 새 요청은 생략한다")]
        [SerializeField] private int _worldSourceCount = 16;

        [Header("Throttle")]
        [Tooltip("같은 클립을 이 시간(초) 안에 다시 요청하면 건너뛴다")]
        [SerializeField] private float _sameClipInterval = 0.05f;
        #endregion

        #region Private Fields
        private AudioSource _bgmSource;
        private AudioSource _uiSource;
        private AudioSource[] _worldSources;

        // _worldSources[i]가 재생을 마치는 Time.time
        private float[] _worldEndTimes;

        // 클립 → 마지막으로 재생한 Time.unscaledTime
        private readonly Dictionary<AudioClip, float> _lastPlayTimes = new();

        // (int)EAudioChannel 인덱스, 0~1
        private readonly float[] _volumes = new float[MixerParams.Length];

        // 지금 틀고 있는 BGM. 씬을 나가며 줄이기 시작하면 null이 되어 곡 넘기기를 막는다.
        private SOAudioData _currentBgm;
        private Coroutine _bgmRoutine;

        // _currentBgm이 플레이리스트일 때 지금 트는 곡 번호. 같은 SO를 공유하므로 SO가 아니라 여기서 기억한다.
        private int _bgmTrackIndex;

        // 지금 곡을 줄일 때 쓰는 시간. 평소에는 BGM SO의 페이드아웃 시간, 씬을 나갈 때는 _sceneExitFadeDuration.
        private float _bgmFadeOutDuration;
        #endregion

        #region Properties
        /// <summary>지금 씬의 오디오 매니저. 매니저를 배치하지 않은 씬이거나 Awake 전이면 null이다.</summary>
        // DontDestroyOnLoad를 쓰지 않으므로 공용 Singleton<T>을 상속하지 않는다.
        public static AudioManager Instance { get; private set; }
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            // 같은 씬에 매니저가 둘이면 배치 실수다. (이전 씬의 매니저가 아직 남아 있는 경우는 씬으로 구분한다)
            if ((Instance != null) && (Instance.gameObject.scene == gameObject.scene))
                Debug.LogError("[AudioManager] 한 씬에 매니저가 두 개 있습니다. 나중에 Awake된 쪽이 Instance가 됩니다.", this);

            Instance = this;

            if (_mixer == null)
                Debug.LogWarning("[AudioManager] AudioMixer가 연결되지 않아 볼륨 설정이 적용되지 않습니다.", this);

            CreateSources();
            LoadVolumes();
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
        }

        // AudioMixer.SetFloat은 Awake/OnEnable에서 호출하면 적용되지 않으므로 Start에서 적용한다.
        private void Start()
        {
            foreach (EAudioChannel channel in Enum.GetValues(typeof(EAudioChannel)))
                ApplyVolume(channel);

            if (_bgm != null)
                StartBgm(_bgm);
        }

        // 씬이 내려가 매니저가 파괴될 때 Instance를 비운다. 새 씬의 매니저가 먼저 Instance가 됐다면 지우지 않는다.
        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// 이 씬 BGM 플레이리스트의 다음 곡으로 넘긴다. 지금 곡을 줄인 뒤 다음 곡을 키운다.
        /// </summary>
        // [호출] 로비의 곡 넘기기 버튼 등. 다음 곡은 SO의 순서 설정(차례로/무작위)을 따른다.
        //        곡이 1개뿐이거나 BGM이 없으면 무시한다. 페이드 중에 다시 누르면 그 자리에서 다음 곡으로 넘어간다.
        public void SkipBgmTrack()
        {
            if ((_currentBgm == null) || (_currentBgm.ClipCount <= 1))
                return;

            RestartBgmRoutine(CoSkipBgmTrack(_currentBgm));
        }

        /// <summary>
        /// UI 소리(버튼 클릭 등)를 낸다. 일시정지 중에도 들린다.
        /// </summary>
        /// <param name="data">낼 소리</param>
        /// <param name="volumeScale">SO 음량에 곱할 배율 (기본 1)</param>
        public void PlayUi(SOAudioData data, float volumeScale = 1f)
        {
            if (!TryPickClip(data, out AudioClip clip))
                return;

            // PlayOneShot은 소리마다 피치를 줄 수 없어 UI 소리는 피치를 쓰지 않는다.
            _uiSource.PlayOneShot(clip, Mathf.Clamp01(data.Volume * volumeScale));
        }

        /// <summary>
        /// 3D 효과음을 지정 위치에서 한 번 낸다.
        /// </summary>
        /// <param name="data">낼 소리. 들리는 거리(min/max)도 SO에서 정한다</param>
        /// <param name="position">소리가 나는 월드 위치</param>
        /// <param name="volumeScale">SO 음량에 곱할 배율 (기본 1). 예: 차지샷은 크게</param>
        /// <param name="pitchScale">SO에서 고른 피치에 곱할 배율 (기본 1). 예: 차지 단계가 높을수록 높게</param>
        // 3D 소스가 모두 재생 중이면 생략한다.
        public void PlaySfx(SOAudioData data, Vector3 position, float volumeScale = 1f, float pitchScale = 1f)
        {
            if (!TryPickClip(data, out AudioClip clip))
                return;

            int index = FindFreeWorldSource();
            if (index < 0)
                return;

            AudioSource source = _worldSources[index];
            float pitch = Mathf.Max(data.PickPitch() * pitchScale, MIN_PITCH);

            source.transform.position = position;
            source.clip = clip;
            source.volume = Mathf.Clamp01(data.Volume * volumeScale);
            source.pitch = pitch;
            source.minDistance = data.MinDistance;
            source.maxDistance = data.MaxDistance;
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
            _bgmSource = CreateSource("BgmSource", _bgmGroup);
            _bgmSource.loop = true;
            _bgmSource.volume = 0f;
            _bgmSource.ignoreListenerPause = true;

            _uiSource = CreateSource("UiSource", _sfxGroup);
            _uiSource.ignoreListenerPause = true;

            _worldSources = new AudioSource[_worldSourceCount];
            _worldEndTimes = new float[_worldSourceCount];

            for (int i = 0; i < _worldSourceCount; i++)
            {
                // 들리는 거리(min/max)는 재생할 때 SO 값으로 정한다.
                AudioSource source = CreateSource($"WorldSource_{i:00}", _sfxGroup);
                source.spatialBlend = 1f;
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

        // 플레이리스트의 _bgmTrackIndex번 곡을 피치를 새로 골라 튼다. 페이드인을 위해 음량 0에서 시작한다.
        private void PlayBgmTrack(SOAudioData data)
        {
            _bgmSource.clip = data.GetClip(_bgmTrackIndex);
            _bgmSource.pitch = data.PickPitch();
            _bgmSource.volume = 0f;
            _bgmSource.Play();
        }

        // 지금 곡이 끝날 때까지 남은 실제 시간 (초). 피치가 높으면 그만큼 빨리 끝난다.
        private float GetRemainingBgmTime()
        {
            AudioClip clip = _bgmSource.clip;
            if (clip == null)
                return 0f;

            return (clip.length - _bgmSource.time) / _bgmSource.pitch;
        }

        // 플레이리스트의 다음 곡 번호. 차례로면 끝에서 처음으로 돌아가고, 무작위면 방금 곡은 빼고 고른다.
        private int PickNextTrackIndex(SOAudioData data)
        {
            int clipCount = data.ClipCount;
            if (!data.IsShuffle)
                return (_bgmTrackIndex + 1) % clipCount;

            int index = UnityEngine.Random.Range(0, clipCount - 1);
            return (index >= _bgmTrackIndex) ? index + 1 : index;
        }

        // 이 씬의 BGM을 페이드인으로 튼다.
        // [호출] Start
        private void StartBgm(SOAudioData data)
        {
            int clipCount = data.ClipCount;
            if (clipCount == 0)
            {
                Debug.LogWarning($"[AudioManager] BGM에 클립이 없습니다: '{data.name}'", this);
                return;
            }

            // 클립이 1개면 그 곡을 반복하고, 여러 개면 곡마다 끝나야 다음 곡으로 넘어간다.
            _currentBgm = data;
            _bgmSource.loop = clipCount == 1;
            _bgmFadeOutDuration = data.FadeOutDuration;
            _bgmTrackIndex = data.IsShuffle ? UnityEngine.Random.Range(0, clipCount) : 0;

            RestartBgmRoutine(CoPlayTracks(data));
        }

        // 새 요청이 오면 진행 중인 곡 교체를 멈추고 마지막 요청만 적용한다.
        private void RestartBgmRoutine(IEnumerator routine)
        {
            if (_bgmRoutine != null)
                StopCoroutine(_bgmRoutine);

            _bgmRoutine = StartCoroutine(routine);
        }

        // 다른 씬으로 가려고 로딩씬이 Additive로 열리면 이 씬의 BGM을 줄인다.
        // 지금 프로젝트에서 Additive로 여는 씬은 로딩씬뿐이라는 전제다. (SceneLoader.TryLoad·TryStartNewGame 모두 이 경로)
        // [이벤트] SceneManager.sceneLoaded (OnEnable에서 구독)
        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if ((mode != LoadSceneMode.Additive) || (scene == gameObject.scene))
                return;

            _currentBgm = null;
            _bgmFadeOutDuration = _sceneExitFadeDuration;
            RestartBgmRoutine(CoFadeOutBgm());
        }
        #endregion

        #region Coroutines
        // 지금 곡을 줄여 끄고, 플레이리스트의 다음 곡부터 이어서 튼다.
        private IEnumerator CoSkipBgmTrack(SOAudioData data)
        {
            yield return CoFadeOutBgm();

            _bgmTrackIndex = PickNextTrackIndex(data);
            yield return CoPlayTracks(data);
        }

        // _bgmTrackIndex번 곡부터 페이드인으로 튼다.
        // 플레이리스트면 곡이 끝나기 페이드아웃 시간 전부터 줄여 끄고 다음 곡으로 넘어가기를 계속 반복한다.
        private IEnumerator CoPlayTracks(SOAudioData data)
        {
            while (true)
            {
                PlayBgmTrack(data);
                yield return CoFadeBgm(data.Volume, data.FadeInDuration);

                if (data.ClipCount <= 1)
                    yield break;

                // 곡이 끝나기 페이드아웃 시간 전까지 기다린다.
                // 에디터 일시정지·앱 비활성 중에는 코루틴도 멈추므로 그동안 곡을 넘기지 않는다.
                while (_bgmSource.isPlaying && (GetRemainingBgmTime() > data.FadeOutDuration))
                    yield return null;

                yield return CoFadeOutBgm();
                _bgmTrackIndex = PickNextTrackIndex(data);
            }
        }

        // 지금 곡을 _bgmFadeOutDuration 동안 줄인 뒤 멈춘다.
        private IEnumerator CoFadeOutBgm()
        {
            if (_bgmSource.isPlaying)
                yield return CoFadeBgm(0f, _bgmFadeOutDuration);

            _bgmSource.Stop();
        }

        // 일시정지(timeScale 0) 중에도 흐르도록 unscaledDeltaTime을 쓴다. duration이 0이면 바로 바꾼다.
        private IEnumerator CoFadeBgm(float targetVolume, float duration)
        {
            float startVolume = _bgmSource.volume;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                _bgmSource.volume = Mathf.Lerp(startVolume, targetVolume, elapsed / duration);
                yield return null;
            }

            _bgmSource.volume = targetVolume;
        }
        #endregion
    }
}
