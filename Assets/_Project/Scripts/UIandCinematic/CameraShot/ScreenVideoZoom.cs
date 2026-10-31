using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.Video;
using Unity.Cinemachine;
using Map.Live;
using UI.Credits;

namespace UIandCinematic
{
    /// <summary>
    /// 씬 스크린(VideoPlayer)에 영상이 나오기 시작하면 TV를 잠시 보여 준 뒤, 카메라를 화면 안으로 밀어 넣고 영상을 전체화면 UI로 띄운다.
    /// 영상이 끝나기 조금 전에 전체화면을 걷어 내고, 카메라를 화면 밖으로 빼 마무리 카메라(TV 앞 인물)로 옮긴다.
    /// 영상을 틀고 끝내는 일은 스크린 쪽(LiveEndingScreen 등)이 맡고, 이 컴포넌트는 카메라와 전체화면만 맡는다.
    /// 카메라는 재생 중인 컷씬의 샷 카메라 위에 켜서 덮는다(Priority가 같으면 나중에 켠 카메라가 화면을 잡는다).
    /// 마무리 카메라에 도착하면 E로 넘기는 대화 컷씬으로 이어지고, 대화가 끝나면 크레딧을 시작한다.
    /// 영상을 보는 동안 E를 길게 누르면 영상을 나오는 위치로 넘기고 바로 TV 밖으로 나온다.
    /// 블렌드 시간은 Brain 기본 블렌드를 잠시 바꿔 정한다. CinematicManager가 다음 샷에서 다시 정하고, 연출이 끝나면 원래 값으로 돌린다.
    /// </summary>
    public class ScreenVideoZoom : MonoBehaviour
    {
        #region Serialized Fields
        [Header("Video")]
        [Tooltip("스크린에 영상을 그리는 VideoPlayer")]
        [SerializeField] private VideoPlayer _videoPlayer;

        [Header("Cameras")]
        [Tooltip("화면 안으로 들어가는 카메라. 스크린에 바짝 붙여 화면이 시야를 채우게 둔다. Priority는 연출 샷 카메라 이상으로 둔다")]
        [SerializeField] private CinemachineCamera _zoomInCamera;
        [Tooltip("전체화면에서 나온 뒤 옮겨 갈 카메라(TV 앞 인물). 영상 뒤 컷씬의 첫 카메라와 같은 카메라를 넣는다")]
        [SerializeField] private CinemachineCamera _outroCamera;

        [Header("Fullscreen")]
        [Tooltip("영상 RenderTexture를 띄우는 전체화면 UI 그룹. 시작할 때 투명하게 둔다")]
        [SerializeField] private CanvasGroup _fullscreenGroup;

        [Header("Video Time (영상 재생 위치, 초)")]
        [Tooltip("전체화면 시작 시각. 영상이 이 위치(초)에 오면 TV 안으로 들어가 전체화면으로 보여 준다. 그 전까지는 TV를 보여 준다")]
        [SerializeField] private float _enterAtVideoSeconds = 4.0f;
        [Tooltip("전체화면 종료 시각. 영상이 이 위치(초)에 오면 전체화면에서 나와 TV 밖의 폰 주인과 펫을 보여 준다. 영상 길이보다 길면 영상이 끝날 때 나온다")]
        [SerializeField] private float _exitAtVideoSeconds = 63.0f;

        [Header("Transition")]
        [Tooltip("화면 안으로 들어가는 카메라 블렌드 시간(초)")]
        [SerializeField] private float _zoomInSeconds = 1.5f;
        [Tooltip("전체화면이 나타나고 사라지는 시간(초)")]
        [SerializeField] private float _fadeSeconds = 0.6f;
        [Tooltip("화면 밖으로 나와 마무리 카메라로 가는 블렌드 시간(초)")]
        [SerializeField] private float _zoomOutSeconds = 3.0f;

