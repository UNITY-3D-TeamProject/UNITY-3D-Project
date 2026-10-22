namespace Core.Cinematic
{
    /// <summary>
    /// 번호로 대화를 시작·중지하고 상태를 알려 주는 쪽(UIManager)의 약속. Core는 UI를 직접 참조하지 않고 DialogueController.Current로 이것만 본다.
    /// </summary>
    public interface IDialogueController
    {
        // 대화가 재생 중이면 true.
        bool IsDialoguePlaying { get; }

        // 펫 안내가 떠 있으면 true. 같은 패널을 쓰므로 그동안에는 연출을 시작하지 않는다.
        bool IsPetGuideOpen { get; }

        // 밖(발판·스테이지)에서 번호로 대화를 재생한다. 카메라 연출 중이면 재생하지 않는다.
        void PlayDialogue(int id);

        // 카메라 연출의 대화 단계에서 번호로 대화를 재생한다. 연출 중에도 재생한다.
        void PlayCinematicDialogue(int id);

        // 재생 중인 대화를 끝까지 보지 않고 정리한다.
        void StopDialogue();
    }
}
