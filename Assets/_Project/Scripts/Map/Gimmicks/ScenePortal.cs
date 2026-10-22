using UnityEngine;
using Interaction;
using SceneTransition;

namespace Map.Gimmicks
{
    /// <summary>
    /// 플레이어가 닿거나 상호작용(E)하면 지정한 씬으로 전환 요청 순서상 갈 수 없는 씬이면 아무 일도 일어나지 않는다.
    /// 닿기 전환은 콜라이더의 Is Trigger를 켜야 하고, 상호작용 전환은 콜라이더를 Interact 레이어로 둔다.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class ScenePortal : MonoBehaviour, IInteractable
    {
        #region Serialized Fields
        [Header("Destination")]
        [Tooltip("이동할 씬")]
        [SerializeField] private ESceneType _destination;

        [Tooltip("켜면 Unlock 호출 전까지 씬 전환을 막습니다.")]
        [SerializeField] private bool _isLocked;

        [Tooltip("켜면 닿기만 해도 전환한다. 끄면 상호작용(E)으로만 전환한다. (로비 앱 포탈은 끈다)")]
        [SerializeField] private bool _shouldEnterOnTouch = true;

        [Header("Return")]
        [Tooltip("켜면 목적지 씬에서 이 씬으로 돌아왔을 때 플레이어가 이 포탈 앞에서 시작한다. (로비의 앱 입구용. 스테이지의 출구 포탈은 꺼 둔다)")]
        [SerializeField] private bool _isReturnPoint;

        [Tooltip("돌아왔을 때 플레이어가 놓일 위치. 포탈 트리거 밖에 둔다. 비워 두면 포탈 위치를 쓴다.")]
        [SerializeField] private Transform _arrivalPoint;
        #endregion

        #region Properties
        /// <summary>이 포탈이 보내는 씬.</summary>
        public ESceneType Destination => _destination;

        public bool IsLocked => _isLocked;

        /// <summary>목적지 씬에서 돌아온 플레이어가 이 포탈 앞에서 시작하는지 여부.</summary>
        public bool IsReturnPoint => _isReturnPoint;

        /// <summary>돌아온 플레이어가 놓일 위치.</summary>
        public Vector3 ArrivalPosition => (_arrivalPoint != null) ? _arrivalPoint.position : transform.position;
        #endregion

        #region Unity Lifecycle
        private void OnTriggerEnter(Collider other)
        {
            if (!_shouldEnterOnTouch || !GimmickUtility.TryGetPlayer(other, out _)) return;

            TryEnter();
        }
        #endregion

        #region Public Methods
        /// <inheritdoc />
        public void Interact(GameObject interactor)
        {
            TryEnter();
        }

        /// <summary>선행 과제가 끝났을 때 포탈을 엽니다.</summary>
        public void Unlock()
        {
            _isLocked = false;
        }
        #endregion

        #region Private Methods
        private void TryEnter()
        {
            if (_isLocked) return;

            SceneLoader.TryLoad(_destination);
        }
        #endregion
    }
}
