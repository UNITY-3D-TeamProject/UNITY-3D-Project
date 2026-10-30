using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Unity.Cinemachine;
using Core;
using UIandCinematic;

namespace UI.Credits
{
    /// <summary>
    /// 라이브 앱 엔딩의 마지막 부분. 공연 영상이 끝났을 때(LiveEndingScreen의 영상 끝 이벤트) Play를 부른다.
    /// 마무리 컷씬(TV 앞 대화 → 여운 샷)을 재생하고, 여운 카메라에 다 도착해 잠시 머물면 화면을 어둡게 한 뒤
    /// 제작진 명단을 아래에서 위로 올린다. 명단이 화면 위로 다 나가면 메인 메뉴로 돌아간다.
    /// 컷씬의 여운 샷은 크레딧보다 길게 잡아 둔다. 크레딧 동안 컷씬이 계속 재생되어 조작 막기와 인게임 UI 숨김이 유지된다.
    /// </summary>
    public class EndingCredits : MonoBehaviour
    {
        #region Serialized Fields
        [Header("Cutscene")]
        [Tooltip("영상이 끝난 뒤 재생할 마무리 컷씬 번호(SOCutsceneTable의 Id)")]
        [SerializeField] private int _cutsceneId = 34;
        [Tooltip("마무리 컷씬 마지막 여운 샷의 카메라. 이 카메라로 블렌드가 다 끝나면 크레딧을 시작할 준비를 한다")]
        [SerializeField] private CinemachineCamera _lingerCamera;
        [Tooltip("여운 카메라에 도착한 뒤 화면을 어둡게 하기 전까지 머무는 시간(초)")]
        [SerializeField] private float _lingerHoldSeconds = 2.0f;

        [Header("Credits")]
        [Tooltip("검은 배경과 명단을 함께 담은 그룹. 시작할 때 투명하게 둔다")]
        [SerializeField] private CanvasGroup _creditsGroup;
        [Tooltip("위로 올라가는 명단. 앵커는 부모 아래쪽 가운데, 피벗은 위쪽 가운데(0.5, 1)로 둔다")]
        [SerializeField] private RectTransform _creditsRoll;
        [Tooltip("화면이 어두워지는 시간(초)")]
        [SerializeField] private float _fadeSeconds = 2.0f;
        [Tooltip("명단이 올라가는 속도(px/초, 캔버스 기준)")]
        [SerializeField] private float _scrollSpeed = 80.0f;
        [Tooltip("명단이 다 올라간 뒤 메인 메뉴로 돌아가기 전까지 기다리는 시간(초)")]
        [SerializeField] private float _endHoldSeconds = 2.0f;
        #endregion

        #region Private Fields
        private bool _hasStarted;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            Debug.Assert(_creditsGroup != null, $"[{name}] Credits Group이 연결되지 않았습니다.");
            Debug.Assert(_creditsRoll != null, $"[{name}] Credits Roll이 연결되지 않았습니다.");

            _creditsGroup.alpha = 0.0f;
            _creditsGroup.blocksRaycasts = false;
        }
        #endregion

        #region Public Methods
        // 마무리 컷씬부터 메인 메뉴 복귀까지 진행한다. 한 번만 동작한다.
        public void Play()
        {
            if (_hasStarted) return;

            _hasStarted = true;
            StartCoroutine(CoPlayEnding());
        }
        #endregion

        #region Private Methods
        // 설정창이 열려 있으면(Pause) true. 그동안은 연출 시간도 세지 않는다.
        private bool IsGamePaused()
        {
            GameManager gameManager = GameManager.Instance;
            return (gameManager != null) && (gameManager.CurrentState != GameManager.GameState.Playing);
        }

        // 여운 카메라로 넘어가 블렌드까지 끝났으면 true.
        private bool IsLingerCameraLive()
        {
            CinemachineBrain brain = (CinemachineBrain.ActiveBrainCount > 0) ? CinemachineBrain.GetActiveBrain(0) : null;
            if (brain == null) return false;

            return ((Object)brain.ActiveVirtualCamera == _lingerCamera) && !brain.IsBlending;
        }
        #endregion

        #region Coroutines
        private IEnumerator CoPlayEnding()
        {
            CinematicManager cinematicManager = CinematicManager.Instance;
            if (cinematicManager != null)
            {
                // 설정창이 열려 있어 시작하지 못했으면 닫힐 때까지 다시 시도한다. 다른 이유로 못 하면 크레딧으로 넘어간다.
                while (true)
                {
                    cinematicManager.PlayCutscene(_cutsceneId);
                    if (cinematicManager.IsPlaying || !IsGamePaused()) break;

                    yield return null;
                }

                // 컷씬이 여운 샷 전에 끝나면(카메라 누락 등) 기다리지 않고 크레딧으로 넘어간다.
                while (!IsLingerCameraLive())
                {
                    if (!cinematicManager.IsPlaying) break;
                    yield return null;
                }
            }
            else
            {
                Debug.LogWarning($"[{name}] 씬에 CinematicManager가 없어 {_cutsceneId}번 컷씬 없이 크레딧을 시작합니다.", this);
            }

            yield return CoWait(_lingerHoldSeconds);
            yield return CoFadeIn();
            yield return CoScrollRoll();
            yield return CoWait(_endHoldSeconds);

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

                position.y = Mathf.Min(endY, position.y + (_scrollSpeed * Time.unscaledDeltaTime));
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
