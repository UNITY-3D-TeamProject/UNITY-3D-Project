using System;
using UnityEngine;

namespace Map.Lobby.SkySign
{
    /// <summary>
    /// 알림 하나의 내용.
    /// </summary>
    [Serializable]
    public struct SNotificationEntry
    {
        public string AppName;
        public Sprite Icon;
        public Color Color;
        [TextArea] public string Message;
    }

    /// <summary>
    /// 알림 전광판이 무작위로 골라 띄우는 알림 목록.
    /// </summary>
    [CreateAssetMenu(fileName = "SONotificationList", menuName = "Map/Lobby/Sky Sign Notification List")]
    public class SONotificationList : ScriptableObject
    {
        #region Serialized Fields
        [SerializeField] private SNotificationEntry[] _entries;
        #endregion

        #region Properties
        public int Count => _entries.Length;
        #endregion

        #region Public Methods
        public SNotificationEntry Get(int index) => _entries[index];
        #endregion
    }
}
