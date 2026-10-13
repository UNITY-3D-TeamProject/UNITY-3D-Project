using UnityEngine;
using Core;
using Facade.Player;
using Interaction;

namespace Map.Gimmicks
{
    /// <summary>
    /// 플레이어가 범위 안에서 상호작용 스킬(E)을 쓰면 같은 오브젝트의 IInteractable 을 실행한다.
    /// 플레이어 쪽에 주변의 IInteractable 을 찾아 실행하는 코드가 아직 없어, PlayerFacade.OnSkillUsed 로 입력을 받는다.
    /// </summary>
    public class PlayerInteractRelay : MonoBehaviour
    {
        #region Constants
        // Interact 스킬 프리팹의 Skill Name
        private const string INTERACT_SKILL_NAME = "Interact";
        #endregion

        #region Serialized Fields
        [Header("Detection")]
        [Tooltip("이 오브젝트와 플레이어 몸 중심 사이의 거리가 이 값 이하일 때만 상호작용을 받는다.")]
        [SerializeField, Min(0f)] private float _range = 3.0f;
        #endregion

        #region Private Fields
        private IInteractable[] _interactables;
        private GameManager _gameManager;
        private PlayerFacade _playerFacade;
        private CharacterController _playerController;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            _interactables = GetComponents<IInteractable>();
        }

        private void OnEnable()
        {
            _gameManager = GameManager.Instance;
            if (_gameManager == null) return;

            _gameManager.OnPlayerSpawned += BindPlayer;

            // 이미 생성된 플레이어가 있다면 바로 연결한다
            BindPlayer(_gameManager.CurrentPlayerFacade);
        }

        private void OnDisable()
        {
            if (_gameManager != null)
            {
                _gameManager.OnPlayerSpawned -= BindPlayer;
            }

            BindPlayer(null);
            _gameManager = null;
        }
        #endregion

        #region Private Methods
        // 다시 스폰된 경우 이전 플레이어의 구독부터 해제한다.
        private void BindPlayer(PlayerFacade playerFacade)
        {
            if (_playerFacade != null)
            {
                _playerFacade.OnSkillUsed -= HandleSkillUsed;
            }

            _playerFacade = playerFacade;
            _playerController = null;

            if (_playerFacade == null) return;

            // 실제로 움직이는 몸체는 플레이어 루트가 아니라 자식의 CharacterController 다
            _playerController = _playerFacade.transform.root.GetComponentInChildren<CharacterController>();
            _playerFacade.OnSkillUsed += HandleSkillUsed;
        }

        private void HandleSkillUsed(string skillName, float cooldown)
        {
            if (skillName != INTERACT_SKILL_NAME) return;
            if (_playerController == null) return;
            if (Vector3.Distance(transform.position, _playerController.bounds.center) > _range) return;

            GameObject interactor = _playerFacade.transform.root.gameObject;
            foreach (IInteractable interactable in _interactables)
            {
                interactable.Interact(interactor);
            }
        }
        #endregion
    }
}
