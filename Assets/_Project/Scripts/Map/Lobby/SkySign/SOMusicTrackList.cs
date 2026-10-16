using System;
using UnityEngine;

namespace Map.Lobby.SkySign
{
    /// <summary>
    /// 곡 하나의 표시 정보.
    /// </summary>
    [Serializable]
    public struct SMusicTrack
    {
        public string Title;
        public string Artist;
        [Tooltip("CD 라벨·배경·재생바에 쓰는 대표 색")]
        public Color Color;
        [Tooltip("곡 길이 (초)")]
        public float Duration;
    }

    /// <summary>
    /// 음악 전광판이 차례로 재생하는 (가상) 곡 목록.
    /// </summary>
    [CreateAssetMenu(fileName = "SOMusicTrackList", menuName = "Map/Lobby/Sky Sign Music Track List")]
    public class SOMusicTrackList : ScriptableObject
    {
        #region Serialized Fields
        [SerializeField] private SMusicTrack[] _tracks;
        #endregion

        #region Properties
        public int Count => _tracks.Length;
        #endregion

        #region Public Methods
        public SMusicTrack Get(int index) => _tracks[index];
        #endregion
    }
}
