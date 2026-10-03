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


        public override void StartStage()
        {
            StageProgress = 0;
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
