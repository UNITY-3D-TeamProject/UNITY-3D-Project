using UnityEngine;
using Core.Stage.Security;

namespace Core.Stage
{
    /// <summary>
    /// 보안 앱(전투 맵) 스테이지. 지금 라운드 번호는 StageBase 가 따라가고, 이 컴포넌트는 번호를 보고 그 라운드의 로직을 직접 부른다. 설정은 인스펙터의 각 라운드 항목에서 한다.
    /// - 1라운드: 잡몹 방(MobRoomRound). 방의 적을 다 잡으면 출구 문이 열린다.
    /// - 2라운드: 특수몹 발판(SpecialMobRound). 특수몹을 잡으면 다음 발판이 나타난다.
    /// - 3라운드: 보스(BossRound). 보스를 쓰러뜨리면 탈출 문이 나타난다.
    /// 라운드 중 플레이어가 죽으면 그 라운드의 못 깬 구역만 다시 시작한다. (보스는 BossFightSpawner 가 다시 만든다)
    /// </summary>
    public class SecurityApp : StageBase
    {
        #region Constants
        /// <summary>잡몹 방 라운드 번호 (0부터, 1라운드).</summary>
        public const int MOB_ROOM_ROUND_INDEX = 0;

        /// <summary>특수몹 발판 라운드 번호 (0부터, 2라운드).</summary>
        public const int SPECIAL_MOB_ROUND_INDEX = 1;

        /// <summary>보스 라운드 번호 (0부터, 3라운드).</summary>
        public const int BOSS_ROUND_INDEX = 2;
        #endregion

        #region Serialized Fields
        [Header("1라운드 · 잡몹 방")]
        [SerializeField] private MobRoomRound _mobRoomRound = new MobRoomRound();

        [Header("2라운드 · 특수몹 발판")]
        [SerializeField] private SpecialMobRound _specialMobRound = new SpecialMobRound();

        [Header("3라운드 · 보스")]
        [SerializeField] private BossRound _bossRound = new BossRound();
        #endregion

        #region Private Fields
        private StageManager _stageManager;
        #endregion

        #region Properties
        public override EStageType StageType => EStageType.SecurityApp;

        /// <summary>1라운드 잡몹 방.</summary>
        public MobRoomRound MobRoomRound => _mobRoomRound;

        /// <summary>2라운드 특수몹 발판.</summary>
        public SpecialMobRound SpecialMobRound => _specialMobRound;

        /// <summary>3라운드 보스.</summary>
        public BossRound BossRound => _bossRound;
        #endregion

        #region Unity Lifecycle
        // 라운드 변경 감지는 부모가 하고, 보스 라운드 동안 다 내려간 발판을 숨긴다.
        protected override void Update()
        {
            base.Update();

            if (CurrentRoundIndex == BOSS_ROUND_INDEX) _bossRound.HideLoweredPlatforms();
        }

        private void OnDestroy()
        {
            // StartStage 에서 준비하지 않았으면 구독도 없다
            if (_stageManager == null) return;

            _stageManager.OnPlayerRespawnRequested -= HandlePlayerRespawnRequested;

            _mobRoomRound.OnProgressChanged -= HandleRoundProgressChanged;
            _specialMobRound.OnProgressChanged -= HandleRoundProgressChanged;
            _bossRound.OnProgressChanged -= HandleRoundProgressChanged;

            _mobRoomRound.End();
            _specialMobRound.End();
            _bossRound.End();
        }
        #endregion

        #region Public Methods
        public override void StartStage()
        {
            // StartStage 는 스테이지마다 한 번 불린다. 혹시 다시 불려도 두 번 준비하지 않는다
            if (_stageManager != null) return;

            _stageManager = GameManager.Instance.CurrentStageManager;
            if (_stageManager == null)
            {
                Debug.LogWarning($"[{name}] StageManager가 없어 라운드를 시작하지 않습니다.", this);
                return;
            }

            _stageManager.OnPlayerRespawnRequested += HandlePlayerRespawnRequested;

            _mobRoomRound.OnProgressChanged += HandleRoundProgressChanged;
            _specialMobRound.OnProgressChanged += HandleRoundProgressChanged;
            _bossRound.OnProgressChanged += HandleRoundProgressChanged;

            _mobRoomRound.Begin();
            _specialMobRound.Begin();
            _bossRound.Begin();
        }

        public override void EndStage()
        {
        }

        public override bool IsRoundCleared(int roundIndex)
        {
            return roundIndex switch
            {
                MOB_ROOM_ROUND_INDEX => _mobRoomRound.IsCleared,
                SPECIAL_MOB_ROUND_INDEX => _specialMobRound.IsCleared,
                BOSS_ROUND_INDEX => _bossRound.IsCleared,
                _ => true,
            };
        }
        #endregion

        #region Protected Methods
        // 보스 라운드로 바뀌면(3라운드 체크포인트 도착) 2라운드 발판을 내리고 앞 구간을 숨긴다.
        protected override void OnRoundChanged()
        {
            if (CurrentRoundIndex != BOSS_ROUND_INDEX) return;

            _bossRound.LowerPlatforms();
            _bossRound.HideAreas();
        }
        #endregion

        #region Private Methods
        // 라운드 중 죽으면 그 라운드의 못 깬 구역만 처음 상태로 돌린다. 보스 라운드는 리스폰 자리 발판만 올린다.
        // [이벤트] StageManager.OnPlayerRespawnRequested (StartStage 에서 구독)
        private void HandlePlayerRespawnRequested()
        {
            switch (CurrentRoundIndex)
            {
                case MOB_ROOM_ROUND_INDEX:
                    _mobRoomRound.ResetUnclearedRooms();
                    break;

                case SPECIAL_MOB_ROUND_INDEX:
                    _specialMobRound.ResetUnclearedSteps();
                    break;

                case BOSS_ROUND_INDEX:
                    _bossRound.RestoreRespawnPlatform();
                    break;
            }
        }

        private void HandleRoundProgressChanged()
        {
            NotifyProgressChanged();
        }
        #endregion
    }
}
