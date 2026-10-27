using TMPro;
using UnityEngine;

namespace UI.InfoPanel
{
    // 제목·본문 두 줄, 셋째 줄이 있으면 세 줄을 보여 준다.
    // 패널 높이는 세 줄 기준이고, 두 줄일 때는 줄 높이와 글자를 키워 같은 높이를 채운다.
    public class InfoPanelView : BaseView
    {
        [Header("Content")]
        [SerializeField] private TMP_Text _titleText;
        [SerializeField] private TMP_Text _bodyText;
        [SerializeField] private TMP_Text _extraText;

        [Header("Two Line Layout")]
        [Tooltip("셋째 줄이 없을 때의 줄 높이. 세 줄 패널과 높이가 같도록 맞춘다")]
        [SerializeField] private float _twoLineRowHeight = 56.5f;
        [SerializeField] private float _twoLineFontSize = 32.0f;

        private float _threeLineRowHeight;
        private float _threeLineFontSize;

        private void Awake()
        {
            _threeLineRowHeight = GetRow(_titleText).rect.height;
            _threeLineFontSize = _titleText.fontSize;
        }

        public void SetTexts(string title, string body)
        {
            SetTexts(title, body, string.Empty);
        }

        public void SetTexts(string title, string body, string extra)
        {
            bool hasExtra = !string.IsNullOrEmpty(extra);

            _titleText.text = title;
            _bodyText.text = body;
            _extraText.text = extra;
            GetRow(_extraText).gameObject.SetActive(hasExtra);

            float rowHeight = hasExtra ? _threeLineRowHeight : _twoLineRowHeight;
            float fontSize = hasExtra ? _threeLineFontSize : _twoLineFontSize;
            ApplyRowSize(_titleText, rowHeight, fontSize);
            ApplyRowSize(_bodyText, rowHeight, fontSize);
        }

        private static void ApplyRowSize(TMP_Text text, float rowHeight, float fontSize)
        {
            GetRow(text).SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, rowHeight);
            text.fontSize = fontSize;
        }

        // 글자는 줄(동그라미 + 글자) 오브젝트의 자식이다.
        private static RectTransform GetRow(TMP_Text text)
        {
            return (RectTransform)text.transform.parent;
        }
    }
}
