using Attribute.Core;
using UnityEngine;
using Core;

namespace UI
{
    public class UIManager : MonoBehaviour
    {
        // 플레이어 데이터를 UI에 보여줘야하는데 그 데이터를 조작하는 Presenter를 갖고있는다.
        [Header("References")]
        [SerializeField] private PlayerHudPresenter _playerHudPresenter;

        // 이벤트 구독용
        private GameManager _gameManager;

        private void OnEnable()
        {
            // 1. 플레이어를 연결할 HUD가 있는지 확인한다.
            if (_playerHudPresenter == null)
            {
                Debug.LogError(
                    "PlayerHudPresenter가 연결되지 않았습니다.", this);
                return;
            }

            // 2. 플레이어 등록을 담당하는 GameManager를 가져온다.
            _gameManager = GameManager.Instance;

            if (_gameManager == null)
            {
                Debug.LogError("씬에 GameManager가 없습니다.", this);
                return;
            }

            // 3. 앞으로 플레이어가 등록되면
            //    HandlePlayerSpawned를 실행해 달라고 등록한다. HandlePlayerSpwned 함수는 => UI를 스폰된 플레이어와 동기화 시키는 함수
            _gameManager.OnPlayerSpawned += HandlePlayerSpawned;

            // 4. UI가 켜지기 전에 이미 등록된 플레이어가 있다면
            //    그 플레이어를 지금 바로 HUD에 연결한다.
            //    아직 플레이어가 없다면 null이 전달된다.
            _playerHudPresenter.SetPlayerAttributes(_gameManager.CurrentPlayerState);
        }
        private void OnDisable()
        {
            // 1. 알림을 신청했던 GameManager에서 구독을 해제한다.
            if (_gameManager != null)
            {
                _gameManager.OnPlayerSpawned -= HandlePlayerSpawned;
            }

            // 2. 기억해 둔 GameManager 참조를 비운다.
            _gameManager = null;

            // 3. HUD와 플레이어의 연결도 해제한다.
            if (_playerHudPresenter != null)
            {
                _playerHudPresenter.SetPlayerAttributes(null);
            }
        }

        private void HandlePlayerSpawned(AttributeSet attributeSet)
        {
            // 전달받은 플레이어를 HUD에 연결한다.
            _playerHudPresenter.SetPlayerAttributes(attributeSet);
        }

        public void ShowPlayerHud() => _playerHudPresenter.Show();
        public void HidePlayerHud() => _playerHudPresenter.Hide();
    }
}
