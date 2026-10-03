using UnityEngine;
using SceneTransition;

namespace Map.Gimmicks
{
    /// <summary>
    /// 플레이어가 닿으면 지정한 씬으로 전환 요청 순서상 갈 수 없는 씬이면 아무 일도 일어나지 않는다.
    /// 콜라이더의 Is Trigger를 켜야 한다.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class ScenePortal : MonoBehaviour
    {
        #region Serialized Fields
        [Header("Destination")]
        [Tooltip("이동할 씬")]
        [SerializeField] private ESceneType _destination;

        [Header("Return")]
        [Tooltip("켜면 목적지 씬에서 이 씬으로 돌아왔을 때 플레이어가 이 포탈 앞에서 시작한다. (로비의 앱 입구용. 스테이지의 출구 포탈은 꺼 둔다)")]
        [SerializeField] private bool _isReturnPoint;

        [Tooltip("돌아왔을 때 플레이어가 놓일 위치. 포탈 트리거 밖에 둔다. 비워 두면 포탈 위치를 쓴다.")]
        [SerializeField] private Transform _arrivalPoint;
        #endregion

        #region Properties
        /// <summary>이 포탈이 보내는 씬.</summary>
        public ESceneType Destination => _destination;

        /// <summary>목적지 씬에서 돌아온 플레이어가 이 포탈 앞에서 시작하는지 여부.</summary>
        public bool IsReturnPoint => _isReturnPoint;

        /// <summary>돌아온 플레이어가 놓일 위치.</summary>
        public Vector3 ArrivalPosition => (_arrivalPoint != null) ? _arrivalPoint.position : transform.position;
        #endregion

        #region Unity Lifecycle
        private void OnTriggerEnter(Collider other)
        {
            if (!GimmickUtility.TryGetPlayer(other, out _)) return;

            SceneLoader.TryLoad(_destination);
        }
        #endregion
    }
}
