using UnityEngine;
using Unity.Cinemachine;
using UI.Dialogue;

namespace UI.Cutscene
{
    /// <summary>
    /// 대화 컷씬 하나. Play를 부르면 플레이어가 지금 있는 자리에서 대화 연출을 재생한다.
    /// 발판(TriggerPlate의 On Player Entered)이나 StageIntroDialogue 같은 곳에서 Play를 연결해 쓴다.
    /// 본 기록은 PlayerPrefs에 대화 Id로 저장한다. (다시 보려면 Edit > Clear All PlayerPrefs)
    /// 설정 순서: 1) 씬의 DialogueDirector 연결 2) 대화 SO 연결 3) 필요하면 고정 카메라 등록 4) 재생할 곳에서 Play 연결
    /// </summary>
    public class DialogueCutscene : MonoBehaviour
    {
        #region Constants
        // 본 기록 키 앞에 붙는 말. 뒤에 대화 Id가 붙는다
        private const string SEEN_KEY_PREFIX = "DialogueSeen.";
        #endregion

        #region Serialized Fields
        [Header("References")]
        [Tooltip("씬에 하나 둔 대화 연출")]
        [SerializeField] private DialogueDirector _director;
        [Tooltip("재생할 대화. 이 대화의 Id로 본 기록을 저장한다")]
        [SerializeField] private SODialogueSequence _sequence;

        [Header("Play")]
        [Tooltip("켜면 본 기록과 상관없이 부를 때마다 재생한다 (테스트용)")]
        [SerializeField] private bool _shouldAlwaysPlay;

        [Header("Fixed Cameras")]
        [Tooltip("대사의 CameraIndex가 0 이상이면 이 배열의 카메라를 쓴다. 시작할 때 모두 끄며, Priority는 플레이어 카메라(Base 0, Aim 10)보다 높게 둔다")]
        [SerializeField] private CinemachineCamera[] _fixedCameras;
        #endregion

        #region Properties
        // 본 기록을 저장할 PlayerPrefs 키 (예: DialogueSeen.Tutorial)
        private string SeenKey => SEEN_KEY_PREFIX + _sequence.Id;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            Debug.Assert(_director != null, $"[{name}] DialogueDirector가 연결되지 않았습니다.");
            Debug.Assert(_sequence != null, $"[{name}] 대화 SO가 연결되지 않았습니다.");

            // 작업하며 켜 둔 고정 카메라가 플레이어 카메라를 덮지 않도록 꺼 둔다.
            if (_fixedCameras == null) return;

            foreach (CinemachineCamera fixedCamera in _fixedCameras)
            {
                if (fixedCamera != null)
                    fixedCamera.enabled = false;
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// 대화 컷씬을 재생한다. 이미 본 대화(항상 재생이 꺼진 경우)거나 다른 대화가 재생 중이면 무시한다.
        /// </summary>
        public void Play()
        {
            // 인스펙터 연결이 빠졌으면 재생하지 않는다. (Awake의 Assert로 알림)
            if ((_director == null) || (_sequence == null)) return;

            // 이미 본 대화는 다시 재생하지 않는다. (항상 재생이 켜져 있으면 건너뛴다)
            if (!_shouldAlwaysPlay && (PlayerPrefs.GetInt(SeenKey, 0) == 1)) return;

            // 연출은 Director가 맡는다. 다른 대화가 재생 중이라 시작하지 못하면 HandleFinished가 불리지 않아 본 기록도 남지 않는다.
            _director.Play(_sequence, _fixedCameras, HandleFinished);
        }
        #endregion

        #region Private Methods
        // 대화가 끝나 Director가 연출을 정리한 뒤 부른다. 다음부터 재생하지 않도록 본 기록을 남긴다.
        private void HandleFinished()
        {
            PlayerPrefs.SetInt(SeenKey, 1);
            // 게임이 강제 종료돼도 기록이 남도록 바로 저장한다.
            PlayerPrefs.Save();
        }
        #endregion
    }
}
