using System.Collections;
using UnityEngine;
using Facade.Player;
using UIandCinematic;

namespace Core.Stage
{
    /// <summary>
    /// 튜토리얼 스테이지. 시작하면 플레이어를 따라오는 펫을 끄고, 첫 상호작용 대화가 끝나면 켠다.
    /// 첫 상호작용(InteractButton)의 On Interacted 에 대화 재생과 함께 UnlockPet 을 연결한다.
    /// 펫을 켜기 전에 죽어 다시 생긴 플레이어도 펫을 끈다. 펫은 PlayerFacade.SetPetEnabled 로만 켜고 끈다.
    /// </summary>
    public class Tutorial : StageBase
    {
        #region Private Fields
        private GameManager _gameManager;
        private bool _isPetUnlocked;
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

            _gameManager.OnPlayerSpawned -= ApplyPetEnabled;
        }
        #endregion

        #region Public Methods
        public override void StartStage()
        {
            // StartStage 는 스테이지마다 한 번 불린다. 혹시 다시 불려도 두 번 준비하지 않는다
            if (_gameManager != null) return;

            _gameManager = GameManager.Instance;
            if (_gameManager == null) return;

            // 다시 생긴 플레이어는 펫이 켜진 채로 나오므로 생길 때마다 적용한다
            _gameManager.OnPlayerSpawned += ApplyPetEnabled;
            ApplyPetEnabled(_gameManager.CurrentPlayerFacade);
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
        #endregion

        #region Private Methods
        /// <summary>
        /// 펫을 켰는지에 맞춰 플레이어의 펫을 켜거나 끈다.
        /// </summary>
        /// <param name="player">적용할 플레이어. null 이면 아무것도 하지 않는다</param>
        private void ApplyPetEnabled(PlayerFacade player)
        {
            if (player == null) return;

            player.SetPetEnabled(_isPetUnlocked);
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
            if (_gameManager != null) ApplyPetEnabled(_gameManager.CurrentPlayerFacade);
        }
        #endregion
    }
}
