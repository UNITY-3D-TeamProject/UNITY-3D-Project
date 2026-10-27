using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Core.Stage;

namespace Map.Feed
{
    /// <summary>
    /// 2라운드 시작 콘솔의 앞면 화면.
    /// 시작 전에는 모은 피드 재료 수와 빠진 재료(태그)를 보여 주고(모자라면 잠금, 다 모으면 준비 완료), 2라운드가 시작되면 꺼진다.
    /// </summary>
    public class FeedConsoleLockScreen : MonoBehaviour
    {
        #region Serialized Fields
        [Header("References")]
        [Tooltip("재료 조건과 시작 여부를 읽을 2라운드")]
        [SerializeField] private FeedRound2 _round;

        [Tooltip("시작하면 끌 화면 묶음")]
        [SerializeField] private GameObject _visualRoot;

        [Tooltip("화면 테두리")]
        [SerializeField] private Graphic _frame;

        [Tooltip("진행 링 (Filled 이미지)")]
        [SerializeField] private Image _progressFill;

        [Tooltip("링 가운데 글자 (n / 전체)")]
        [SerializeField] private TMP_Text _countText;

        [Tooltip("윗줄 작은 글자 (UPLOAD LOCKED / READY)")]
        [SerializeField] private TMP_Text _headerText;

        [Tooltip("큰 글자")]
        [SerializeField] private TMP_Text _titleText;

        [Tooltip("빠진 재료 목록과 안내")]
        [SerializeField] private TMP_Text _detailText;

        [Header("Colors")]
        [Tooltip("재료가 모자랄 때의 강조색")]
        [SerializeField] private Color _lockedColor = new Color(1.0f, 0.3f, 0.43f);

        [Tooltip("재료를 다 모았을 때의 강조색")]
        [SerializeField] private Color _readyColor = new Color(0.44f, 0.9f, 1.0f);
        #endregion

        #region Private Fields
        private static readonly string[] ItemNames = { "제목", "해시태그", "노래", "문구", "사진" };

        private readonly StringBuilder _builder = new StringBuilder();
        private FeedApp _feedApp;
        private int _lastCollectedCount = -1;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            Debug.Assert(_round != null, $"[{name}] Round가 연결되지 않았습니다.");
            Debug.Assert(_visualRoot != null, $"[{name}] Visual Root가 연결되지 않았습니다.");
            Debug.Assert(_titleText != null, $"[{name}] Title Text가 연결되지 않았습니다.");
            Debug.Assert(_detailText != null, $"[{name}] Detail Text가 연결되지 않았습니다.");
        }

        private void Start()
        {
            _feedApp = FindAnyObjectByType<FeedApp>();
        }

        private void Update()
        {
            if (_round.IsStarted)
            {
                if (_visualRoot.activeSelf)
                {
                    _visualRoot.SetActive(false);
                }
                return;
            }

            int collectedCount = CountCollected();
            if (collectedCount == _lastCollectedCount) return;

            _lastCollectedCount = collectedCount;
            Refresh(collectedCount);
        }
        #endregion

        #region Private Methods
        private int CountCollected()
        {
            int count = 0;
            foreach (EFeedItemType itemType in _round.RequiredItems)
            {
                if ((_feedApp != null) && _feedApp.HasItem(itemType))
                {
                    count++;
                }
            }
            return count;
        }

        // 모은 수가 바뀌었을 때만 화면 전체를 다시 쓴다
        private void Refresh(int collectedCount)
        {
            int totalCount = _round.RequiredItems.Count;
            bool isReady = collectedCount >= totalCount;
            Color accent = isReady ? _readyColor : _lockedColor;

            if (_frame != null)
            {
                _frame.color = accent;
            }

            if (_progressFill != null)
            {
                _progressFill.fillAmount = (totalCount > 0) ? ((float)collectedCount / totalCount) : 1.0f;
            }

            if (_countText != null)
            {
                _countText.text = $"{collectedCount} / {totalCount}";
            }

            if (_headerText != null)
            {
                _headerText.text = isReady ? "UPLOAD READY" : "UPLOAD LOCKED";
                _headerText.color = accent;
            }

            if (isReady)
            {
                _titleText.text = "게시물 재료를\n모두 모았습니다";
                _detailText.text = "콘솔을 작동해 2라운드를 시작하세요";
                return;
            }

            _titleText.text = "게시물 재료가\n부족합니다";
            _detailText.text = BuildMissingList();
        }

        // 빠진 재료마다 "재료 — #태그 피드" 한 줄 (태그 색), 마지막에 안내 한 줄
        private string BuildMissingList()
        {
            _builder.Clear();

            foreach (EFeedItemType itemType in _round.RequiredItems)
            {
                if ((_feedApp != null) && _feedApp.HasItem(itemType)) continue;

                int index = (int)itemType;
                string itemName = ((index >= 0) && (index < ItemNames.Length)) ? ItemNames[index] : itemType.ToString();
                bool hasWall = (index >= 0) && (index < _round.Walls.Count);

                if (hasWall)
                {
                    SFeedWallConfig wall = _round.Walls[index];
                    _builder.Append("<color=#").Append(ColorUtility.ToHtmlStringRGB(wall.Color)).Append('>')
                        .Append(itemName).Append("</color>  <color=#9A8FB8>— ").Append(wall.Label).Append(" 피드</color>\n");
                }
                else
                {
                    _builder.Append(itemName).Append('\n');
                }
            }

            _builder.Append("<color=#A79DC4>아래 1라운드로 돌아가 모아 오세요</color>");
            return _builder.ToString();
        }
        #endregion
    }
}
