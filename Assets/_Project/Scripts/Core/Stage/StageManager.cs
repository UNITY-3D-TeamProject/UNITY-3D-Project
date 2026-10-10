using Attribute.Effect;
using Facade.Player;
using JetBrains.Annotations;
using Map.Gimmicks;
using System;
using UnityEngine;

namespace Core.Stage
{
    public class StageManager : MonoBehaviour
    {

        [Header("Checkpoints")]
        [Tooltip("진행 경로 순서로 체크포인트를 연결합니다. 씬 전환으로 스테이지에 진입하면 " +
            "Round Checkpoint Indices의 첫 값이 가리키는 체크포인트의 RespawnPoint에 최초 스폰합니다. " +
            "이 배열의 0번을 최초 스폰 위치로 사용하려면 해당 첫 값을 0으로 설정하세요.")]
        [SerializeField] private CheckpointTrigger[] _checkpoints;

        [Header("Round Checkpoints")]
        [Tooltip("배열 순서는 라운드 순서이며, 각 값은 Checkpoints 배열의 인덱스입니다.")]
        [SerializeField] private int[] _roundCheckpointIndices;

        [Header("Fall")]
        [SerializeField] private FallZoneTrigger[] _fallZones;
        [Tooltip("낙하 구역에 닿았을 때 플레이어에게 적용할 Effect (예: CurrentHp 감소)")]
        [SerializeField] private SOAttributeEffect _fallEffect;

        // 현재 라운드. 0부터 시작하며 -1은 아직 시작하지 않은 상태.
        private int _currentRoundIndex = -1;

        // 도달한 체크포인트 중 가장 높은 인덱스.
        // 사망으로 라운드를 재시작하면 해당 라운드 시작점까지 되돌린다.
        private int _currentCheckpointIndex = -1;

        private const string CURRENT_HP_KEY = "CurrentHp";

        private Transform _playerTransform;
        private PlayerFacade _playerFacade;
        private CharacterController _playerController;

        private Vector3 _initialSpawnPosition;
        private Quaternion _initialSpawnRotation;

        // 첫 라운드 스냅샷까지 준비됐는지 확인한다.
        private bool _isInitialSnapshotReady;

        // 사망 복구 중인지 확인한다. 사망 통지 중복 처리와 복구 중 낙하·체크포인트 판정을 막는다.
        private bool _isRecovering;

        private GameManager _gameManager;

        private StageBase _currentStageBaseInstance;

        public int CurrentRoundIndex => _currentRoundIndex;
        public int CurrentCheckpointIndex => _currentCheckpointIndex;

        // 현재 스테이지의 clear 완성도 => 각 스테이지의 clear 완성도와 같은 값을 공유
        public int CurrentClearProgress { get; private set; }

        // 1.튜토리얼,2.인스타,3.파일,4.보안,5.라이브앱
        public bool IsSuccess { get; private set; }

        // 현재 스테이지
        public EStageType CurrentStage { get; private set; }

        // 어디 스테이지까지 실행한건지

        // 지금 스테이지 중인건지
        public bool IsRunning { get; private set; }

        // 이벤트
        public event Action OnStageStarted;
        public event Action OnStageCompleted;
        public event Action OnAllStagesCompleted;
        public event Action OnStageFailed;

        // 사망 후 라운드 체크포인트에서 플레이어를 다시 스폰해 달라는 요청. PlayerSpawner가 구독한다.
        public event Action OnPlayerRespawnRequested;

        private void OnEnable()
        {
            _gameManager = GameManager.Instance;

            if (_gameManager == null)
            {
                return;
            }

            BindCheckpoints();
            BindFallZones();
            _gameManager.OnPlayerSpawned += HandlePlayerSpawned;

            // 이미 생성된 플레이어가 있다면 바로 연결한다.
            BindPlayer();
        }

        private void OnDisable()
        {
            UnbindCheckpoints();
            UnbindFallZones();

            if (_gameManager != null)
            {
                _gameManager.OnPlayerSpawned -= HandlePlayerSpawned;
            }

            UnbindPlayer();
            _playerController = null;
            _gameManager = null;
        }

        // [호출] GameManager.HandleSceneLoaded에서 씬 로드 직후 호출된다.
        public void TakeCurrentStage(EStageType currentStage)
        {
            // 준범이가 만드는 씬 로더 같은 곳에서 현재 씬이 어딘지 가져온다.
            CurrentStage = currentStage;

            _currentStageBaseInstance = FindAnyObjectByType<StageBase>();

            if (_currentStageBaseInstance == null)
            {
                Debug.LogError($"{CurrentStage} 씬에서 StageBase 컴포넌트를 찾을 수 없습니다.");
                return;
            }
            if (_currentStageBaseInstance.StageType != CurrentStage)
            {
                Debug.LogError(
                    $"전달받은 스테이지는 {CurrentStage}이지만 " +
                    $"씬에서 찾은 스테이지는 {_currentStageBaseInstance.StageType}입니다."
                );
                _currentStageBaseInstance = null;
            }

        }

