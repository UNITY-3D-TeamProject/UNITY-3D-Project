using UnityEngine;
using Interaction;
using SceneTransition;
using UIandCinematic;

namespace Map.Gimmicks
{
    /// <summary>
    /// 플레이어가 닿거나 상호작용(E)하면 지정한 씬으로 전환 요청 순서상 갈 수 없는 씬이면 아무 일도 일어나지 않는다.
    /// 닿기 전환은 콜라이더의 Is Trigger를 켜야 하고, 상호작용 전환은 콜라이더를 Interact 레이어로 둔다.
    /// 입장 전 컷씬 번호를 넣으면 상호작용 시 그 컷씬을 먼저 재생하고, 끝나면 전환한다.
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

        [Header("Cutscene Before Enter")]
        [Tooltip("0보다 크면 상호작용(E) 시 이 번호의 컷씬을 먼저 재생하고, 끝나면 전환한다. 재생하지 못하면 바로 전환한다. (로비 앱 포탈의 입장 전 대화)")]
        [SerializeField] private int _cutsceneIdBeforeEnter;

        [Tooltip("입장 전 컷씬이 시작될 때 끌 오브젝트. 대화 카메라를 가리지 않게 한다. (빛기둥 NextMarker, 이 포탈 자신)")]
        [SerializeField] private GameObject[] _hideDuringCutscene;
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
        private void OnDestroy()
        {
            CinematicManager.OnPlayFinished -= HandleCutsceneFinished;
        }

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
            if (_isLocked) return;
            if (TryPlayCutsceneBeforeEnter()) return;

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

        // 입장 전 컷씬을 재생하고 끝나면 전환하도록 기다린다. 재생을 시작하지 못했으면 false.
        private bool TryPlayCutsceneBeforeEnter()
        {
            CinematicManager cinematicManager = CinematicManager.Instance;
            if ((_cutsceneIdBeforeEnter <= 0) || (cinematicManager == null)) return false;

            // 한 번만 보는 대화를 이미 봤거나 다른 연출 중이면 재생되지 않는다.
            cinematicManager.PlayCutscene(_cutsceneIdBeforeEnter);
            if (!cinematicManager.IsPlaying) return false;

            // 이 포탈 자신을 꺼도 static 이벤트 구독은 남아 있어 끝나면 전환된다.
            CinematicManager.OnPlayFinished += HandleCutsceneFinished;

            foreach (GameObject hideObject in _hideDuringCutscene)
            {
                if (hideObject == null) continue;

                hideObject.SetActive(false);
            }

            return true;
        }

        private void HandleCutsceneFinished(CinematicManager cinematicManager)
        {
            CinematicManager.OnPlayFinished -= HandleCutsceneFinished;
            TryEnter();
        }
        #endregion
    }
}
