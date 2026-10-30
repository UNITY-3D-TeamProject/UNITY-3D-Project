using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SceneTransition
{
    /// <summary>
    /// 로딩 화면의 준비 단계 한 줄 (체크 표시 + 문구).
    /// </summary>
    [Serializable]
    public struct SLoadingStep
    {
        public Image Check;
        public TMP_Text Label;
    }

    /// <summary>
    /// 스테이지로 갈 때만 켜지는 로딩 화면 부분. 앱 순서 줄(지금 들어가는 앱 강조), "○○ 앱에 접속 중..." 문구, 랜덤 팁, 준비 단계 체크를 보여 준다.
    /// LoadingScreen이 목적지를 알면 TryShow를, 진행 바가 바뀔 때마다 SetProgress를 부른다.
    /// 로딩씬은 이동할 때마다 새로 열리므로, 옮긴 러너 위치 등은 되돌리지 않는다.
    /// </summary>
    public class LoadingStageView : MonoBehaviour
    {
        #region Serialized Fields
        [Header("필수 (SOLoadingStageData ← ScriptableObjects/SceneTransition)")]
        [SerializeField] private SOLoadingStageData _stageData;

        [Header("필수 (UI)")]
        [Tooltip("앱 순서 줄의 아이콘 칸. SO 목록 순서대로 채운다")]
        [SerializeField] private RawImage[] _rowIcons;
        [Tooltip("지금 들어가는 앱 칸으로 옮길 강조 테두리")]
        [SerializeField] private RectTransform _currentFrame;
        [SerializeField] private TMP_Text _connectText;
        [SerializeField] private GameObject _tipGroup;
        [SerializeField] private TMP_Text _tipText;
        [SerializeField] private SLoadingStep[] _steps;

        [Header("스테이지 화면일 때 바꿀 기본 화면")]
        [Tooltip("기본 \"Loading...\" 문구. 스테이지 화면이면 숨기고, 아니면 목적지 문구로 바꾼다")]
        [SerializeField] private TMP_Text _defaultLoadingText;
        [Tooltip("옮길 달리는 플레이어·펫 RawImage")]
        [SerializeField] private RectTransform _runners;
        [SerializeField] private Vector2 _runnersPosition = new Vector2(-720f, -59f);

        [Header("App Row")]
        [SerializeField] private float _currentIconScale = 1.5f;
        [SerializeField] private Color _currentIconColor = Color.white;
        [SerializeField] private Color _otherIconColor = new Color(1f, 1f, 1f, 0.35f);

        [Header("Text")]
        [Tooltip("{0}에 앱 이름이 들어간다")]
        [SerializeField] private string _connectFormat = "{0} 앱에 접속 중...";

        [Header("Step")]
        [SerializeField] private Sprite _doneSprite;
        [SerializeField] private Sprite _pendingSprite;
        [SerializeField] private Color _doneColor = new Color(0.35f, 0.9f, 1f, 1f);
        [SerializeField] private Color _pendingColor = new Color(1f, 1f, 1f, 0.35f);
        [SerializeField] private Color _doneLabelColor = Color.white;
        [SerializeField] private Color _pendingLabelColor = new Color(1f, 1f, 1f, 0.45f);
        #endregion

        #region Public Methods
        /// <summary>
        /// 목적지가 스테이지 목록에 있으면 앱 정보를 채우고 켠다. 없으면 꺼 두고 기본 화면을 그대로 둔다.
        /// </summary>
        /// <param name="scene">목적지 씬</param>
        /// <returns>스테이지 화면을 켰으면 true</returns>
        public bool TryShow(ESceneType scene)
        {
            if ((_stageData == null) || !_stageData.TryGetStage(scene, out SLoadingStageInfo stage, out int stageIndex))
            {
                if ((_stageData != null) &&
                    (_defaultLoadingText != null) &&
                    _stageData.TryGetMoveText(scene, out string moveText))
                {
                    _defaultLoadingText.text = moveText;
                }

                gameObject.SetActive(false);
                return false;
            }

            SetAppRow(stageIndex);

            if (_connectText != null) _connectText.text = string.Format(_connectFormat, stage.AppName);

            bool hasTip = (stage.Tips != null) && (stage.Tips.Length > 0);
            if (_tipGroup != null) _tipGroup.SetActive(hasTip);
            if (hasTip && (_tipText != null)) _tipText.text = stage.Tips[UnityEngine.Random.Range(0, stage.Tips.Length)];

            if (_defaultLoadingText != null) _defaultLoadingText.gameObject.SetActive(false);
            if (_runners != null) _runners.anchoredPosition = _runnersPosition;

            gameObject.SetActive(true);
            SetProgress(0f);
            return true;
        }

        /// <summary>
        /// 진행률에 맞춰 준비 단계를 위에서부터 체크한다. 단계 n개 중 i번째는 진행률 (i + 1) / n 에서 체크된다.
        /// </summary>
        /// <param name="progress">진행률 0~1</param>
        public void SetProgress(float progress)
        {
            if (!gameObject.activeSelf || (_steps == null)) return;

            for (int i = 0; i < _steps.Length; i++)
            {
                bool isDone = progress >= (float)(i + 1) / _steps.Length;
                SetStep(_steps[i], isDone);
            }
        }
        #endregion

        #region Private Methods
        // SO 목록 순서대로 아이콘을 채우고, 지금 들어가는 앱만 크게·밝게 하고 강조 테두리를 옮긴다.
        private void SetAppRow(int currentIndex)
        {
            if (_rowIcons == null) return;

            for (int i = 0; i < _rowIcons.Length; i++)
            {
                RawImage icon = _rowIcons[i];
                if (icon == null) continue;

                bool hasStage = i < _stageData.Stages.Count;
                Texture2D texture = hasStage ? _stageData.Stages[i].Icon : null;
                icon.texture = texture;
                icon.enabled = texture != null;

                bool isCurrent = i == currentIndex;
                icon.color = isCurrent ? _currentIconColor : _otherIconColor;
                icon.rectTransform.localScale = Vector3.one * (isCurrent ? _currentIconScale : 1f);

                if (isCurrent && (_currentFrame != null)) _currentFrame.anchoredPosition = icon.rectTransform.anchoredPosition;
            }
        }

        private void SetStep(SLoadingStep step, bool isDone)
        {
            if (step.Check != null)
            {
                step.Check.sprite = isDone ? _doneSprite : _pendingSprite;
                step.Check.color = isDone ? _doneColor : _pendingColor;
            }

            if (step.Label != null) step.Label.color = isDone ? _doneLabelColor : _pendingLabelColor;
        }
        #endregion
    }
}