        [Header("Skip")]
        [Tooltip("영상을 보는 동안 E를 이 시간(초)만큼 누르고 있으면 영상을 건너뛰고 TV 밖 대화로 간다")]
        [Min(0.1f)]
        [SerializeField] private float _skipHoldSeconds = 1.5f;
        [Tooltip("영상을 보는 동안 띄우는 스킵 안내. TV 밖으로 나가기 시작하면 숨긴다")]
        [SerializeField] private GameObject _skipPrompt;
        [Tooltip("E를 누른 시간만큼 차는 원형 게이지(Filled 이미지)")]
        [SerializeField] private Image _skipProgress;

        [Header("Outro Cutscene")]
        [SerializeField] private LiveEndingScreen _endingScreen;
        [Tooltip("TV 밖 대화 컷씬 번호. 마지막 단계는 TV 앞을 길게 비추는 카메라 단계로 둔다. 그 단계가 시작되면(대화가 끝나면) 카메라·HUD를 그대로 둔 채 크레딧을 시작한다")]
        [SerializeField] private int _outroCutsceneId = 35;
        [SerializeField] private EndingCredits _endingCredits;
        #endregion

        #region Private Fields
        private bool _hasStarted;
        private float _skipHeldTime;
        private bool _isOutroDialogueDone;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            Debug.Assert(_videoPlayer != null, $"[{name}] VideoPlayer가 연결되지 않았습니다.");
            Debug.Assert(_zoomInCamera != null, $"[{name}] Zoom In Camera가 연결되지 않았습니다.");
            Debug.Assert(_outroCamera != null, $"[{name}] Outro Camera가 연결되지 않았습니다.");
            Debug.Assert(_fullscreenGroup != null, $"[{name}] Fullscreen Group이 연결되지 않았습니다.");

            _fullscreenGroup.alpha = 0.0f;
            _fullscreenGroup.blocksRaycasts = false;
            if (_skipPrompt != null) _skipPrompt.SetActive(false);
            if (_skipProgress != null) _skipProgress.fillAmount = 0.0f;
        }

        private void OnDestroy()
        {
            CinematicManager.OnCutsceneStepStarted -= HandleCutsceneStepStarted;
        }
        #endregion

        #region Public Methods
        // 스크린 영상이 나오기를 기다렸다가 연출을 시작한다. 스크린 컷씬을 시작하는 발판에서 함께 부른다. 한 번만 동작한다.
        public void Play()
        {
            if (_hasStarted) return;

            _hasStarted = true;
            StartCoroutine(CoPlay());
        }
        #endregion

        #region Private Methods
        // previous를 끄고 target을 켜서 blendSeconds 동안 넘어간다.
        private void SwitchCamera(CinemachineCamera target, CinemachineCamera previous, float blendSeconds)
        {
            CinemachineBrain brain = (CinemachineBrain.ActiveBrainCount > 0) ? CinemachineBrain.GetActiveBrain(0) : null;
            if (brain != null)
                brain.DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.EaseInOut, blendSeconds);

            target.enabled = true;

