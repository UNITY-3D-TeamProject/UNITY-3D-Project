using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;
using Core;
using UI.Settings;

namespace SceneTransition
{
    /// <summary>
    /// 로딩씬 루트에 배치한다. SceneLoader가 로딩씬을 지금 씬 위에 겹쳐 열면 다음 순서로 전환한다.
    /// 1. 로딩 화면 페이드인 (지금 씬을 덮음. 컷씬이 있으면 진행 바 묶음은 숨긴 채)
    /// 2. 지금 씬 정리 (정리하며 생기는 멈춤은 로딩 화면 뒤에서 일어남)
    /// 3. 컷씬 재생 (튜토리얼 시작은 배경을 검게 하고, 영상이 끝난 뒤 진행 바 없이 목적지 씬을 불러옴)
    /// 4. 일반 이동의 목적지 씬 불러오기 + 진행 바 (실제 진행률을 일정 속도 이하로 따라감)
    /// 5. 목적지 씬 켜기 (로딩 화면만 DontDestroyOnLoad로 남김)
    /// 6. 로딩 화면 페이드아웃 후 삭제
    /// 컷씬 재생 중 ESC를 누르면 영상을 멈추고 설정창을 연다. (로딩씬에는 PlayerInput이 없어 키보드를 직접 읽는다)
    /// </summary>
    public class LoadingScreen : MonoBehaviour
    {
        #region Constants
        // allowSceneActivation이 false인 동안 progress는 0.9에서 멈춘다. (Unity AsyncOperation 동작)
        private const float READY_PROGRESS = 0.9f;

        // 씬 정리 등으로 한 프레임이 길어져도 진행 바·페이드가 한 번에 튀지 않도록 한 프레임에 반영할 최대 시간(초).
        private const float MAX_STEP_DELTA_TIME = 0.05f;

        // 목적지 씬을 켠 직후 몇 프레임은 첫 Start·첫 렌더링으로 길어지므로, 로딩 화면으로 덮은 채 넘긴 뒤 걷어 낸다.
        private const int SETTLE_FRAMES_AFTER_ACTIVATION = 2;
        #endregion

        #region Serialized Fields
        [Header("필수 (Slider ← 진행 바)")]
        [SerializeField] private Slider _progressBar;

        [Header("안 해도 됨 (TMP_Text ← 진행 바 오른쪽 퍼센트)")]
        [SerializeField] private TMP_Text _percentText;

        [Header("필수 (CanvasGroup ← 이 오브젝트, 저장 시 Alpha 0)")]
        [SerializeField] private CanvasGroup _canvasGroup;

        [Header("필수 (GameObject ← 로딩씬 Main Camera, 비활성으로 저장)")]
        [Tooltip("지금 씬을 정리한 뒤에 켠다. 처음부터 켜 두면 지금 씬의 카메라·AudioListener와 겹친다")]
        [SerializeField] private GameObject _loadingCamera;

        [Header("필수 (GameObject ← ProgressGroup, 컷씬 재생 중 숨김)")]
        [SerializeField] private GameObject _progressGroup;

        [Header("필수 (VideoPlayer ← Cutscene 오브젝트)")]
        [SerializeField] private VideoPlayer _videoPlayer;

        [Header("필수 (RawImage ← Cutscene 오브젝트, 저장 시 꺼 둠)")]
        [SerializeField] private RawImage _cutsceneScreen;

        [Header("안 해도 됨 (Image ← Background)")]
        [Tooltip("튜토리얼 시작(메인 메뉴 → 영상 → 튜토리얼)에서는 검게 칠한다")]
        [SerializeField] private Image _background;

        [Header("안 해도 됨 (SOLoadingCutsceneData ← ScriptableObjects/SceneTransition)")]
        [Tooltip("비워 두면 컷씬 없이 넘어간다")]
        [SerializeField] private SOLoadingCutsceneData _cutsceneData;

