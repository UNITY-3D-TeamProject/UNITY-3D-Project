using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

namespace SceneTransition
{
    /// <summary>
    /// 로딩씬 루트에 배치한다. SceneLoader가 로딩씬을 지금 씬 위에 겹쳐 열면 다음 순서로 전환한다.
    /// 1. 로딩 화면 페이드인 (지금 씬을 덮음)
    /// 2. 지금 씬 정리 (정리하며 생기는 멈춤은 로딩 화면 뒤에서 일어남)
    /// 3. 목적지 씬 불러오기 + 진행 바 (실제 진행률을 일정 속도 이하로 따라감)
    /// 4. 이 이동에 컷씬이 있으면 재생 (로딩이 다 끝난 뒤, 목적지를 켜기 전)
    /// 5. 목적지 씬 켜기 (로딩 화면만 DontDestroyOnLoad로 남김)
    /// 6. 로딩 화면 페이드아웃 후 삭제
    /// </summary>
    public class LoadingScreen : MonoBehaviour
    {
        #region Constants
        // allowSceneActivation이 false인 동안 progress는 0.9에서 멈춘다. (Unity AsyncOperation 동작)
        private const float READY_PROGRESS = 0.9f;

        // 씬 정리 등으로 한 프레임이 길어져도 진행 바·페이드가 한 번에 튀지 않도록 한 프레임에 반영할 최대 시간(초).
        private const float MAX_STEP_DELTA_TIME = 0.05f;
        #endregion

        #region Serialized Fields
        [Header("필수 (Slider ← 진행 바)")]
        [SerializeField] private Slider _progressBar;

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

        [Header("안 해도 됨 (SOLoadingCutsceneData ← ScriptableObjects/SceneTransition)")]
        [Tooltip("비워 두면 컷씬 없이 넘어간다")]
        [SerializeField] private SOLoadingCutsceneData _cutsceneData;

        [Header("Timing")]
        [Tooltip("로딩 화면이 나타나고 사라지는 데 걸리는 시간(초)")]
        [SerializeField] private float _fadeDuration = 0.3f;

        [Tooltip("진행 바가 0에서 끝까지 차는 데 걸리는 최소 시간(초). 로딩이 빨리 끝나도 이보다 빨리 차지 않는다")]
        [SerializeField] private float _fillDuration = 1.0f;

        [Tooltip("진행 바가 끝까지 찬 뒤 목적지 씬을 켜기 전까지 머무는 시간(초)")]
        [SerializeField] private float _holdAfterFull = 0.2f;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
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
            if (!SceneLoader.PendingScene.HasValue)
            {
                // 로딩씬에서 바로 Play한 경우 등. 포탈(SceneLoader.TryLoad)을 거쳐야 목적지가 있다.
                Debug.LogWarning("[LoadingScreen] 불러올 목적지 씬이 없습니다.", this);
                SetAlpha(1f);
                SetLoadingCameraActive(true);
                yield break;
            }

            // 목적지 로드가 끝나면 SceneLoader 값이 비워지므로 이번 이동을 미리 받아 둔다.
            ESceneType? from = SceneLoader.PreviousScene;
            ESceneType to = SceneLoader.PendingScene.Value;

            SetProgress(0f);
            yield return CoFade(0f, 1f);

            // 화면이 다 덮인 뒤 지금 씬을 정리한다.
            SetLoadingCameraActive(true);
            yield return CoUnloadOtherScenes();

            AsyncOperation operation = SceneLoader.LoadPendingSceneAsync();
            yield return CoFillProgress(operation);
            yield return new WaitForSecondsRealtime(_holdAfterFull);

            // 컷씬은 로딩이 다 끝난 뒤, 목적지를 켜기 전에 튼다. (불러오기·켜기와 겹치지 않아 끊기지 않음)
            if (TryGetCutscene(from, to, out VideoClip clip))
            {
                yield return CoPlayCutscene(clip);
            }

            // 목적지 씬을 켜면 로딩씬이 정리되므로, 페이드아웃을 위해 로딩 화면만 남겨 둔다.
            DontDestroyOnLoad(gameObject);
            operation.allowSceneActivation = true;
            while (!operation.isDone)
            {
                yield return null;
            }

            yield return CoFade(1f, 0f);
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

        // 진행 바를 숨기고 컷씬을 끝까지 재생한다. 재생 오류가 나면 경고만 남기고 넘어간다.
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

                while (!isFinished)
                {
                    yield return null;
                }
            }

            _videoPlayer.loopPointReached -= onFinished;
            _videoPlayer.errorReceived -= onError;
            _videoPlayer.Stop();

            // 목적지를 켜는 동안에는 검은 배경만 보이게 한다.
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
