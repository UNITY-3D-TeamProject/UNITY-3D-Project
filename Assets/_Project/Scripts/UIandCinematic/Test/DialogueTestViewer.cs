using System;
using UIandCinematic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace UIandCinematic.Test
{
    /// <summary>원본 컷신과 자동 안내를 독립 씬에서 확인하는 테스트 전용 뷰어.</summary>
    public class DialogueTestViewer : MonoBehaviour
    {
        #region Serialized Fields
        [SerializeField] private SOCutsceneTable _cutscenes;
        [SerializeField] private SOMonologueTable _monologues;
        #endregion

        #region Private Fields
        private struct SEntry
        {
            public string Group;
            public string Label;
            public int Id;
            public bool IsMonologue;

            public SEntry(string group, string label, int id, bool isMonologue = false)
            {
                Group = group;
                Label = label;
                Id = id;
                IsMonologue = isMonologue;
            }
        }

        private static readonly Key[] DigitKeys = { Key.Digit0, Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4, Key.Digit5, Key.Digit6, Key.Digit7, Key.Digit8, Key.Digit9 };
        private static readonly Key[] NumpadKeys = { Key.Numpad0, Key.Numpad1, Key.Numpad2, Key.Numpad3, Key.Numpad4, Key.Numpad5, Key.Numpad6, Key.Numpad7, Key.Numpad8, Key.Numpad9 };

        private static readonly SEntry[] Entries =
        {
            new("튜토리얼", "눈을 뜨다", 1), new("튜토리얼", "펫과 첫 만남", 2),
            new("튜토리얼", "소각장과 발판", 3), new("튜토리얼", "원거리 상호작용", 4),
            new("튜토리얼", "대시 안내", 9, true), new("튜토리얼", "2단 점프", 8, true),
            new("튜토리얼", "배터리 안내", 10, true), new("튜토리얼", "과열 안내 (등록)", 5),
            new("튜토리얼", "음악 소리 안내", 1, true), new("튜토리얼", "음악 파일 접근 안내", 16, true),
            new("튜토리얼", "음악 파일 접근 (등록)", 6), new("튜토리얼", "음악 파일", 7),
            new("튜토리얼", "스캔 안내", 8), new("튜토리얼", "데이터 지우개", 9),
            new("튜토리얼", "탈것 안내", 11, true), new("튜토리얼", "탈것 이동", 12, true),
            new("튜토리얼", "보드 점프", 13, true), new("튜토리얼", "보드 2단 점프", 14, true),
            new("튜토리얼", "보드 활공", 15, true), new("튜토리얼", "출구", 10),
            new("로비 · 피드 전", "튜토리얼 귀환", 11), new("로비 · 피드 전", "피드 진입 전", 12),
            new("피드", "피드 진입", 13), new("피드", "첫 악성 피드", 14),
            new("피드", "상호작용 안내", 2, true), new("피드", "풍선 안내", 3, true),
            new("피드", "타이밍 안내", 4, true), new("피드", "카드 안내", 5, true),
            new("피드", "다른 그림 찾기", 6, true), new("피드", "2구역 도착", 15),
            new("피드", "피드 정리 완료", 16), new("피드", "업로드 완료", 17),
            new("로비 · 파일 전", "피드 귀환", 18), new("로비 · 파일 전", "파일 진입 전", 19),
            new("파일", "파일 진입", 20), new("파일", "2라운드 전", 21),
            new("파일", "3라운드 전", 22), new("파일", "파일 회수", 23),
            new("파일", "파일 업로드 카메라", 34),
            new("로비 · 보안 전", "파일 귀환", 24), new("로비 · 보안 전", "보안 진입 전", 25),
            new("보안", "보안 진입", 26), new("보안", "보스 구역", 27), new("보안", "보스 처치", 28),
            new("로비 · 라이브 전", "보안 귀환", 29), new("로비 · 라이브 전", "라이브 진입 전", 30),
            new("라이브", "라이브 진입", 31), new("라이브", "TV 앞", 32),
            new("라이브", "무대 화면 카메라", 33), new("라이브", "공연 이후", 35),
            new("추가 등록 안내", "과열 안내 (자동)", 7, true),
        };

        private string _digits = string.Empty;
        private float _lastDigitTime;
        private bool _showList = true;
        private int _selected = -1;
        private int _step;
        private Vector2 _scroll;
        private Font _font;
        private GUIStyle _title;
        private GUIStyle _text;
        private GUIStyle _small;
        private GUIStyle _button;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            _font = Font.CreateDynamicFontFromOSFont(new[] { "Malgun Gothic", "Arial" }, 20);
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return;
            if (keyboard.f1Key.wasPressedThisFrame) _showList = !_showList;
            if (keyboard.eKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame) Next();
            for (int i = 0; i < 10; i++)
            {
                if (!keyboard[DigitKeys[i]].wasPressedThisFrame &&
                    !keyboard[NumpadKeys[i]].wasPressedThisFrame) continue;
                _digits += i.ToString();
                _lastDigitTime = Time.unscaledTime;
            }
            if (_digits.Length == 0) return;
            bool canGrow = false;
            for (int i = 1; i <= Entries.Length; i++)
            {
                string number = i.ToString();
                if ((number.Length > _digits.Length) && number.StartsWith(_digits, StringComparison.Ordinal)) canGrow = true;
            }
            if (canGrow && (Time.unscaledTime - _lastDigitTime < 0.7f)) return;
            if (int.TryParse(_digits, out int selected)) Select(selected - 1);
            _digits = string.Empty;
        }

        private void OnGUI()
        {
            SetupStyles();
            float scale = Screen.height / 1080.0f;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1.0f));
            float width = Screen.width / scale;
            GUI.Box(new Rect(20, 20, 650, 72), GUIContent.none);
            GUI.Label(new Rect(40, 30, 610, 28), "대사 테스트", _title);
            GUI.Label(new Rect(40, 61, 610, 22), "숫자 선택 · E / Space 다음 · F1 목록", _small);
            if (_digits.Length > 0) GUI.Label(new Rect(40, 100, 400, 30), "입력: " + _digits, _text);
            DrawDialogue(width);
            if (_showList) DrawList(width);
            GUI.matrix = Matrix4x4.identity;
        }
        #endregion

        #region Private Methods
        private void Select(int index)
        {
            if ((index < 0) || (index >= Entries.Length)) return;
            _selected = index;
            _step = 0;
        }

        private void Next()
        {
            if (_selected < 0) return;
            SEntry entry = Entries[_selected];
            int count = 0;
            if (entry.IsMonologue && _monologues != null && _monologues.TryGetMonologue(entry.Id, out SOMonologue mono)) count = mono.LineCount;
            if (!entry.IsMonologue && _cutscenes != null && _cutscenes.TryGetCutscene(entry.Id, out SOCutscene cutscene)) count = cutscene.StepCount;
            if (_step < count) _step++;
        }

        private void DrawDialogue(float width)
        {
            Rect panel = new Rect(20, 750, Mathf.Min(980, width - 40), 285);
            GUI.Box(panel, GUIContent.none);
            if (_selected < 0)
            {
                GUI.Label(new Rect(panel.x + 25, panel.y + 30, panel.width - 50, 100), "오른쪽 목록에서 번호를 입력하거나 항목을 선택하세요.", _text);
                return;
            }
            SEntry entry = Entries[_selected];
            GUI.Label(new Rect(panel.x + 25, panel.y + 15, panel.width - 50, 30), $"{_selected + 1}. {entry.Group} / {entry.Label}", _title);
            string speaker = "";
            string message = "대화가 끝났습니다. 다른 번호를 선택하세요.";
            string camera = "";
            if (entry.IsMonologue && _monologues != null && _monologues.TryGetMonologue(entry.Id, out SOMonologue mono) && mono.TryGetLine(_step, out SMonologueLine line))
            {
                speaker = line.SpeakerName + " · 자동 안내";
                message = line.Message;
            }
            else if (!entry.IsMonologue && _cutscenes != null && _cutscenes.TryGetCutscene(entry.Id, out SOCutscene cutscene) && cutscene.TryGetStep(_step, out SCutsceneStep step))
            {
                if (step.Type == ECutsceneStepType.Line)
                {
                    speaker = step.SpeakerName;
                    message = step.Message;
                    camera = string.IsNullOrEmpty(step.CameraKey) ? "카메라: 화자 기본 구도" : "카메라: " + step.CameraKey;
                }
                else
                {
                    speaker = "카메라 연출";
                    message = "";
                    if (step.CameraShot != null)
                    {
                        for (int i = 0; i < step.CameraShot.ShotCount; i++)
                            if (step.CameraShot.TryGetShot(i, out SCameraShot shot)) message += $"{shot.CameraKey}  ({shot.Duration:0.##}초)\n";
                    }
                    if (message.Length == 0) message = "카메라 샷이 지정되지 않았습니다.";
                    camera = "실제 맵과 동적 연출은 이 화면에서 재생되지 않습니다.";
                }
            }
            GUI.Label(new Rect(panel.x + 25, panel.y + 57, panel.width - 50, 30), speaker, _title);
            GUI.Label(new Rect(panel.x + 25, panel.y + 95, panel.width - 50, 115), message, _text);
            GUI.Label(new Rect(panel.x + 25, panel.y + 235, panel.width - 50, 30), camera, _small);
        }

        private void DrawList(float width)
        {
            Rect area = new Rect(Mathf.Max(690, width - 800), 20, 780, 1015);
            GUI.Box(area, GUIContent.none);
            GUI.Label(new Rect(area.x + 18, area.y + 12, 740, 32), "씬별 대사 목록", _title);
            GUI.Label(new Rect(area.x + 18, area.y + 46, 740, 25), "숫자 키 또는 클릭 · 스크롤로 전체 확인", _small);
            GUILayout.BeginArea(new Rect(area.x + 15, area.y + 77, area.width - 30, area.height - 90));
            _scroll = GUILayout.BeginScrollView(_scroll);
            string group = "";
            for (int i = 0; i < Entries.Length; i++)
            {
                if (group != Entries[i].Group)
                {
                    group = Entries[i].Group;
                    GUILayout.Space(8);
                    GUILayout.Label(group, _title);
                }
                if (GUILayout.Button($"{i + 1:00}   {Entries[i].Label}" + (Entries[i].IsMonologue ? "  · 자동" : ""), _button, GUILayout.Height(30))) Select(i);
            }
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private void SetupStyles()
        {
            if (_text != null) return;
            _title = new GUIStyle(GUI.skin.label) { font = _font, fontSize = 22, fontStyle = FontStyle.Bold, normal = { textColor = new Color(0.25f, 0.85f, 1f) } };
            _text = new GUIStyle(GUI.skin.label) { font = _font, fontSize = 23, wordWrap = true, normal = { textColor = Color.white } };
            _small = new GUIStyle(GUI.skin.label) { font = _font, fontSize = 16, normal = { textColor = new Color(0.75f, 0.82f, 0.88f) } };
            _button = new GUIStyle(GUI.skin.button) { font = _font, fontSize = 17, alignment = TextAnchor.MiddleLeft, padding = new RectOffset(12, 4, 0, 0) };
        }
        #endregion
    }
}
