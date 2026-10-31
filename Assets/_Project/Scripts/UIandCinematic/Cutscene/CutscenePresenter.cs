using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace UIandCinematic
{
    // 대화 묶음을 한 줄씩 펫 안내 패널에 띄운다. 입력은 모르고, ShowNextLine 호출로만 넘어간다.
    // 입력(E)과 카메라 전환은 UIManager·CutsceneDirector가 맡고, 여기서는 "지금 몇 번째 줄인지"만 관리한다.
    public class CutscenePresenter : MonoBehaviour
    {
        // 말하는 사람별 기본 얼굴. 대사에 초상화가 없을 때 쓴다.
        [Serializable]
        private struct SSpeakerPortrait
        {
            [Tooltip("대사의 SpeakerName과 똑같이 적는다 (예: 비트, 코리, 관리자)")]
            public string SpeakerName;
            public Sprite DefaultPortrait;
        }

        // 대사를 그릴 패널. 펫 안내와 같은 패널을 같이 쓴다.
        [FormerlySerializedAs("_view")]
        [SerializeField] private PetGuideView _petGuideView;

        [Header("Speakers")]
        [FormerlySerializedAs("_defaultPortraits")]
        [SerializeField] private SSpeakerPortrait[] _speakerDefaultPortraits;

        [Header("Next Label")]
        [Tooltip("다음 줄이 남아 있을 때 패널 아래에 보이는 안내")]
        [SerializeField] private string _nextLineLabel = "E ▶ 다음";
        [Tooltip("마지막 줄일 때 패널 아래에 보이는 안내")]
        [SerializeField] private string _lastLineLabel = "E ▶ 닫기";

        // 지금 재생 중인 대화. 대화 중이 아니면 null이다.
        private SODialogue _currentDialogue;
        // 지금 보여 주고 있는 줄 번호 (0부터)
        private int _currentLineIndex;

        // 재생 중인 대화가 있으면 true
        public bool IsDialoguePlaying => _currentDialogue != null;

        // 줄이 바뀔 때 그 줄(말하는 사람, 카메라 번호 등)을 알린다.
        public event Action<SDialogueLine> OnDialogueLineChanged;
        // 대화가 끝났을 때 발생한다. 마지막 줄을 넘겼을 때와 StopDialogue로 중간에 끝냈을 때 모두 발생한다.
        public event Action OnDialogueEnded;

        // 대화를 처음 줄부터 띄운다. 패널이 없거나, 빈 대화거나, 이미 대화 중이면 시작하지 않고 false를 돌려준다.
        public bool StartDialogue(SODialogue dialogue)
        {
            if ((_petGuideView == null) ||
                (dialogue == null) ||
                (dialogue.LineCount == 0) ||
                IsDialoguePlaying)
                return false;

            _currentDialogue = dialogue;
            _currentLineIndex = 0;
            ShowCurrentLine();
            return true;
        }

        // 다음 줄로 넘긴다. 마지막 줄에서 부르면 대화를 끝낸다.
        public void ShowNextLine()
        {
            if (!IsDialoguePlaying) return;

            _currentLineIndex++;

            // 마지막 줄을 지나면 대화 종료
            if (_currentLineIndex >= _currentDialogue.LineCount)
            {
                EndDialogue();
                return;
            }

            ShowCurrentLine();
        }

        // 끝까지 보지 않고 대화를 중간에 끝낸다. 끝난 것으로 알린다.
        public void StopDialogue()
        {
            if (!IsDialoguePlaying) return;

            EndDialogue();
        }

        // 대화 묶음 없이 한 줄만 띄운다(컷씬 대사). 넘기기와 다음 줄은 부르는 쪽이 정한다.
        // 패널이 없거나 대화 중이면 띄우지 않고 false를 돌려준다.
        public bool ShowSingleLine(
            string speakerName,
            Sprite portrait,
            string message,
            bool isLastLine)
        {
            if ((_petGuideView == null) || IsDialoguePlaying) return false;

            DrawLine(speakerName, portrait, message, isLastLine);
            return true;
        }

        // ShowSingleLine으로 띄운 줄을 숨긴다. 대화 중이면 건드리지 않는다.
        public void HideSingleLine()
        {
            if ((_petGuideView == null) || IsDialoguePlaying) return;

            _petGuideView.HideNextLabel();
            _petGuideView.Hide();
        }

        // 지금 줄을 패널에 띄우고, 바뀐 줄을 알린다 (카메라 전환 등은 받는 쪽에서 처리).
        private void ShowCurrentLine()
        {
            if (!_currentDialogue.TryGetLine(_currentLineIndex, out SDialogueLine line))
                return;

            bool isLastLine = (_currentLineIndex >= _currentDialogue.LineCount - 1);
            DrawLine(line.SpeakerName, line.Portrait, line.Message, isLastLine);

            OnDialogueLineChanged?.Invoke(line);
        }

        // 한 줄을 패널에 그린다.
        private void DrawLine(
            string speakerName,
            Sprite portrait,
            string message,
            bool isLastLine)
        {
            // 대사에 초상화를 넣었으면 그 표정, 비워 두었으면 말하는 사람의 기본 얼굴
            Sprite shownPortrait = (portrait != null) ? portrait : GetSpeakerDefaultPortrait(speakerName);

            _petGuideView.ShowLine(speakerName, shownPortrait, message);

            // 마지막 줄이면 E로 대화가 닫힌다는 것을 알려 준다.
            _petGuideView.ShowNextLabel(isLastLine ? _lastLineLabel : _nextLineLabel);
        }

        // 기본 얼굴이 없으면 null을 돌려줘 초상화 칸을 숨긴다.
        private Sprite GetSpeakerDefaultPortrait(string speakerName)
        {
            if (_speakerDefaultPortraits == null) return null;

            foreach (SSpeakerPortrait speaker in _speakerDefaultPortraits)
            {
                if (speaker.SpeakerName == speakerName)
                    return speaker.DefaultPortrait;
            }

            return null;
        }

        // 상태를 비우고 패널을 숨긴 뒤 끝났다고 알린다.
        // OnDialogueEnded를 받은 쪽이 바로 새 대화를 시작할 수 있도록 상태를 먼저 비운다.
        private void EndDialogue()
        {
            _currentDialogue = null;
            _currentLineIndex = 0;
            _petGuideView.HideNextLabel();
            _petGuideView.Hide();
            OnDialogueEnded?.Invoke();
        }
    }
}
