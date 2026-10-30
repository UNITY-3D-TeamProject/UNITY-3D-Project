using UnityEngine;
using AI.Boss.Fight;
using AI.Boss.Logic;
using Core.Audio;
using Core.Stage.Security;
using Map.Gimmicks;

namespace Core.Stage
{
    /// <summary>
    /// 보안 앱(전투 맵) 스테이지. 지금 라운드 번호는 StageBase 가 따라가고, 이 컴포넌트는 번호를 보고 그 라운드의 로직을 직접 부른다. 설정은 인스펙터의 각 라운드 항목에서 한다.
    /// - 1라운드: 잡몹 방(MobRoomRound). 방의 적을 다 잡으면 출구 문이 열린다. 이 라운드 동안 구역 바닥으로 위험물이 떨어진다.
    /// - 2라운드: 특수몹 발판(SpecialMobRound). 특수몹을 잡으면 다음 발판이 나타난다. 이 라운드 동안 발판 위로 위험물이 떨어진다.
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
        [Header("BGM")]
        [SerializeField] private SOAudioData _preBossBgm;
        [SerializeField] private SOAudioData _bossEarlyBgm;
        [SerializeField] private SOAudioData _bossFinalBgm;
        [SerializeField] private BossEncounter _bossEncounter;

        [Header("1라운드 · 잡몹 방")]
        [SerializeField] private MobRoomRound _mobRoomRound = new MobRoomRound();

        [Tooltip("1라운드 동안 구역(복도·방·연결 통로)마다 바닥으로 위험물을 떨어뜨리는 생성기. 구역별 개수·간격은 각 생성기에서 정한다.")]
        [SerializeField] private FallingHazardSpawner[] _zoneHazardSpawners;

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
            if (_bossEncounter != null) _bossEncounter.OnStageChanged -= HandleBossStageChanged;

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
            if (_bossEncounter != null) _bossEncounter.OnStageChanged += HandleBossStageChanged;

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
        // 1라운드 동안은 구역 바닥으로, 2라운드 동안은 발판 위로 위험물을 떨어뜨린다. 보스 라운드에는 둘 다 멈춘다.
        // 보스 라운드로 바뀌면(3라운드 체크포인트 도착) 2라운드 발판을 내리고 앞 구간을 숨긴다.
        protected override void OnRoundChanged()
        {
            if ((CurrentRoundIndex >= MOB_ROOM_ROUND_INDEX) && (CurrentRoundIndex < BOSS_ROUND_INDEX))
                AudioManager.Instance?.ChangeBgm(_preBossBgm);
            else if (CurrentRoundIndex == BOSS_ROUND_INDEX)
                AudioManager.Instance?.ChangeBgm(_bossEarlyBgm);

            if (CurrentRoundIndex == SPECIAL_MOB_ROUND_INDEX) _specialMobRound.StartHazards();
            else _specialMobRound.StopHazards();

            SetZoneHazards(CurrentRoundIndex == MOB_ROOM_ROUND_INDEX);

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
                    ClearZoneHazards();
                    _mobRoomRound.ResetUnclearedRooms();
                    break;

                case SPECIAL_MOB_ROUND_INDEX:
                    _specialMobRound.ResetUnclearedSteps();
                    break;

                case BOSS_ROUND_INDEX:
                    _bossRound.RestoreRespawnPlatform();
                    AudioManager.Instance?.ChangeBgm(_bossEarlyBgm);
                    break;
            }
        }

        private void HandleRoundProgressChanged()
        {
            NotifyProgressChanged();
        }

        private void HandleBossStageChanged(EBossStage stage)
        {
            if (CurrentRoundIndex == BOSS_ROUND_INDEX && stage == EBossStage.Final)
                AudioManager.Instance?.ChangeBgm(_bossFinalBgm);
        }

        // 구역 생성기를 모두 시작하거나(떨어지는 중인 위험물까지) 멈춘다.
        private void SetZoneHazards(bool isOn)
        {
            if (_zoneHazardSpawners == null) return;

            foreach (FallingHazardSpawner spawner in _zoneHazardSpawners)
            {
                if (spawner == null) continue;

                if (isOn) spawner.StartSpawning();
                else spawner.StopSpawning();
            }
        }

        // 리스폰 직후 맞지 않게 떨어지는 중인 위험물만 지우고 생성은 계속한다.
        private void ClearZoneHazards()
        {
            if (_zoneHazardSpawners == null) return;

            foreach (FallingHazardSpawner spawner in _zoneHazardSpawners)
            {
                if (spawner != null) spawner.ClearHazards();
            }
        }
        #endregion
    }
}
