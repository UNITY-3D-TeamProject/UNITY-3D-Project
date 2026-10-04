using Attribute.Core;
using Attribute.Effect;
using Core;
using Core.Stage;
using System;
using System.Runtime.CompilerServices;
using UnityEngine;

public class PlayerSpawner : MonoBehaviour
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





    private void Start()
    {
        SpawnPlayer();
    }

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
        Transform spawnPoint = _isLobbySpawner
            ? GetLobbySpawnPoint(gameManager.SpawnStage)
            : transform;

        GameObject player = Instantiate(
            _playerPrefab,
            spawnPoint.position,
            spawnPoint.rotation);

        // 데이터 요청해서 가져와서 플레이어의 SO값(딕셔너리)에 넣어주기

        // 방금 생성한 플레이어 인스턴스의 어트리뷰트셋 가져오기
        // AttributeSet은 Dictionary <string,AttributeData> 형식
        AttributeSet _spawnedPlayerattributeSet = player.GetComponent<AttributeSet>();


        if (_spawnedPlayerattributeSet == null)
        {
            Debug.LogError("Player에 AttributeSet이 없습니다.", player);
            Destroy(player);
            return;
        }

        // 새 게임과 부활에서는 저장값 대신 Effect SO의 초기값을 사용한다.
        bool shouldInitialize =
            gameManager.SpawnReason == GameManager.EPlayerSpawnReason.NewGame ||
            gameManager.SpawnReason == GameManager.EPlayerSpawnReason.Respawn;

        if (shouldInitialize && _playerEffects != null)
        {
            foreach (SOAttributeEffect effect in _playerEffects)
            {
                if (effect == null)
                {
                    continue;
                }

                // Effect SO에 설정된 Float 값을 대상 어트리뷰트에 적용한다.
                effect.Apply(_spawnedPlayerattributeSet);
            }
        }

        // GameManager 하나에만 알린다. UI도 모르고 PlayerState에서도 모른다. (GameManager가 모두 직접 알려준다.)
        gameManager.CompletePlayerSpawn(_spawnedPlayerattributeSet);

        // 로비에서 생성·등록을 완료했을 때만 기록을 비운다.
        if (_isLobbySpawner)
        {
            gameManager.ClearSpawnStage();
        }
    }

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