        [Header("안 해도 됨 (LoadingStageView ← ProgressGroup/StageInfo, 저장 시 꺼 둠)")]
        [Tooltip("비워 두면 스테이지로 갈 때도 기본 로딩 화면을 쓴다")]
        [SerializeField] private LoadingStageView _stageView;

        [Header("Video Skip")]
        [SerializeField] private GameObject _skipPrompt;
        [SerializeField] private UnityEngine.UI.Image _skipProgress;
        [Min(0.1f)]
        [SerializeField] private float _skipHoldDuration = 1.5f;

        [Header("Timing")]
        [Tooltip("로딩 화면이 나타나고 사라지는 데 걸리는 시간(초)")]
        [SerializeField] private float _fadeDuration = 0.3f;

        [Tooltip("진행 바가 0에서 끝까지 차는 데 걸리는 최소 시간(초). 로딩이 빨리 끝나도 이보다 빨리 차지 않는다")]
        [SerializeField] private float _fillDuration = 1.0f;

        [Tooltip("진행 바가 끝까지 찬 뒤 목적지 씬을 켜기 전까지 머무는 시간(초)")]
        [SerializeField] private float _holdAfterFull = 0.2f;

        [Header("안 해도 됨 (컷씬 중 ESC 설정창)")]
        [Tooltip("비워 두면 컷씬 중 ESC로 설정창을 열지 않는다")]
        [SerializeField] private SettingsPresenter _settingsPresenter;
        [Tooltip("설정창이 열린 동안만 켠다. 이전 씬·목적지 씬의 EventSystem과 겹치지 않게 꺼 둔 채로 저장")]
        [SerializeField] private GameObject _eventSystem;
        #endregion

        #region Private Fields
        private bool _isSettingsOpen;
        // 설정창을 닫을 때 돌려놓을 커서 상태
        private CursorLockMode _cursorLockModeBeforeSettings;
        private bool _isCursorVisibleBeforeSettings;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            SetSkipProgress(0f);
            if (_skipPrompt != null) _skipPrompt.SetActive(false);
            Debug.Assert(_progressBar != null, $"[{name}] Progress Bar(Slider)가 연결되지 않았습니다.");
            Debug.Assert(_canvasGroup != null, $"[{name}] CanvasGroup이 연결되지 않았습니다.");
            Debug.Assert(_loadingCamera != null, $"[{name}] Loading Camera가 연결되지 않았습니다.");
            Debug.Assert(_progressGroup != null, $"[{name}] Progress Group이 연결되지 않았습니다.");
            Debug.Assert(_videoPlayer != null, $"[{name}] VideoPlayer가 연결되지 않았습니다.");
            Debug.Assert(_cutsceneScreen != null, $"[{name}] Cutscene Screen(RawImage)이 연결되지 않았습니다.");
        }

        private void Start()
        {
            StartCoroutine(CoLoad());
        }
        #endregion

        #region Private Methods
        private void SetProgress(float value)
        {
            if (_progressBar == null) return;

            _progressBar.value = Mathf.Clamp01(value);
            if (_percentText != null) _percentText.text = $"{Mathf.RoundToInt(_progressBar.value * 100f)}%";
            if (_stageView != null) _stageView.SetProgress(_progressBar.value);
        }

        private void SetSkipProgress(float value)
        {
            if (_skipProgress != null) _skipProgress.fillAmount = Mathf.Clamp01(value);
        }

        private bool IsSkipHeld()
        {
            Keyboard keyboard = Keyboard.current;
            return (keyboard != null) && keyboard.eKey.isPressed;
        }

        private bool IsSettingsKeyPressed()
        {
            Keyboard keyboard = Keyboard.current;
            return (keyboard != null) && keyboard.escapeKey.wasPressedThisFrame;
        }

