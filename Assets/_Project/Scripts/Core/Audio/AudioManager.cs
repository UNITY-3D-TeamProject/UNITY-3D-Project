using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Serialization;
using UnityEngine.SceneManagement;
using SceneTransition;

namespace Core.Audio
{
    // 씬 단위 소리 재생 창구. 소리가 필요한 씬에 하나씩 배치한다.
    // 어디서든 AudioManager.Instance로 접근하지만, DontDestroyOnLoad가 아니라 씬이 내려가면 함께 사라진다.
    // 재생과 볼륨만 담당하고, 언제 어떤 소리를 낼지는 각 기능이 정한다.
    // 사용 흐름 (ObjectPoolManager와 같은 씬 단위 방식)
    // 1. 소리가 필요한 씬에 AudioManager 프리팹을 배치하고, 인스펙터에서 이 씬의 BGM을 넣는다.
    //    BGM 클립이 여러 개면 플레이리스트가 된다. 비우면 무음. 매니저가 없는 씬에서는 Instance가 null이다.
    // 2. 효과음은 소리를 내는 쪽이 [SerializeField] SOAudioData 필드로 들고 있다가 넘긴다.
    //    AudioManager.Instance.PlaySfx(_shootSound, 위치);   AudioManager.Instance.PlayUi(_clickSound);
    //    매니저에 미리 등록할 필요가 없다. 씬에 있는 오브젝트가 참조하는 소리만 그 씬과 함께 메모리에 올라온다.
    //    BGM은 밖에서 직접 바꾸지 않고, PlayNextBgm으로 이 씬 플레이리스트 안에서만 다음 곡으로 넘긴다.
    // 3. Awake에서 스피커(AudioSource)를 자식으로 만든다.
    //    - BGM 1개: 기본은 Start에서 페이드인으로 튼다. 설정에 따라 입력 후 시작할 수 있다.
    //      플레이리스트는 곡이 끝나기 페이드아웃 시간 전부터 줄이고 다음 곡을 0에서 키운다.
    //    - UI 1개: PlayOneShot으로 짧은 소리를 겹쳐 낸다.
    //    - 3D N개: 발생 위치로 옮겨 단발음을 낸다. 끝나는 시각만 적어 두고, 새 요청 때 끝난 소스를 고른다.
    //      끝난 소스가 없으면 상한(_maxSfxSourceCount)까지 하나씩 더 만든다.
    //      반복음(차지음 등)은 PlaySfxLoop로 같은 소스를 빌려 대상 Transform을 따라가고, StopSfx할 때까지 점유한다.
    //      PlaySfx·PlaySfxLoop가 돌려준 핸들(SAudioHandle)을 StopSfx에 넘기면 그 소리를 중간에 끊는다.
    //      _chargeHandle = AudioManager.Instance.PlaySfxLoop(_chargeSound, transform);
    //      AudioManager.Instance?.StopSfx(_chargeHandle); _chargeHandle = default;
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
        private const string LOBBY_SCENE_NAME = "Map_1_Lobby";
        private const string MAIN_SCENE_NAME = "MainScene";

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

        [Tooltip("파일 앱에서 로비로 돌아왔을 때만 재생할 BGM")]
        [SerializeField] private SOAudioData _fileReturnLobbyBgm;

        [Tooltip("끄면 Start에서 재생하지 않고 StartSceneBgm 호출을 기다린다")]
        [SerializeField] private bool _playBgmOnStart = true;

        // LoadingScreen은 화면 페이드(_fadeDuration, 0.3초)가 끝나면 바로 이 씬을 내린다. 이보다 길면 줄어들다 끊긴다.
        [Tooltip("다른 씬으로 갈 때 BGM을 줄이는 시간 (초). LoadingScreen의 Fade Duration 이하로 맞춘다")]
        [Min(0f)]
        [SerializeField] private float _sceneExitFadeDuration = 0.3f;

