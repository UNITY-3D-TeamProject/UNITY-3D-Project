using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Core.Stage;
using Core.Stage.Security;

namespace UI.InfoPanel
{
    // 피드 앱은 라운드 진행도를, 파일 앱은 지금 라운드 번호와 진행 중인 라운드(숫자 퍼즐, 도둑 잡기)의 진행 상황을,
    // 보안 앱은 지금 라운드 번호와 지금 구역·발판 번호, 남은 적 수를 패널에 보여 준다.
    // 셋 다 없거나 스테이지 진행 전이면 패널을 숨긴다.
    // 도둑 잡기 라운드의 남은 시간은 화면 위 가운데 타이머 패널에 따로 보여 준다.
    public class InfoPanelPresenter : MonoBehaviour
    {
        private const string EMPTY_HELD_TEXT = "_";
        // 밑줄끼리 붙어 보이지 않도록 칸 사이를 두 칸 띄운다.
        private const string HELD_SEPARATOR = "  ";

        [SerializeField] private InfoPanelView _view;
        [SerializeField] private RoundTimerView _timerView;

        private FeedApp _feedApp;
        private FileApp _fileApp;
        private SecurityApp _securityApp;
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

            _securityApp = FindAnyObjectByType<SecurityApp>();
            if (_securityApp != null)
                _securityApp.OnProgressChanged += Refresh;

            Refresh();
        }

        private void OnDestroy()
        {
            if (_feedApp != null)
                _feedApp.OnProgressChanged -= Refresh;

            if (_fileApp != null)
                _fileApp.OnProgressChanged -= Refresh;

            if (_securityApp != null)
                _securityApp.OnProgressChanged -= Refresh;
        }

        private void Refresh()
        {
            RefreshTimer();

            if (_feedApp != null)
            {
                _view.Show();
                RefreshFeedApp();
                return;
            }

            if ((_fileApp != null) && (_fileApp.CurrentRoundIndex >= 0))
            {
                _view.Show();
                RefreshFileApp();
                return;
            }

            if ((_securityApp != null) && (_securityApp.CurrentRoundIndex >= 0))
            {
                _view.Show();
                RefreshSecurityApp();
                return;
            }

            _view.Hide();
        }

        // 1라운드는 구역, 2라운드는 발판 번호와 남은 적 수, 3라운드는 보스 안내.
        private void RefreshSecurityApp()
        {
            string title = $"{_securityApp.CurrentRoundIndex + 1}라운드";

            switch (_securityApp.CurrentRoundIndex)
            {
                case SecurityApp.MOB_ROOM_ROUND_INDEX:
                    MobRoomRound mobRoom = _securityApp.MobRoomRound;
                    RefreshZone(
                        title,
                        "구역",
                        mobRoom.IsCleared,
                        mobRoom.CurrentRoomNumber,
                        mobRoom.RoomCount,
                        mobRoom.RemainingEnemyCount);
                    return;

                case SecurityApp.SPECIAL_MOB_ROUND_INDEX:
                    SpecialMobRound specialMob = _securityApp.SpecialMobRound;
                    RefreshZone(
                        title,
                        "발판",
                        specialMob.IsCleared,
                        specialMob.CurrentStepNumber,
                        specialMob.StepCount,
                        specialMob.RemainingEnemyCount);
                    return;

                case SecurityApp.BOSS_ROUND_INDEX:
                    _view.SetTexts(title, _securityApp.BossRound.IsCleared ? "탈출 문이 열렸습니다" : "보스를 쓰러뜨리세요");
                    return;

                default:
                    _view.SetTexts(title, string.Empty);
                    return;
            }
        }

        // 둘째 줄은 지금 구역 번호, 셋째 줄은 남은 적 수. 예: "구역 2/4" / "남은 적 5명"
        private void RefreshZone(string title, string zoneLabel, bool isCleared, int currentNumber, int totalCount, int remainingEnemyCount)
        {
            if (isCleared)
            {
                _view.SetTexts(title, "모든 구역을 해제했습니다");
                return;
            }

            _view.SetTexts(title, $"{zoneLabel} {currentNumber}/{totalCount}", $"남은 적 {remainingEnemyCount}명");
        }

