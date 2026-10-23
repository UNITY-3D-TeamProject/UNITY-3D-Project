using UnityEngine;
using Map.Gimmicks;
using SceneTransition;

namespace Map.Lobby
{
    /// <summary>
    /// 로비 앱 발판 위의 위치 표시(빛기둥 + 핀)와 그 포탈로 가는 경로 발판의 작은 핀을
    /// SceneLoader 순서상 지금 갈 수 있는 포탈에만 켠다. 그 포탈의 대화 박스·포탈 오브젝트·로고도 같이 켜고 끈다.
    /// 로비는 돌아올 때마다 다시 불러오므로 Start에서 한 번만 확인한다.
    /// </summary>
    public class NextDestinationMarker : MonoBehaviour
    {
        #region Serialized Fields
        [Header("References")]
        [Tooltip("이 발판의 씬 포탈")]
        [SerializeField] private ScenePortal _portal;

        [Tooltip("켜고 끌 위치 표시 오브젝트 (빛기둥·핀의 부모)")]
        [SerializeField] private GameObject _marker;

        [Tooltip("이 포탈로 가는 길의 경로 발판 위 작은 핀들. 포탈 표시와 같이 켜고 끈다.")]
        [SerializeField] private GameObject[] _pathMarkers;

        [Tooltip("이 포탈이 다음 목적지일 때만 켤 대화 박스(TriggerPlate). 이 포탈 입장 전 대화와, 직전 스테이지에서 돌아왔을 때의 복귀 대화를 함께 넣는다.")]
        [SerializeField] private GameObject[] _dialogueTriggers;

        [Tooltip("이 포탈이 다음 목적지일 때만 보일 오브젝트 (상호작용 포탈 미니 발판, 떠 있는 로고)")]
        [SerializeField] private GameObject[] _unlockedObjects;
        #endregion

        #region Unity Lifecycle
        private void Start()
        {
            Debug.Assert(_portal != null, $"[{name}] ScenePortal이 연결되지 않았습니다.");
            Debug.Assert(_marker != null, $"[{name}] 위치 표시 오브젝트가 연결되지 않았습니다.");

            bool isNext = SceneLoader.CanEnter(_portal.Destination);
            _marker.SetActive(isNext);

            foreach (GameObject pathMarker in _pathMarkers)
            {
                if (pathMarker == null) continue;

                pathMarker.SetActive(isNext);
            }

            foreach (GameObject dialogueTrigger in _dialogueTriggers)
            {
                if (dialogueTrigger == null) continue;

                dialogueTrigger.SetActive(isNext);
            }

            foreach (GameObject unlockedObject in _unlockedObjects)
            {
                if (unlockedObject == null) continue;

                unlockedObject.SetActive(isNext);
            }
        }
        #endregion
    }
}