        [Header("3D SFX")]
        [Tooltip("미리 만들어 둘 3D 효과음 소스 수. 모두 재생 중이면 Max Sfx Source Count까지 하나씩 더 만든다")]
        [FormerlySerializedAs("_worldSourceCount")]
        [SerializeField] private int _sfxSourceCount = 16;

        // 실제로 동시에 들리는 수는 프로젝트 설정 Max Real Voices(32)까지이고, 넘으면 Unity가 priority·크기로 고른다.
        [Tooltip("3D 효과음 소스를 늘릴 수 있는 최대 수. 이만큼 모두 재생 중이면 새 요청은 생략하고 경고를 한 번 남긴다")]
        [Min(1)]
        [SerializeField] private int _maxSfxSourceCount = 64;

        [Header("Throttle")]
        [Tooltip("UI에서 같은 클립을 이 시간(초) 안에 다시 요청하면 건너뛴다. 3D 효과음에는 적용하지 않는다")]
        [SerializeField] private float _sameClipInterval = 0.05f;
        #endregion

        #region Private Fields
        private AudioSource _bgmSource;
        private AudioSource _uiSource;
        private AudioSource[] _sfxSources;

        // _sfxSources[i]가 재생을 마치는 Time.time
        private float[] _sfxEndTimes;

        // _sfxSources[i]로 마지막에 낸 소리의 번호(0 = 없음·이미 끊음). StopSfx가 끊을 소스를 찾을 때 쓴다.
        private int[] _sfxPlayIds;
        private int _nextSfxId = 1;

        // 이 매니저가 발급한 핸들인지 구분하는 토큰. 이전 씬 매니저의 핸들로 이 씬의 소리를 끊지 않게 한다.
        private readonly object _handleOwner = new();

        // _sfxSources[i]가 반복음일 때 따라갈 대상
        private Transform[] _sfxFollowTargets;

        // 소스 상한 경고를 이미 남겼는지. 요청마다 찍으면 로그가 넘치므로 한 번만 남긴다.
        private bool _hasWarnedSfxLimit;

        // UI 클립 → 마지막으로 재생한 Time.unscaledTime
        private readonly Dictionary<AudioClip, float> _lastPlayTimes = new();

        // (int)EAudioChannel 인덱스, 0~1
        private readonly float[] _volumes = new float[MixerParams.Length];

        // 이 씬의 BGM을 틀고 있는지. 씬을 나가며 줄이기 시작하면 false가 되어 곡 넘기기를 막는다.
        private bool _isBgmPlaying;
        private Coroutine _bgmRoutine;

        // _bgm이 플레이리스트일 때 지금 트는 곡 번호. 같은 SO를 공유하므로 SO가 아니라 여기서 기억한다.
        private int _bgmTrackIndex;

        // 지금 곡을 줄일 때 쓰는 시간. 평소에는 BGM SO의 페이드아웃 시간, 씬을 나갈 때는 _sceneExitFadeDuration.
        private float _bgmFadeOutDuration;
        #endregion

        #region Properties
        // 지금 씬의 오디오 매니저. 매니저를 배치하지 않은 씬이거나 Awake 전이면 null이다.
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

            SetupSources();
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

            if (gameObject.scene.name == LOBBY_SCENE_NAME &&
                SceneLoader.PreviousScene == ESceneType.File &&
                _fileReturnLobbyBgm != null)
                _bgm = _fileReturnLobbyBgm;

