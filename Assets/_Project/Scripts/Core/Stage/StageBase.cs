using UnityEngine;

namespace Core.Stage
{
    public abstract class StageBase : MonoBehaviour
    {
        public abstract EStageType StageType { get; }
        public int StageProgress { get; protected set; }

        public abstract void StartStage();
        public abstract void EndStage();

        //protected virtual void Update()
        //{
        //    // 모든 스테이지에 공통으로 필요한 매 프레임 처리
        //    // 시간? 
        //}
    }
}
