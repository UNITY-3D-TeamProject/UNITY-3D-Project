using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class InfoPanelView : MonoBehaviour
    {
        [Header("Root")]
        [SerializeField] private GameObject _root;

        [Header("Content")]
        [SerializeField] private Text _titleText;
        [SerializeField] private Text _bodyText;


        // 루트가 연결되어있는지, 활성화 상태인지 체크
        public bool IsVisible => _root != null && _root.activeSelf;

        // HUD 보이게 하는 함수
        public void Show()
        {
            SetVisible(true);
        }
        // HUD 숨기는 함수
        public void Hide()
        {
            SetVisible(false);
        }

        // 실제로 HUD를 켜고 끄는 핵심 함수
        public void SetVisible(bool isVisible)
        {
            if (_root != null)
            {
                _root.SetActive(isVisible);
            }
        }

    }
}
