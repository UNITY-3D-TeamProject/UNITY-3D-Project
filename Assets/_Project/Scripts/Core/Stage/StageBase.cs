using System;
using UnityEngine;

namespace Core.Stage
{
    public abstract class StageBase : MonoBehaviour
    {
        public abstract EStageType StageType { get; }
        public int StageProgress { get; protected set; }

        // 지금 라운드 번호 (0부터). StageManager 값을 따라가며, 스테이지 진행 전이면 -1.
        public int CurrentRoundIndex { get; private set; } = -1;

        // 라운드가 바뀌거나 진행 상황이 바뀌면 호출된다. 화면 갱신용.
        public event Action OnProgressChanged;

        public abstract void StartStage();
        public abstract void EndStage();

        // roundIndex 번 라운드를 깼는지. 깰 조건이 없는 라운드는 true 다. 라운드 로직이 있는 앱이 덮어쓴다.
        public virtual bool IsRoundCleared(int roundIndex)
        {
            return true;
        }

        // 라운드가 바뀌었다는 이벤트가 없어서 매 프레임 비교한다. 자식이 덮어쓸 때는 base.Update() 를 먼저 부른다.
        protected virtual void Update()
        {
            StageManager stageManager = (GameManager.Instance != null) ? GameManager.Instance.CurrentStageManager : null;
            int currentRoundIndex = ((stageManager != null) && stageManager.IsRunning) ? stageManager.CurrentRoundIndex : -1;
            if (currentRoundIndex == CurrentRoundIndex) return;

            CurrentRoundIndex = currentRoundIndex;
            OnRoundChanged();
            NotifyProgressChanged();
        }

        // 라운드가 바뀐 직후, 화면에 알리기 전에 호출된다.
        protected virtual void OnRoundChanged()
        {
        }

        protected void NotifyProgressChanged()
        {
            OnProgressChanged?.Invoke();
        }
    }
}
