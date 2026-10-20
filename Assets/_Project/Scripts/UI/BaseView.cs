using UnityEngine;

namespace UI
{
    // 루트 하나를 켜고 끄는 View들의 공통 부모
    public abstract class BaseView : MonoBehaviour
    {
        [Header("Root")]
        [SerializeField] private GameObject _root;

        // 루트가 연결되어있는지, 활성화 상태인지 체크
        public bool IsVisible => _root != null && _root.activeSelf;

        public void Show()
        {
            SetVisible(true);
        }

        public void Hide()
        {
            SetVisible(false);
        }

        private void SetVisible(bool isVisible)
        {
            if (_root != null)
            {
                _root.SetActive(isVisible);
            }
        }
    }
}