        // 영상을 멈추고 설정창을 연다. 버튼을 누를 수 있게 커서를 풀고, 닫을 때 원래대로 돌린다.
        private void OpenSettings()
        {
            if (_isSettingsOpen || (_settingsPresenter == null)) return;

            _isSettingsOpen = true;
            _videoPlayer.Pause();

            _cursorLockModeBeforeSettings = Cursor.lockState;
            _isCursorVisibleBeforeSettings = Cursor.visible;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            if (_eventSystem != null) _eventSystem.SetActive(true);

            _settingsPresenter.CloseRequested += CloseSettings;
            _settingsPresenter.MainMenuRequested += HandleMainMenuRequested;
            _settingsPresenter.QuitRequested += HandleQuitRequested;
            _settingsPresenter.Open();
        }

        // 설정창을 닫고 영상을 이어서 재생한다.
        private void CloseSettings()
        {
            if (!_isSettingsOpen) return;

            ReleaseSettings();
            _videoPlayer.Play();
        }

        // 설정창 구독을 풀고 닫은 뒤 EventSystem·커서를 돌려놓는다. 영상은 건드리지 않는다.
        private void ReleaseSettings()
        {
            if (!_isSettingsOpen) return;

            _isSettingsOpen = false;
            _settingsPresenter.CloseRequested -= CloseSettings;
            _settingsPresenter.MainMenuRequested -= HandleMainMenuRequested;
            _settingsPresenter.QuitRequested -= HandleQuitRequested;
            _settingsPresenter.Close();

            if (_eventSystem != null) _eventSystem.SetActive(false);
            Cursor.lockState = _cursorLockModeBeforeSettings;
            Cursor.visible = _isCursorVisibleBeforeSettings;
        }

        // 설정창의 "메인 화면으로". 이번 이동을 멈추고 인게임 설정창과 같은 경로로 메인 메뉴에 간다.
        private void HandleMainMenuRequested()
        {
            StopLoadingForExit();
            GameManager.Instance?.ReturnToMainMenu();
        }

        // 설정창의 "게임 종료". 이번 이동을 멈추고 인게임 설정창과 같은 경로로 끝낸다.
        private void HandleQuitRequested()
        {
            StopLoadingForExit();
            GameManager.Instance?.QuitGame();
        }

        // 컷씬·로딩 진행을 멈추고, 로딩 중이면 거절하는 GameManager의 메인 화면·종료가 동작하도록 이동을 취소한다.
        private void StopLoadingForExit()
        {
            StopAllCoroutines();
            ReleaseSettings();
            _videoPlayer.Stop();
            SceneLoader.CancelPendingLoad();
        }

        private void SetAlpha(float value)
        {
            if (_canvasGroup == null) return;

            _canvasGroup.alpha = Mathf.Clamp01(value);
        }

        private void SetLoadingCameraActive(bool isActive)
        {
            if (_loadingCamera == null) return;

            _loadingCamera.SetActive(isActive);
        }

        private float GetStepDeltaTime()
        {
            return Mathf.Min(Time.unscaledDeltaTime, MAX_STEP_DELTA_TIME);
        }

        // 이번 이동(from → to)에 틀 컷씬을 찾는다. 출발 씬이 없으면 튜토리얼 시작 영상을 찾는다.
        private bool TryGetCutscene(ESceneType? from, ESceneType to, out VideoClip clip)
        {
            clip = null;

            if (_cutsceneData == null) return false;

            return _cutsceneData.TryGetClip(from, to, out clip);
        }
        #endregion

