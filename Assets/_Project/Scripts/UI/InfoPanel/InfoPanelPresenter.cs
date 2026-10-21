using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Core.Stage;

namespace UI.InfoPanel
{
    // 피드 앱은 라운드 진행도를, 파일 앱은 진행 중인 라운드(숫자 퍼즐, 도둑 잡기)의 진행 상황을 패널에 보여 준다.
    // 둘 다 없거나 보여 줄 라운드가 없으면 패널을 숨긴다.
    public class InfoPanelPresenter : MonoBehaviour
    {
        private const string EMPTY_HELD_TEXT = "_";

        [SerializeField] private InfoPanelView _view;

        private FeedApp _feedApp;
        private FileApp _fileApp;
        private readonly StringBuilder _builder = new StringBuilder();

        private void Start()
        {
            if (_view == null)
                return;

            _feedApp = FindAnyObjectByType<FeedApp>();
            if (_feedApp != null)
                _feedApp.OnProgressChanged += Refresh;

            _fileApp = FindAnyObjectByType<FileApp>();
            if (_fileApp != null)
                _fileApp.OnProgressChanged += Refresh;

            Refresh();
        }

        private void OnDestroy()
        {
            if (_feedApp != null)
                _feedApp.OnProgressChanged -= Refresh;

            if (_fileApp != null)
                _fileApp.OnProgressChanged -= Refresh;
        }

        private void Refresh()
        {
            if (_feedApp != null)
            {
                _view.Show();
                RefreshFeedApp();
                return;
            }

            if ((_fileApp != null) && TryRefreshFileApp(_fileApp.ActiveRound))
                return;

            _view.Hide();
        }

        // 1라운드는 모은 재료 수, 2라운드는 벽으로 올려 보낸 피드 수를 보여 준다.
        private void RefreshFeedApp()
        {
            if (_feedApp.CurrentRound == 1)
                _view.SetTexts("1라운드", $"피드 재료 {_feedApp.CollectedItemCount}/{_feedApp.RequiredItemCount}");
            else
                _view.SetTexts("2라운드", $"피드 분류 {_feedApp.Round2SentCount}/{_feedApp.Round2TotalCount}");
        }

        // 보여 줄 라운드면 내용을 채우고 true 를 돌려준다.
        private bool TryRefreshFileApp(StageRound round)
        {
            if (round is NumberPuzzleRound puzzle && puzzle.IsReady)
            {
                _view.Show();
                _view.SetTexts($"{round.RoundNumber}라운드", BuildPuzzleText(puzzle));
                return true;
            }

            if (round is ThiefChaseRound thiefChase)
            {
                _view.Show();
                _view.SetTexts($"{round.RoundNumber}라운드", BuildThiefChaseText(thiefChase));
                return true;
            }

            return false;
        }

        // 목표값과 가진 숫자·연산자. 예: "목표 24 · 숫자 3 5 _ · 연산 + _"
        private string BuildPuzzleText(NumberPuzzleRound puzzle)
        {
            if (puzzle.IsSolved)
                return "정답! 문이 열렸습니다";

            _builder.Clear();
            _builder.Append("목표 ").Append(puzzle.TargetValue);
            _builder.Append(" · 숫자 ");
            AppendHeld(puzzle.HeldNumbers, puzzle.MaxHeldNumberCount);
            _builder.Append(" · 연산 ");
            AppendHeld(puzzle.HeldOperators, puzzle.MaxHeldOperatorCount);
            return _builder.ToString();
        }

        // 남은 시간과 잡은 도둑 수. 예: "남은 시간 45초 · 도둑 3/20"
        private static string BuildThiefChaseText(ThiefChaseRound thiefChase)
        {
            if (thiefChase.IsCleared)
                return "도둑을 모두 잡았습니다";

            int remainingSeconds = Mathf.CeilToInt(thiefChase.RemainingSeconds);
            return $"남은 시간 {remainingSeconds}초 · 도둑 {thiefChase.CaughtCount}/{thiefChase.TotalCount}";
        }

        // 가진 값을 차례로 쓰고, 빈 자리는 "_" 로 채운다.
        private void AppendHeld(IReadOnlyList<SMazeToken> held, int maxCount)
        {
            for (int i = 0; i < maxCount; i++)
            {
                if (i > 0)
                    _builder.Append(' ');

                _builder.Append((i < held.Count) ? held[i].ToDisplayString() : EMPTY_HELD_TEXT);
            }
        }
    }
}
