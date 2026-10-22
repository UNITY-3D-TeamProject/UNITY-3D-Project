using System.Collections.Generic;
using UnityEngine;
using Facade.Player;

namespace Core.InputMap
{
    // 입력 맵 종류. 요청이 여러 개면 값이 큰 쪽을 쓴다.
    public enum EInputMapType
    {
        Player,
        Dialogue,
        UI,
    }

    /// <summary>
    /// 현재 플레이어의 입력 맵(Player / Dialogue / UI)을 한곳에서 정한다. 씬마다 하나 둔다(Canvas_InGame 프리팹).
    /// 조작을 막거나 설정창을 여는 쪽이 자기 이름으로 맵을 요청·해제하면, 남은 요청 중 가장 우선하는 맵을 쓴다(UI > Dialogue > Player).
    /// 요청이 없으면 Player 맵이다. 플레이어가 새로 스폰되면 지금 맵을 다시 적용한다.
    /// 꺼진 동안은 Player 맵으로 돌려 두되 요청은 남겨 두고, 다시 켜지면 남은 요청(진행 중인 연출 등)대로 다시 적용한다.
    /// </summary>
    public class InputMapController : MonoBehaviour
    {
        #region Constants
        // 대화 중에 쓰는 입력 맵 이름 (PlayerInput의 Action Asset 기준)
        private const string ACTION_MAP_DIALOGUE = "Dialogue";
        #endregion

        #region Private Fields
        // 요청한 쪽과 그쪽이 원하는 입력 맵
        private readonly Dictionary<Object, EInputMapType> _requests = new();

        private GameManager _gameManager;
        // 현재 플레이어. Player·UI 맵은 PlayerFacade로 바꾸고, PlayerFacade에 없는 Dialogue 맵은 PlayerInput에서 직접 바꾼다.
        private PlayerFacade _playerFacade;
        private UnityEngine.InputSystem.PlayerInput _playerInput;
        #endregion

        #region Properties
        // 지금 씬의 InputMapController. 배치하지 않은 씬이거나 Awake 전이면 null이다.
        public static InputMapController Instance { get; private set; }
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            if ((Instance != null) && (Instance.gameObject.scene == gameObject.scene))
                Debug.LogError("[InputMapController] 한 씬에 InputMapController가 두 개 있습니다. 나중에 Awake된 쪽이 Instance가 됩니다.", this);

            Instance = this;
        }

        private void OnEnable()
        {
            _gameManager = GameManager.Instance;
            if (_gameManager == null) return;

            _gameManager.OnPlayerSpawned += HandlePlayerSpawned;
            HandlePlayerSpawned(_gameManager.CurrentPlayerFacade);
        }

        private void OnDisable()
        {
            // 꺼진 동안은 입력 맵을 맡지 않으므로 플레이어 조작으로 돌려 둔다.
            // 요청은 지우지 않는다. 요청한 쪽(연출 등)은 다시 요청하지 않으므로, 다시 켜질 때 OnEnable에서 그대로 다시 적용한다.
            if (_playerFacade != null)
                _playerFacade.SwitchToPlayerInput();

            if (_gameManager != null)
                _gameManager.OnPlayerSpawned -= HandlePlayerSpawned;

            _gameManager = null;
            _playerFacade = null;
            _playerInput = null;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }
        #endregion

        #region Public Methods
        // requester 이름으로 입력 맵을 요청한다. 같은 requester가 다시 부르면 요청한 맵만 바꾼다.
        public void RequestMap(Object requester, EInputMapType map)
        {
            if (requester == null) return;

            _requests[requester] = map;
            ApplyMap();
        }

        // requester의 요청을 푼다. 남은 요청이 없으면 Player 맵으로 돌아간다.
        public void ReleaseMap(Object requester)
        {
            if ((requester == null) || !_requests.Remove(requester)) return;

            ApplyMap();
        }
        #endregion

        #region Private Methods
        // 플레이어가 스폰되거나 바뀌면 새 플레이어에 지금 맵을 적용한다. null이면 연결만 해제한다.
        private void HandlePlayerSpawned(PlayerFacade playerFacade)
        {
            _playerFacade = playerFacade;
            _playerInput = null;

            if (_playerFacade != null)
                _playerFacade.TryGetComponent(out _playerInput);

            ApplyMap();
        }

        // 남은 요청 중 가장 우선하는 맵을 현재 플레이어에 적용한다.
        private void ApplyMap()
        {
            if (_playerFacade == null) return;

            EInputMapType map = EInputMapType.Player;
            foreach (EInputMapType requestedMap in _requests.Values)
            {
                if (requestedMap > map)
                    map = requestedMap;
            }

            switch (map)
            {
                case EInputMapType.UI:
                    _playerFacade.SwitchToUIInput();
                    break;
                case EInputMapType.Dialogue:
                    if (_playerInput != null)
                        _playerInput.SwitchCurrentActionMap(ACTION_MAP_DIALOGUE);
                    break;
                default:
                    _playerFacade.SwitchToPlayerInput();
                    break;
            }
        }
        #endregion
    }
}
