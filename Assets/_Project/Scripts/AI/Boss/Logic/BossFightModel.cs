using System;

namespace AI.Boss.Logic
{
    /// <summary>
    /// 보스전 단계·B 다운 횟수·그로기 타이머를 관리한다. Unity 를 참조하지 않는다.
    /// 1-1 → B 1번째 다운(그로기) → 1-2 → B 2번째 다운 → 최종 → A 사망(보스 격파) 순으로 진행한다.
    /// </summary>
    public class BossFightModel
    {
        #region Private Fields
        private readonly float _groggySeconds;
        #endregion

        #region Properties
        public EBossStage Stage { get; private set; }

        /// <summary>B 가 다운된 횟수(0~2).</summary>
        public int DownCount { get; private set; }

        public bool IsGroggy { get; private set; }

        /// <summary>그로기 남은 시간(초). 그로기가 아니면 0.</summary>
        public float GroggyRemaining { get; private set; }
        #endregion

        #region Events
        public event Action OnGroggyStarted;
        public event Action OnGroggyEnded;
        public event Action<EBossStage> OnStageChanged;
        #endregion

        #region Constructors
        /// <param name="groggySeconds">B 1번째 다운 뒤 그로기 시간(초)</param>
        public BossFightModel(float groggySeconds)
        {
            _groggySeconds = groggySeconds;
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// B 다운을 알린다. 1번째는 그로기를 시작하고, 1-2 단계의 2번째는 최종 단계로 넘긴다.
        /// 그로기 중 중복 알림, 최종 이후 알림은 무시한다.
        /// </summary>
        public void NotifyBDown()
        {
            if (IsGroggy) return;

            if (DownCount == 0 && Stage == EBossStage.Stage1_1)
            {
                DownCount = 1;
                IsGroggy = true;
                GroggyRemaining = _groggySeconds;
                OnGroggyStarted?.Invoke();
                return;
            }

            if (DownCount == 1 && Stage == EBossStage.Stage1_2)
            {
                DownCount = 2;
                ChangeStage(EBossStage.Final);
            }
        }

        /// <summary>
        /// 그로기 타이머를 진행한다. 끝나면 그로기를 풀고 1-2 단계로 넘긴다.
        /// </summary>
        public void Tick(float deltaTime)
        {
            if (!IsGroggy) return;

            GroggyRemaining -= deltaTime;
            if (GroggyRemaining > 0.0f) return;

            GroggyRemaining = 0.0f;
            IsGroggy = false;
            OnGroggyEnded?.Invoke();
            ChangeStage(EBossStage.Stage1_2);
        }

        /// <summary>
        /// A 사망을 알린다. 단계를 보스 격파(Defeated)로 바꾸고, 이후 모든 알림을 무시한다.
        /// </summary>
        public void NotifyADeath()
        {
            if (Stage == EBossStage.Defeated) return;

            IsGroggy = false;
            GroggyRemaining = 0.0f;
            ChangeStage(EBossStage.Defeated);
        }

        /// <summary>
        /// 1-1 단계, 다운 0회, 그로기 아님으로 되돌린다. 이벤트를 보내지 않는다.
        /// </summary>
        public void Reset()
        {
            Stage = EBossStage.Stage1_1;
            DownCount = 0;
            IsGroggy = false;
            GroggyRemaining = 0.0f;
        }
        #endregion

        #region Private Methods
        private void ChangeStage(EBossStage stage)
        {
            Stage = stage;
            OnStageChanged?.Invoke(stage);
        }
        #endregion
    }
}
