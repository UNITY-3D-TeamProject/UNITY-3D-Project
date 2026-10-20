using UnityEngine;
using Core.Stage;

namespace UI.InfoPanel
{
    // 피드 앱의 라운드 진행도를 패널에 보여 준다. 피드 앱이 아닌 씬에서는 패널을 숨긴다.
    public class InfoPanelPresenter : MonoBehaviour
    {
        [SerializeField] private InfoPanelView _view;

        private FeedApp _feedApp;

        private void Start()
        {
            if (_view == null)
                return;

            _feedApp = FindAnyObjectByType<FeedApp>();
            if (_feedApp == null)
            {
                _view.Hide();
                return;
            }

            _feedApp.OnProgressChanged += Refresh;
            _view.Show();
            Refresh();
        }

        private void OnDestroy()
        {
            if (_feedApp != null)
                _feedApp.OnProgressChanged -= Refresh;
        }

        // 1라운드는 모은 재료 수, 2라운드는 벽으로 올려 보낸 피드 수를 보여 준다.
        private void Refresh()
        {
            if (_feedApp.CurrentRound == 1)
                _view.SetTexts("1라운드", $"피드 재료 {_feedApp.CollectedItemCount}/{_feedApp.RequiredItemCount}");
            else
                _view.SetTexts("2라운드", $"피드 분류 {_feedApp.Round2SentCount}/{_feedApp.Round2TotalCount}");
        }
    }
}
