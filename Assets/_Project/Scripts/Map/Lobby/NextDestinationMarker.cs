using UnityEngine;
using Map.Gimmicks;
using SceneTransition;

namespace Map.Lobby
{
    /// <summary>
    /// 로비 앱 발판 위의 위치 표시(빛기둥 + 핀)와 그 포탈로 가는 경로 발판의 작은 핀을
    /// SceneLoader 순서상 지금 갈 수 있는 포탈에만 켠다.
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
        }
        #endregion
    }
}
