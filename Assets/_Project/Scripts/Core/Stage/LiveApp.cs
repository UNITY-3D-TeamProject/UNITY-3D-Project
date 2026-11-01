using Facade.Player;

namespace Core.Stage
{
    /// <summary>
    /// 라이브 앱 스테이지. 엔딩 연출만 있는 맵이라 구르기·블루투스(탈것)·스캔 스킬을 끈다.
    /// 재스폰 플레이어에도 다시 끈다. 스킬은 PlayerFacade를 통해 제어한다.
    /// </summary>
    public class LiveApp : StageBase
    {
        #region Constants
        private const string ROLL_SKILL_NAME = "Roll";
        private const string VEHICLE_SKILL_NAME = "SpawnVehicle";
        private const string SCAN_SKILL_NAME = "Scan";
        #endregion

        #region Private Fields
        private GameManager _gameManager;
        #endregion

        #region Properties
        public override EStageType StageType => EStageType.LiveApp;
        #endregion

        #region Unity Lifecycle
        private void OnDestroy()
        {
            // StartStage 에서 준비하지 않았으면 구독도 없다
            if (_gameManager == null) return;

            _gameManager.OnPlayerSpawned -= DisableSkills;
        }
        #endregion

        #region Public Methods
        public override void StartStage()
        {
            // StartStage 는 스테이지마다 한 번 불린다. 혹시 다시 불려도 두 번 구독하지 않는다
            if (_gameManager != null) return;

            _gameManager = GameManager.Instance;
            if (_gameManager == null) return;

            // 다시 생긴 플레이어에도 스킬을 끈다.
            _gameManager.OnPlayerSpawned += DisableSkills;
            DisableSkills(_gameManager.CurrentPlayerFacade);
        }

        public override void EndStage()
        {
        }
        #endregion

        #region Private Methods
        // 구르기·블루투스·스캔 스킬을 끈다. null 이면 아무것도 하지 않는다.
        private void DisableSkills(PlayerFacade player)
        {
            if (player == null) return;

            player.SetSkillEnabled(ROLL_SKILL_NAME, false);
            player.SetSkillEnabled(VEHICLE_SKILL_NAME, false);
            player.SetSkillEnabled(SCAN_SKILL_NAME, false);
        }
        #endregion
    }
}
