using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Checkpoint
{
    public class CheckpointNoticeView : BaseView
    {
        [Header("Content")]
        [SerializeField] private TMP_Text _titleText;
        [SerializeField] private TMP_Text _bodyText;
        [Tooltip("종류별 색을 입힐 이미지 (아이콘, 테두리 빛)")]
        [SerializeField] private Image[] _tintImages;

        // 제목·설명을 바꾸고, 제목과 강조 이미지에 색을 입힌다.
        public void SetContent(string title, string body, Color color)
        {
            if (_titleText != null)
            {
                _titleText.text = title;
                _titleText.color = color;
            }

            if (_bodyText != null)
                _bodyText.text = body;

            if (_tintImages == null) return;

            foreach (Image image in _tintImages)
            {
                if (image != null)
                    image.color = color;
            }
        }
    }
}