        // [호출] GameManager.CompletePlayerSpawn에서 플레이어 등록과 첫 라운드 저장이 끝난 뒤 호출된다.
        public void StartStage()
        {
            if (IsRunning)
            {
                return;
            }

            if (_currentStageBaseInstance == null)
            {
                Debug.LogError("현재 스테이지가 설정되지 않았습니다.", this);
                return;
            }

            if (!_isInitialSnapshotReady)
            {
                Debug.LogError("첫 라운드 스냅샷이 준비되지 않았습니다.", this);
                return;
            }

            IsSuccess = false;
            IsRunning = true;

            _currentStageBaseInstance.StartStage();
            OnStageStarted?.Invoke();
        }

        // 현재 라운드에 지정된 체크포인트를 스폰 위치로 사용한다.
        // 최초 입장에서는 첫 라운드, 사망 후 재스폰에서는 현재 라운드의 체크포인트를 반환한다.
        // [호출] PlayerSpawner.SpawnPlayer가 최초 스폰·재스폰 위치를 얻을 때 호출한다. InitializeFirstRound도 위치 검증용으로 호출한다.
        public Transform GetSpawnPoint()
        {
            if (_checkpoints == null || _checkpoints.Length == 0)
            {
                Debug.LogError("체크포인트 목록이 없습니다.", this);
                return null;
            }

            if (_roundCheckpointIndices == null ||
                _roundCheckpointIndices.Length == 0)
            {
                Debug.LogError("라운드 시작 체크포인트 설정이 없습니다.", this);
                return null;
            }

            int roundIndex = _isInitialSnapshotReady ? _currentRoundIndex : 0;

            if (roundIndex < 0 || roundIndex >= _roundCheckpointIndices.Length)
            {
                Debug.LogError("현재 라운드 인덱스가 올바르지 않습니다.", this);
                return null;
            }

            int checkpointIndex = _roundCheckpointIndices[roundIndex];

            if (checkpointIndex < 0 || checkpointIndex >= _checkpoints.Length)
            {
                Debug.LogError("라운드 체크포인트 인덱스가 올바르지 않습니다.", this);
                return null;
            }

            CheckpointTrigger checkpoint = _checkpoints[checkpointIndex];
            if (checkpoint == null)
            {
                Debug.LogError("라운드 CheckpointTrigger가 연결되지 않았습니다.", this);
                return null;
            }

            Transform spawnPoint = checkpoint.RespawnPoint;

            if (spawnPoint == null)
            {
                Debug.LogError("라운드 체크포인트의 복귀 위치가 없습니다.", this);
                return null;
            }

            return spawnPoint;
        }

        // 최초 스폰의 능력치 적용이 끝난 뒤 호출한다.
        // 같은 씬의 사망 복구에서는 호출하지 않는다.
        // [호출] GameManager.CompletePlayerSpawn에서 최초 능력치 설정 직후 호출된다.
        public void InitializeFirstRound(PlayerState playerState)
        {
            if (_isInitialSnapshotReady)
            {
                return;
            }

            if (playerState == null || playerState.CurrentFacade == null)
            {
                Debug.LogError("첫 라운드를 준비할 플레이어가 없습니다.", this);
                return;
            }

            if (_currentStageBaseInstance == null)
            {
                Debug.LogError("첫 라운드를 준비할 스테이지가 없습니다.", this);
                return;
            }

            if (GetSpawnPoint() == null)
            {
                return;
            }

            // 새로운 스테이지 진입이므로 이전 스테이지의 라운드 기록을 비운다.
            // 씬 전환용 능력치 저장값은 건드리지 않는다.
            playerState.ClearRoundSnapshots();
            playerState.SaveRoundSnapshot(0);

            // HP 초기화 실패 등으로 저장되지 않았다면 시작하지 않는다.
            if (!playerState.HasRoundSnapshot(0))
            {
                Debug.LogError("첫 라운드 스냅샷 저장에 실패했습니다.", this);
                return;
            }

            _currentRoundIndex = 0;
            _currentCheckpointIndex = _roundCheckpointIndices[0];
            _isInitialSnapshotReady = true;
        }

        // GameManager가 플레이어 생성 완료를 알릴 때 호출된다. (HUD 등 다른 구독자와 시그니처를 맞춘 매개변수라 사용하지 않는다.)
        // [이벤트] GameManager.OnPlayerSpawned가 발생하면 호출된다. (OnEnable에서 구독)
        private void HandlePlayerSpawned(PlayerFacade playerFacade)
        {
            BindPlayer();
        }

