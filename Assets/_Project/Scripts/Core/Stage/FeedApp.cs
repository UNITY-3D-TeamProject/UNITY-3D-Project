namespace Core.Stage
{
    public class FeedApp : StageBase
    {
        public override EStageType StageType => EStageType.FeedApp;

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
