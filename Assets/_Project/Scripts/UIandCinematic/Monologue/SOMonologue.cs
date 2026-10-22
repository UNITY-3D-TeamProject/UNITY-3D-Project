using System;
using UnityEngine;

namespace UIandCinematic
{
    // 독백 한 줄. 말하는 사람, 초상화, 대사를 담는다.
    [Serializable]
    public struct SMonologueLine
    {
        public string SpeakerName;
        [Tooltip("비워 두면 말하는 사람의 기본 얼굴(MonologuePresenter에 지정)을 쓴다. 표정을 바꿀 때만 넣는다")]
        public Sprite Portrait;
        [TextArea(3, 5)] public string Message;
    }

    // 독백 한 묶음. 줄 순서대로, 줄마다 정해진 시간이 지나면 다음 줄로 넘어간다.
    [CreateAssetMenu(fileName = "SOMonologue", menuName = "UIandCinematic/Monologue")]
    public class SOMonologue : ScriptableObject
    {
        [Tooltip("한 줄을 보여 주는 시간(초). 0이면 MonologuePresenter의 기본 시간을 쓴다")]
        [SerializeField] private float _lineDuration;
        [SerializeField] private SMonologueLine[] _lines;

        public float LineDuration => _lineDuration;
        public int LineCount => (_lines != null) ? _lines.Length : 0;

        public bool TryGetLine(int index, out SMonologueLine line)
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
