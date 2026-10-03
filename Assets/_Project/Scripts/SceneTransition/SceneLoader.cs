using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SceneTransition
{
    /// <summary>
    /// 정해진 순서대로만 씬을 전환하고, 전환 직전에 이벤트를 발행한다.
    /// 순서: 튜토리얼 → 로비 → 피드 → 로비 → 파일 → 로비 → 보안 → 로비 → 라이브.
    /// 현재 단계는 처음 사용할 때 활성 씬의 Build Settings 번호로 정한다.
    /// (로비는 순서에 여러 번 나오므로, 로비에서 바로 플레이를 시작하면 첫 번째 로비로 본다)
    /// </summary>
    public static class SceneLoader
    {
        #region Private Fields
        private static readonly ESceneType[] Sequence =
        {
            ESceneType.Tutorial,
            ESceneType.Lobby,
            ESceneType.Feed,
            ESceneType.Lobby,
            ESceneType.File,
            ESceneType.Lobby,
            ESceneType.Security,
            ESceneType.Lobby,
            ESceneType.Live,
        };

        private static int  _step;
        private static bool _hasStep;
        private static bool _isLoading;
        #endregion

        #region Properties
        /// <summary>씬을 불러오는 중인지 여부.</summary>
        public static bool IsLoading => _isLoading;

        /// <summary>
        /// 지금 씬으로 넘어오기 직전에 있던 씬. 플레이를 시작한 뒤 한 번도 전환하지 않았으면 null.
        /// </summary>
        public static ESceneType? PreviousScene { get; private set; }
        #endregion

        #region Events
        /// <summary>
        /// 씬 전환 직전에 (이전 씬, 목적지 씬)과 함께 호출된다.
        /// 플레이어 데이터 저장, 이전 씬 번호 기록 등에 사용한다.
        /// </summary>
        public static event Action<ESceneType, ESceneType> OnBeforeSceneChange;
        #endregion

        #region Public Methods
        /// <summary>
        /// 지금 단계에서 destination으로 넘어갈 수 있는지 확인한다.
        /// </summary>
        /// <param name="destination">가려는 씬</param>
        /// <returns>순서상 다음 씬이면 true</returns>
        public static bool CanEnter(ESceneType destination)
        {
            EnsureStep();

            bool hasNextStep = (_step >= 0) && (_step < Sequence.Length - 1);
            return hasNextStep && (Sequence[_step + 1] == destination);
        }

        /// <summary>
        /// 순서상 다음 씬일 때만 전환 직전 이벤트를 발행하고 씬을 불러온다.
        /// </summary>
        /// <param name="destination">가려는 씬</param>
        /// <returns>전환을 시작했으면 true</returns>
        public static bool TryLoad(ESceneType destination)
        {
            if (_isLoading) return false;

            if (!CanEnter(destination))
            {
                Debug.LogWarning($"[SceneLoader] 지금 단계에서는 {destination} 씬으로 갈 수 없습니다.");
                return false;
            }

            int buildIndex = (int)destination;
            if (!Application.CanStreamedLevelBeLoaded(buildIndex))
            {
                Debug.LogError($"[SceneLoader] Build Settings {buildIndex}번에 {destination} 씬이 등록되지 않았습니다.");
                return false;
            }

            ESceneType previous = Sequence[_step];
            _step++;
            _isLoading = true;
            PreviousScene = previous;

            OnBeforeSceneChange?.Invoke(previous, destination);

            AsyncOperation operation = SceneManager.LoadSceneAsync(buildIndex);
            operation.completed += _ => _isLoading = false;
            return true;
        }
        #endregion

        #region Private Methods
        // 도메인 리로드 없이 플레이 모드에 들어가도 이전 플레이의 값이 남지 않도록 초기화한다.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetState()
        {
            _step = 0;
            _hasStep = false;
            _isLoading = false;
            PreviousScene = null;
            OnBeforeSceneChange = null;
        }

        private static void EnsureStep()
        {
            if (_hasStep) return;

            // 순서에 없는 씬(테스트 씬 등)이면 -1이 되어 어디로도 전환할 수 없다.
            ESceneType current = (ESceneType)SceneManager.GetActiveScene().buildIndex;
            _step = Array.IndexOf(Sequence, current);
            _hasStep = true;
        }
        #endregion
    }
}
