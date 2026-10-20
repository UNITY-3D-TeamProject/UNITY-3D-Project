using UnityEngine;
using UnityEngine.UI;

namespace UI.InfoPanel
{
    public class InfoPanelView : BaseView
    {
        [Header("Content")]
        [SerializeField] private Text _titleText;
        [SerializeField] private Text _bodyText;
    }
}
