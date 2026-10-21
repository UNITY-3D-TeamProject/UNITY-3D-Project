using System;
using UnityEngine.InputSystem;
using Facade.Player;

namespace UI
{
    /// <summary>
    /// UI가 쓰는 플레이어 입력(설정창 열기·닫기, 대사 넘기기)과 입력 맵 전환을 한곳에서 맡는다.
    /// Player·UI 맵과 설정창 입력은 PlayerFacade를 거치고, PlayerFacade에 없는 대화 맵(Dialogue)은 PlayerInput에서 직접 받는다.
    /// </summary>
    public class UIInputHandler
    {
        #region Constants
        // 대화 중에 쓰는 입력 맵 이름 (PlayerInput의 Action Asset 기준)
        private const string ACTION_MAP_DIALOGUE = "Dialogue";
        // 대화 맵에서 대사를 넘기는 액션 (E)
        private const string ACTION_NEXT_LINE = "Dialogue/NextLine";
        // 대화 맵에서 설정창을 여는 액션 (ESC)
        private const string ACTION_OPEN_SETTINGS_IN_DIALOGUE = "Dialogue/OpenSettings";
        #endregion

        #region Private Fields
        // 현재 플레이어의 PlayerFacade. 설정창 입력 이벤트와 Player·UI 맵 전환에 사용한다.
        private PlayerFacade _playerFacade;
        // 현재 플레이어의 PlayerInput. 대화 맵은 PlayerFacade에 없으므로 여기서 직접 전환·구독한다.
        private UnityEngine.InputSystem.PlayerInput _playerInput;
        // 구독을 풀 때 쓰려고 찾아 둔 대화 맵 액션
        private InputAction _nextLineAction;
        private InputAction _openSettingsInDialogueAction;
        #endregion

        #region Properties
        // 연결된 플레이어가 있으면 true
        public bool HasPlayer => _playerFacade != null;
        #endregion

        #region Events
        // 설정창 열기 입력 (Player 맵 ESC, Dialogue 맵 ESC)
        public event Action OnOpenSettingsRequested;
        // 설정창 닫기 입력 (UI 맵 ESC)
        public event Action OnCloseSettingsRequested;
        // 다음 대사 입력 (Dialogue 맵 E)
        public event Action OnNextLineRequested;
        #endregion

        #region Public Methods
        // 이전 플레이어의 구독을 풀고, 새 플레이어의 설정창 이벤트와 대화 맵 액션을 구독한다. null이면 해제만 한다.
        public void BindPlayer(PlayerFacade playerFacade)
        {
            UnbindPlayer();

            if (playerFacade == null) return;

            _playerFacade = playerFacade;
            _playerFacade.OnOpenSettingsRequested += HandleOpenSettings;
            _playerFacade.OnCloseSettingsRequested += HandleCloseSettings;

            // 대화 입력은 플레이어의 PlayerInput에서 직접 받는다.
            if (_playerFacade.TryGetComponent(out _playerInput) && (_playerInput.actions != null))
            {
                _nextLineAction = _playerInput.actions.FindAction(ACTION_NEXT_LINE);
                _openSettingsInDialogueAction = _playerInput.actions.FindAction(ACTION_OPEN_SETTINGS_IN_DIALOGUE);

                if (_nextLineAction != null) _nextLineAction.performed += HandleNextLine;
                if (_openSettingsInDialogueAction != null) _openSettingsInDialogueAction.performed += HandleOpenSettingsInDialogue;
            }
        }

        // 현재 플레이어의 이벤트·액션 구독을 모두 풀고 참조를 비운다.
        public void UnbindPlayer()
        {
            if (_playerFacade != null)
            {
                _playerFacade.OnOpenSettingsRequested -= HandleOpenSettings;
                _playerFacade.OnCloseSettingsRequested -= HandleCloseSettings;
            }

            if (_nextLineAction != null) _nextLineAction.performed -= HandleNextLine;
            if (_openSettingsInDialogueAction != null) _openSettingsInDialogueAction.performed -= HandleOpenSettingsInDialogue;

            _playerFacade = null;
            _playerInput = null;
            _nextLineAction = null;
            _openSettingsInDialogueAction = null;
        }

        // 입력 맵을 플레이어 조작용으로 바꾼다.
        public void SwitchToPlayerInput()
        {
            if (_playerFacade != null)
                _playerFacade.SwitchToPlayerInput();
        }

        // 입력 맵을 UI(설정창)용으로 바꾼다.
        public void SwitchToUIInput()
        {
            if (_playerFacade != null)
                _playerFacade.SwitchToUIInput();
        }

        // 입력 맵을 대화용으로 바꾼다. PlayerFacade에는 대화 맵 전환이 없으므로 PlayerInput에서 직접 바꾼다.
        public void SwitchToDialogueInput()
        {
            if (_playerInput != null)
                _playerInput.SwitchCurrentActionMap(ACTION_MAP_DIALOGUE);
        }
        #endregion

        #region Private Methods
        // Player 맵 ESC (PlayerFacade 이벤트)
        private void HandleOpenSettings()
        {
            OnOpenSettingsRequested?.Invoke();
        }

        // UI 맵 ESC (PlayerFacade 이벤트)
        private void HandleCloseSettings()
        {
            OnCloseSettingsRequested?.Invoke();
        }

        // 대화 맵 E
        private void HandleNextLine(InputAction.CallbackContext context)
        {
            OnNextLineRequested?.Invoke();
        }

        // 대화 맵 ESC
        private void HandleOpenSettingsInDialogue(InputAction.CallbackContext context)
        {
            OnOpenSettingsRequested?.Invoke();
        }
        #endregion
    }
}
