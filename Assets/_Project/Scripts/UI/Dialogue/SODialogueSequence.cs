using System;
using UnityEngine;

namespace UI.Dialogue
{
    // 대화 한 줄. 말하는 사람, 초상화, 이 줄에서 켤 카메라 번호, 대사를 담는다.
    [Serializable]
    public struct SDialogueLine
    {
        public string SpeakerName;
        [Tooltip("비워 두면 말하는 사람의 기본 얼굴(DialoguePresenter에 지정)을 쓴다. 표정을 바꿀 때만 넣는다")]
        public Sprite Portrait;
        [Tooltip("-1이면 말하는 사람을 비추는 카메라(자동). 0 이상이면 StageIntroDialogue의 Fixed Cameras 배열 순서의 고정 카메라")]
        public int CameraIndex;
        [TextArea(3, 5)] public string Message;
    }

    // 대화 한 묶음. 줄 순서대로 재생한다.
    [CreateAssetMenu(fileName = "SODialogueSequence", menuName = "UI/Dialogue Sequence")]
    public class SODialogueSequence : ScriptableObject
    {
        [Tooltip("본 기록 저장 키. 대화마다 겹치지 않게 짓는다")]
        [SerializeField] private string _id;
        [SerializeField] private SDialogueLine[] _lines;

        public string Id => _id;
        public int LineCount => (_lines != null) ? _lines.Length : 0;

        public bool TryGetLine(int index, out SDialogueLine line)
        {
            if ((index < 0) || (index >= LineCount))
            {
                line = default;
                return false;
            }

            line = _lines[index];
            return true;
        }
    }
}
