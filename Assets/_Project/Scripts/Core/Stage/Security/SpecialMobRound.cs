using System;
using UnityEngine;
using Map.Gimmicks;

namespace Core.Stage.Security
{
    /// <summary>
    /// 2라운드 특수몹 발판. 발판은 순서대로 하나씩 진행한다.
    /// - 특수몹은 미리 만들지 않고, 플레이어가 발판 구역에 들어오면 그때 만든다. (건너가기 전에 원거리로 미리 잡지 못하게)
    /// - 발판 싸움이 시작되면 바로 뒤 발판을 가라앉혀(다 내려가면 끈다) 되돌아가지 못하게 하고, 떨어지면 이 발판으로 돌아오게 한다.
    /// - 특수몹을 다 잡으면 꺼 두었던 다음 발판이 나타난다.
    /// - 이 라운드 동안 켜져 있는 발판 위로 위험물이 떨어진다. (FallingHazardSpawner)
    /// SecurityApp 이 필드로 들고, 스테이지 시작 때 Begin, 끝날 때 End, 이 라운드 중 죽으면 ResetSteps 를 부른다.
    /// 이 라운드가 되면 StartHazards, 다른 라운드가 되면 StopHazards 를, 이 라운드 동안 매 프레임 HideLoweredPlatforms 를 부른다.
    /// </summary>
    [Serializable]
    public class SpecialMobRound
    {
        #region Nested Types
        [Serializable]
        private struct SPlatformStep
        {
            [Tooltip("이 구역이 놓인 발판. 다음 발판 싸움이 시작되면 가라앉는다.")]
            public StateMover Platform;

            [Tooltip("이 발판 싸움 중 떨어지면 돌아올 자리 (앞 방향 = 바라볼 방향)")]
            public Transform FallReturnPoint;

            [Tooltip("발판 위 특수몹 구역")]
            public CombatZone Zone;

            [Tooltip("이 발판을 깨면 나타나는 다음 발판. 스테이지가 시작될 때 꺼 둔다.")]
            public GameObject NextPlatform;
        }
        #endregion

        #region Serialized Fields
        [Tooltip("진행 순서대로 발판마다 발판·낙하 복귀 자리·특수몹 구역·다음 발판")]
        [SerializeField] private SPlatformStep[] _steps;

        [Tooltip("이 라운드 동안 켜져 있는 발판 위로 위험물을 떨어뜨리는 생성기 (04_Platforms/FallingHazardSpawner)")]
        [SerializeField] private FallingHazardSpawner _hazardSpawner;
        #endregion

        #region Private Fields
        private StageManager _stageManager;
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
        /// <summary>스테이지가 시작될 때 한 번 호출된다. 다음 발판을 꺼 두고 구독한다. 낙하 복귀 자리는 stageManager 에 알린다.</summary>
        public void Begin(StageManager stageManager)
        {
            _stageManager = stageManager;
            if (_steps == null) return;

            foreach (SPlatformStep step in _steps)
            {
                if (step.NextPlatform != null) step.NextPlatform.SetActive(false);
                if (step.Zone == null) continue;

                step.Zone.OnStarted += HandleZoneStarted;
                step.Zone.OnCleared += HandleZoneCleared;
                step.Zone.OnProgressChanged += HandleZoneProgressChanged;
            }
        }

        /// <summary>스테이지가 없어질 때 한 번 호출된다. 구독을 푼다.</summary>
        public void End()
        {
            if (_steps == null) return;

            foreach (SPlatformStep step in _steps)
            {
                if (step.Zone == null) continue;

                step.Zone.OnStarted -= HandleZoneStarted;
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

        /// <summary>다른 라운드가 되면 호출된다. 낙하 복귀 자리를 원래대로(체크포인트) 돌린다.</summary>
        public void ClearFallReturnPoint()
        {
            if (_stageManager != null) _stageManager.SetFallReturnPoint(null);
        }

        /// <summary>
        /// 이 라운드 중 죽으면 호출된다. 깬 발판까지 모두 처음 상태로 돌린다: 첫 발판만 제자리에 켜 두고 나머지 발판은 끈다.
        /// 특수몹은 다시 발판을 밟으면 만든다. 떨어지는 중인 위험물은 지우고(리스폰 직후 맞지 않게) 생성은 계속한다.
        /// </summary>
        public void ResetSteps()
        {
            if (_hazardSpawner != null) _hazardSpawner.ClearHazards();
            ClearFallReturnPoint();
            if (_steps == null) return;

            for (int i = 0; i < _steps.Length; i++)
            {
                SPlatformStep step = _steps[i];
                if (step.Zone != null) step.Zone.ResetAll();
                if (step.NextPlatform != null) step.NextPlatform.SetActive(false);
                if (step.Platform == null) continue;

                // 한 번도 켜진 적 없는 발판은 원위치를 아직 모르므로, 가라앉힌 발판만 되돌린다
                if (step.Platform.IsActivated) step.Platform.ResetImmediately();
                step.Platform.gameObject.SetActive(i == 0);
            }
        }

        /// <summary>이 라운드 동안 매 프레임 호출된다. 다 내려간 발판을 꺼서 숨긴다.</summary>
        public void HideLoweredPlatforms()
        {
            if (_steps == null) return;

            foreach (SPlatformStep step in _steps)
            {
                HideIfLowered(step.Platform);
            }
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

        // 발판에 들어와 싸움이 시작되면 바로 뒤 발판을 가라앉히고, 떨어지면 이 발판으로 돌아오게 한다.
        private void HandleZoneStarted(CombatZone zone)
        {
            for (int i = 0; i < _steps.Length; i++)
            {
                if (_steps[i].Zone != zone) continue;

                if ((i > 0) && (_steps[i - 1].Platform != null)) _steps[i - 1].Platform.Activate();
                if ((_stageManager != null) && (_steps[i].FallReturnPoint != null)) _stageManager.SetFallReturnPoint(_steps[i].FallReturnPoint);
                return;
            }
        }

        // 깬 발판의 다음 발판을 켠다. 특수몹은 그 발판을 밟으면 만든다.
        private void HandleZoneCleared(CombatZone zone)
        {
            foreach (SPlatformStep step in _steps)
            {
                if ((step.Zone == zone) && (step.NextPlatform != null)) step.NextPlatform.SetActive(true);
            }
        }

        private void HandleZoneProgressChanged()
        {
            OnProgressChanged?.Invoke();
        }

        // 내려가는 쪽으로 움직여 도착해 멈춘 발판만 끈다. 내려가는 중이거나 이미 꺼져 있으면 그대로다. (BossRound 와 같은 방식)
        private static void HideIfLowered(StateMover platform)
        {
            if ((platform == null) || !platform.gameObject.activeSelf) return;
            if (!platform.IsActivated || platform.IsMoving) return;

            platform.gameObject.SetActive(false);
        }
        #endregion
    }
}
