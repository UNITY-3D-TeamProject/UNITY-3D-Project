using System;
using System.Collections.Generic;
using UnityEngine;
using Facade.Player;
using Map.Feed;
using Map.Gimmicks;

namespace Core.Stage
{
    public class FeedApp : StageBase
    {
        public override EStageType StageType => EStageType.FeedApp;

        // 4개의 임무를 순서대로 할 수 있게끔만 하면 됨 

        // 악성 게시물 수 = 3
        // 악성 게시물 없앴을 때 나오는 악성 데이터 수 
        // 아이템 위치 숨겨놓는 곳
        // 해시태그 입구 위치 정하기


        // 악성 데이터가 남아 있는 동안은 피격 무적을 0초로 두고, 모두 사라지면 이 값으로 되돌린다. 플레이어 프리팹의 HitInvincibility 값과 맞춘다.
        [SerializeField, Min(0.0f)] private float _defaultHitInvincibilityDuration = 1.0f;

        private readonly HashSet<EFeedItemType> _collectedItems = new HashSet<EFeedItemType>();

        // 악성 데이터 여러 개에 연달아 닿으면 그만큼 데미지를 입도록 살아 있는 악성 데이터를 센다.
        private readonly HashSet<HomingHazard> _aliveMaliciousData = new HashSet<HomingHazard>();
        private MaliciousFeed[] _maliciousFeeds;
        private GameManager _gameManager;
        private StageManager _stageManager;

        // 2라운드. 남은 피드 수가 바뀌어도 알려 주지 않아서 Update에서 직접 비교한다.
        private FeedRound2 _round2;
        private int _lastRound2RemainingCount;

        // 2라운드가 시작됐으면 2, 아니면 1
        public int CurrentRound => ((_round2 != null) && _round2.IsStarted) ? 2 : 1;

        public int CollectedItemCount => _collectedItems.Count;

        // 2라운드를 시작하는 데 필요한 아이템 수. 2라운드가 없거나 조건이 비어 있으면 아이템 종류 수
        public int RequiredItemCount
        {
            get
            {
                if ((_round2 != null) && (_round2.RequiredItems != null) && (_round2.RequiredItems.Count > 0))
                    return _round2.RequiredItems.Count;

                return Enum.GetValues(typeof(EFeedItemType)).Length;
            }
        }

        // 2라운드 시작 때의 피드 수
        public int Round2TotalCount { get; private set; }

        // 2라운드에서 벽으로 올려 보낸 피드 수
        public int Round2SentCount => (_round2 != null) ? (Round2TotalCount - _round2.RemainingCount) : 0;

        public event Action<EFeedItemType> OnItemCollected;

        // 아이템을 먹거나, 2라운드가 시작되거나, 2라운드 남은 피드 수가 바뀌면 NotifyProgressChanged 로 알린다.
        protected override void Update()
        {
            base.Update();

            if ((_round2 == null) || !_round2.IsStarted)
                return;

            int remainingCount = _round2.RemainingCount;
            if (remainingCount == _lastRound2RemainingCount)
                return;

            _lastRound2RemainingCount = remainingCount;
            NotifyProgressChanged();
        }

        private void OnDestroy()
        {
            if (_stageManager != null)
                _stageManager.OnPlayerRespawnRequested -= HandlePlayerRespawnRequested;

            if (_round2 != null)
                _round2.OnStarted.RemoveListener(HandleRound2Started);

            UnsubscribeMaliciousFeeds();

            // 씬을 나갈 때 악성 데이터가 이 오브젝트보다 늦게 지워져도 부르지 않게 먼저 끊는다.
            foreach (HomingHazard data in _aliveMaliciousData)
            {
                if (data != null)
                    data.OnDestroyed -= HandleMaliciousDataDestroyed;
            }
            _aliveMaliciousData.Clear();

            if (_gameManager != null)
                _gameManager.OnPlayerSpawned -= HandlePlayerSpawned;
        }

