using UnityEngine;

namespace UIandCinematic
{
    /// <summary>
    /// 번호로 컷씬을 재생한다. 프리팹은 씬의 CinematicManager를 가리킬 수 없으므로, 프리팹 안의 UnityEvent(TriggerPlate 등)는 이 컴포넌트의 Play를 연결한다.
    /// 지금 씬의 CinematicManager.Instance로 재생하며, 재생하지 못하는 조건은 CinematicManager.PlayCutscene과 같다.
    /// </summary>
    public class CutscenePlayer : MonoBehaviour
    {
        #region Serialized Fields
        [Tooltip("재생할 컷씬 번호(SOCutsceneTable의 Id)")]
        [SerializeField] private int _cutsceneId;
        #endregion

        #region Public Methods
        public void Play()
        {
            if (CinematicManager.Instance == null)
            {
                Debug.LogWarning($"[{name}] 씬에 CinematicManager가 없어 {_cutsceneId}번 컷씬을 재생하지 못했습니다.", this);
                return;
            }

            CinematicManager.Instance.PlayCutscene(_cutsceneId);
        }
        #endregion
    }
}
