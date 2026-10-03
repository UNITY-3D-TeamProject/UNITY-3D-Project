using JetBrains.Annotations;
using System;
using UnityEngine;

namespace Core.Stage
{
    public class StageManager : MonoBehaviour
    {
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

        void StartStage()
        {
            if (_currentStageBaseInstance == null)
            {
                Debug.LogError("실행할 스테이지가 설정되지 않았습니다.");
                return;
            }

            IsRunning = true;

            _currentStageBaseInstance.StartStage();
            OnStageStarted?.Invoke();
        }

        void EndStage()
        {
            if (_currentStageBaseInstance == null)
                return;

            _currentStageBaseInstance.EndStage();
            IsRunning = false;
            _currentStageBaseInstance = null;
        }

    }
}

// 플레이어가 씬에 입장하면 => 스테이지 매니저한테 알려서 이 스테이지 들어갔다고
// 하는걸로
