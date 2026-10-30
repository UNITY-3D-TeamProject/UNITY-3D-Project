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
    /// 스테이지 목록에 없는 씬(튜토리얼, 로비 등)으로 갈 때 기본 로딩 화면에 쓸 문구.
    /// </summary>
    [Serializable]
    public struct SLoadingMoveText
    {
        [Tooltip("목적지 씬")]
        public ESceneType Scene;

        [Tooltip("\"Loading...\" 대신 쓸 문구 (예: 로비로 가는 중...)")]
        public string Text;
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

        [Header("Move Texts")]
        [Tooltip("스테이지 목록에 없는 씬으로 갈 때 기본 화면 문구. 목록에 없으면 \"Loading...\" 그대로")]
        [SerializeField] private SLoadingMoveText[] _moveTexts;

        [Header("Lobby")]
        [Tooltip("로비로 갈 때 \"현재 스테이지 → 로비\" 경로에 띄울 로비 아이콘. 비우면 로비도 기본 화면")]
        [SerializeField] private Texture2D _lobbyIcon;
        [Tooltip("로비로 갈 때 준비 단계 문구 (위에서부터)")]
        [SerializeField] private string[] _lobbyStepLabels;
        #endregion

        #region Properties
        public IReadOnlyList<SLoadingStageInfo> Stages => _stages ?? Array.Empty<SLoadingStageInfo>();
        public Texture2D LobbyIcon => _lobbyIcon;
        public IReadOnlyList<string> LobbyStepLabels => _lobbyStepLabels ?? Array.Empty<string>();
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

        /// <summary>
        /// 스테이지가 아닌 목적지 씬의 기본 화면 문구를 찾는다.
        /// </summary>
        /// <param name="scene">목적지 씬</param>
        /// <param name="text">찾은 문구</param>
        /// <returns>목록에 있으면 true</returns>
        public bool TryGetMoveText(ESceneType scene, out string text)
        {
            if (_moveTexts != null)
            {
                foreach (SLoadingMoveText moveText in _moveTexts)
                {
                    if (moveText.Scene == scene)
                    {
                        text = moveText.Text;
                        return true;
                    }
                }
            }

            text = null;
            return false;
        }
        #endregion
    }
}
