using TMPro;
using UnityEngine;

namespace UI.InfoPanel
{
    // 화면 위 가운데에 라운드 남은 시간을 보여 준다.
    public class RoundTimerView : BaseView
    {
        [Header("Content")]
        [SerializeField] private TMP_Text _timeText;

        public void SetText(string text)
        {
            if (_timeText != null)
                _timeText.text = text;
        }
    }
}
