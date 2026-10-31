using System.Collections;
using UnityEngine;
using Facade.Player;
using UIandCinematic;

namespace Core.Stage
{
    /// <summary>
    /// 튜토리얼 스테이지. 시작 시 펫과 미안내 스킬을 끄고, 안내 진행에 따라 다시 켠다.
    /// 첫 상호작용(InteractButton)의 On Interacted 에 대화 재생과 함께 UnlockPet 을 연결한다.
    /// 재스폰 플레이어에는 해금 상태를 재적용한다. 펫과 스킬은 PlayerFacade를 통해 제어한다.
    /// </summary>
    public class Tutorial : StageBase
    {
        #region Constants
        private const string MULTI_JUMP_SKILL_NAME = "MultiJump";
        private const string INTERACT_SKILL_NAME = "Interact";
        private const string ROLL_SKILL_NAME = "Roll";
        private const string ATTACK_SKILL_NAME = "Attack";
        private const string SCAN_SKILL_NAME = "Scan";
        private const string VEHICLE_SKILL_NAME = "SpawnVehicle";
        #endregion

        #region Private Fields
        private GameManager _gameManager;
        private bool _isPetUnlocked;
        private bool _isRollUnlocked;
        private bool _isScanUnlocked;
        private bool _isVehicleUnlocked;
        private Coroutine _unlockPetRoutine;
        #endregion

        #region Properties
        public override EStageType StageType => EStageType.Tutorial;
        #endregion

        #region Unity Lifecycle
        private void OnDestroy()
        {
            // StartStage 에서 준비하지 않았으면 구독도 없다
            if (_gameManager == null) return;

            _gameManager.OnPlayerSpawned -= ApplyPlayerState;
        }
        #endregion

        #region Public Methods
        public override void StartStage()
        {
            // StartStage 는 스테이지마다 한 번 불린다. 혹시 다시 불려도 두 번 준비하지 않는다
            if (_gameManager != null) return;

            _gameManager = GameManager.Instance;
            if (_gameManager == null) return;

            // 이어하기는 이미 배운 스킬을 다시 막지 않는다.
            if (_gameManager.IsContinueSpawn)
            {
                _isPetUnlocked = true;
                _isRollUnlocked = true;
                _isScanUnlocked = true;
                _isVehicleUnlocked = true;
            }

            // 다시 생긴 플레이어에도 펫과 스킬 해금 상태를 적용한다.
            _gameManager.OnPlayerSpawned += ApplyPlayerState;
            ApplyPlayerState(_gameManager.CurrentPlayerFacade);
        }

        public override void EndStage()
        {
        }

        /// <summary>
        /// 지금 재생 중인 대화가 끝나면 펫을 켠다. 첫 상호작용의 On Interacted 에 대화 재생과 함께 연결한다.
        /// </summary>
        public void UnlockPet()
        {
            if (_isPetUnlocked || (_unlockPetRoutine != null)) return;

            _unlockPetRoutine = StartCoroutine(CoUnlockPetAfterDialogue());
        }

        public void UnlockRoll()
        {
            if (_isRollUnlocked) return;
            _isRollUnlocked = true;
            SetCurrentPlayerSkill(ROLL_SKILL_NAME);
        }

        public void UnlockScan()
        {
            if (_isScanUnlocked) return;
            _isScanUnlocked = true;
            SetCurrentPlayerSkill(SCAN_SKILL_NAME);
        }

        public void UnlockVehicle()
        {
            if (_isVehicleUnlocked) return;
            _isVehicleUnlocked = true;
            SetCurrentPlayerSkill(VEHICLE_SKILL_NAME);
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// 현재 튜토리얼 진행 상태를 플레이어에 적용한다.
        /// </summary>
        /// <param name="player">적용할 플레이어. null 이면 아무것도 하지 않는다</param>
        private void ApplyPlayerState(PlayerFacade player)
        {
            if (player == null) return;

            player.SetPetEnabled(_isPetUnlocked);
            player.SetSkillEnabled(MULTI_JUMP_SKILL_NAME, true);
            player.SetSkillEnabled(INTERACT_SKILL_NAME, true);
            player.SetSkillEnabled(ROLL_SKILL_NAME, _isRollUnlocked);
            player.SetSkillEnabled(ATTACK_SKILL_NAME, true);
            player.SetSkillEnabled(SCAN_SKILL_NAME, _isScanUnlocked);
            player.SetSkillEnabled(VEHICLE_SKILL_NAME, _isVehicleUnlocked);
        }

        private void SetCurrentPlayerSkill(string skillName)
        {
            PlayerFacade player = _gameManager != null ? _gameManager.CurrentPlayerFacade : null;
            if (player != null) player.SetSkillEnabled(skillName, true);
        }
        #endregion

        #region Coroutines
        private IEnumerator CoUnlockPetAfterDialogue()
        {
            // 같은 On Interacted 에서 대화가 UnlockPet 보다 늦게 시작해도 기다리도록 한 프레임 넘긴다
            yield return null;

            while ((CutsceneController.Current != null) && CutsceneController.Current.IsDialoguePlaying)
                yield return null;

            _unlockPetRoutine = null;
            _isPetUnlocked = true;
            if (_gameManager != null && _gameManager.CurrentPlayerFacade != null)
                _gameManager.CurrentPlayerFacade.SetPetEnabled(true);
        }
        #endregion
    }
}
