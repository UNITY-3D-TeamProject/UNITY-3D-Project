using Attribute.Core;
using Core.Scanning;
using Core.Stage;
using Facade.Player;
using SceneTransition;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Core
{
    public class GameManager : Singleton<GameManager>
    {
        // 게임 상태 관리
        public enum GameState
        {
            Title,
            Playing,
            Pause,
        }

        // 지금은 실험을 위해 CurrentState의 초기값을 Playing으로 설정
        public GameState CurrentState { get; private set; } = GameState.Playing;

        // 플레이어 스폰 이유
        public enum EPlayerSpawnReason
        { 
            NewGame, // 새 게임 
            SceneTransition, // 씬 변경될 때
            RoundRecovery, // 사망 후 라운드 체크포인트에서 다시 스폰할 때
            StageRestart, // 첫 라운드 사망 후 씬을 다시 불러와 스폰할 때
        }

        // 플레이어 스폰 이유는 초기값은 당연히 NewGame으로 설정 
        public EPlayerSpawnReason SpawnReason { get; private set; }
            = EPlayerSpawnReason.NewGame;
        public bool IsContinueSpawn { get; private set; }


        // 저장할 능력치 이름 목록을 확인하기 위한 플레이어 SO 원본 (playerState를 초기화 할 때 넣어줄 매개변수 값)
        [SerializeField] private SOAttributeData _playerSOAttributeData;

        // 게임중에 바뀔 플레이어 데이터에 관련해서 만들 스크립트 변수
        public PlayerState playerState { get; private set; }

        // 현재 플레이어의 PlayerFacade. 플레이어가 없으면 null 반환
        public PlayerFacade CurrentPlayerFacade => playerState?.CurrentFacade;

        // playerState가 new를 통해 생성되면 그 후 부터 true
        public bool HasPlayerState => playerState != null;
        // playerState가 저장된 어트리뷰트값을 가지고 있는지
        public bool HasSavedPlayerAttributes => playerState?.HasSavedAttributes ?? false;

        // 플레이어를 스폰할 때 이벤트 
        public event Action<PlayerFacade> OnPlayerSpawned;

        private StageManager _stageManager;

        public StageManager CurrentStageManager => _stageManager;

        // 어떤 스테이지에서 넘어왔는지를 체크해서 그에 따라 다르게 스폰하게 저장할 값
        public EStageType? SpawnStage { get; private set; }

        // 게임 시간을 멈춰 달라고 요청한 쪽들. 하나라도 남아 있으면 게임 시간이 멈춘다.
        private readonly HashSet<UnityEngine.Object> _timePauseRequesters = new();

        public override void Awake()
        {
            base.Awake();
            if (Instance != this)
            {
                return;
            }
            // 인스펙터에서 받은 SO를 인자로 넣어줘서 playerState를 만들어준다.
            playerState = new PlayerState(_playerSOAttributeData);
            // 커서는 여기서 풀어 둔다. 맵 씬에 들어오면 HandleSceneLoaded가 잠근다.
            // 에디터에서는 이전 Play의 커서 잠금이 남아 있어서, 풀지 않으면 메인 메뉴에서 커서가 안 보일 수 있다.
            SetCursorLocked(false);
        }

        private void OnEnable()
        {
            if (Instance != this)
            {
                return;
            }

            // 씬 로딩 완료 시점을 알리는 이벤트 구독 => 새 씬에 배치된 StageManager 탐색 => 플레이어 스폰...
            SceneManager.sceneLoaded += HandleSceneLoaded;
            
            // 씬 전환 직전 시점을 알리는 이벤트 구독 => 기존 플레이어가 파괴되기전 현재 
            // 능력치를 임시 저장하고 스폰 사유를 Scene Transition으로 설정
            SceneLoader.OnBeforeSceneChange += HandleBeforeSceneChange;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            SceneLoader.OnBeforeSceneChange -= HandleBeforeSceneChange;
        }

        // 새 게임 시작
        // 맨처음 첫 게임 시작
        public void StartGame()
        {
            // NewGame으로 설정한다.
            PrepareSpawn(EPlayerSpawnReason.NewGame);
            CurrentState = GameState.Playing;
        }

        public void PauseGame()
        {
            CurrentState = GameState.Pause;
            ApplyGameTime();
            SetCursorLocked(false);
        }

        // 설정창을 닫으면 부른다. 대화·연출 등이 아직 시간 정지를 요청 중이면 시간은 멈춘 채로 둔다.
        public void ResumeGame()
        {
            CurrentState = GameState.Playing;
            ApplyGameTime();
            SetCursorLocked(true);
        }

        // 설정창의 "메인 화면으로" 버튼. 진행 순번과 현재 체력·배터리를 저장하고 런타임 상태를 비운다.
        public void ReturnToMainMenu()
        {
            if (SceneLoader.IsLoading) return;

            playerState?.SaveProgressAttributes();
            if (!SceneLoader.TryLoadMainMenu()) return;

            _timePauseRequesters.Clear();
            CurrentState = GameState.Playing;
            ApplyGameTime();
            SetCursorLocked(false);

            SpawnReason = EPlayerSpawnReason.NewGame;
            SpawnStage = null;
            _stageManager = null;
            playerState?.ResetSession();
        }

        // 설정창의 "게임 종료" 버튼. 에디터에서는 Play를 멈춘다. 씬을 불러오는 중이면 무시한다.
        public void QuitGame()
        {
            if (SceneLoader.IsLoading) return;

            playerState?.SaveProgressAttributes();
            SceneLoader.SaveCurrentProgress();

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        // 대화·펫 가이드·연출 동안 requester 이름으로 게임 시간과 3D 효과음을 멈춰 달라고 요청한다. BGM·UI 소리는 계속 들린다.
        // 상태는 Playing 그대로라 그동안에도 설정창을 열 수 있고, 커서는 잠근 채로 둔다. 같은 requester가 여러 번 불러도 요청은 하나다.
        public void PauseGameTime(UnityEngine.Object requester)
        {
            if (requester == null) return;

            _timePauseRequesters.Add(requester);
            ApplyGameTime();
        }

        // requester의 시간 정지 요청을 푼다. 다른 요청이 남아 있거나 설정창으로 멈춘 중(Pause)이면 시간은 멈춘 채로 둔다.
        public void ResumeGameTime(UnityEngine.Object requester)
        {
            if (!_timePauseRequesters.Remove(requester)) return;

            ApplyGameTime();
        }

        // 설정창으로 멈춘 중(Pause)이거나 시간 정지 요청이 하나라도 있으면 게임 시간과 3D 효과음을 멈추고, 아니면 돌린다.
        private void ApplyGameTime()
        {
            bool shouldPause = (CurrentState == GameState.Pause) || (_timePauseRequesters.Count > 0);

            Time.timeScale = shouldPause ? 0f : 1f;
            // 3D 효과음만 멈춘다. BGM·UI 소스는 ignoreListenerPause라 계속 들린다. (AudioManager)
            AudioListener.pause = shouldPause;
        }


        // 사망 팝업은 게임을 멈추지 않고 커서만 풀었다가, 다시하기로 리스폰하면 다시 잠근다. (UIManager)
        public void SetCursorLocked(bool isLocked)
        {
            Cursor.lockState = isLocked
                ? CursorLockMode.Locked
                : CursorLockMode.None;

            Cursor.visible = !isLocked;
            Debug.Log(isLocked ? "[GameManager] 마우스 커서 화면 중앙에 고정됨" : "[GameManager] 마우스 커서 제한 해제됨", this);
        }

        // 새 게임 또는 씬 전환으로 플레이어를 생성하기 전에 호출한다.
        // [호출] 새 게임은 StartGame, 씬 전환은 HandleBeforeSceneChange, 사망 복구는 StageManager.HandlePlayerDeath에서 호출한다.
        public void PrepareSpawn(EPlayerSpawnReason reason)
        {
            // 씬 이동일 때만 현재 능력치를 저장한다.
            if (reason == EPlayerSpawnReason.SceneTransition)
            {
                playerState.SaveCurrentAttributes();
            }

            SpawnReason = reason;
        }


        // [호출] PlayerSpawner.SpawnPlayer에서 플레이어 생성과 초기 Effect 적용 직후 호출된다.
        public void CompletePlayerSpawn(PlayerFacade spawnedPlayerFacade)
        {
            if (spawnedPlayerFacade == null)
            {
                Debug.LogError("등록할 플레이어의 PlayerFacade가 없습니다.", this);
                return;
            }

            if (!HasPlayerState)
            {
                Debug.LogError("PlayerState가 초기화되지 않았습니다.", this);
                return;
            }

            playerState.SetCurrentPlayer(spawnedPlayerFacade);

            // 벽 뒤 아이템 스캔 실루엣이 플레이어 몸 위에 그려지지 않도록 플레이어 렌더러에 스텐실용 렌더링 레이어를 더한다.
            PlayerStencilRendererFeature.MarkRenderers(spawnedPlayerFacade.gameObject);

            // 스포너에서 초기 Effect 적용은 이미 끝난 상태다.
            // 씬 이동이라면 여기서 이전 씬의 능력치를 복원한다.
            // 씬 재시작이면 씬 입장 때 저장해 둔 능력치를 다시 복원한다.
            bool shouldContinue = SceneLoader.ConsumeContinueRequest();
            IsContinueSpawn = shouldContinue;
            bool shouldRestore =
                (SpawnReason == EPlayerSpawnReason.SceneTransition) ||
                (SpawnReason == EPlayerSpawnReason.StageRestart);

            if (shouldContinue)
            {
                playerState.ClearSavedAttributes();
                playerState.RestoreProgressAttributes();
            }
            else if (shouldRestore)
            {
                playerState.RestoreSavedAttributes();
            }
            else
            {
                playerState.ClearSavedAttributes();
            }

            // 최종 능력치가 결정된 다음 첫 라운드 스냅샷을 저장한다.
            if (_stageManager != null)
            {
                _stageManager.InitializeFirstRound(playerState);
            }

            // 초기 Effect와 이어하기 복원이 끝난 최종 체력·배터리를 저장한다.
            if (SpawnReason != EPlayerSpawnReason.RoundRecovery)
            {
                playerState.SaveProgressAttributes();
            }

            // HUD 연결과 StageManager의 기존 사망 이벤트 구독을 실행한다.
            OnPlayerSpawned?.Invoke(CurrentPlayerFacade);

            // 스냅샷 저장과 이벤트 연결이 끝난 뒤 시작한다.
            if (_stageManager != null)
            {
                _stageManager.StartStage();
            }
        }

        // [호출] PlayerSpawner.SpawnPlayer에서 로비 스폰을 마친 뒤 호출된다.
        public void ClearSpawnStage()
        {
            SpawnStage = null;
        }

        // Scene => 방금 로드된 씬의 정보가 들어가 있다. scene.name으로 씬 이름 확인 가능
        // LoadSceneMode => 씬을 어떻게 로드 했는지에 대한 enum 타입 => Single(새 씬만), Additive(기존 씬 유지)
        // [이벤트] SceneManager.sceneLoaded(씬 로드 완료)가 발생하면 호출된다. (OnEnable에서 구독)
        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // 로딩씬은 이전 씬 위에 Additive로 겹쳐 열리므로 무시한다. (이때 찾으면 이전 씬의 StageManager가 잡힌다)
            if (mode == LoadSceneMode.Additive)
            {
                return;
            }

            // 이전 씬과 함께 파괴된 쪽의 시간 정지 요청이 남아 새 씬이 멈춘 채 시작하지 않도록 지운다.
            _timePauseRequesters.RemoveWhere(requester => requester == null);
            ApplyGameTime();

            _stageManager = FindAnyObjectByType<StageManager>();

            if (_stageManager == null)
            {
                return;
            }

            // 메인 메뉴에서 풀어 둔 커서를 맵 씬에 들어오면 다시 잠근다.
            SetCursorLocked(true);

            StageBase stage = FindAnyObjectByType<StageBase>();

            if (stage == null)
            {
                Debug.LogError("현재 씬에 StageBase가 없습니다.", this);
                return;
            }

            _stageManager.TakeCurrentStage(stage.StageType);
            //_stageManager.StartStage();
        }

        // SceneLoader의 일반 씬 이동 직전에 현재 능력치를 저장한다.
        // [이벤트] SceneLoader.OnBeforeSceneChange(씬 이동 직전)가 발생하면 호출된다. (OnEnable에서 구독)
        private void HandleBeforeSceneChange(ESceneType previous, ESceneType destination)
        {
            if(CurrentPlayerFacade!=null)
                PrepareSpawn(EPlayerSpawnReason.SceneTransition);

            // 로비로 돌아올 때만 "어디서 왔는지" 기록
            if(destination == ESceneType.Lobby)
            {
                SpawnStage = ToStageType(previous); // ESceneType -> EStageType? 변환
            }
            else
            {
                SpawnStage = null;
            }



            // 여기서 무슨 씬으로 바꿀건지도 가져온다.
            // SpawnStage = destinationStage.Value;
        }

        private EStageType? ToStageType(ESceneType sceneType)
        {
            return sceneType switch
            {
                ESceneType.Tutorial => EStageType.Tutorial,
                ESceneType.Feed => EStageType.FeedApp,
                ESceneType.File => EStageType.FileApp,
                ESceneType.Security => EStageType.SecurityApp,
                ESceneType.Live => EStageType.LiveApp,
                _ => null,
            };
        }





        //private void HandleBeforeSceneChange(EStageType? destinationStage)
        //{
        //    playerState.SaveCurrentAttributes();

        //    if (destinationStage.HasValue)
        //    {
        //        SpawnStage = destinationStage.Value;
        //    }
        //}
    }
}
