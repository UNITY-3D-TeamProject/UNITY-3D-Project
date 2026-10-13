using System;
using UnityEngine;
using UnityEngine.InputSystem;
using Core;
using Facade.Player;

namespace MapTest
{
    /// <summary>테스트 이동 지점 하나. 번호는 목록 순서대로 1씩 늘어나고, Number 를 넣으면 그 번호부터 다시 센다.</summary>
    [Serializable]
    public struct SMapTestDestination
    {
        [Tooltip("화면 목록에 보일 이름")]
        public string Label;

        [Tooltip("플레이어를 옮길 위치와 방향")]
        public Transform Point;

        [Tooltip("이 지점의 번호. 0이면 바로 앞 지점의 번호 + 1")]
        public int Number;

        [Tooltip("목록에서 이 지점부터 새로 시작할 묶음의 제목. 비우면 앞 지점과 같은 묶음이다.")]
        public string Group;
    }

    /// <summary>
    /// 맵 테스트용 순간 이동. Game 화면에서 숫자 키로 번호를 입력하면 그 번호의 테스트 구역으로 플레이어를 옮긴다.
    /// 두 자리 번호는 숫자를 이어서 누른다(예: 1 → 7 = 17번). 마지막 입력 뒤 잠깐 기다리면 이동한다.
    /// F1 로 번호 목록을 켜고 끈다. 테스트 전용이며 Scenes/Test 의 테스트 씬에서만 쓴다.
    /// </summary>
    public class MapTestTeleporter : MonoBehaviour
    {
        #region Constants
        // 플레이어가 바닥에 파묻히지 않도록 이동 지점에서 띄우는 높이
        private const float HEIGHT_OFFSET = 0.2f;

        // 화면 배치는 이 높이를 기준으로 잡고, 실제 화면 높이에 맞춰 통째로 늘리거나 줄인다
        private const float REFERENCE_HEIGHT = 1080.0f;

        private const float MARGIN = 20.0f;
        private const float PANEL_WIDTH = 540.0f;
        private const float PANEL_PADDING = 16.0f;
        private const float HEADER_HEIGHT = 50.0f;
        private const float GROUP_HEIGHT = 32.0f;
        private const float ROW_HEIGHT = 27.0f;
        private const float NUMBER_WIDTH = 38.0f;
        private const int COLUMN_COUNT = 2;

        private const float TOAST_WIDTH = 420.0f;
        private const float TOAST_HEIGHT = 54.0f;
        private const float TOAST_SHOW_TIME = 2.0f;
        private const float TOAST_FADE_TIME = 0.4f;
        #endregion

        #region Serialized Fields
        [Header("Destinations")]
        [Tooltip("목록의 첫 항목에 붙는 번호. 그 뒤로 1씩 늘어난다.")]
        [SerializeField, Min(0)] private int _startNumber = 1;

        [Tooltip("이동 지점 목록")]
        [SerializeField] private SMapTestDestination[] _destinations;

        [Header("Input")]
        [Tooltip("마지막 숫자를 누른 뒤 이 시간(초) 동안 다음 숫자가 없으면 이동한다.")]
        [SerializeField, Min(0.1f)] private float _confirmDelay = 0.7f;

        [Tooltip("시작할 때 번호 목록을 보일지 여부 (F1 로 켜고 끈다)")]
        [SerializeField] private bool _shouldShowList = true;
        #endregion

        #region Private Fields
        private static readonly Key[] DigitKeys =
        {
            Key.Digit0, Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4,
            Key.Digit5, Key.Digit6, Key.Digit7, Key.Digit8, Key.Digit9,
        };

        private static readonly Key[] NumpadKeys =
        {
            Key.Numpad0, Key.Numpad1, Key.Numpad2, Key.Numpad3, Key.Numpad4,
            Key.Numpad5, Key.Numpad6, Key.Numpad7, Key.Numpad8, Key.Numpad9,
        };

