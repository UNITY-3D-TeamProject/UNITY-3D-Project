using System;
using System.Collections.Generic;

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

        public event Action<EFeedItemType> OnItemCollected;

        public override void StartStage()
        {
            StageProgress = 0;
            _collectedItems.Clear();
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
        }

        public override void EndStage()
        {
            
        }

        //protected override void Update()
        //{
        //    base.Update();
        //}
    }
}
