using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.Video;
using Core;
using Core.InputMap;

namespace UI.Credits
{
    /// <summary>
    /// 라이브 앱 엔딩의 마지막 부분. 스크린 연출을 시작하는 발판에서 Play를 부른다.
    /// 엔딩 영상이 _startAtVideoSeconds에 오면(영상이 먼저 끝나면 그때) 화면을 어둡게 하고 제작진 명단을 아래에서 위로 올린다.
    /// 영상이 끝나기 전에 시작하므로 노래가 나오는 채로 크레딧이 시작된다. E를 누르고 있으면 명단이 빨리 올라간다.
    /// 명단이 화면 위로 다 나가면 화면을 검게 덮은 뒤 메인 메뉴로 돌아간다.
    /// 영상이 끝나면 스크린 연출이 끝나며 조작 막기가 풀리므로, 크레딧을 시작할 때 Cutscene 입력 맵을 직접 요청해 막아 둔다.
    /// </summary>
    public class EndingCredits : MonoBehaviour
    {
        #region Serialized Fields
        [Header("Start")]
        [Tooltip("시작 시각을 잴 엔딩 영상(스크린 VideoPlayer)")]
        [SerializeField] private VideoPlayer _videoPlayer;
        [Tooltip("영상이 이 위치(초)에 오면 크레딧을 시작한다. 영상 길이보다 길면 영상이 끝날 때 시작한다")]
        [SerializeField] private float _startAtVideoSeconds = 63.0f;

        [Header("Credits")]
        [Tooltip("검은 배경과 명단을 함께 담은 그룹. 시작할 때 투명하게 둔다")]
        [SerializeField] private CanvasGroup _creditsGroup;
        [Tooltip("위로 올라가는 명단. 앵커는 부모 아래쪽 가운데, 피벗은 위쪽 가운데(0.5, 1)로 둔다")]
        [SerializeField] private RectTransform _creditsRoll;
        [Tooltip("화면이 어두워지는 시간(초)")]
        [SerializeField] private float _fadeSeconds = 2.0f;
        [Tooltip("명단이 올라가는 속도(px/초, 캔버스 기준)")]
        [SerializeField] private float _scrollSpeed = 80.0f;
        [Tooltip("E를 누르고 있는 동안 명단 속도에 곱하는 배수. 바꾸면 화면의 안내 문구도 같이 고친다")]
        [SerializeField] private float _fastForwardMultiplier = 2.0f;
        [Tooltip("명단이 다 올라간 뒤 메인 메뉴로 돌아가기 전까지 기다리는 시간(초)")]
        [SerializeField] private float _endHoldSeconds = 2.0f;

        [Header("End Fade Out")]
        [Tooltip("크레딧 맨 위를 덮는 검은 이미지. 시작할 때 투명하게 두고, 끝날 때 불투명하게 만든다")]
        [SerializeField] private Graphic _fadeOutCover;
        [Tooltip("끝날 때 화면이 검게 덮이는 시간(초)")]
        [SerializeField] private float _fadeOutSeconds = 2.0f;

        [Header("안 해도 됨 (GameObject ← EndingCastStage, 꺼 둠)")]
        [Tooltip("크레딧 왼쪽 캐릭터 무대. 크레딧이 시작될 때 켠다")]
        [SerializeField] private GameObject _castStage;
        #endregion

        #region Private Fields
        private bool _hasStarted;
        private bool _isVideoFinished;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            Debug.Assert(_creditsGroup != null, $"[{name}] Credits Group이 연결되지 않았습니다.");
            Debug.Assert(_creditsRoll != null, $"[{name}] Credits Roll이 연결되지 않았습니다.");

            _creditsGroup.alpha = 0.0f;
            _creditsGroup.blocksRaycasts = false;
            SetFadeOutAlpha(0.0f);

            if (_videoPlayer != null)
                _videoPlayer.loopPointReached += HandleVideoFinished;
        }

        private void OnDestroy()
        {
            if (_videoPlayer != null)
                _videoPlayer.loopPointReached -= HandleVideoFinished;
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
        private void HandleVideoFinished(VideoPlayer videoPlayer)
        {
            _isVideoFinished = true;
        }

        private void SetFadeOutAlpha(float alpha)
        {
            if (_fadeOutCover == null) return;

            Color color = _fadeOutCover.color;
            color.a = alpha;
            _fadeOutCover.color = color;
        }

        // 영상이 시작 위치에 왔거나 이미 끝났으면 true. 영상을 연결하지 않았으면 바로 true.
        private bool IsStartTime()
        {
            if ((_videoPlayer == null) || _isVideoFinished) return true;

            return _videoPlayer.isPlaying && (_videoPlayer.time >= _startAtVideoSeconds);
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
            while (!IsStartTime())
                yield return null;

            // 영상이 끝나 스크린 연출의 조작 막기가 풀려도 크레딧 동안은 막아 둔다. 메인 메뉴로 가며 씬과 함께 정리된다.
            InputMapController.Instance?.RequestMap(this, EInputMapType.Cutscene);

            if (_castStage != null) _castStage.SetActive(true);
            yield return CoFadeIn();
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
                _creditsGroup.alpha = Mathf.Clamp01(elapsed / _fadeSeconds);
            }

            _creditsGroup.alpha = 1.0f;
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
