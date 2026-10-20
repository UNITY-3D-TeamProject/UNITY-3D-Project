using UnityEngine;
using Unity.Cinemachine;
using UI.Dialogue;

namespace Cutscene
{
    /// <summary>
    /// 대화 컷씬 하나. Play를 부르면 플레이어가 지금 있는 자리에서 대화 연출을 재생한다.
    /// 발판(TriggerPlate의 On Player Entered)이나 StageIntroDialogue 같은 곳에서 Play를 연결해 쓴다.
    /// 본 기록은 PlayerPrefs에 대화 Id로 저장한다. (다시 보려면 Edit > Clear All PlayerPrefs)
    /// </summary>
    public class DialogueCutscene : MonoBehaviour
    {
        #region Constants
        private const string SEEN_KEY_PREFIX = "DialogueSeen.";
        #endregion

        #region Serialized Fields
        [Header("References")]
        [Tooltip("씬에 하나 둔 대화 연출")]
        [SerializeField] private DialogueDirector _director;
        [SerializeField] private SODialogueSequence _sequence;

        [Header("Play")]
        [Tooltip("켜면 본 기록과 상관없이 부를 때마다 재생한다 (테스트용)")]
        [SerializeField] private bool _shouldAlwaysPlay;

        [Header("Fixed Cameras")]
        [Tooltip("대사의 CameraIndex가 0 이상이면 이 배열의 카메라를 쓴다. 시작할 때 모두 끄며, Priority는 플레이어 카메라(Base 0, Aim 10)보다 높게 둔다")]
        [SerializeField] private CinemachineCamera[] _fixedCameras;
        #endregion

        #region Properties
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
            if ((_director == null) || (_sequence == null)) return;
            if (!_shouldAlwaysPlay && (PlayerPrefs.GetInt(SeenKey, 0) == 1)) return;

            _director.Play(_sequence, _fixedCameras, HandleFinished);
        }
        #endregion

        #region Private Methods
        private void HandleFinished()
        {
            PlayerPrefs.SetInt(SeenKey, 1);
            PlayerPrefs.Save();
        }
        #endregion
    }
}