        private static readonly Color PanelColor = new Color(0.06f, 0.08f, 0.11f, 0.86f);
        private static readonly Color PanelLineColor = new Color(1.0f, 1.0f, 1.0f, 0.10f);
        private static readonly Color AccentColor = new Color(0.25f, 0.85f, 1.0f, 1.0f);
        private static readonly Color MatchColor = new Color(0.25f, 0.85f, 1.0f, 0.22f);
        private static readonly Color CurrentColor = new Color(1.0f, 0.78f, 0.25f, 0.20f);
        private static readonly Color ChipColor = new Color(1.0f, 1.0f, 1.0f, 0.08f);
        private static readonly Color TextColor = new Color(0.90f, 0.93f, 0.96f, 1.0f);
        private static readonly Color DimTextColor = new Color(0.90f, 0.93f, 0.96f, 0.35f);
        private static readonly Color FailColor = new Color(1.0f, 0.45f, 0.40f, 1.0f);

        private string[] _numberTexts;
        private int[] _numbers;
        private string _typedDigits = string.Empty;
        private float _lastDigitTime;
        private string _message = string.Empty;
        private float _messageTime = float.NegativeInfinity;
        private bool _isMessageFail;
        private int _currentIndex = -1;

        private GUIStyle _titleStyle;
        private GUIStyle _hintStyle;
        private GUIStyle _groupStyle;
        private GUIStyle _numberStyle;
        private GUIStyle _labelStyle;
        private GUIStyle _toastStyle;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            BuildNumbers();
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return;

            if (keyboard[Key.F1].wasPressedThisFrame)
            {
                _shouldShowList = !_shouldShowList;
            }

            for (int digit = 0; digit < DigitKeys.Length; digit++)
            {
                bool isPressed = keyboard[DigitKeys[digit]].wasPressedThisFrame || keyboard[NumpadKeys[digit]].wasPressedThisFrame;
                if (!isPressed) continue;

                _typedDigits += digit.ToString();
                _lastDigitTime = Time.unscaledTime;
            }

            if (_typedDigits.Length == 0) return;

            // 더 이어 눌러도 목록에 없는 번호가 되면 기다리지 않고 바로 이동한다
            bool hasWaited = (Time.unscaledTime - _lastDigitTime) >= _confirmDelay;
            if (CanGrow(_typedDigits) && !hasWaited) return;

            int number = int.Parse(_typedDigits);
            _typedDigits = string.Empty;
            TeleportTo(number);
        }

