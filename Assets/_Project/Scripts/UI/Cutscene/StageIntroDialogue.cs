using UnityEngine;
using Core.Stage;

namespace UI.Cutscene
{
    /// <summary>
    /// 스테이지가 시작되면 지정한 번호의 컷씬을 한 번 재생한다.
    /// 번호와 대화는 SOCutsceneTable, 연출은 DialogueManager가 맡는다.
    /// </summary>
    public class StageIntroDialogue : MonoBehaviour
    {
        #region Serialized Fields
        [Header("References")]
        [SerializeField] private StageManager _stageManager;
        [Tooltip("스테이지 시작 때 재생할 컷씬 번호 (SOCutsceneTable의 Id)")]
        [SerializeField] private int _cutsceneId = 1;
        #endregion

        #region Private Fields
        private bool _hasPlayed;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            Debug.Assert(_stageManager != null, $"[{name}] StageManager가 연결되지 않았습니다.");
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
            if (_hasPlayed || (UIManager.Instance == null)) return;

            _hasPlayed = true;
            UIManager.Instance.PlayCutscene(_cutsceneId);
        }
        #endregion
    }
}
