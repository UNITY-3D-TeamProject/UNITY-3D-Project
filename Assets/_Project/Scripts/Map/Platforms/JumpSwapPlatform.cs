using UnityEngine;
using Core;
using Facade.Player;
using Movement;

namespace Map.Platforms
{
    /// <summary>
    /// 플레이어가 점프할 때마다 나타났다 사라지기를 번갈아 하는 발판. (공중 점프도 한 번으로 센다)
    /// 두 묶음(A·B)으로 나눠 두면, A 가 나타나 있을 때 B 는 사라져 있고 점프할 때마다 서로 바뀐다.
    /// </summary>
    public class JumpSwapPlatform : MonoBehaviour
    {
        #region Serialized Fields
        [Header("Group")]
        [Tooltip("켜면 처음에 나타나 있는 묶음(A), 끄면 처음에 사라져 있는 묶음(B)")]
        [SerializeField] private bool _isSolidAtStart = true;
        #endregion

        #region Private Fields
        private PlatformBody _body;
        private GameManager _gameManager;
        private CharacterMotor _playerMotor;
        private bool _isSolid = true;
        #endregion

        public bool IsSolid => _isSolid;

        #region Unity Lifecycle
        private void Start()
        {
            // 발판 위에 놓인 오브젝트까지 함께 사라지도록, 자식이 모두 붙은 뒤인 Start 에서 모은다
            _body = new PlatformBody(gameObject);
            SetSolid(_isSolidAtStart);
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

        #region Public Methods
        /// <summary>
        /// 코드로 붙인 직후 호출해 처음 상태를 정한다.
        /// </summary>
        /// <param name="isSolidAtStart">처음에 나타나 있을지 여부</param>
        public void Initialize(bool isSolidAtStart)
        {
            _isSolidAtStart = isSolidAtStart;
        }
        #endregion

        #region Private Methods
        // 다시 스폰된 경우 이전 플레이어의 구독부터 해제한다.
        private void BindPlayer(PlayerFacade playerFacade)
        {
            if (_playerMotor != null)
            {
                _playerMotor.OnJumpStarted -= HandleJumpStarted;
            }

            _playerMotor = (playerFacade != null)
                ? playerFacade.transform.root.GetComponentInChildren<CharacterMotor>()
                : null;

            if (_playerMotor != null)
            {
                _playerMotor.OnJumpStarted += HandleJumpStarted;
            }
        }

        private void HandleJumpStarted()
        {
            if (_body == null) return;

            SetSolid(!_isSolid);
        }

        private void SetSolid(bool isSolid)
        {
            _isSolid = isSolid;
            _body.SetSolid(isSolid);
        }
        #endregion
    }
}
