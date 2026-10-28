using System;
using UnityEngine;
using AI.Boss.Fight;
using Map.Gimmicks;

namespace Core.Stage.Security
{
    /// <summary>
    /// 3라운드 보스. 보스 생성·단계 진행은 BossFightSpawner·BossEncounter 가 하고(사망 시 재생성도 스포너가 한다),
    /// 이 라운드는 보스(A)를 쓰러뜨리면 꺼 두었던 탈출 문을 켠다.
    /// 2라운드 발판은 보스 라운드가 시작되면 내리고, 리스폰 자리 발판은 보스 바닥에 올라서면 내렸다가 리스폰할 때만 즉시 올린다. 다 내려간 발판은 꺼서 숨긴다.
    /// 보스 라운드가 시작되면 앞 구간(1라운드 방 등)도 꺼서 숨긴다.
    /// 플레이어가 들어온 쪽 투명 벽은 보스 바닥에 올라서면 켜고, 리스폰할 때 꺼서 발판에서 다시 들어올 수 있게 한다.
    /// SecurityApp 이 필드로 들고, 스테이지 시작 때 Begin, 끝날 때 End, 보스 라운드가 되면 LowerPlatforms·HideAreas, 보스 라운드 동안 매 프레임 HideLoweredPlatforms,
    /// 이 라운드 중 죽으면 RestoreRespawnPlatform 을 부른다.
    /// </summary>
    [Serializable]
    public class BossRound
    {
        #region Serialized Fields
        [Tooltip("보스전을 여는 스포너 (05_Boss/BossFight)")]
        [SerializeField] private BossFightSpawner _bossFightSpawner;

        [Tooltip("보스를 쓰러뜨리면 나타나는 탈출 문. 스테이지가 시작될 때 꺼 둔다.")]
        [SerializeField] private GameObject _exitDoor;

        [Tooltip("보스 라운드가 시작되면(3라운드 체크포인트 도착) 아래로 내려가는 2라운드 발판 (Platform_1~5)")]
        [SerializeField] private StateMover[] _sinkPlatforms;

        [Tooltip("3라운드 리스폰 자리 발판 (Platform_6). 보스 바닥에 올라서면 내려가고, 리스폰할 때 즉시 원위치로 올라온다.")]
        [SerializeField] private StateMover _respawnPlatform;

        [Tooltip("보스 바닥에 올라섰는지 알아채는 트리거. 닿으면 리스폰 자리 발판을 내린다.")]
        [SerializeField] private TriggerPlate _arenaEntrance;

        [Tooltip("보스 라운드가 시작되면 꺼서 숨기는 앞 구간 (03_Round1_Battle 등)")]
        [SerializeField] private GameObject[] _hiddenAreas;

        [Tooltip("플레이어가 들어온 쪽 아레나 투명 벽. 보스 바닥에 올라서면 켜고, 리스폰할 때 끈다.")]
        [SerializeField] private GameObject _arenaWall;
        #endregion

        #region Properties
        /// <summary>보스를 쓰러뜨렸는지 여부.</summary>
        public bool IsCleared { get; private set; }
        #endregion

        #region Events
        /// <summary>보스를 쓰러뜨리면 호출된다. 화면 갱신용.</summary>
        public event Action OnProgressChanged;
        #endregion

        #region Public Methods
        /// <summary>스테이지가 시작될 때 한 번 호출된다. 탈출 문을 꺼 두고 보스 격파를 구독한다.</summary>
        public void Begin()
        {
            if (_exitDoor != null) _exitDoor.SetActive(false);
            if (_arenaWall != null) _arenaWall.SetActive(false);
            if (_arenaEntrance != null) _arenaEntrance.OnPlayerEntered.AddListener(HandleArenaEntered);

            if (_bossFightSpawner == null)
            {
                Debug.LogWarning("BossFightSpawner가 연결되지 않아 탈출 문을 열 수 없습니다.");
                return;
            }

            _bossFightSpawner.OnBossDefeated += HandleBossDefeated;
        }

        /// <summary>스테이지가 없어질 때 한 번 호출된다. 구독을 푼다.</summary>
        public void End()
        {
            if (_bossFightSpawner != null) _bossFightSpawner.OnBossDefeated -= HandleBossDefeated;
            if (_arenaEntrance != null) _arenaEntrance.OnPlayerEntered.RemoveListener(HandleArenaEntered);
        }

        /// <summary>보스 라운드가 시작될 때 호출된다. 2라운드 발판을 내린다.</summary>
        public void LowerPlatforms()
        {
            if (_sinkPlatforms == null) return;

            foreach (StateMover platform in _sinkPlatforms)
            {
                if (platform != null) platform.Activate();
            }
        }

        /// <summary>보스 라운드가 시작될 때 호출된다. 앞 구간을 꺼서 숨긴다.</summary>
        public void HideAreas()
        {
            if (_hiddenAreas == null) return;

            foreach (GameObject area in _hiddenAreas)
            {
                if (area != null) area.SetActive(false);
            }
        }

        /// <summary>보스 라운드 동안 매 프레임 호출된다. 다 내려간 발판을 꺼서 숨긴다.</summary>
        public void HideLoweredPlatforms()
        {
            if (_sinkPlatforms != null)
            {
                foreach (StateMover platform in _sinkPlatforms)
                {
                    HideIfLowered(platform);
                }
            }

            HideIfLowered(_respawnPlatform);
        }

        /// <summary>보스 라운드 중 죽었을 때 호출된다. 아레나 벽을 끄고, 리스폰 자리 발판을 즉시 원위치로 올리고 다시 켠다.</summary>
        public void RestoreRespawnPlatform()
        {
            if (_arenaWall != null) _arenaWall.SetActive(false);
            if (_respawnPlatform == null) return;

            _respawnPlatform.ResetImmediately();
            _respawnPlatform.gameObject.SetActive(true);
        }
        #endregion

        #region Private Methods
        // 보스 바닥에 올라서면 아레나 벽을 켜고 리스폰 자리 발판을 내린다. 이미 내려가 있으면 그대로다.
        // [이벤트] TriggerPlate.OnPlayerEntered (Begin 에서 구독)
        private void HandleArenaEntered()
        {
            if (_arenaWall != null) _arenaWall.SetActive(true);
            if (_respawnPlatform != null) _respawnPlatform.Activate();
        }

        // 내려가는 쪽으로 움직여 도착해 멈춘 발판만 끈다. 내려가는 중이거나 이미 꺼져 있으면 그대로다.
        private static void HideIfLowered(StateMover platform)
        {
            if ((platform == null) || !platform.gameObject.activeSelf) return;
            if (!platform.IsActivated || platform.IsMoving) return;

            platform.gameObject.SetActive(false);
        }

        private void HandleBossDefeated()
        {
            IsCleared = true;
            if (_exitDoor != null) _exitDoor.SetActive(true);

            OnProgressChanged?.Invoke();
        }
        #endregion
    }
}
