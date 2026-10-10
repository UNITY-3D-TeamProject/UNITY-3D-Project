using Attribute.Core;
using Attribute.Effect;
using Core;
using Core.Stage;
using Facade.Player;
using System;
using System.Collections;
using System.Runtime.CompilerServices;
using UnityEngine;

public class PlayerSpawner : SpawnerBase
{
    [SerializeField]
    // 플레이어 프리팹
    private GameObject _playerPrefab;

    // PlayerEffects SO 배열
    [SerializeField]
    private SOAttributeEffect[] _playerEffects;


    // 로비 씬에서만 체크한다.
    [SerializeField] private bool _isLobbySpawner;

    // 로비에서 스테이지 타입별 복귀 위치를 연결한다.
    // 타입의 길이 0짜리 빈 배열을 반환한다.
    [SerializeField]
    private SLobbySpawnPoint[] _lobbySpawnPoints;

    // 직접 만든 구조체의 데이터를 Unity가 저장하고 인스펙터에서 편집할 수 있음
    [Serializable]
    private struct SLobbySpawnPoint
    {
        // 어떤 스테이지에서 돌아오는지에 대한 값 
        [SerializeField] private EStageType _stageType;

        // 스테이지에서 로비로 돌아올 위치
        [SerializeField] private Transform _point;

        public EStageType StageType => _stageType;
        public Transform Point => _point;
    }





    // 사망 후 재스폰 요청을 받기 위해 구독한 StageManager
    private StageManager _subscribedStageManager;

    private void Start()
    {
        SpawnPlayer();

        // 스테이지 스포너만 StageManager의 재스폰 요청을 받는다. 위치 판단은 하지 않는다.
        GameManager gameManager = GameManager.Instance;

        if (!_isLobbySpawner && gameManager != null && gameManager.CurrentStageManager != null)
        {
            _subscribedStageManager = gameManager.CurrentStageManager;
            _subscribedStageManager.OnPlayerRespawnRequested += HandleRespawnRequested;
        }
    }

    private void OnDestroy()
    {
        if (_subscribedStageManager != null)
        {
            _subscribedStageManager.OnPlayerRespawnRequested -= HandleRespawnRequested;
        }
    }

    // [이벤트] StageManager.OnPlayerRespawnRequested(사망 후 재스폰 요청)가 발생하면 호출된다. (Start에서 구독)
    private void HandleRespawnRequested()
    {
        StartCoroutine(CoSpawnPlayerNextFrame());
    }

    // 사망한 플레이어는 프레임이 끝날 때 파괴된다.
    // 이전 플레이어가 남아 있는 동안 생성하면 PlayerInput이 입력 장치를 새 플레이어에게 연결하지 못하므로 다음 프레임에 생성한다.
    // [호출] HandleRespawnRequested에서 코루틴으로 시작한다.
    private IEnumerator CoSpawnPlayerNextFrame()
    {
        yield return null;
        SpawnPlayer();
    }

    // [호출] Start(최초 스폰)와 CoSpawnPlayerNextFrame(사망 후 재스폰)에서 호출된다.
    void SpawnPlayer()
    {
        GameManager gameManager = GameManager.Instance;

        if (gameManager == null || !gameManager.HasPlayerState)
        {
            Debug.LogError("GameManager가 초기화되지 않았습니다.", this);
            return;
        }

        if (_playerPrefab == null)
        {
            Debug.LogError("Player 프리팹이 연결되지 않았습니다.", this);
            return;
        }

        // 스테이지에서는 자기 위치, 로비에서는 복귀 위치를 사용한다.
        Transform spawnPoint;

        if (_isLobbySpawner)
        {
            spawnPoint = GetLobbySpawnPoint(gameManager.SpawnStage);
        }
        else
        {
            StageManager stageManager = gameManager.CurrentStageManager;

            if (stageManager == null)
            {
                Debug.LogError("스폰 위치를 결정할 StageManager가 없습니다.", this);
                return;
            }

            spawnPoint = stageManager.GetSpawnPoint();
        }

        if (spawnPoint == null)
        {
            Debug.LogError("플레이어 스폰 위치가 없습니다.", this);
            return;
        }

        if (!TrySpawnWithAttributes(
            _playerPrefab,
            spawnPoint,
            out GameObject player,
            out AttributeSet _spawnedPlayerattributeSet))
        {
            return;
        }

        PlayerFacade playerFacade = player.GetComponentInChildren<PlayerFacade>(true);

        if (playerFacade == null)
        {
            Debug.LogError("Player 프리팹에 PlayerFacade가 없습니다.", player);
            Destroy(player);
            return;
        }

        // TODO: 지연 활성화가 필요하면 프리팹을 비활성 상태로 생성하고 AttributeSet의 Awake 초기화를 분리한다.
        // 활성 프리팹을 Instantiate한 뒤 SetActive(false)하면 Awake/OnEnable은 이미 실행된 상태다.
        // player.SetActive(false);

        // 새 게임에서 최초 생성할 때만 Effect SO의 초기값을 사용한다.
        // 사망 후 라운드 복구 스폰도 새 인스턴스이므로 기본 Effect를 적용한다. (체력·배터리는 StageManager가 이후 복원)
        bool shouldInitialize =
            (gameManager.SpawnReason == GameManager.EPlayerSpawnReason.NewGame) ||
            (gameManager.SpawnReason == GameManager.EPlayerSpawnReason.RoundRecovery);

        if (shouldInitialize)
        {
            ApplySpawnEffects(_spawnedPlayerattributeSet, _playerEffects);
        }

        // player.SetActive(true);

        // GameManager 하나에만 알린다. UI도 모르고 PlayerState에서도 모른다. (GameManager가 모두 직접 알려준다.)
        gameManager.CompletePlayerSpawn(playerFacade);

        // 로비에서 생성·등록을 완료했을 때만 기록을 비운다.
        if (_isLobbySpawner)
        {
            gameManager.ClearSpawnStage();
        }
    }

    // [호출] SpawnPlayer에서 로비 스포너일 때 스폰 위치를 고를 때 호출한다.
    private Transform GetLobbySpawnPoint(EStageType? spawnStage)
    {
        // 최초 로비 진입: 스포너 자신의 위치가 DefaultSpawn이다.
        if (!spawnStage.HasValue)
        {
            return transform;
        }
        if (_lobbySpawnPoints == null)
        {
            return transform;
        }

        foreach (SLobbySpawnPoint entry in _lobbySpawnPoints)
        {
            if (entry.StageType == spawnStage.Value && entry.Point != null)
            {
                return entry.Point;
            }
        }

        Debug.LogWarning(
            $"{spawnStage.Value} 복귀 위치가 없어 기본 위치를 사용합니다.",
            this);

        return transform;
    }
}
