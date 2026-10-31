using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Audio;
using UnityEngine.UI;
using UnityEngine.Video;
using Core;
using Core.InputMap;

namespace UI.Credits
{
    /// <summary>
    /// 라이브 앱 엔딩의 마지막 부분. 스크린 연출을 시작하는 발판에서 Play를 부른다.
    /// TV 밖 대화가 끝나면 대화 장면을 영상 소리와 함께 천천히 검게 어둡게 한 뒤,
    /// 검은 화면에서 노래 2절을 서서히 키우며 크레딧(왼쪽 무대·명단)을 드러낸다.
    /// E를 누르고 있으면 명단이 빨리 올라간다.
    /// 명단이 화면 위로 다 나가면 화면을 검게 덮은 뒤 메인 메뉴로 돌아간다.
    /// 크레딧을 시작할 때 Cutscene 입력 맵을 직접 요청해 조작을 막아 둔다.
    /// </summary>
    public class EndingCredits : MonoBehaviour
    {
        #region Serialized Fields
        [Header("Start")]
        [Tooltip("크레딧 시작 때 오디오를 끌 스크린 VideoPlayer")]
        [SerializeField] private VideoPlayer _videoPlayer;

        [Header("Credits Music")]
        [SerializeField] private AudioClip _creditsMusic;
        [SerializeField] private AudioMixerGroup _creditsMusicGroup;
        [Tooltip("노래 2절이 시작하는 음원 위치(초)")]
        [SerializeField] private float _musicStartSeconds = 67.5f;

        [Header("Credits")]
        [Tooltip("검은 배경과 명단을 함께 담은 그룹. 시작할 때 투명하게 둔다")]
        [SerializeField] private CanvasGroup _creditsGroup;
        [Tooltip("위로 올라가는 명단. 앵커는 부모 아래쪽 가운데, 피벗은 위쪽 가운데(0.5, 1)로 둔다")]
        [SerializeField] private RectTransform _creditsRoll;
        [Tooltip("대화 장면이 검게 어두워지는 시간(초). 영상 소리도 이 시간 동안 줄어든다")]
        [SerializeField] private float _fadeSeconds = 3.0f;
        [Tooltip("검은 화면에서 크레딧(왼쪽 무대·명단)이 드러나는 시간(초). 노래도 이 시간 동안 커진다")]
        [SerializeField] private float _revealSeconds = 2.0f;
        [Tooltip("명단이 올라가는 속도(px/초, 캔버스 기준)")]
        [SerializeField] private float _scrollSpeed = 80.0f;
        [Tooltip("E를 누르고 있는 동안 명단 속도에 곱하는 배수. 바꾸면 화면의 안내 문구도 같이 고친다")]
        [SerializeField] private float _fastForwardMultiplier = 2.0f;
        [Tooltip("명단이 다 올라간 뒤 메인 메뉴로 돌아가기 전까지 기다리는 시간(초)")]
        [SerializeField] private float _endHoldSeconds = 2.0f;

        [Header("End Fade Out")]
        [Tooltip("크레딧 맨 위를 덮는 검은 이미지. 시작할 때는 이것으로 검은 화면을 만든 뒤 걷어 크레딧을 드러내고, 끝날 때 다시 덮는다")]
        [SerializeField] private Graphic _fadeOutCover;
        [Tooltip("끝날 때 화면이 검게 덮이는 시간(초)")]
        [SerializeField] private float _fadeOutSeconds = 2.0f;

        [Header("안 해도 됨 (GameObject ← EndingCastStage, 꺼 둠)")]
        [Tooltip("크레딧 왼쪽 캐릭터 무대. 크레딧이 시작될 때 켠다")]
        [SerializeField] private GameObject _castStage;
        #endregion

        #region Private Fields
        private bool _hasStarted;
        private AudioSource _musicSource;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            Debug.Assert(_creditsGroup != null, $"[{name}] Credits Group이 연결되지 않았습니다.");
            Debug.Assert(_creditsRoll != null, $"[{name}] Credits Roll이 연결되지 않았습니다.");
            Debug.Assert(_creditsMusic != null, $"[{name}] Credits Music이 연결되지 않았습니다.");

            _creditsGroup.alpha = 0.0f;
            _creditsGroup.blocksRaycasts = false;
            SetFadeOutAlpha(0.0f);

            _musicSource = gameObject.AddComponent<AudioSource>();
            _musicSource.playOnAwake = false;
            _musicSource.spatialBlend = 0.0f;
            _musicSource.ignoreListenerPause = true;
            _musicSource.outputAudioMixerGroup = _creditsMusicGroup;
        }
        #endregion

        #region Public Methods
        // 시작 위치를 기다렸다가 크레딧부터 메인 메뉴 복귀까지 진행한다. 한 번만 동작한다.
        public void Play()
        {
            if (_hasStarted) return;

            _hasStarted = true;
            StartCoroutine(CoPlayEnding());
        }
        #endregion

        #region Private Methods
        // 영상 소리 크기를 바꾼다. Direct 출력일 때만 조절할 수 있다.
        private void SetVideoVolume(float volume)
        {
            if ((_videoPlayer == null) || (_videoPlayer.audioOutputMode != VideoAudioOutputMode.Direct)) return;

            _videoPlayer.SetDirectAudioVolume(0, volume);
        }