        // 현재 플레이어의 PlayerFacade를 StageManager에 연결하는 함수
        // [호출] OnEnable(이미 플레이어가 있을 때)과 HandlePlayerSpawned(새 플레이어 등록 시)에서 호출된다.
        private void BindPlayer()
        {
            // 재스폰된 경우 이전 플레이어의 구독부터 해제한다.
            UnbindPlayer();

            _playerFacade = _gameManager != null ? _gameManager.CurrentPlayerFacade : null;

            if (_playerFacade == null)
            {
                _playerController = null;
                return;
            }

            _playerController = _playerFacade.transform.root
                .GetComponentInChildren<CharacterController>();

            _playerFacade.OnDeath += HandlePlayerDeath;

            // 사망 후 재스폰이면 새 플레이어에게 현재 라운드 최초 체력·배터리를 복원한다.
            if (_isRecovering)
            {
                _gameManager.playerState.RestoreRoundSnapshot(_currentRoundIndex);
                _isRecovering = false;
            }
        }

        private void BindCheckpoints()
        {
            if (_checkpoints == null)
            {
                return;
            }

            foreach (CheckpointTrigger checkpoint in _checkpoints)
            {
                if (checkpoint == null)
                {
                    continue;
                }
                // 같은 이벤트의 중복 구독을 방지한다.
                checkpoint.OnPlayerEntered -= HandleCheckpointEntered;
                checkpoint.OnPlayerEntered += HandleCheckpointEntered;
            }
        }

        private void UnbindCheckpoints()
        {
            if (_checkpoints == null)
            {
                return;
            }

            foreach (CheckpointTrigger checkpoint in _checkpoints)
            {
                if (checkpoint != null)
                {
                    checkpoint.OnPlayerEntered -= HandleCheckpointEntered;
                }
            }
        }

        // [이벤트] CheckpointTrigger.OnPlayerEntered가 발생하면 호출된다. (BindCheckpoints에서 구독)
        private void HandleCheckpointEntered(CheckpointTrigger checkpoint, Collider other)
        {
            if (!IsLivingPlayerCollider(other))
            {
                return;
            }

            if (checkpoint == null || _checkpoints == null)
            {
                return;
            }
            // 이벤트를 보낸 체크포인트가 목록의 몇 번째인지 찾는다.
            int checkpointIndex = Array.IndexOf(_checkpoints, checkpoint);
            if (checkpointIndex < 0)
            {
                return;
            }

            // 이전 지점에 다시 닿아도 일반 진행도는 뒤로 가지 않는다.
            _currentCheckpointIndex = Mathf.Max(_currentCheckpointIndex, checkpointIndex);

            // 일반 진행도와 별도로 실제 도달한 다음 라운드 시작점을 검사한다.
            int nextRoundIndex = _currentRoundIndex + 1;
            if ((_roundCheckpointIndices == null) ||
                (nextRoundIndex >= _roundCheckpointIndices.Length) ||
                (_roundCheckpointIndices[nextRoundIndex] != checkpointIndex))
            {
                return;
            }

            PlayerState playerState = _gameManager.playerState;
            // PlayerState가 이미 저장된 라운드의 최초 값을 보호한다.
            playerState.SaveRoundSnapshot(nextRoundIndex);

            // 저장 실패 시 라운드를 갱신하지 않는다. 재접촉 시 최초 값은 유지된다.
            // 저장에 성공했을 때만 현재 라운드를 변경한다.
            if (playerState.HasRoundSnapshot(nextRoundIndex))
            {
                _currentRoundIndex = nextRoundIndex;
            }
        }

        private void BindFallZones()
        {
            if (_fallZones == null)
            {
                return;
            }

            foreach (FallZoneTrigger fallZone in _fallZones)
            {
                if (fallZone == null)
                {
                    continue;
                }
                // 같은 이벤트의 중복 구독을 방지한다.
                fallZone.OnPlayerEntered -= HandleFallZoneEntered;
                fallZone.OnPlayerEntered += HandleFallZoneEntered;
            }
        }

        private void UnbindFallZones()
        {
            if (_fallZones == null)
            {
                return;
            }

            foreach (FallZoneTrigger fallZone in _fallZones)
            {
                if (fallZone != null)
                {
                    fallZone.OnPlayerEntered -= HandleFallZoneEntered;
                }
            }
        }