        public override void StartStage()
        {
            StageProgress = 0;
            _collectedItems.Clear();

            // 2라운드는 맵 생성기가 Awake에서 만들어 두므로 여기서 찾을 수 있다.
            if (_round2 != null)
                _round2.OnStarted.RemoveListener(HandleRound2Started);

            _round2 = FindAnyObjectByType<FeedRound2>();
            if (_round2 != null)
                _round2.OnStarted.AddListener(HandleRound2Started);

            Round2TotalCount = 0;
            _lastRound2RemainingCount = 0;

            // 악성 피드도 맵 생성기가 Awake에서 만들어 두므로 여기서 찾을 수 있다.
            UnsubscribeMaliciousFeeds();
            _maliciousFeeds = FindObjectsByType<MaliciousFeed>(FindObjectsSortMode.None);
            foreach (MaliciousFeed feed in _maliciousFeeds)
                feed.OnCleared += HandleMaliciousFeedCleared;

            // StartStage는 스테이지마다 한 번만 불리므로, 다시 스폰한 플레이어에는 스폰 이벤트로 무적 시간을 다시 넣는다.
            if (_gameManager != null)
                _gameManager.OnPlayerSpawned -= HandlePlayerSpawned;

            _gameManager = GameManager.Instance;
            if (_gameManager != null)
                _gameManager.OnPlayerSpawned += HandlePlayerSpawned;

            if (_stageManager != null)
                _stageManager.OnPlayerRespawnRequested -= HandlePlayerRespawnRequested;

            _stageManager = (_gameManager != null) ? _gameManager.CurrentStageManager : null;
            if (_stageManager != null)
                _stageManager.OnPlayerRespawnRequested += HandlePlayerRespawnRequested;

            NotifyProgressChanged();
        }

        public bool HasItem(EFeedItemType itemType)
        {
            return _collectedItems.Contains(itemType);
        }

        // StageProgress = collected item count
        public void CollectItem(EFeedItemType itemType)
        {
            if (!_collectedItems.Add(itemType))
            {
                return;
            }

            StageProgress = _collectedItems.Count;
            OnItemCollected?.Invoke(itemType);
            NotifyProgressChanged();
        }

        public override void EndStage()
        {
            if (_stageManager != null)
                _stageManager.OnPlayerRespawnRequested -= HandlePlayerRespawnRequested;
        }

        private void HandlePlayerRespawnRequested()
        {
            if ((_round2 == null) || !_round2.IsStarted || _round2.IsCleared)
                return;

            _round2.RestartFeeds();
            _lastRound2RemainingCount = _round2.RemainingCount;
            NotifyProgressChanged();
        }

        // 2라운드 시작 직후에는 피드가 모두 남아 있으므로 그 수를 전체 수로 기록한다.
        private void HandleRound2Started()
        {
            Round2TotalCount = _round2.RemainingCount;
            _lastRound2RemainingCount = Round2TotalCount;
            NotifyProgressChanged();
        }

        // 악성 피드는 악성 데이터를 만든 다음 OnCleared를 부르므로, 이때 찾으면 새로 생긴 데이터가 들어 있다.
        private void HandleMaliciousFeedCleared()
        {
            foreach (HomingHazard data in FindObjectsByType<HomingHazard>(FindObjectsSortMode.None))
            {
                // 앞서 제거한 악성 피드에서 나와 이미 세고 있는 데이터는 다시 구독하지 않는다.
                if (_aliveMaliciousData.Add(data))
                    data.OnDestroyed += HandleMaliciousDataDestroyed;
            }

            if (_aliveMaliciousData.Count > 0)
                ApplyHitInvincibilityDuration(_gameManager != null ? _gameManager.CurrentPlayerFacade : null);
        }

        // 닿거나 공격받아 사라진 악성 데이터를 빼고, 모두 사라지면 무적 시간을 되돌린다.
        private void HandleMaliciousDataDestroyed(HomingHazard data)
        {
            if (!_aliveMaliciousData.Remove(data))
                return;

            if (_aliveMaliciousData.Count > 0)
                return;

            ApplyHitInvincibilityDuration(_gameManager != null ? _gameManager.CurrentPlayerFacade : null);
        }

        // 다시 스폰한 플레이어는 프리팹 기본값으로 나오므로, 악성 데이터가 남아 있으면 다시 0초를 넣는다.
        private void HandlePlayerSpawned(PlayerFacade player)
        {
            if (_aliveMaliciousData.Count == 0)
                return;

            ApplyHitInvincibilityDuration(player);
        }

        // 악성 데이터가 남아 있으면 0초, 없으면 평소 값으로 플레이어의 피격 무적 시간을 바꾼다.
        private void ApplyHitInvincibilityDuration(PlayerFacade player)
        {
            if (player == null)
                return;

            float duration = (_aliveMaliciousData.Count > 0) ? 0.0f : _defaultHitInvincibilityDuration;
            player.SetHitInvincibilityDuration(duration);
        }

        private void UnsubscribeMaliciousFeeds()
        {
            if (_maliciousFeeds == null)
                return;

            // 이미 제거된 악성 피드는 건너뛴다.
            foreach (MaliciousFeed feed in _maliciousFeeds)
            {
                if (feed != null)
                    feed.OnCleared -= HandleMaliciousFeedCleared;
            }
            _maliciousFeeds = null;
        }
    }
}
