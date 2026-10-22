using TMPro;
using UnityEngine;

namespace UI.InfoPanel
{
    public class InfoPanelView : BaseView
    {
        [Header("Content")]
        [SerializeField] private TMP_Text _titleText;
        [SerializeField] private TMP_Text _bodyText;

        public void SetTexts(string title, string body)
        {
            if (_titleText != null)
                _titleText.text = title;

            if (_bodyText != null)
                _bodyText.text = body;
        }
    }
}
