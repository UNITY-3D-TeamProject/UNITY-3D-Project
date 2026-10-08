using Attribute.Core;
using JetBrains.Annotations;
using Map.Gimmicks;
using Mediator.SubMediators;
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
        [SerializeField] private BoxCollider[] _fallZones;
        [SerializeField, Min(0f)] private float _fallDamage = 10f;

        // 현재 라운드. 0부터 시작하며 -1은 아직 시작하지 않은 상태.
        private int _currentRoundIndex = -1;

        // 도달한 체크포인트 중 가장 높은 인덱스.
        // 사망으로 라운드를 재시작하면 해당 라운드 시작점까지 되돌린다.
        private int _currentCheckpointIndex = -1;

        private Transform _playerTransform;
        private Collider _playerBodyCollider;
        private AttributeSet _playerAttributeSet;

        private Vector3 _initialSpawnPosition;
        private Quaternion _initialSpawnRotation;

        // 첫 라운드 스냅샷까지 준비됐는지 확인한다.
        private bool _isInitialSnapshotReady;

        private GameManager _gameManager;
        private CombatMediator _playerCombatMediator;

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

        private void OnEnable()
        {
            _gameManager = GameManager.Instance;

            if (_gameManager == null)
            {
                return;
            }

            BindCheckpoints();
            _gameManager.OnPlayerSpawned += HandlePlayerSpawned;

            // 이미 생성된 플레이어가 있다면 바로 연결한다.
            HandlePlayerSpawned(_gameManager.CurrentPlayerState);
        }

        private void OnDisable()
        {
            UnbindCheckpoints();

            if (_gameManager != null)
            {
                _gameManager.OnPlayerSpawned -= HandlePlayerSpawned;
            }

            UnbindPlayerDeath();
            _playerAttributeSet = null;
            _playerBodyCollider = null;
            _gameManager = null;
        }

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

        // 첫 라운드에 지정된 체크포인트를 최초 스폰 위치로 사용한다.
        public Transform GetInitialSpawnPoint()
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

            int checkpointIndex = _roundCheckpointIndices[0];

            if (checkpointIndex < 0 || checkpointIndex >= _checkpoints.Length)
            {
                Debug.LogError("첫 라운드 체크포인트 인덱스가 올바르지 않습니다.", this);
                return null;
            }

            CheckpointTrigger checkpoint = _checkpoints[checkpointIndex];
            if (checkpoint == null)
            {
                Debug.LogError("첫 라운드 CheckpointTrigger가 연결되지 않았습니다.", this);
                return null;
            }

            Transform spawnPoint = checkpoint.RespawnPoint;

            if (spawnPoint == null)
            {
                Debug.LogError("첫 라운드 체크포인트의 복귀 위치가 없습니다.", this);
                return null;
            }

            return spawnPoint;
        }

        // 최초 스폰의 능력치 적용이 끝난 뒤 호출한다.
        // 같은 씬의 사망 복구에서는 호출하지 않는다.
        public void InitializeFirstRound(PlayerState playerState)
        {
            if (_isInitialSnapshotReady)
            {
                return;
            }

            if (playerState == null || playerState.CurrentAttributeSet == null)
            {
                Debug.LogError("첫 라운드를 준비할 플레이어가 없습니다.", this);
                return;
            }

            if (_currentStageBaseInstance == null)
            {
                Debug.LogError("첫 라운드를 준비할 스테이지가 없습니다.", this);
                return;
            }

            if (GetInitialSpawnPoint() == null)
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

        private void EndStage()
        {
            IsRunning = false;

            StageBase stage = _currentStageBaseInstance;
            _currentStageBaseInstance = null;

            if (stage != null)
            {
                stage.EndStage();
            }
        }

        // 새로 생성된 플레이어의 사망이벤트를 StageManager에 연결하는 함수
        private void HandlePlayerSpawned(AttributeSet attributeSet)
        {
            // 재스폰된 경우 이전 플레이어의 구독부터 해제한다.
            UnbindPlayerDeath();

            _playerAttributeSet = attributeSet;
            _playerBodyCollider = attributeSet != null
                ? attributeSet.GetComponentInParent<CharacterController>()
                : null;

            if (attributeSet == null)
            {
                return;
            }

            // AttributeSet과 CombatMediator가 각각 자식에 있으므로
            // 현재 플레이어 루트 아래에서 찾는다.
            _playerCombatMediator = attributeSet.transform.root
                .GetComponentInChildren<CombatMediator>();

            if (_playerCombatMediator == null)
            {
                Debug.LogError("플레이어의 CombatMediator가 없습니다.", this);
                return;
            }

            _playerCombatMediator.OnDeath += HandlePlayerDeath;
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

        private void HandleCheckpointEntered(CheckpointTrigger checkpoint, Collider other)
        {
            // 플레이 중이고 첫 라운드 준비가 끝났는지 확인한다.
            if ((!isActiveAndEnabled || !IsRunning || !_isInitialSnapshotReady) ||
                (_gameManager == null) ||
                (_gameManager.CurrentState != GameManager.GameState.Playing))
            {
                return;
            }
            // 접촉한 Collider가 현재 플레이어의 몸체에 속하는지 확인한다.
            if ((_playerAttributeSet == null || _playerBodyCollider == null || other == null) ||
                (_gameManager.CurrentPlayerState != _playerAttributeSet) ||
                (other.GetComponentInParent<CharacterController>() != _playerBodyCollider))
            {
                return;
            }
            // 사망 상태에서는 진행도를 갱신하지 않는다.
            if ((_playerCombatMediator == null || _playerCombatMediator.IsDead()) ||
                (!_playerAttributeSet.IsValidTarget("CurrentHp")) ||
                (_playerAttributeSet.GetValue("CurrentHp") <= 0f))
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

        private void UnbindPlayerDeath()
        {
            if (_playerCombatMediator != null)
            {
                _playerCombatMediator.OnDeath -= HandlePlayerDeath;
            }

            _playerCombatMediator = null;
        }

        // 플레이어가 죽었다는 이벤트를 받았을 때, 현재 스테이지를 실패로 끝내는 함수
        private void HandlePlayerDeath()
        {
            // 진행 중인 스테이지만 실패 처리한다.
            // 사망 이벤트가 반복되어도 실패는 한 번만 처리된다.
            if (!IsRunning)
            {
                return;
            }

            IsSuccess = false;

            EndStage();
            OnStageFailed?.Invoke();

            // 씬로더에 이벤트 보내기?
        }

    }
}

// 플레이어가 씬에 입장하면 => 스테이지 매니저한테 알려서 이 스테이지 들어갔다고
// 하는걸로