        #region Coroutines
        private IEnumerator CoLoad()
        {
            bool isMainMenu = SceneLoader.IsMainMenuPending;
            if (!isMainMenu && !SceneLoader.PendingScene.HasValue)
            {
                // 로딩씬에서 바로 Play한 경우 등. 포탈(SceneLoader.TryLoad)을 거쳐야 목적지가 있다.
                Debug.LogWarning("[LoadingScreen] 불러올 목적지 씬이 없습니다.", this);
                SetAlpha(1f);
                SetLoadingCameraActive(true);
                yield break;
            }

            // 목적지 로드가 끝나면 SceneLoader 값이 비워지므로 이번 이동을 미리 받아 둔다.
            ESceneType? from = SceneLoader.PreviousScene;
            ESceneType? to = SceneLoader.PendingScene;

            // 스테이지·로비로 가면 앱 아이콘·접속 문구·팁을, 메인 메뉴로 가면 기본 화면 문구를 페이드인 전에 채운다.
            if (_stageView != null)
            {
                if (isMainMenu)
                    _stageView.ShowMainMenu();
                else
                    _stageView.TryShow(from, to.Value);
            }

            // 튜토리얼 시작은 진행 바 없이 영상에서 바로 목적지로 넘어간다. 메인 메뉴로 갈 때는 컷씬이 없다.
            VideoClip clip = null;
            bool hasCutscene = !isMainMenu && TryGetCutscene(from, to.Value, out clip);
            bool isTutorialIntro = hasCutscene && (to == ESceneType.Tutorial);
            if (hasCutscene) _progressGroup.SetActive(false);

            // 메인 메뉴 → 영상 → 튜토리얼은 파란 로딩 배경 대신 검은 화면으로 잇는다.
            if (isTutorialIntro && (_background != null)) _background.color = Color.black;

            SetProgress(0f);
            yield return CoFade(0f, 1f);

            // 화면이 다 덮인 뒤 지금 씬을 정리한다. (지금 씬의 소리가 영상과 겹치지 않음)
            SetLoadingCameraActive(true);
            yield return CoUnloadOtherScenes();

            AsyncOperation operation;
            if (isTutorialIntro)
            {
                // 영상이 끝난 뒤 튜토리얼을 불러온다. 미리 불러 둔 씬은 취소할 수 없어서,
                // 영상 중 설정창에서 메인 화면으로 가면 튜토리얼이 먼저 켜져 버린다.
                yield return CoPlayCutscene(clip);
                operation = SceneLoader.LoadPendingSceneAsync();
                while (operation.progress < READY_PROGRESS)
                {
                    yield return null;
                }
            }
            else
            {
                // 일반 컷씬은 목적지를 불러오기 전에 튼다. (불러오기와 겹치지 않아 끊기지 않음)
                if (hasCutscene)
                {
                    yield return CoPlayCutscene(clip);
                    _progressGroup.SetActive(true);
                }

                operation = SceneLoader.LoadPendingSceneAsync();
                yield return CoFillProgress(operation);
                yield return new WaitForSecondsRealtime(_holdAfterFull);
            }

            // 목적지 씬을 켜면 로딩씬이 정리되므로, 페이드아웃을 위해 로딩 화면만 남겨 둔다.
            DontDestroyOnLoad(gameObject);
            operation.allowSceneActivation = true;
            while (!operation.isDone)
            {
                yield return null;
            }

            for (int i = 0; i < SETTLE_FRAMES_AFTER_ACTIVATION; i++)
            {
                yield return null;
            }

            if (isTutorialIntro)
            {
                SetAlpha(0f);
            }
            else
            {
                yield return CoFade(1f, 0f);
            }
            Destroy(gameObject);
        }

        // 로딩씬을 제외한 열린 씬(이전 씬)을 모두 정리한다.
        private IEnumerator CoUnloadOtherScenes()
        {
            Scene loadingScene = gameObject.scene;
            List<Scene> otherScenes = new List<Scene>();

            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if ((scene != loadingScene) && scene.isLoaded)
                {
                    otherScenes.Add(scene);
                }
            }

            SceneManager.SetActiveScene(loadingScene);

            List<AsyncOperation> operations = new List<AsyncOperation>();
            foreach (Scene scene in otherScenes)
            {
                AsyncOperation operation = SceneManager.UnloadSceneAsync(scene);
                if (operation != null)
                {
                    operations.Add(operation);
                }
            }

