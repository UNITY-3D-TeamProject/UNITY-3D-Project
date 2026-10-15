using System;
using UnityEngine;
using UnityEngine.Video;

namespace SceneTransition
{
    /// <summary>
    /// 한 번의 씬 이동(출발 씬 → 목적지 씬)에 틀 컷씬 영상.
    /// </summary>
    [Serializable]
    public struct SLoadingCutscene
    {
        [Tooltip("출발 씬")]
        public ESceneType From;

        [Tooltip("목적지 씬")]
        public ESceneType To;

        [Tooltip("이 이동에서 틀 영상")]
        public VideoClip Clip;
    }

    /// <summary>
    /// 로딩씬에서 씬 이동마다 틀 컷씬 영상 목록. LoadingScreen이 로딩을 다 끝낸 뒤 목적지 씬을 켜기 전에 재생한다.
    /// 로비는 순서에 여러 번 나오므로 목적지만이 아니라 (출발 씬, 목적지 씬) 쌍으로 고른다.
    /// </summary>
    [CreateAssetMenu(fileName = "SOLoadingCutsceneData", menuName = "SceneTransition/Loading Cutscene Data")]
    public class SOLoadingCutsceneData : ScriptableObject
    {
        #region Serialized Fields
        [Header("Cutscenes")]
        [Tooltip("이동별 컷씬. 같은 이동이 여러 칸이면 위쪽 칸을 쓴다. 목록에 없는 이동은 영상 없이 넘어간다.")]
        [SerializeField] private SLoadingCutscene[] _cutscenes;
        #endregion

        #region Public Methods
        /// <summary>
        /// from → to 이동에 틀 영상을 찾는다.
        /// </summary>
        /// <param name="from">출발 씬</param>
        /// <param name="to">목적지 씬</param>
        /// <param name="clip">찾은 영상. 없으면 null</param>
        /// <returns>영상이 있으면 true</returns>
        public bool TryGetClip(ESceneType from, ESceneType to, out VideoClip clip)
        {
            if (_cutscenes != null)
            {
                foreach (SLoadingCutscene cutscene in _cutscenes)
                {
                    if ((cutscene.From == from) &&
                        (cutscene.To == to) &&
                        (cutscene.Clip != null))
                    {
                        clip = cutscene.Clip;
                        return true;
                    }
                }
            }

            clip = null;
            return false;
        }
        #endregion
    }
}
