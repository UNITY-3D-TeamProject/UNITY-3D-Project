using System;
using UnityEngine;
using Map.Gimmicks;

namespace Core.Stage.Security
{
    /// <summary>
    /// 2라운드 특수몹 발판. 발판은 순서대로 하나씩 진행한다.
    /// - 지금 발판(아직 못 깬 첫 발판)의 특수몹은 미리 만들어 제자리에 묶어 두고, 플레이어가 발판 구역에 들어오면 움직인다.
    /// - 특수몹을 다 잡으면 꺼 두었던 다음 발판이 나타나고, 그 발판의 특수몹을 만들어 둔다.
    /// - 이 라운드 동안 켜져 있는 발판 위로 위험물이 떨어진다. (FallingHazardSpawner)
    /// SecurityApp 이 필드로 들고, 스테이지 시작 때 Begin, 끝날 때 End, 이 라운드 중 죽으면 ResetUnclearedSteps 를 부른다.
    /// 이 라운드가 되면 StartHazards, 다른 라운드가 되면 StopHazards 를 부른다.
    /// </summary>
    [Serializable]
    public class SpecialMobRound
    {
        #region Nested Types
        [Serializable]
        private struct SPlatformStep
        {
            [Tooltip("발판 위 특수몹 구역")]
            public CombatZone Zone;

            [Tooltip("이 발판을 깨면 나타나는 다음 발판. 스테이지가 시작될 때 꺼 둔다.")]
            public GameObject NextPlatform;
        }
        #endregion

        #region Serialized Fields
        [Tooltip("진행 순서대로 발판마다 특수몹 구역·다음 발판")]
        [SerializeField] private SPlatformStep[] _steps;

        [Tooltip("이 라운드 동안 켜져 있는 발판 위로 위험물을 떨어뜨리는 생성기 (04_Platforms/FallingHazardSpawner)")]
        [SerializeField] private FallingHazardSpawner _hazardSpawner;
        #endregion

        #region Properties
        /// <summary>발판 수.</summary>
        public int StepCount => (_steps != null) ? _steps.Length : 0;

        /// <summary>지금 발판 번호 (1부터, 아직 못 깬 첫 발판). 다 깼으면 StepCount.</summary>
        public int CurrentStepNumber
        {
            get
            {
                int index = FindCurrentStepIndex();
                return (index >= 0) ? index + 1 : StepCount;
            }
        }

        /// <summary>지금 발판에 남은 적 수. 다 깼으면 0.</summary>
        public int RemainingEnemyCount
        {
            get
            {
                int index = FindCurrentStepIndex();
                return ((index >= 0) && (_steps[index].Zone != null)) ? _steps[index].Zone.AliveCount : 0;
            }
        }

        /// <summary>모든 발판을 깼는지 여부.</summary>
        public bool IsCleared => FindCurrentStepIndex() < 0;
        #endregion

        #region Events
        /// <summary>발판 진행·남은 적 수가 바뀌면 호출된다. 화면 갱신용.</summary>
        public event Action OnProgressChanged;
        #endregion

        #region Public Methods
        /// <summary>스테이지가 시작될 때 한 번 호출된다. 다음 발판을 꺼 두고, 구독하고, 첫 발판 특수몹을 만들어 둔다.</summary>
        public void Begin()
        {
            if (_steps == null) return;

            foreach (SPlatformStep step in _steps)
            {
                if (step.NextPlatform != null) step.NextPlatform.SetActive(false);
                if (step.Zone == null) continue;

                step.Zone.OnCleared += HandleZoneCleared;
                step.Zone.OnProgressChanged += HandleZoneProgressChanged;
            }

            PrepareCurrentStep();
        }

        /// <summary>스테이지가 없어질 때 한 번 호출된다. 구독을 푼다.</summary>
        public void End()
        {
            if (_steps == null) return;

            foreach (SPlatformStep step in _steps)
            {
                if (step.Zone == null) continue;

                step.Zone.OnCleared -= HandleZoneCleared;
                step.Zone.OnProgressChanged -= HandleZoneProgressChanged;
            }
        }

        /// <summary>이 라운드가 되면 호출된다. 발판 위로 위험물을 떨어뜨리기 시작한다.</summary>
        public void StartHazards()
        {
            if (_hazardSpawner != null) _hazardSpawner.StartSpawning();
        }

        /// <summary>다른 라운드가 되면 호출된다. 위험물 생성을 멈추고 떨어지는 중인 위험물을 지운다.</summary>
        public void StopHazards()
        {
            if (_hazardSpawner != null) _hazardSpawner.StopSpawning();
        }

        /// <summary>아직 못 깬 발판만 처음 상태로 돌리고 지금 발판 특수몹을 다시 만들어 둔다. 이미 나타난 발판은 그대로 둔다.
        /// 떨어지는 중인 위험물은 지우고(리스폰 직후 맞지 않게) 생성은 계속한다.</summary>
        public void ResetUnclearedSteps()
        {
            if (_hazardSpawner != null) _hazardSpawner.ClearHazards();
            if (_steps == null) return;

            foreach (SPlatformStep step in _steps)
            {
                if (step.Zone != null) step.Zone.ResetIfNotCleared();
            }

            PrepareCurrentStep();
        }
        #endregion

        #region Private Methods
        // 아직 못 깬 첫 발판. 다 깼으면 -1.
        private int FindCurrentStepIndex()
        {
            if (_steps == null) return -1;

            for (int i = 0; i < _steps.Length; i++)
            {
                if ((_steps[i].Zone != null) && !_steps[i].Zone.IsCleared) return i;
            }

            return -1;
        }

        private void PrepareCurrentStep()
        {
            int index = FindCurrentStepIndex();
            if (index >= 0) _steps[index].Zone.Prepare();
        }

        // 깬 발판의 다음 발판을 켜고, 그 발판의 특수몹을 만들어 둔다. (발판이 먼저 켜져야 그 위에 만들 수 있다)
        private void HandleZoneCleared(CombatZone zone)
        {
            foreach (SPlatformStep step in _steps)
            {
                if ((step.Zone == zone) && (step.NextPlatform != null)) step.NextPlatform.SetActive(true);
            }

            PrepareCurrentStep();
        }

        private void HandleZoneProgressChanged()
        {
            OnProgressChanged?.Invoke();
        }
        #endregion
    }
}