        // 노래를 소리 없이 틀어 둔다. 크레딧이 드러나는 동안 키운다.
        private void StartCreditsMusic()
        {
            if ((_videoPlayer != null) && (_videoPlayer.audioOutputMode == VideoAudioOutputMode.Direct))
                _videoPlayer.SetDirectAudioMute(0, true);

            if (_creditsMusic == null) return;

            _musicSource.clip = _creditsMusic;
            _musicSource.time = Mathf.Clamp(_musicStartSeconds, 0.0f, _creditsMusic.length - 0.1f);
            _musicSource.volume = 0.0f;
            _musicSource.Play();
        }

        private void SetFadeOutAlpha(float alpha)
        {
            if (_fadeOutCover == null) return;

            Color color = _fadeOutCover.color;
            color.a = alpha;
            _fadeOutCover.color = color;
        }

        // 설정창이 열려 있으면(Pause) true. 그동안은 연출 시간도 세지 않는다.
        private bool IsGamePaused()
        {
            GameManager gameManager = GameManager.Instance;
            return (gameManager != null) && (gameManager.CurrentState != GameManager.GameState.Playing);
        }

        // E를 누르고 있으면 true. 로딩 영상 스킵처럼 키보드를 직접 읽는다.
        private bool IsFastForwardHeld()
        {
            Keyboard keyboard = Keyboard.current;
            return (keyboard != null) && keyboard.eKey.isPressed;
        }
        #endregion

        #region Coroutines
        private IEnumerator CoPlayEnding()
        {
            InputMapController.Instance?.RequestMap(this, EInputMapType.Cutscene);

            // 대화 장면을 영상 소리와 함께 천천히 검게 어둡게 한다. 덮개를 먼저 불투명하게 둬서 그룹이 보이면 검은 화면만 보인다.
            SetFadeOutAlpha(1.0f);
            yield return CoFadeIn();

            // 검은 화면에서 노래를 키우며 크레딧을 드러낸다.
            StartCreditsMusic();
            if (_castStage != null) _castStage.SetActive(true);
            yield return CoReveal();
            yield return CoScrollRoll();
            yield return CoWait(_endHoldSeconds);
            yield return CoFadeOut();

            // 설정창의 "메인 화면으로"와 같은 경로로 돌아가 커서·게임 시간·세션 상태도 함께 정리한다.
            GameManager.Instance?.ReturnToMainMenu();
        }

        private IEnumerator CoFadeIn()
        {
            float elapsed = 0.0f;
            while (elapsed < _fadeSeconds)
            {
                yield return null;
                if (IsGamePaused()) continue;

                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsed / _fadeSeconds);
                _creditsGroup.alpha = progress;
                SetVideoVolume(1.0f - progress);
            }

            _creditsGroup.alpha = 1.0f;
            SetVideoVolume(0.0f);
        }

        // 검은 덮개를 걷어 크레딧을 드러내고 노래를 키운다. 덮개를 연결하지 않았으면 노래만 바로 키운다.
        private IEnumerator CoReveal()
        {
            float elapsed = 0.0f;
            while ((_fadeOutCover != null) && (elapsed < _revealSeconds))
            {
                yield return null;
                if (IsGamePaused()) continue;

                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsed / _revealSeconds);
                SetFadeOutAlpha(1.0f - progress);
                _musicSource.volume = progress;
            }

            SetFadeOutAlpha(0.0f);
            _musicSource.volume = 1.0f;
        }

        // 크레딧 화면 전체(왼쪽 무대·명단)를 검게 덮는다. 덮개를 연결하지 않았으면 바로 넘어간다.
        private IEnumerator CoFadeOut()
        {
            if (_fadeOutCover == null) yield break;

            float elapsed = 0.0f;
            while (elapsed < _fadeOutSeconds)
            {
                yield return null;
                if (IsGamePaused()) continue;

                elapsed += Time.unscaledDeltaTime;
                SetFadeOutAlpha(Mathf.Clamp01(elapsed / _fadeOutSeconds));
            }

            SetFadeOutAlpha(1.0f);
        }

        // 명단 위쪽 끝을 화면 아래쪽에서 시작해, 아래쪽 끝이 화면 위로 다 나갈 때까지 올린다.
        private IEnumerator CoScrollRoll()
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(_creditsRoll);

            RectTransform viewport = _creditsRoll.parent as RectTransform;
            float viewportHeight = (viewport != null) ? viewport.rect.height : 0.0f;
            float endY = _creditsRoll.rect.height + viewportHeight;

            Vector2 position = _creditsRoll.anchoredPosition;
            position.y = 0.0f;
            _creditsRoll.anchoredPosition = position;

            while (position.y < endY)
            {
                yield return null;
                if (IsGamePaused()) continue;

                float speed = IsFastForwardHeld() ? _scrollSpeed * _fastForwardMultiplier : _scrollSpeed;
                position.y = Mathf.Min(endY, position.y + (speed * Time.unscaledDeltaTime));
                _creditsRoll.anchoredPosition = position;
            }
        }

        private IEnumerator CoWait(float seconds)
        {
            float elapsed = 0.0f;
            while (elapsed < seconds)
            {
                yield return null;
                if (IsGamePaused()) continue;

                elapsed += Time.unscaledDeltaTime;
            }
        }
        #endregion
    }
}
