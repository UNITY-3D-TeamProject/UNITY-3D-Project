using System.Text;
using UnityEngine;
using TMPro;
using Core.Stage;

namespace Map.Feed
{
    /// <summary>
    /// 2라운드 가운데에 떠 있는 홀로그램 안내판.
    /// 시작 전에는 모은 피드 재료 수(모자라서 시작을 못 하면 경고), 진행 중에는 남은 피드 수와 태그별 남은 수를 보여 주고, 모두 분류하면 사라진다.
    /// </summary>
    public class FeedRound2Counter : MonoBehaviour
    {
        #region Constants
        private const string COLLECTED_COLOR = "#FFFFFF";
        private const string MISSING_COLOR = "#7A6E95";
        private const string WARNING_COLOR = "#FF4D6D";
        #endregion

        #region Serialized Fields
        [Header("References")]
        [Tooltip("남은 피드 수를 읽을 2라운드")]
        [SerializeField] private FeedRound2 _round;

        [Tooltip("분류가 끝나면 끌 겉모습 묶음 (판·글자)")]
        [SerializeField] private GameObject _visualRoot;

        [Tooltip("큰 글자 (남은 피드 N)")]
        [SerializeField] private TMP_Text _titleText;

        [Tooltip("작은 글자 (태그별 남은 수 / 재료 목록)")]
        [SerializeField] private TMP_Text _detailText;

        [Header("Timing")]
        [Tooltip("재료가 모자라 시작하지 못했을 때 경고를 보여 주는 시간(초)")]
        [SerializeField, Min(0f)] private float _warningDuration = 2.5f;
        #endregion

        #region Private Fields
        private static readonly string[] ItemNames = { "제목", "해시태그", "노래", "문구", "사진" };

        private readonly StringBuilder _builder = new StringBuilder();
        private FeedApp _feedApp;
        private float _warningEndTime;
        private string _lastTitle;
        private string _lastDetail;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            Debug.Assert(_round != null, $"[{name}] Round가 연결되지 않았습니다.");
            Debug.Assert(_titleText != null, $"[{name}] Title Text가 연결되지 않았습니다.");
            Debug.Assert(_detailText != null, $"[{name}] Detail Text가 연결되지 않았습니다.");
        }

        private void OnEnable()
        {
            _round.OnStartRefused += HandleStartRefused;
        }

        private void OnDisable()
        {
            _round.OnStartRefused -= HandleStartRefused;
        }

        private void Start()
        {
            _feedApp = FindAnyObjectByType<FeedApp>();
        }

        private void Update()
        {
            if (_round.IsCleared)
            {
                if ((_visualRoot != null) && _visualRoot.activeSelf)
                {
                    _visualRoot.SetActive(false);
                }
                return;
            }

            if (_round.IsStarted)
            {
                ShowRemaining();
            }
            else
            {
                ShowItems();
            }
        }
        #endregion

        #region Private Methods
        private void HandleStartRefused()
        {
            _warningEndTime = Time.time + _warningDuration;
        }

        // 남은 피드 N + 태그별 남은 수 (태그 색)
        private void ShowRemaining()
        {
            _builder.Clear();
            for (int i = 0; i < _round.Walls.Count; i++)
            {
                SFeedWallConfig wall = _round.Walls[i];
                if (i > 0)
                {
                    _builder.Append((i == 3) ? "\n" : "   ");
                }
                _builder.Append("<color=#").Append(ColorUtility.ToHtmlStringRGB(wall.Color)).Append('>')
                    .Append(wall.Label).Append(' ').Append(_round.GetRemainingCount(i)).Append("</color>");
            }

            SetTexts($"남은 피드 {_round.RemainingCount}", _builder.ToString());
        }

        // 피드 재료 n / 전체 + 재료 이름(모은 것은 흰색, 빠진 것은 흐리게). 시작을 거절당한 직후에는 경고를 띄운다.
        private void ShowItems()
        {
            int collectedCount = 0;
            _builder.Clear();

            for (int i = 0; i < _round.RequiredItems.Count; i++)
            {
                EFeedItemType itemType = _round.RequiredItems[i];
                bool hasItem = (_feedApp != null) && _feedApp.HasItem(itemType);
                if (hasItem)
                {
                    collectedCount++;
                }

                if (i > 0)
                {
                    _builder.Append("  ");
                }
                _builder.Append("<color=").Append(hasItem ? COLLECTED_COLOR : MISSING_COLOR).Append('>')
                    .Append(GetItemName(itemType)).Append("</color>");
            }

            string title = (Time.time < _warningEndTime)
                ? $"<color={WARNING_COLOR}>재료가 부족해요!</color>"
                : $"피드 재료 {collectedCount}/{_round.RequiredItems.Count}";

            SetTexts(title, _builder.ToString());
        }

        // 글자가 바뀌었을 때만 다시 그린다
        private void SetTexts(string title, string detail)
        {
            if (title != _lastTitle)
            {
                _lastTitle = title;
                _titleText.text = title;
            }

            if (detail != _lastDetail)
            {
                _lastDetail = detail;
                _detailText.text = detail;
            }
        }

        private static string GetItemName(EFeedItemType itemType)
        {
            int index = (int)itemType;
            return ((index >= 0) && (index < ItemNames.Length)) ? ItemNames[index] : itemType.ToString();
        }
        #endregion
    }
}
