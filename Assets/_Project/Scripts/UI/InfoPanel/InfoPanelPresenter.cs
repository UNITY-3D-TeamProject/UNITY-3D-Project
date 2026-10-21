using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Core.Stage;

namespace UI.InfoPanel
{
    // 피드 앱은 라운드 진행도를, 파일 앱은 지금 라운드 번호와 진행 중인 라운드(숫자 퍼즐, 도둑 잡기)의 진행 상황을 패널에 보여 준다.
    // 둘 다 없거나 스테이지 진행 전이면 패널을 숨긴다.
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

            if ((_fileApp != null) && (_fileApp.CurrentRoundIndex >= 0))
            {
                _view.Show();
                _view.SetTexts($"{_fileApp.CurrentRoundIndex + 1}라운드", BuildFileAppText());
                return;
            }

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

        // 2라운드는 먹은 숫자·연산자, 3라운드는 남은 도둑 수. 로직이 없는 라운드(1라운드)는 비운다.
        private string BuildFileAppText()
        {
            switch (_fileApp.CurrentRoundIndex)
            {
                case FileApp.NUMBER_PUZZLE_ROUND_INDEX:
                    return _fileApp.NumberPuzzleRound.IsReady ? BuildPuzzleText(_fileApp.NumberPuzzleRound) : string.Empty;

                case FileApp.THIEF_CHASE_ROUND_INDEX:
                    return BuildThiefChaseText(_fileApp.ThiefChaseRound);

                default:
                    return string.Empty;
            }
        }

        // 목표값과 가진 숫자·연산자. 예: "목표 12 · 숫자 3 5 _ · 연산 + _"
        private string BuildPuzzleText(NumberPuzzleRound puzzle)
        {
            if (puzzle.IsSolved)
                return "정답! 문이 열렸습니다";

            _builder.Clear();
            _builder.Append("목표 ");
            _builder.Append(puzzle.TargetValue);
            _builder.Append(" · 숫자 ");
            AppendHeld(puzzle.HeldNumbers, puzzle.MaxHeldNumberCount);
            _builder.Append(" · 연산 ");
            AppendHeld(puzzle.HeldOperators, puzzle.MaxHeldOperatorCount);
            return _builder.ToString();
        }

        // 남은 도둑 수와 남은 시간. 예: "남은 도둑 17명 · 남은 시간 45초"
        private static string BuildThiefChaseText(ThiefChaseRound thiefChase)
        {
            if (thiefChase.IsCleared)
                return "도둑을 모두 잡았습니다";

            int remainingSeconds = Mathf.CeilToInt(thiefChase.RemainingSeconds);
            return $"남은 도둑 {thiefChase.TotalCount - thiefChase.CaughtCount}명 · 남은 시간 {remainingSeconds}초";
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