            if (_playBgmOnStart)
                StartSceneBgm();
        }

        // 반복 효과음 소스를 대상 위치로 옮긴다. 대상이 파괴되면 마지막 위치에 남긴다.
        private void LateUpdate()
        {
            for (int i = 0; i < _sfxFollowTargets.Length; i++)
            {
                Transform target = _sfxFollowTargets[i];
                if (target == null)
                    continue;

                _sfxSources[i].transform.position = target.position;
            }
        }

        // 씬이 내려가 매니저가 파괴될 때 Instance를 비운다. 새 씬의 매니저가 먼저 Instance가 됐다면 지우지 않는다.
        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }
        #endregion

        #region Public Methods
        // 씬 BGM을 아직 틀지 않았다면 시작한다. 메인 메뉴는 스플래시 입력 뒤에 호출한다.
        public void StartSceneBgm()
        {
            if ((_bgm == null) || _isBgmPlaying)
                return;

            StartBgm(_bgm);
        }

        // 라운드 전환 등 씬 안에서 BGM을 바꿀 때 현재 곡을 페이드아웃한 뒤 새 곡을 튼다.
        public void ChangeBgm(SOAudioData audioData)
        {
            if ((audioData == null) || ((_bgm == audioData) && _isBgmPlaying))
                return;

            _bgm = audioData;
            _isBgmPlaying = false;
            RestartBgmRoutine(CoChangeBgm(audioData));
        }

        // 이 씬 BGM 플레이리스트의 다음 곡으로 넘긴다. 지금 곡을 줄인 뒤 다음 곡을 키운다.
        // [호출] 로비의 곡 넘기기 버튼 등. 다음 곡은 SO의 순서 설정(차례로/무작위)을 따른다.
        //        곡이 1개뿐이거나 BGM이 없으면 무시한다. 페이드 중에 다시 누르면 그 자리에서 다음 곡으로 넘어간다.
        public void PlayNextBgm()
        {
            if (!_isBgmPlaying || (_bgm.ClipCount <= 1))
                return;

            RestartBgmRoutine(CoPlayNextBgm(_bgm));
        }

        // 지금 BGM을 fadeDuration 동안 줄여 끈다. 다시 틀려면 ChangeBgm을 부른다.
        // [호출] 라이브 앱 엔딩 영상이 시작되기 전(씬 UnityEvent). 영상 소리와 BGM이 겹치지 않게 한다.
        public void StopBgm(float fadeDuration)
        {
            _isBgmPlaying = false;
            _bgmFadeOutDuration = fadeDuration;
            RestartBgmRoutine(CoFadeOutBgm());
        }

        // UI 소리(버튼 클릭 등)를 낸다. 일시정지 중에도 들린다.
        public void PlayUi(SOAudioData audioData, float volumeScale = 1f)
        {
            if (!TryPickClip(audioData, out AudioClip clip))
                return;

            // PlayOneShot은 소리마다 피치를 줄 수 없어 UI 소리는 피치를 쓰지 않는다.
            _uiSource.PlayOneShot(clip, Mathf.Clamp01(audioData.Volume * volumeScale));
        }

        // 3D 효과음을 지정 위치에서 한 번 낸다.
        // 같은 클립의 동시 요청도 허용한다. 서로 떨어진 장애물의 소리를 누락시키지 않는다.
        // 3D 소스가 상한까지 모두 재생 중이면 생략한다.
        // 돌려주는 핸들은 중간에 끊을 때만 StopSfx에 넘긴다. 끊을 일이 없으면 무시해도 된다. 생략되면 default
        public SAudioHandle PlaySfx(SOAudioData audioData, Vector3 position, float volumeScale = 1f, float pitchScale = 1f)
        {
            if (gameObject.scene.name == MAIN_SCENE_NAME)
                return default;

            if (!TryPickClip(audioData, out AudioClip clip, false))
                return default;

            int index = FindFreeSfxSource();
            if (index < 0)
                return default;

            AudioSource source = _sfxSources[index];
            float pitch = Mathf.Max(audioData.PickPitch() * pitchScale, MIN_PITCH);

            source.transform.position = position;
            source.clip = clip;
            source.spread = 0f;
            source.volume = Mathf.Clamp01(audioData.Volume * volumeScale);
            source.pitch = pitch;
            source.rolloffMode = audioData.RolloffMode;
            source.minDistance = audioData.MinDistance;
            source.maxDistance = audioData.MaxDistance;
            source.Play();

            _sfxEndTimes[index] = Time.time + (clip.length / pitch);

            int id = _nextSfxId++;
            _sfxPlayIds[index] = id;
            return new SAudioHandle(id, _handleOwner);
        }

        // 3D 효과음을 target 위치에서 반복한다. StopSfx를 부를 때까지 3D 소스 하나를 점유한다.
        // [호출] 차지 시작 등. 같은 클립 중복 차단은 하지 않는다. 3D 소스가 상한까지 모두 재생 중이면 default를 돌려준다.
        //        target이 파괴되면 마지막 위치에서 계속 나므로, 부르는 쪽이 OnDisable 등에서 꼭 멈춘다.
        //        spread(0~360도)를 180으로 주면 거리 감쇠는 그대로 두고 좌우 쏠림만 없앤다.
        public SAudioHandle PlaySfxLoop(SOAudioData audioData, Transform target, float volumeScale = 1f, float pitchScale = 1f, float spread = 0f)
        {
            if (gameObject.scene.name == MAIN_SCENE_NAME)
                return default;

            AudioClip clip = (audioData != null) ? audioData.PickClip() : null;
            if ((clip == null) || (target == null))
            {
                Debug.LogWarning($"[AudioManager] 반복할 클립 또는 대상이 없습니다: '{(audioData != null ? audioData.name : "null")}'", this);
                return default;
            }

            int index = FindFreeSfxSource();
            if (index < 0)
                return default;

            AudioSource source = _sfxSources[index];
            source.transform.position = target.position;
            source.clip = clip;
            source.loop = true;
            source.spread = Mathf.Clamp(spread, 0f, 360f);
            source.volume = Mathf.Clamp01(audioData.Volume * volumeScale);
            source.pitch = Mathf.Max(audioData.PickPitch() * pitchScale, MIN_PITCH);
            source.rolloffMode = audioData.RolloffMode;
            source.minDistance = audioData.MinDistance;
            source.maxDistance = audioData.MaxDistance;
            source.Play();

            int id = _nextSfxId++;
            _sfxEndTimes[index] = float.MaxValue;
            _sfxPlayIds[index] = id;
            _sfxFollowTargets[index] = target;
            return new SAudioHandle(id, _handleOwner);
        }

        // PlaySfx·PlaySfxLoop가 돌려준 핸들의 소리를 끊는다. fadeDuration(초)을 주면 그동안 줄인 뒤 끊는다.
        // [호출] 발사·취소·소유자 OnDisable. default·이미 끝난 소리·이미 끊은 핸들·다른 매니저의 핸들은 무시한다.
        //        끝난 소리의 소스를 다른 소리가 다시 쓰고 있어도 그 소리는 끊지 않는다.
        public void StopSfx(SAudioHandle handle, float fadeDuration = 0f)
        {
            if ((!handle.IsValid) || (!handle.IsOwnedBy(_handleOwner)))
                return;

            int index = Array.IndexOf(_sfxPlayIds, handle.Id);
            if (index < 0)
                return;

            // 번호는 바로 끝낸다.
            _sfxPlayIds[index] = 0;

            // 이미 다 재생된 단발음이면 할 일이 없다.
            if (_sfxEndTimes[index] <= Time.time)
                return;

            if (fadeDuration > 0f)
            {
                // 페이드가 끝날 때까지 소스를 점유한 채로 둔다.
                _sfxEndTimes[index] = float.MaxValue;
                StartCoroutine(CoFadeOutSfx(index, fadeDuration));
            }
            else
            {
                ReleaseSfx(index);
            }
        }

        // 채널 볼륨을 바꿔 바로 믹서에 적용한다. 디스크 저장은 SaveVolume에서 한다.
        // [호출] SettingsPresenter가 슬라이더를 움직일 때
        public void SetVolume(EAudioChannel channel, float value)
        {
            _volumes[(int)channel] = Mathf.Clamp01(value);
            ApplyVolume(channel);
        }

        public float GetVolume(EAudioChannel channel) => _volumes[(int)channel];

        // 지금 볼륨을 PlayerPrefs에 저장한다.
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
        private void SetupSources()
        {
            _bgmSource = CreateSource("BgmSource", _bgmGroup);
            _bgmSource.loop = true;
            _bgmSource.volume = 0f;
            _bgmSource.ignoreListenerPause = true;

            _uiSource = CreateSource("UiSource", _sfxGroup);
            _uiSource.ignoreListenerPause = true;

            _sfxSources = new AudioSource[_sfxSourceCount];
            _sfxEndTimes = new float[_sfxSourceCount];
            _sfxPlayIds = new int[_sfxSourceCount];
            _sfxFollowTargets = new Transform[_sfxSourceCount];

            for (int i = 0; i < _sfxSourceCount; i++)
                _sfxSources[i] = CreateSfxSource(i);
        }

        // 들리는 거리(min/max)는 재생할 때 SO 값으로 정한다.
        private AudioSource CreateSfxSource(int index)
        {
            AudioSource source = CreateSource($"SfxSource_{index:00}", _sfxGroup);
            source.spatialBlend = 1f;
            return source;
        }

        // 3D 소스를 하나 더 만들어 배열 끝에 붙이고 그 번호를 돌려준다.
        // [호출] FindFreeSfxSource에서 빈 소스가 없을 때. 늘린 소스는 씬이 내려갈 때까지 남아 다시 쓴다.
        private int AddSfxSource()
        {
            int index = _sfxSources.Length;
            int count = index + 1;

            Array.Resize(ref _sfxSources, count);
            Array.Resize(ref _sfxEndTimes, count);
            Array.Resize(ref _sfxPlayIds, count);
            Array.Resize(ref _sfxFollowTargets, count);

            _sfxSources[index] = CreateSfxSource(index);
            return index;
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

        // 끝나는 시각이 지난 3D 소스를 찾는다. 모두 재생 중이면 상한까지 하나 더 만들고, 상한이면 -1
        private int FindFreeSfxSource()
        {
            for (int i = 0; i < _sfxEndTimes.Length; i++)
            {
                if (_sfxEndTimes[i] <= Time.time)
                    return i;
            }

            if (_sfxSources.Length < _maxSfxSourceCount)
                return AddSfxSource();

            // 상한에 닿는 건 대개 StopSfx를 빠뜨린 반복음이 쌓인 경우다.
            if (!_hasWarnedSfxLimit)
            {
                _hasWarnedSfxLimit = true;
                Debug.LogWarning($"[AudioManager] 3D 효과음 소스가 상한({_maxSfxSourceCount}개)까지 모두 재생 중이라 새 요청을 생략합니다. 멈추지 않은 반복음이 있는지 확인하세요.", this);
            }

            return -1;
        }

        // 3D 소스를 멈추고 다음 요청이 쓸 수 있게 돌려놓는다. 반복음이었으면 반복·따라가기도 끈다.
        private void ReleaseSfx(int index)
        {
            AudioSource source = _sfxSources[index];
            source.Stop();
            source.loop = false;

            _sfxEndTimes[index] = 0f;
            _sfxFollowTargets[index] = null;
        }

        // 클립을 고른다. UI만 짧은 간격의 중복 요청을 거르고, 서로 다른 장애물이 쓰는 3D 효과음은 모두 허용한다.
        private bool TryPickClip(SOAudioData audioData, out AudioClip clip, bool shouldThrottle = true)
        {
            clip = (audioData != null) ? audioData.PickClip() : null;
            if (clip == null)
            {
                Debug.LogWarning($"[AudioManager] 재생할 클립이 없습니다: '{(audioData != null ? audioData.name : "null")}'", this);
                return false;
            }

            if (!shouldThrottle)
                return true;

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
        private void PlayBgmTrack(SOAudioData audioData)
        {
            _bgmSource.clip = audioData.GetClip(_bgmTrackIndex);
            _bgmSource.pitch = audioData.PickPitch();
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
        private int PickNextTrackIndex(SOAudioData audioData)
        {
            int clipCount = audioData.ClipCount;
            if (!audioData.IsShuffle)
                return (_bgmTrackIndex + 1) % clipCount;

            int index = UnityEngine.Random.Range(0, clipCount - 1);
            return (index >= _bgmTrackIndex) ? index + 1 : index;
        }

        // 이 씬의 BGM을 페이드인으로 튼다.
        // [호출] StartSceneBgm
        private void StartBgm(SOAudioData audioData)
        {
            int clipCount = audioData.ClipCount;
            if (clipCount == 0)
            {
                Debug.LogWarning($"[AudioManager] BGM에 클립이 없습니다: '{audioData.name}'", this);
                return;
            }

            // 클립이 1개면 그 곡을 반복하고, 여러 개면 곡마다 끝나야 다음 곡으로 넘어간다.
            _isBgmPlaying = true;
            _bgmSource.loop = clipCount == 1;
            _bgmFadeOutDuration = audioData.FadeOutDuration;
            int randomFirstTrackCount = audioData.RandomFirstTrackCount;
            if (randomFirstTrackCount > 0)
                _bgmTrackIndex = UnityEngine.Random.Range(0, randomFirstTrackCount);
            else
                _bgmTrackIndex = audioData.IsShuffle ? UnityEngine.Random.Range(0, clipCount) : 0;

            RestartBgmRoutine(CoPlayTracks(audioData));
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

            _isBgmPlaying = false;
            _bgmFadeOutDuration = _sceneExitFadeDuration;
            RestartBgmRoutine(CoFadeOutBgm());
        }
        #endregion

        #region Coroutines
        private IEnumerator CoChangeBgm(SOAudioData audioData)
        {
            yield return CoFadeOutBgm();
            _bgmRoutine = null;
            StartBgm(audioData);
        }

        // 지금 곡을 줄여 끄고, 플레이리스트의 다음 곡부터 이어서 튼다.
        private IEnumerator CoPlayNextBgm(SOAudioData audioData)
        {
            yield return CoFadeOutBgm();

            _bgmTrackIndex = PickNextTrackIndex(audioData);
            yield return CoPlayTracks(audioData);
        }

        // _bgmTrackIndex번 곡부터 페이드인으로 튼다.
        // 플레이리스트면 곡이 끝나기 페이드아웃 시간 전부터 줄여 끄고 다음 곡으로 넘어가기를 계속 반복한다.
        private IEnumerator CoPlayTracks(SOAudioData audioData)
        {
            while (true)
            {
                PlayBgmTrack(audioData);
                yield return CoFadeBgm(audioData.Volume, audioData.FadeInDuration);

                if (audioData.ClipCount <= 1)
                    yield break;

                // 곡이 끝나기 페이드아웃 시간 전까지 기다린다.
                // 에디터 일시정지·앱 비활성 중에는 코루틴도 멈추므로 그동안 곡을 넘기지 않는다.
                while (GetRemainingBgmTime() > audioData.FadeOutDuration)
                    yield return null;

                yield return CoFadeOutBgm();
                _bgmTrackIndex = PickNextTrackIndex(audioData);
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

        // 3D 효과음을 duration 동안 줄인 뒤 멈춘다. 3D 소리이므로 일시정지(timeScale 0) 중에는 페이드도 멈춘다.
        private IEnumerator CoFadeOutSfx(int index, float duration)
        {
            AudioSource source = _sfxSources[index];
            float startVolume = source.volume;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                source.volume = Mathf.Lerp(startVolume, 0f, elapsed / duration);
                yield return null;
            }

            ReleaseSfx(index);
        }
        #endregion
    }
}
