using System;
using UnityEngine;
using SceneTransition;

namespace Core.Audio
{
    /// <summary>
    /// 씬 하나와 그 씬의 BGM.
    /// </summary>
    [Serializable]
    public struct SBgmEntry
    {
        public ESceneType Scene;

        [Tooltip("비워 두면 이 씬에서는 BGM을 끈다")]
        public SOAudioData Bgm;
    }

    /// <summary>
    /// 씬마다 어떤 BGM을 틀지 모아 둔 표. AudioManager가 씬이 로드될 때 찾아본다.
    /// </summary>
    // 표에 없는 씬은 지금 곡을 그대로 유지한다.
    // MainScene은 ESceneType에 없으므로 메인메뉴 곡은 따로 둔다.
    [CreateAssetMenu(fileName = "SOBgmTable", menuName = "Audio/BgmTable")]
    public class SOBgmTable : ScriptableObject
    {
        #region Serialized Fields
        [Tooltip("MainScene에서 트는 곡. 비워 두면 메인메뉴에서는 BGM을 끈다")]
        [SerializeField] private SOAudioData _mainMenuBgm;

        [SerializeField] private SBgmEntry[] _entries;
        #endregion

        #region Properties
        public SOAudioData MainMenuBgm => _mainMenuBgm;
        #endregion

        #region Public Methods
        /// <summary>
        /// 씬에 등록된 BGM을 찾는다.
        /// </summary>
        /// <param name="scene">찾을 씬</param>
        /// <param name="bgm">등록된 곡. 무음으로 등록했으면 null</param>
        /// <returns>표에 씬이 있으면 true</returns>
        public bool TryGetBgm(ESceneType scene, out SOAudioData bgm)
        {
            if (_entries != null)
            {
                foreach (SBgmEntry entry in _entries)
                {
                    if (entry.Scene == scene)
                    {
                        bgm = entry.Bgm;
                        return true;
                    }
                }
            }

            bgm = null;
            return false;
        }
        #endregion
    }
}