            if (previous != null)
                previous.enabled = false;
        }

        // 일시정지(창이 포커스를 잃으면 VideoPlayer가 스스로 멈춘다)도 재생 중으로 본다. 끝까지 재생했거나 멈췄을 때만 false.
        private bool IsVideoRunning()
        {
            return _videoPlayer.isPlaying || _videoPlayer.isPaused;
        }

        // 매 프레임 불러 E를 누른 시간을 쌓고 게이지에 표시한다. 떼면 처음부터 다시 센다. 로딩 영상 스킵처럼 키보드를 직접 읽는다.
        private bool IsSkipHoldComplete()
        {
            Keyboard keyboard = Keyboard.current;
            bool isHeld = (keyboard != null) && keyboard.eKey.isPressed;

            _skipHeldTime = isHeld ? (_skipHeldTime + Time.unscaledDeltaTime) : 0.0f;
            if (_skipProgress != null) _skipProgress.fillAmount = Mathf.Clamp01(_skipHeldTime / _skipHoldSeconds);

            return _skipHeldTime >= _skipHoldSeconds;
        }

        // 대화 컷씬의 마지막 카메라 단계가 시작되면 대화가 끝난 것으로 본다.
        private void HandleCutsceneStepStarted(SOCutscene cutscene, int index)
        {
            if (index != cutscene.StepCount - 1) return;
            if (!cutscene.TryGetStep(index, out SCutsceneStep step) || (step.Type != ECutsceneStepType.Camera)) return;

            _isOutroDialogueDone = true;
        }

        // 영상을 나오는 위치로 넘긴다. 대화 동안 그 뒤 노래가 이어진다.
        private void SkipVideoToExit()
        {
            if (_exitAtVideoSeconds < _videoPlayer.length)
                _videoPlayer.time = _exitAtVideoSeconds;
        }
        #endregion

        #region Coroutines
        private IEnumerator CoPlay()
        {
            // 스크린 쪽이 스크린 카메라 도착 뒤 영상을 틀 때까지 기다린다.
            while (!_videoPlayer.isPlaying)
                yield return null;

            if (_skipPrompt != null) _skipPrompt.SetActive(true);

            // 들어갈 위치까지 TV를 보여 준다. 영상이 먼저 멈추면 들어가지 않는다. E를 길게 누르면 들어가지 않고 바로 나온다.
            bool isSkipped = false;
            while (_videoPlayer.time < _enterAtVideoSeconds)
            {
                if (!IsVideoRunning())
                {
                    if (_skipPrompt != null) _skipPrompt.SetActive(false);
                    yield break;
                }
                if (IsSkipHoldComplete())
                {
                    isSkipped = true;
                    break;
                }
                yield return null;
            }

            if (!isSkipped)
            {
                // 화면 안으로 들어가다가 다 들어갈 무렵 전체화면으로 바꾼다.
                SwitchCamera(_zoomInCamera, null, _zoomInSeconds);
                yield return CoWait(Mathf.Max(0.0f, _zoomInSeconds - _fadeSeconds));
                yield return CoFade(1.0f);

                // 나올 위치까지 전체화면으로 본다. 영상이 먼저 멈추면 바로 나온다. E를 길게 누르면 그 자리에서 나온다.
                while (IsVideoRunning() && (_videoPlayer.time < _exitAtVideoSeconds))
                {
                    if (IsSkipHoldComplete())
                    {
                        isSkipped = true;
                        break;
                    }
                    yield return null;
                }
            }

            if (_skipPrompt != null) _skipPrompt.SetActive(false);
            if (isSkipped)
                SkipVideoToExit();

            yield return CoFade(0.0f);
            SwitchCamera(_outroCamera, _zoomInCamera, _zoomOutSeconds);

            // TV 밖으로 다 나오면 스크린 컷씬을 E 대화 컷씬에 넘긴다.
            yield return CoWait(_zoomOutSeconds);
            CinematicManager cinematicManager = CinematicManager.Instance;
            if (cinematicManager == null) yield break;

            _endingScreen?.HandOffCutscene();
            if (cinematicManager.IsPlaying) cinematicManager.Stop();

            _isOutroDialogueDone = false;
            CinematicManager.OnCutsceneStepStarted += HandleCutsceneStepStarted;
            cinematicManager.PlayCutscene(_outroCutsceneId);
            if (!cinematicManager.IsPlaying)
            {
                CinematicManager.OnCutsceneStepStarted -= HandleCutsceneStepStarted;
                yield break;
            }

            // 대화가 끝나 마지막 카메라 단계가 시작되면 그 장면 그대로 크레딧으로 넘어간다. 그 단계가 없으면 컷씬이 끝날 때 넘어간다.
            while (cinematicManager.IsPlaying && !_isOutroDialogueDone)
                yield return null;

            CinematicManager.OnCutsceneStepStarted -= HandleCutsceneStepStarted;
            _endingCredits?.Play();
        }

        private IEnumerator CoFade(float targetAlpha)
        {
            float startAlpha = _fullscreenGroup.alpha;
            float elapsed = 0.0f;

            while (elapsed < _fadeSeconds)
            {
                yield return null;

                elapsed += Time.unscaledDeltaTime;
                _fullscreenGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / _fadeSeconds);
            }

            _fullscreenGroup.alpha = targetAlpha;
        }

        private IEnumerator CoWait(float seconds)
        {
            float elapsed = 0.0f;
            while (elapsed < seconds)
            {
                yield return null;
                elapsed += Time.unscaledDeltaTime;
            }
        }
        #endregion
    }
}
