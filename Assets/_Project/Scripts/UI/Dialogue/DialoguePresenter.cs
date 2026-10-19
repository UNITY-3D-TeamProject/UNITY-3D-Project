using System;
using UnityEngine;
using UI.PetGuide;

namespace UI.Dialogue
{
    // 대화 묶음을 한 줄씩 펫 안내 패널에 띄운다. 입력은 모르고, Next 호출로만 넘어간다.
    public class DialoguePresenter : MonoBehaviour
    {
        // 말하는 사람별 기본 얼굴. 대사에 초상화가 없을 때 쓴다.
        [Serializable]
        private struct SSpeakerPortrait
        {
            [Tooltip("대사의 SpeakerName과 똑같이 적는다 (예: PET, PLAYER)")]
            public string SpeakerName;
            public Sprite DefaultPortrait;
        }

        [SerializeField] private PetGuideView _view;

        [Header("Speakers")]
        [SerializeField] private SSpeakerPortrait[] _defaultPortraits;

        private SODialogueSequence _sequence;
        private int _lineIndex;

        public bool IsPlaying => _sequence != null;

        // 줄이 바뀔 때 그 줄(말하는 사람, 카메라 번호 등)을 알린다.
        public event Action<SDialogueLine> OnLineChanged;
        // 마지막 줄 다음으로 넘어가 대화가 끝났을 때 발생한다.
        public event Action OnFinished;

        public bool Play(SODialogueSequence sequence)
        {
            if ((_view == null) ||
                (sequence == null) ||
                (sequence.LineCount == 0) ||
                IsPlaying)
                return false;

            _sequence = sequence;
            _lineIndex = 0;
            ShowCurrentLine();
            return true;
        }

        public void Next()
        {
            if (!IsPlaying) return;

            _lineIndex++;

            if (_lineIndex >= _sequence.LineCount)
            {
                Finish();
                return;
            }

            ShowCurrentLine();
        }

        // 끝까지 보지 않고 대화를 정리한다. 끝난 것으로 알린다.
        public void Stop()
        {
            if (!IsPlaying) return;

            Finish();
        }

        private void ShowCurrentLine()
        {
            if (!_sequence.TryGetLine(_lineIndex, out SDialogueLine line))
                return;

            Sprite portrait = (line.Portrait != null) ? line.Portrait : GetDefaultPortrait(line.SpeakerName);

            _view.ShowLine(line.SpeakerName, portrait, line.Message);
            OnLineChanged?.Invoke(line);
        }

        // 기본 얼굴이 없으면 null을 돌려줘 초상화 칸을 숨긴다.
        private Sprite GetDefaultPortrait(string speakerName)
        {
            if (_defaultPortraits == null) return null;

            foreach (SSpeakerPortrait speaker in _defaultPortraits)
            {
                if (speaker.SpeakerName == speakerName)
                    return speaker.DefaultPortrait;
            }

            return null;
        }

        private void Finish()
        {
            _sequence = null;
            _lineIndex = 0;
            _view.Hide();
            OnFinished?.Invoke();
        }
    }
}
