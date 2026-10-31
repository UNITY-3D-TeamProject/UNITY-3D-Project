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
    /// 로딩씬에서 씬 이동마다 틀 컷씬 영상 목록. 튜토리얼 시작 영상은 진행 바 없이, 나머지는 진행 바 전에 재생한다.
    /// 메인 메뉴에서 튜토리얼로 이동할 때는 출발 씬이 없고, 나머지는 (출발 씬, 목적지 씬) 쌍으로 고른다.
    /// </summary>
    [CreateAssetMenu(fileName = "SOLoadingCutsceneData", menuName = "SceneTransition/Loading Cutscene Data")]
    public class SOLoadingCutsceneData : ScriptableObject
    {
        #region Serialized Fields
        [Header("Cutscenes")]
        [Tooltip("메인 메뉴에서 튜토리얼을 시작하기 전에 틀 영상")]
        [SerializeField] private VideoClip _beforeTutorialClip;

        [Tooltip("이동별 컷씬. 같은 이동이 여러 칸이면 위쪽 칸을 쓴다. 목록에 없는 이동은 영상 없이 넘어간다.")]
        [SerializeField] private SLoadingCutscene[] _cutscenes;
        #endregion

        #region Public Methods
        /// <summary>
        /// from → to 이동에 틀 영상을 찾는다. from이 없으면 튜토리얼 시작 영상을 찾는다.
        /// </summary>
        /// <param name="from">출발 씬. 메인 메뉴에서 시작하면 null</param>
        /// <param name="to">목적지 씬</param>
        /// <param name="clip">찾은 영상. 없으면 null</param>
        /// <returns>영상이 있으면 true</returns>
        public bool TryGetClip(ESceneType? from, ESceneType to, out VideoClip clip)
        {
            if (!from.HasValue)
            {
                clip = to == ESceneType.Tutorial ? _beforeTutorialClip : null;
                return clip != null;
            }

            if (_cutscenes != null)
            {
                foreach (SLoadingCutscene cutscene in _cutscenes)
                {
                    if ((cutscene.From == from.Value) &&
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