        // 낙하 데미지를 주고, 생존하면 도달한 체크포인트 중 가장 높은 곳으로 이동시킨다.
        // 데미지로 사망하면 사망 이벤트가 라운드 복구를 처리하므로 여기서는 이동하지 않는다.
        // [이벤트] FallZoneTrigger.OnPlayerEntered가 발생하면 호출된다. (BindFallZones에서 구독)
        private void HandleFallZoneEntered(FallZoneTrigger fallZone, Collider other)
        {
            if (!IsLivingPlayerCollider(other))
            {
                return;
            }

            // 데미지는 Effect SO로 적용한다. 값이 줄면 PlayerFacade.OnHit이 발생한다.
            if (_fallEffect == null || _playerFacade.EffectTarget == null)
            {
                Debug.LogError("낙하 Effect 또는 플레이어 EffectTarget이 없습니다.", this);
            }
            else
            {
                _fallEffect.Apply(_playerFacade.EffectTarget);
            }

            if (_isRecovering)
            {
                return;
            }

            if (_checkpoints == null ||
                _currentCheckpointIndex < 0 ||
                _currentCheckpointIndex >= _checkpoints.Length ||
                _checkpoints[_currentCheckpointIndex] == null)
            {
                Debug.LogError("낙하 후 복귀할 체크포인트가 없습니다.", this);
                return;
            }

            MovePlayerTo(_checkpoints[_currentCheckpointIndex].RespawnPoint);
        }

        // 플레이 중인 현재 플레이어의 살아있는 몸체에 접촉한 것인지 확인한다.
        // [호출] HandleCheckpointEntered, HandleFallZoneEntered에서 처리 대상인지 확인할 때 호출한다.
        private bool IsLivingPlayerCollider(Collider other)
        {
            // 플레이 중이고 첫 라운드 준비가 끝났는지 확인한다.
            if ((!isActiveAndEnabled || !IsRunning || !_isInitialSnapshotReady || _isRecovering) ||
                (_gameManager == null) ||
                (_gameManager.CurrentState != GameManager.GameState.Playing))
            {
                return false;
            }
            // 접촉한 Collider가 현재 플레이어에 속하는지 확인한다.
            if ((_playerFacade == null || other == null) ||
                (_gameManager.CurrentPlayerFacade != _playerFacade) ||
                (other.transform.root != _playerFacade.transform.root))
            {
                return false;
            }
            // 사망 상태에서는 판정하지 않는다.
            return _playerFacade.HasAttribute(CURRENT_HP_KEY) &&
                (_playerFacade.GetAttribute(CURRENT_HP_KEY) > 0f);
        }

        // 기존 플레이어를 destination 위치와 회전으로 이동시킨다.
        // [호출] HandleFallZoneEntered에서 생존 낙하 시 체크포인트로 되돌릴 때 호출한다.
        private void MovePlayerTo(Transform destination)
        {
            if (destination == null || _playerController == null)
            {
                Debug.LogError("플레이어를 이동시킬 위치 또는 CharacterController가 없습니다.", this);
                return;
            }

            // CharacterController가 켜져 있으면 위치를 직접 바꿔도 되돌아갈 수 있어 잠시 끈다.
            _playerController.enabled = false;
            _playerController.transform.SetPositionAndRotation(destination.position, destination.rotation);
            _playerController.enabled = true;
        }

        // [호출] OnDisable과 BindPlayer(새 플레이어로 교체 전)에서 호출된다.
        private void UnbindPlayer()
        {
            if (_playerFacade != null)
            {
                _playerFacade.OnDeath -= HandlePlayerDeath;
            }

            _playerFacade = null;
        }

        // 플레이어가 죽었다는 이벤트를 받았을 때, 현재 라운드의 체크포인트에서 다시 스폰을 요청하는 함수
        // 사망한 플레이어는 파괴되므로 이동이 아니라 재스폰한다. 스폰 위치는 GetSpawnPoint가 알려준다.
        // [이벤트] PlayerFacade.OnDeath(플레이어 사망)가 발생하면 호출된다. (BindPlayer에서 구독)
        // [이후] OnPlayerRespawnRequested 발생 -> PlayerSpawner.HandleRespawnRequested -> 다음 프레임 재스폰.
        private void HandlePlayerDeath()
        {
            // 진행 중인 스테이지에서만 처리한다.
            // 낙하 사망과 전투 사망이 겹치거나 사망 이벤트가 반복되어도 복구는 한 번만 처리된다.
            if (!IsRunning || _isRecovering)
            {
                return;
            }

            if (_roundCheckpointIndices == null ||
                _currentRoundIndex < 0 ||
                _currentRoundIndex >= _roundCheckpointIndices.Length)
            {
                Debug.LogError("복구할 라운드 체크포인트가 없습니다.", this);
                return;
            }

            _isRecovering = true;

            // 사망하면 일반 체크포인트 진행도도 라운드 시작점으로 되돌린다.
            _currentCheckpointIndex = _roundCheckpointIndices[_currentRoundIndex];

            _gameManager.PrepareSpawn(GameManager.EPlayerSpawnReason.RoundRecovery);
            OnPlayerRespawnRequested?.Invoke();
        }

    }
}

// 플레이어가 씬에 입장하면 => 스테이지 매니저한테 알려서 이 스테이지 들어갔다고
// 하는걸로