        // 1라운드는 모은 재료 수, 2라운드는 벽으로 올려 보낸 피드 수를 셋째 줄에 보여 준다. 예: "피드 재료 모으기" / "3/5"
        private void RefreshFeedApp()
        {
            if (_feedApp.CurrentRound == 1)
                _view.SetTexts("1라운드", "피드 재료 모으기", $"{_feedApp.CollectedItemCount}/{_feedApp.RequiredItemCount}");
            else
                _view.SetTexts("2라운드", "피드 분류", $"{_feedApp.Round2SentCount}/{_feedApp.Round2TotalCount}");
        }

        // 2라운드는 목표값과 먹은 숫자·연산자, 3라운드는 남은 도둑 수. 로직이 없는 라운드(1라운드)는 비운다.
        private void RefreshFileApp()
        {
            string title = $"{_fileApp.CurrentRoundIndex + 1}라운드";

            switch (_fileApp.CurrentRoundIndex)
            {
                case FileApp.NUMBER_PUZZLE_ROUND_INDEX:
                    RefreshPuzzle(title, _fileApp.NumberPuzzleRound);
                    return;

                case FileApp.THIEF_CHASE_ROUND_INDEX:
                    _view.SetTexts(title, BuildThiefChaseText(_fileApp.ThiefChaseRound));
                    return;

                default:
                    _view.SetTexts(title, string.Empty);
                    return;
            }
        }

        // 둘째 줄은 목표값, 셋째 줄은 가진 숫자·연산자. 예: "목표 : 12" / "숫자 : 3  5  _    연산자 : +  _"
        private void RefreshPuzzle(string title, NumberPuzzleRound puzzle)
        {
            if (!puzzle.IsReady)
            {
                _view.SetTexts(title, string.Empty);
                return;
            }

            if (puzzle.IsSolved)
            {
                _view.SetTexts(title, "정답! 문이 열렸습니다");
                return;
            }

            _builder.Clear();
            _builder.Append("숫자 : ");
            AppendHeld(puzzle.HeldNumbers, puzzle.MaxHeldNumberCount);
            _builder.Append("    연산자 : ");
            AppendHeld(puzzle.HeldOperators, puzzle.MaxHeldOperatorCount);
            _view.SetTexts(title, $"목표 : {puzzle.TargetValue}", _builder.ToString());
        }

        // 남은 도둑 수. 예: "남은 도둑 17명". 남은 시간은 타이머 패널에 보여 준다.
        private static string BuildThiefChaseText(ThiefChaseRound thiefChase)
        {
            if (thiefChase.IsCleared)
                return "도둑을 모두 잡았습니다";

            return $"남은 도둑 {thiefChase.TotalCount - thiefChase.CaughtCount}명";
        }

        // 도둑 잡기 라운드 진행 중에만 남은 시간을 보여 준다. 예: "남은시간 : 45초"
        private void RefreshTimer()
        {
            if (_timerView == null)
                return;

            bool isThiefChasing = (_fileApp != null) &&
                (_fileApp.CurrentRoundIndex == FileApp.THIEF_CHASE_ROUND_INDEX) &&
                (!_fileApp.ThiefChaseRound.IsCleared);

            if (!isThiefChasing)
            {
                _timerView.Hide();
                return;
            }

            _timerView.Show();
            _timerView.SetText($"남은시간 : {Mathf.CeilToInt(_fileApp.ThiefChaseRound.RemainingSeconds)}초");
        }

        // 가진 값을 차례로 쓰고, 빈 자리는 "_" 로 채운다.
        private void AppendHeld(IReadOnlyList<SMazeToken> held, int maxCount)
        {
            for (int i = 0; i < maxCount; i++)
            {
                if (i > 0)
                    _builder.Append(HELD_SEPARATOR);

                _builder.Append((i < held.Count) ? held[i].ToDisplayString() : EMPTY_HELD_TEXT);
            }
        }
    }
}
