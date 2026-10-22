using UnityEngine;

namespace UIandCinematic
{
    /// <summary>
    /// 번호로 대화를 시작·중지하고 상태를 알려 주는 쪽(UIManager)의 약속. Core는 UI를 직접 참조하지 않고 CutsceneController.Current로 이것만 본다.
    /// </summary>
    public interface ICutsceneController
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

        // 컷씬 대사 한 줄이 E로 넘기기를 기다리는 중이면 true.
        bool IsCutsceneLineWaiting { get; }

        // 컷씬(SOCutscene)의 대사 한 줄을 띄운다. 처음 부를 때 펫을 플레이어 앞에 세운다.
        // shouldUseSpeakerCamera가 true면 말하는 사람 카메라를 켜고, false면 끈다(카메라 키로 고른 카메라는 CinematicManager가 켠다).
        // 대화 중이거나 띄울 수 없으면 false.
        bool ShowCutsceneLine(
            string speakerName,
            Sprite portrait,
            string message,
            bool isLastLine,
            bool shouldUseSpeakerCamera);

        // 컷씬 대사를 숨기고 말하는 사람 카메라를 끈다. 카메라 단계 앞에서 부른다.
        void HideCutsceneLine();

        // 컷씬 대사를 숨기고 세워 둔 펫을 돌려놓는다. 컷씬 대사를 띄운 적이 없으면 아무것도 하지 않는다.
        void EndCutscene();
    }
}
