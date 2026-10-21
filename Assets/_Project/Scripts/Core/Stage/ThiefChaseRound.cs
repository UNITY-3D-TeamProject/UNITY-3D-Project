using System;
using UnityEngine;
using AI.Thief;
using Facade.Player;

namespace Core.Stage
{
    /// <summary>
    /// 경찰과 도둑 라운드. 라운드에 들어오면 타이머가 돌고, 제한 시간 안에 도둑(씬의 ThiefSpawner)을 다 잡으면 클리어다.
    /// 시간이 다 되면 플레이어 체력을 0으로 만들어 기존 사망 흐름(라운드 시작점에서 재스폰)을 탄다.
    /// 재스폰되면 타이머는 처음부터 다시 센다. 도둑은 ThiefSpawner 가 스스로 다시 만든다.
    /// </summary>
    [Serializable]
    public class ThiefChaseRound : StageRound
    {
        #region Constants
        private const int DEFAULT_ROUND_INDEX = 2;  // 3라운드
        private const string HP_ATTRIBUTE = "CurrentHp";
        #endregion

        #region Serialized Fields
        [Tooltip("이 시간(초) 안에 도둑을 다 잡지 못하면 플레이어가 죽는다.")]
        [SerializeField, Min(1.0f)] private float _timeLimitSeconds = 60.0f;
        #endregion

        #region Private Fields
        [NonSerialized] private ThiefSpawner _thiefSpawner;
        [NonSerialized] private bool _isTimerRunning;
        [NonSerialized] private int _lastShownSeconds;
        #endregion

        #region Properties
        /// <summary>제한 시간 (초).</summary>
        public float TimeLimitSeconds => _timeLimitSeconds;

        /// <summary>남은 시간 (초).</summary>
        public float RemainingSeconds { get; private set; }

        /// <summary>잡은 도둑 수.</summary>
        public int CaughtCount => (_thiefSpawner != null) ? _thiefSpawner.CaughtCount : 0;

        /// <summary>이번 라운드 도둑 전체 수. 생성 전에는 0.</summary>
        public int TotalCount => (_thiefSpawner != null) ? _thiefSpawner.TotalCount : 0;

        public override bool IsCleared => (_thiefSpawner != null) && _thiefSpawner.IsCleared;
        #endregion

        public ThiefChaseRound() : base(DEFAULT_ROUND_INDEX)
        {
        }

        #region Public Methods
        public override void Begin()
        {
            RemainingSeconds = _timeLimitSeconds;
            _lastShownSeconds = -1;

            _thiefSpawner = UnityEngine.Object.FindAnyObjectByType<ThiefSpawner>();
            if (_thiefSpawner == null)
            {
                Debug.LogWarning($"씬에 ThiefSpawner가 없어 {RoundNumber}라운드 도둑 잡기를 깰 수 없습니다.");
                return;
            }

            _thiefSpawner.OnCountChanged += HandleCountChanged;
            _thiefSpawner.OnAllCaught += HandleAllCaught;
        }

        public override void End()
        {
            if (_thiefSpawner == null) return;

            _thiefSpawner.OnCountChanged -= HandleCountChanged;
            _thiefSpawner.OnAllCaught -= HandleAllCaught;
        }

        public override void Tick(float deltaTime)
        {
            if (!_isTimerRunning) return;

            RemainingSeconds = Mathf.Max(0.0f, RemainingSeconds - deltaTime);
            NotifyIfShownSecondsChanged();

            if (RemainingSeconds > 0.0f) return;

            _isTimerRunning = false;
            KillPlayer();
        }
        #endregion

        #region Protected Methods
        protected override void OnRoundEntered()
        {
            RestartTimer();
        }

        protected override void OnRoundExited()
        {
            _isTimerRunning = false;
        }

        protected override void OnPlayerRespawnRequested()
        {
            RestartTimer();
        }
        #endregion

        #region Private Methods
        // 이미 다 잡았으면 다시 돌리지 않는다.
        private void RestartTimer()
        {
            if (IsCleared) return;

            RemainingSeconds = _timeLimitSeconds;
            _isTimerRunning = true;
            NotifyIfShownSecondsChanged();
        }

        // 화면에는 초 단위로 보이므로, 보이는 초가 바뀔 때만 알린다.
        private void NotifyIfShownSecondsChanged()
        {
            int shownSeconds = Mathf.CeilToInt(RemainingSeconds);
            if (shownSeconds == _lastShownSeconds) return;

            _lastShownSeconds = shownSeconds;
            NotifyProgressChanged();
        }

        private void KillPlayer()
        {
            PlayerFacade player = (GameManager.Instance != null) ? GameManager.Instance.CurrentPlayerFacade : null;
            if ((player == null) || !player.HasAttribute(HP_ATTRIBUTE))
            {
                Debug.LogWarning("플레이어가 없어 시간 초과 사망을 처리하지 못했습니다.");
                return;
            }

            player.SetAttribute(HP_ATTRIBUTE, 0.0f);
        }

        private void HandleCountChanged(int caughtCount, int totalCount)
        {
            NotifyProgressChanged();
        }

        private void HandleAllCaught()
        {
            _isTimerRunning = false;
            NotifyCleared();
        }
        #endregion
    }
}
