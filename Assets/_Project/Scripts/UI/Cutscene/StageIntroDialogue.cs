using UnityEngine;
using Core.Stage;

namespace UI.Cutscene
{
    /// <summary>
    /// 스테이지가 시작되면 연결한 대화 컷씬을 재생한다.
    /// 연출과 본 기록은 DialogueCutscene·DialogueDirector가 맡는다.
    /// </summary>
    public class StageIntroDialogue : MonoBehaviour
    {
        #region Serialized Fields
        [Header("References")]
        [SerializeField] private StageManager _stageManager;
        [Tooltip("스테이지 시작 때 재생할 대화 컷씬")]
        [SerializeField] private DialogueCutscene _cutscene;
        #endregion

        #region Private Fields
        private bool _hasPlayed;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            Debug.Assert(_stageManager != null, $"[{name}] StageManager가 연결되지 않았습니다.");
            Debug.Assert(_cutscene != null, $"[{name}] DialogueCutscene이 연결되지 않았습니다.");
        }

        private void OnEnable()
        {
            if (_stageManager != null)
                _stageManager.OnStageStarted += TryPlay;
        }

        private void OnDisable()
        {
            if (_stageManager != null)
                _stageManager.OnStageStarted -= TryPlay;
        }

        private void Start()
        {
            // 이 컴포넌트가 구독하기 전에 스테이지가 이미 시작됐으면 여기서 재생한다.
            if ((_stageManager != null) && _stageManager.IsRunning)
                TryPlay();
        }
        #endregion

        #region Private Methods
        private void TryPlay()
        {
            if (_hasPlayed || (_cutscene == null)) return;

            _hasPlayed = true;
            _cutscene.Play();
        }
        #endregion
    }
}