        private void OnGUI()
        {
            if (Event.current.type != EventType.Repaint) return;

            SetUpStyles();

            // 화면 높이가 달라도 같은 비율로 보이도록 통째로 늘리거나 줄인다
            float scale = Screen.height / REFERENCE_HEIGHT;
            float screenWidth = Screen.width / scale;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1.0f));

            DrawToast(screenWidth);

            if (_shouldShowList)
            {
                DrawList(screenWidth);
            }
            else
            {
                DrawCollapsedHint(screenWidth);
            }

            GUI.matrix = Matrix4x4.identity;
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// number 번 테스트 구역으로 플레이어를 옮긴다. 없는 번호이거나 플레이어가 없으면 화면에 이유를 표시하고 아무것도 하지 않는다.
        /// </summary>
        /// <param name="number">이동할 구역의 번호 (목록에 표시된 번호)</param>
        /// <returns>이동했으면 true</returns>
        public bool TeleportTo(int number)
        {
            if (_numbers == null)
            {
                BuildNumbers();
            }

            int index = Array.IndexOf(_numbers, number);
            if ((index < 0) || (_destinations[index].Point == null))
            {
                ShowMessage($"{number}번 구역이 없습니다", true);
                return false;
            }

            GameManager gameManager = GameManager.Instance;
            PlayerFacade playerFacade = (gameManager != null) ? gameManager.CurrentPlayerFacade : null;
            CharacterController controller = (playerFacade != null)
                ? playerFacade.transform.root.GetComponentInChildren<CharacterController>()
                : null;

            if (controller == null)
            {
                ShowMessage("플레이어를 찾지 못했습니다", true);
                return false;
            }

            Transform point = _destinations[index].Point;

            // CharacterController가 켜져 있으면 위치를 직접 바꿔도 되돌아갈 수 있어 잠시 끈다.
            controller.enabled = false;
            controller.transform.SetPositionAndRotation(point.position + (Vector3.up * HEIGHT_OFFSET), point.rotation);
            controller.enabled = true;

            _currentIndex = index;
            ShowMessage($"{number}  {_destinations[index].Label}", false);
            return true;
        }
        #endregion

        #region Private Methods
        private int GetCount()
        {
            return (_destinations != null) ? _destinations.Length : 0;
        }

        // 지점마다 번호를 매긴다. Number 가 있는 지점은 그 번호부터 다시 센다.
        private void BuildNumbers()
        {
            int count = GetCount();
            _numbers = new int[count];
            _numberTexts = new string[count];

            int number = _startNumber;
            for (int i = 0; i < count; i++)
            {
                if (_destinations[i].Number > 0)
                {
                    number = _destinations[i].Number;
                }

                _numbers[i] = number;
                _numberTexts[i] = number.ToString();
                number++;
            }
        }

        // typed 뒤에 숫자를 더 눌러서 될 수 있는 번호가 목록에 있는지
        private bool CanGrow(string typed)
        {
            foreach (string numberText in _numberTexts)
            {
                if ((numberText.Length > typed.Length) && numberText.StartsWith(typed, StringComparison.Ordinal)) return true;
            }

            return false;
        }

        private void ShowMessage(string message, bool isFail)
        {
            _message = message;
            _isMessageFail = isFail;
            _messageTime = Time.unscaledTime;
        }

        private void SetUpStyles()
        {
            if (_titleStyle != null) return;

            _titleStyle = CreateStyle(20, FontStyle.Bold, TextAnchor.MiddleLeft, TextColor);
            _hintStyle = CreateStyle(14, FontStyle.Normal, TextAnchor.MiddleRight, DimTextColor);
            _groupStyle = CreateStyle(14, FontStyle.Bold, TextAnchor.MiddleLeft, AccentColor);
            _numberStyle = CreateStyle(15, FontStyle.Bold, TextAnchor.MiddleCenter, TextColor);
            _labelStyle = CreateStyle(15, FontStyle.Normal, TextAnchor.MiddleLeft, TextColor);
            _toastStyle = CreateStyle(22, FontStyle.Bold, TextAnchor.MiddleCenter, TextColor);
        }

        private static GUIStyle CreateStyle(int fontSize, FontStyle fontStyle, TextAnchor alignment, Color color)
        {
            GUIStyle style = new GUIStyle(GUI.skin.label)
            {
                fontSize = fontSize,
                fontStyle = fontStyle,
                alignment = alignment,
                richText = true,
                clipping = TextClipping.Clip,
                wordWrap = false,
                padding = new RectOffset(0, 0, 0, 0),
            };
            style.normal.textColor = color;
            return style;
        }

        private static void FillRect(Rect rect, Color color, float radius)
        {
            GUI.DrawTexture(rect, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0.0f, color, 0.0f, radius);
        }

        private static void DrawText(Rect rect, string text, GUIStyle style, Color color)
        {
            style.normal.textColor = color;
            GUI.Label(rect, text, style);
        }

        // 화면 위쪽 가운데: 입력 중인 번호, 또는 방금 이동한 결과
        private void DrawToast(float screenWidth)
        {
            bool isTyping = _typedDigits.Length > 0;
            float messageAge = Time.unscaledTime - _messageTime;
            if (!isTyping && (messageAge > (TOAST_SHOW_TIME + TOAST_FADE_TIME))) return;

            float alpha = isTyping ? 1.0f : Mathf.Clamp01(1.0f - ((messageAge - TOAST_SHOW_TIME) / TOAST_FADE_TIME));
            Rect rect = new Rect((screenWidth - TOAST_WIDTH) * 0.5f, MARGIN, TOAST_WIDTH, TOAST_HEIGHT);

            Color panel = PanelColor;
            panel.a *= alpha;
            FillRect(rect, panel, TOAST_HEIGHT * 0.5f);

            Color accent = (!isTyping && _isMessageFail) ? FailColor : AccentColor;
            accent.a = alpha;
            FillRect(new Rect(rect.x + 18.0f, rect.y + 21.0f, 12.0f, 12.0f), accent, 6.0f);

            Color text = TextColor;
            text.a = alpha;
            string content = isTyping ? $"이동  <color=#40D9FF>{_typedDigits}</color>_" : _message;
            DrawText(new Rect(rect.x + 40.0f, rect.y, rect.width - 60.0f, rect.height), content, _toastStyle, text);
        }

        // 목록을 접었을 때 오른쪽 위에 남기는 안내
        private void DrawCollapsedHint(float screenWidth)
        {
            Rect rect = new Rect(screenWidth - 150.0f - MARGIN, MARGIN, 150.0f, 34.0f);
            FillRect(rect, PanelColor, 17.0f);
            _hintStyle.alignment = TextAnchor.MiddleCenter;
            DrawText(rect, "<b>F1</b>  이동 목록", _hintStyle, TextColor);
            _hintStyle.alignment = TextAnchor.MiddleRight;
        }

        private void DrawList(float screenWidth)
        {
            int count = GetCount();
            float innerWidth = PANEL_WIDTH - (PANEL_PADDING * 2.0f);
            float columnWidth = innerWidth / COLUMN_COUNT;

            Rect panel = new Rect(screenWidth - PANEL_WIDTH - MARGIN, MARGIN, PANEL_WIDTH, GetListHeight());
            FillRect(panel, PanelColor, 14.0f);

            // 머리말
            float x = panel.x + PANEL_PADDING;
            float y = panel.y;
            FillRect(new Rect(x, y + 16.0f, 4.0f, 18.0f), AccentColor, 2.0f);
            DrawText(new Rect(x + 12.0f, y, innerWidth, HEADER_HEIGHT), "테스트 구역 이동", _titleStyle, TextColor);
            DrawText(new Rect(x, y, innerWidth, HEADER_HEIGHT), "숫자 키 입력  ·  <b>F1</b> 접기", _hintStyle, DimTextColor);
            y += HEADER_HEIGHT;
            FillRect(new Rect(x, y - 1.0f, innerWidth, 1.0f), PanelLineColor, 0.0f);

            int column = 0;
            for (int i = 0; i < count; i++)
            {
                bool isGroupStart = (i == 0) || !string.IsNullOrEmpty(_destinations[i].Group);
                if (isGroupStart)
                {
                    // 앞 묶음의 마지막 줄이 덜 찼으면 줄을 넘긴다
                    if (column != 0)
                    {
                        column = 0;
                        y += ROW_HEIGHT;
                    }

                    string group = string.IsNullOrEmpty(_destinations[i].Group) ? "구역" : _destinations[i].Group;
                    DrawText(new Rect(x, y + 6.0f, innerWidth, GROUP_HEIGHT - 6.0f), group, _groupStyle, AccentColor);
                    y += GROUP_HEIGHT;
                }

                DrawRow(new Rect(x + (column * columnWidth), y, columnWidth - 6.0f, ROW_HEIGHT - 3.0f), i);

                column++;
                if (column >= COLUMN_COUNT)
                {
                    column = 0;
                    y += ROW_HEIGHT;
                }
            }
        }

        private void DrawRow(Rect rect, int index)
        {
            bool isTyping = _typedDigits.Length > 0;
            bool isMatch = isTyping && _numberTexts[index].StartsWith(_typedDigits, StringComparison.Ordinal);

            if (isMatch)
            {
                FillRect(rect, MatchColor, 6.0f);
            }
            else if (index == _currentIndex)
            {
                FillRect(rect, CurrentColor, 6.0f);
            }

            // 입력 중에는 후보가 아닌 줄을 흐리게 한다
            Color text = (isTyping && !isMatch) ? DimTextColor : TextColor;

            Rect numberRect = new Rect(rect.x + 2.0f, rect.y + 2.0f, NUMBER_WIDTH, rect.height - 4.0f);
            FillRect(numberRect, isMatch ? AccentColor : ChipColor, 5.0f);
            DrawText(numberRect, _numberTexts[index], _numberStyle, isMatch ? Color.black : text);

            Rect labelRect = new Rect(numberRect.xMax + 8.0f, rect.y, rect.width - NUMBER_WIDTH - 12.0f, rect.height);
            DrawText(labelRect, _destinations[index].Label, _labelStyle, text);
        }

        private float GetListHeight()
        {
            float height = HEADER_HEIGHT + PANEL_PADDING;
            int column = 0;

            for (int i = 0; i < GetCount(); i++)
            {
                bool isGroupStart = (i == 0) || !string.IsNullOrEmpty(_destinations[i].Group);
                if (isGroupStart)
                {
                    if (column != 0)
                    {
                        column = 0;
                        height += ROW_HEIGHT;
                    }

                    height += GROUP_HEIGHT;
                }

                column++;
                if (column >= COLUMN_COUNT)
                {
                    column = 0;
                    height += ROW_HEIGHT;
                }
            }

            if (column != 0)
            {
                height += ROW_HEIGHT;
            }

            return height;
        }
        #endregion
    }
}
