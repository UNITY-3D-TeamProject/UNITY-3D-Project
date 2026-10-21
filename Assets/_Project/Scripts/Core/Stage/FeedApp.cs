using System;
using System.Collections.Generic;
using Map.Feed;

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


        private readonly HashSet<EFeedItemType> _collectedItems = new HashSet<EFeedItemType>();

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
            if (_round2 != null)
                _round2.OnStarted.RemoveListener(HandleRound2Started);
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
            
        }

        // 2라운드 시작 직후에는 피드가 모두 남아 있으므로 그 수를 전체 수로 기록한다.
        private void HandleRound2Started()
        {
            Round2TotalCount = _round2.RemainingCount;
            _lastRound2RemainingCount = Round2TotalCount;
            NotifyProgressChanged();
        }
    }
}
