using System;
using System.Collections.Generic;
using UnityEngine;

namespace SceneTransition
{
    /// <summary>
    /// 한 스테이지(목적지 씬)로 갈 때 로딩 화면에 보여 줄 앱 정보.
    /// </summary>
    [Serializable]
    public struct SLoadingStageInfo
    {
        [Tooltip("목적지 씬")]
        public ESceneType Scene;

        [Tooltip("접속 문구에 들어갈 앱 이름 (예: 피드 → \"피드 앱에 접속 중...\")")]
        public string AppName;

        [Tooltip("화면 가운데에 띄울 앱 아이콘 (네온 로고)")]
        public Texture2D Icon;

        [Tooltip("진행 바 아래에 하나를 랜덤으로 띄울 팁")]
        [TextArea(1, 3)]
        public string[] Tips;
    }

    /// <summary>
    /// 목적지 씬별 로딩 화면 앱 정보 목록. LoadingStageView가 로딩을 시작할 때 목적지 씬으로 찾는다.
    /// 목록 순서가 로딩 화면의 앱 순서 줄 순서다. 목록에 없는 씬(튜토리얼, 로비 등)은 기본 로딩 화면을 쓴다.
    /// </summary>
    [CreateAssetMenu(fileName = "SOLoadingStageData", menuName = "SceneTransition/Loading Stage Data")]
    public class SOLoadingStageData : ScriptableObject
    {
        #region Serialized Fields
        [Header("Stages")]
        [Tooltip("스테이지별 앱 정보. 이 순서대로 앱 순서 줄에 놓인다. 같은 씬이 여러 칸이면 위쪽 칸을 쓴다.")]
        [SerializeField] private SLoadingStageInfo[] _stages;
        #endregion

        #region Properties
        public IReadOnlyList<SLoadingStageInfo> Stages => _stages ?? Array.Empty<SLoadingStageInfo>();
        #endregion

        #region Public Methods
        /// <summary>
        /// 목적지 씬의 앱 정보와 순서 번호를 찾는다.
        /// </summary>
        /// <param name="scene">목적지 씬</param>
        /// <param name="stage">찾은 앱 정보</param>
        /// <param name="index">목록에서의 순서 번호. 없으면 -1</param>
        /// <returns>목록에 있으면 true</returns>
        public bool TryGetStage(ESceneType scene, out SLoadingStageInfo stage, out int index)
        {
            for (int i = 0; i < Stages.Count; i++)
            {
                if (Stages[i].Scene == scene)
                {
                    stage = Stages[i];
                    index = i;
                    return true;
                }
            }

            stage = default;
            index = -1;
            return false;
        }
        #endregion
    }
}
