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
    /// FileApp 이 필드로 들고, 라운드에 들어오고 나갈 때 SetActive, 매 프레임 Tick, 라운드 중 죽으면 RestartTimer 를 부른다.
    /// </summary>
    [Serializable]
    public class ThiefChaseRound
    {
        #region Constants
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

        /// <summary>이 라운드를 깼는지 여부.</summary>
        public bool IsCleared => (_thiefSpawner != null) && _thiefSpawner.IsCleared;
        #endregion

        #region Events
        /// <summary>도둑 수나 화면에 보이는 남은 초가 바뀌거나 클리어되면 호출된다. 화면 갱신용.</summary>
        public event Action OnProgressChanged;

        /// <summary>도둑을 다 잡은 순간 한 번 호출된다.</summary>
        public event Action OnCleared;
        #endregion

        #region Public Methods
        /// <summary>스테이지가 시작될 때 한 번 호출된다. 씬의 ThiefSpawner 를 찾는다.</summary>
        public void Begin()
        {
            RemainingSeconds = _timeLimitSeconds;
            _lastShownSeconds = -1;

            _thiefSpawner = UnityEngine.Object.FindAnyObjectByType<ThiefSpawner>();
            if (_thiefSpawner == null)
            {
                Debug.LogWarning("씬에 ThiefSpawner가 없어 도둑 잡기를 깰 수 없습니다.");
                return;
            }

            _thiefSpawner.OnCountChanged += HandleCountChanged;
            _thiefSpawner.OnAllCaught += HandleAllCaught;
        }

        /// <summary>스테이지가 없어질 때 한 번 호출된다. 구독을 푼다.</summary>
        public void End()
        {
            if (_thiefSpawner == null) return;

            _thiefSpawner.OnCountChanged -= HandleCountChanged;
            _thiefSpawner.OnAllCaught -= HandleAllCaught;
        }

        /// <summary>이 라운드에 들어오면 타이머를 처음부터 돌리고, 나가면 멈춘다.</summary>
        public void SetActive(bool isActive)
        {
            if (isActive)
            {
                RestartTimer();
            }
            else
            {
                _isTimerRunning = false;
            }
        }

        /// <summary>타이머를 처음부터 다시 돌린다. 이미 다 잡았으면 다시 돌리지 않는다.</summary>
        public void RestartTimer()
        {
            if (IsCleared) return;

            RemainingSeconds = _timeLimitSeconds;
            _isTimerRunning = true;
            NotifyIfShownSecondsChanged();
        }

        /// <summary>매 프레임 호출된다. 타이머가 돌 때만 시간을 줄인다.</summary>
        public void Tick(float deltaTime)
        {
            if (!_isTimerRunning) return;

            RemainingSeconds = Mathf.Max(0.0f, RemainingSeconds - deltaTime);
            NotifyIfShownSecondsChanged();

            if (RemainingSeconds > 0.0f) return;

            // 피격 무적 중이면 체력 감소가 막히므로, 죽을 때까지 매 프레임 다시 시도한다
            if (TryKillPlayer())
            {
                _isTimerRunning = false;
            }
        }
        #endregion

        #region Private Methods
        // 화면에는 초 단위로 보이므로, 보이는 초가 바뀔 때만 알린다.
        private void NotifyIfShownSecondsChanged()
        {
            int shownSeconds = Mathf.CeilToInt(RemainingSeconds);
            if (shownSeconds == _lastShownSeconds) return;

            _lastShownSeconds = shownSeconds;
            OnProgressChanged?.Invoke();
        }

        // 체력을 0으로 만들고, 실제로 0이 됐는지 돌려준다. 플레이어가 없으면 더 시도하지 않도록 true 를 돌려준다.
        private bool TryKillPlayer()
        {
            PlayerFacade player = (GameManager.Instance != null) ? GameManager.Instance.CurrentPlayerFacade : null;
            if ((player == null) || !player.HasAttribute(HP_ATTRIBUTE))
            {
                Debug.LogWarning("플레이어가 없어 시간 초과 사망을 처리하지 못했습니다.");
                return true;
            }

            player.SetAttribute(HP_ATTRIBUTE, 0.0f);
            return player.GetAttribute(HP_ATTRIBUTE) <= 0.0f;
        }

        private void HandleCountChanged(int caughtCount, int totalCount)
        {
            OnProgressChanged?.Invoke();
        }

        private void HandleAllCaught()
        {
            _isTimerRunning = false;
            OnCleared?.Invoke();
            OnProgressChanged?.Invoke();
        }
        #endregion
    }
}