            foreach (AsyncOperation operation in operations)
            {
                while (!operation.isDone)
                {
                    yield return null;
                }
            }
        }

        // 표시 진행률이 실제 진행률(준비 완료 = 1)을 일정 속도 이하로 따라가 끝까지 찰 때까지 기다린다.
        private IEnumerator CoFillProgress(AsyncOperation operation)
        {
            float displayedProgress = 0f;
            float fillSpeed = 1f / Mathf.Max(0.01f, _fillDuration);
            SetProgress(displayedProgress);

            while (displayedProgress < 1f)
            {
                float targetProgress = (operation.progress >= READY_PROGRESS)
                    ? 1f
                    : operation.progress / READY_PROGRESS;

                displayedProgress = Mathf.MoveTowards(displayedProgress, targetProgress, fillSpeed * GetStepDeltaTime());
                SetProgress(displayedProgress);
                yield return null;
            }
        }

        // 진행 바를 숨기고 영상을 재생한다. E 유지·자연 종료·재생 오류는 같은 정리 경로를 쓴다.
        // ESC로 설정창이 열린 동안은 영상이 멈추고 E 스킵 게이지는 0으로 돌아간다.
        private IEnumerator CoPlayCutscene(VideoClip clip)
        {
            bool isFinished = false;
            VideoPlayer.EventHandler onFinished = _ => isFinished = true;
            VideoPlayer.ErrorEventHandler onError = (_, message) =>
            {
                Debug.LogWarning($"[LoadingScreen] 컷씬을 재생하지 못했습니다: {message}", this);
                isFinished = true;
            };

            _progressGroup.SetActive(false);

            _videoPlayer.loopPointReached += onFinished;
            _videoPlayer.errorReceived += onError;
            _videoPlayer.clip = clip;
            _videoPlayer.Prepare();

            while (!_videoPlayer.isPrepared && !isFinished)
            {
                yield return null;
            }

            if (!isFinished)
            {
                _cutsceneScreen.texture = _videoPlayer.texture;
                _cutsceneScreen.enabled = true;
                _videoPlayer.Play();

                float heldTime = 0f;
                bool canSkip = !IsSkipHeld();
                SetSkipProgress(0f);
                if (_skipPrompt != null) _skipPrompt.SetActive(true);

                while (!isFinished)
                {
                    if (IsSettingsKeyPressed())
                    {
                        if (_isSettingsOpen)
                            CloseSettings();
                        else
                            OpenSettings();
                    }

                    if (_isSettingsOpen)
                    {
                        // 설정창이 열리면 스킵 게이지를 비우고, 닫은 뒤에는 E를 한 번 뗐다 눌러야 다시 찬다.
                        heldTime = 0f;
                        canSkip = false;
                        SetSkipProgress(0f);
                        yield return null;
                        continue;
                    }

                    bool isHeld = IsSkipHeld();
                    if (!isHeld)
                    {
                        canSkip = true;
                        heldTime = 0f;
                    }
                    else if (canSkip && _videoPlayer.isPlaying)
                    {
                        heldTime += Time.unscaledDeltaTime;
                    }

                    float progress = heldTime / Mathf.Max(0.1f, _skipHoldDuration);
                    SetSkipProgress(progress);
                    if (progress >= 1f) break;

                    yield return null;
                }
            }

            if (_skipPrompt != null) _skipPrompt.SetActive(false);
            SetSkipProgress(0f);
            _videoPlayer.loopPointReached -= onFinished;
            _videoPlayer.errorReceived -= onError;
            _videoPlayer.Stop();

            // 영상 화면을 끄고 로딩 화면 배경으로 돌아간다.
            _cutsceneScreen.enabled = false;
        }

        private IEnumerator CoFade(float from, float to)
        {
            float duration = Mathf.Max(0.01f, _fadeDuration);
            float elapsed = 0f;
            SetAlpha(from);

            while (elapsed < duration)
            {
                yield return null;
                elapsed += GetStepDeltaTime();
                SetAlpha(Mathf.Lerp(from, to, elapsed / duration));
            }

            SetAlpha(to);
        }
        #endregion
    }
}
