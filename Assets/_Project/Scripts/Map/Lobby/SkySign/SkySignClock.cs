using System;
using TMPro;
using UnityEngine;

namespace Map.Lobby.SkySign
{
    /// <summary>
    /// 시계 글자들의 표시 형식.
    /// </summary>
    public enum EClockFormat
    {
        Hour12Minute,   // 3:38
        Hour24Minute,   // 15:38
        DateKorean,     // 10월 16일 금요일
    }

    /// <summary>
    /// 시계 글자 하나와 그 표시 형식.
    /// </summary>
    [Serializable]
    public struct SClockText
    {
        public TMP_Text Text;
        public EClockFormat Format;
    }

    /// <summary>
    /// 전광판 화면의 시각·날짜 글자를 현재 시각으로 갱신한다. 초가 바뀔 때만 다시 쓴다.
    /// </summary>
    public class SkySignClock : MonoBehaviour
    {
        #region Constants
        private const string DAY_NAMES = "일월화수목금토";
        #endregion

        #region Serialized Fields
        [Tooltip("갱신할 글자와 형식 목록")]
        [SerializeField] private SClockText[] _texts;
        #endregion

        #region Private Fields
        private int _lastSecond = -1;
        #endregion

        #region Unity Lifecycle
        private void Update()
        {
            DateTime now = DateTime.Now;
            if (now.Second == _lastSecond) return;
            _lastSecond = now.Second;

            foreach (SClockText clockText in _texts)
            {
                if (clockText.Text == null) continue;
                clockText.Text.text = Format(now, clockText.Format);
            }
        }
        #endregion

        #region Private Methods
        private static string Format(DateTime time, EClockFormat format)
        {
            switch (format)
            {
                case EClockFormat.Hour12Minute:
                    int hour12 = time.Hour % 12 == 0 ? 12 : time.Hour % 12;
                    return $"{hour12}:{time.Minute:00}";
                case EClockFormat.Hour24Minute:
                    return $"{time.Hour:00}:{time.Minute:00}";
                case EClockFormat.DateKorean:
                    return $"{time.Month}월 {time.Day}일 {DAY_NAMES[(int)time.DayOfWeek]}요일";
                default:
                    return string.Empty;
            }
        }
        #endregion
    }
}
