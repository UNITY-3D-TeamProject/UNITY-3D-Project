using JetBrains.Annotations;
using System;
using UnityEngine;
using Attribute.Core;
using Mediator.SubMediators;

namespace Core.Stage
{
    public class StageManager : MonoBehaviour
    {
        private GameManager _gameManager;
        private CombatMediator _playerCombatMediator;

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

        private StageBase _currentStageBaseInstance;

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

            IsSuccess = false;
            IsRunning = true;

            _currentStageBaseInstance.StartStage();
            OnStageStarted?.Invoke();
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

        private void OnEnable()
        {
            _gameManager = GameManager.Instance;

            if (_gameManager == null)
            {
                return;
            }

            _gameManager.OnPlayerSpawned += HandlePlayerSpawned;

            // 이미 생성된 플레이어가 있다면 바로 연결한다.
            HandlePlayerSpawned(_gameManager.CurrentPlayerState);
        }

        private void OnDisable()
        {
            if (_gameManager != null)
            {
                _gameManager.OnPlayerSpawned -= HandlePlayerSpawned;
            }

            UnbindPlayerDeath();
            _gameManager = null;
        }

        // 새로 생성된 플레이어의 사망이벤트를 StageManager에 연결하는 함수
        private void HandlePlayerSpawned(AttributeSet attributeSet)
        {
            // 재스폰된 경우 이전 플레이어의 구독부터 해제한다.
            UnbindPlayerDeath();

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
        }

    }
}

// 플레이어가 씬에 입장하면 => 스테이지 매니저한테 알려서 이 스테이지 들어갔다고
// 하는걸로
