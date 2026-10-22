using System;
using System.Collections;
using UnityEngine;
using UI.PetGuide;

namespace UI.Dialogue
{
    // 게임을 멈추지 않는 독백. 대화 묶음을 한 줄씩 펫 안내 패널에 띄우고, 줄마다 정해진 시간이 지나면 다음 줄로 넘긴다.
    // 패널 아래 게이지로 남은 시간을 보여 주고, 마지막 줄의 게이지가 다 줄면 패널을 숨긴다. 입력·게임 시간·카메라는 건드리지 않는다.
    // 게이지는 게임 시간으로 줄므로 설정창·펫 안내로 게임이 멈춘 동안에는 같이 멈춘다.
    public class MonologuePresenter : MonoBehaviour
    {
        // 말하는 사람별 기본 얼굴. 대사에 초상화가 없을 때 쓴다.
        [Serializable]
        private struct SSpeakerPortrait
        {
            [Tooltip("대사의 SpeakerName과 똑같이 적는다 (예: PET, PLAYER)")]
            public string SpeakerName;
            public Sprite DefaultPortrait;
        }

        #region Serialized Fields
        // 대사를 그릴 패널. 컷씬·펫 안내와 같은 패널을 같이 쓴다.
        [SerializeField] private PetGuideView _petGuideView;
        [Tooltip("한 줄을 보여 주는 기본 시간(초). 독백 테이블 항목의 시간이 0이면 이 값을 쓴다. 이 시간 동안 게이지가 가득 찬 상태에서 0까지 준다")]
        [SerializeField] private float _defaultLineDuration = 3.0f;

        [Header("Speakers")]
        [SerializeField] private SSpeakerPortrait[] _speakerDefaultPortraits;
        #endregion

        #region Private Fields
        private Coroutine _playCoroutine;
        #endregion

        #region Properties
        // 독백이 떠 있으면 true
        public bool IsPlaying => _playCoroutine != null;
        #endregion

        #region Unity Lifecycle
        private void OnDisable()
        {
            Stop();
        }
        #endregion

        #region Public Methods
        // 독백을 처음 줄부터 띄운다. 이미 독백 중이면 끊고 새 독백을 처음부터 띄운다.
        // lineDuration은 한 줄을 보여 주는 시간(초)이며, 0 이하면 기본 시간을 쓴다.
        // 패널이 없거나 빈 대화면 띄우지 않는다.
        public void Play(SODialogue dialogue, float lineDuration)
        {
            if ((_petGuideView == null) ||
                (dialogue == null) ||
                (dialogue.LineCount == 0))
                return;

            Stop();

            // 독백은 키로 넘기지 않으므로 넘기기 안내는 숨긴다.
            _petGuideView.HideNextLabel();
            float duration = (lineDuration > 0.0f) ? lineDuration : _defaultLineDuration;
            _playCoroutine = StartCoroutine(CoPlay(dialogue, duration));
        }

        // 독백을 바로 끝내고 패널을 숨긴다. 독백 중이 아니면 패널을 건드리지 않는다.
        public void Stop()
        {
            if (!IsPlaying) return;

            StopCoroutine(_playCoroutine);
            _playCoroutine = null;
            HidePanel();
        }
        #endregion

        #region Private Methods
        private void HidePanel()
        {
            _petGuideView.HideGauge();
            _petGuideView.Hide();
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
        #endregion

        #region Coroutines
        private IEnumerator CoPlay(SODialogue dialogue, float lineDuration)
        {
            for (int i = 0; i < dialogue.LineCount; i++)
            {
                if (!dialogue.TryGetLine(i, out SDialogueLine line))
                    continue;

                // 대사에 초상화를 넣었으면 그 표정, 비워 두었으면 말하는 사람의 기본 얼굴
                Sprite portrait = (line.Portrait != null) ? line.Portrait : GetSpeakerDefaultPortrait(line.SpeakerName);
                _petGuideView.ShowLine(line.SpeakerName, portrait, line.Message);

                // 줄마다 게이지를 가득 채운 뒤 lineDuration 동안 0까지 줄인다.
                float elapsed = 0.0f;
                while (elapsed < lineDuration)
                {
                    _petGuideView.ShowGauge(1.0f - (elapsed / lineDuration));
                    yield return null;
                    elapsed += Time.deltaTime;
                }
            }

            _playCoroutine = null;
            HidePanel();
        }
        #endregion
    }
}
