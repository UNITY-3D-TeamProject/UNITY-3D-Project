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
    /// 메인 메뉴처럼 순서에 없는 씬에서는 TryStartNewGame으로 순서를 처음부터 시작한다.
    /// 전환할 때는 로딩씬을 지금 씬 위에 겹쳐 띄우고, 로딩씬의 LoadingScreen이 지금 씬을 정리한 뒤 목적지 씬을 비동기로 불러온다.
    /// </summary>
    public static class SceneLoader
    {
        #region Constants
        // Build Settings 맨 끝에 등록한 로딩씬 이름.
        // ESceneType에 넣지 않아서 포탈 목적지로 고를 수 없게 한다.
        private const string LOADING_SCENE_NAME = "Loading";
        #endregion

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
        /// <summary>씬을 불러오는 중인지 여부. 로딩씬에 들어간 뒤 목적지 씬 로드가 끝날 때까지 true.</summary>
        public static bool IsLoading => _isLoading;

        /// <summary>
        /// 지금 씬으로 넘어오기 직전에 있던 씬. 플레이를 시작한 뒤 한 번도 전환하지 않았으면 null.
        /// </summary>
        public static ESceneType? PreviousScene { get; private set; }

        /// <summary>
        /// 로딩씬이 불러갈 목적지 씬. 전환 중이 아니면 null.
        /// </summary>
        public static ESceneType? PendingScene { get; private set; }
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
        /// 순서상 다음 씬일 때만 전환 직전 이벤트를 발행하고 로딩씬으로 넘어간다.
        /// 목적지 씬은 로딩씬에서 불러온다.
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

            if (!IsLoadable(destination)) return false;

            ESceneType previous = Sequence[_step];
            _step++;
            _isLoading = true;
            PreviousScene = previous;

            OnBeforeSceneChange?.Invoke(previous, destination);

            // 로딩씬은 지금 씬 위에 겹쳐 띄운다. 지금 씬 정리와 목적지 불러오기는 로딩씬의 LoadingScreen이 한다.
            // _isLoading은 목적지 로드가 끝날 때 푼다.
            PendingScene = destination;
            SceneManager.LoadSceneAsync(LOADING_SCENE_NAME, LoadSceneMode.Additive);
            return true;
        }

        /// <summary>
        /// 새 게임을 시작한다. 순서를 처음(튜토리얼)으로 되돌리고 로딩씬을 거쳐 튜토리얼을 연다.
        /// 메인 메뉴처럼 순서에 없는 씬에서 호출한다. 이전 씬이 없으므로 전환 직전 이벤트는 발행하지 않는다.
        /// </summary>
        /// <returns>전환을 시작했으면 true</returns>
        public static bool TryStartNewGame()
        {
            if (_isLoading) return false;

            ESceneType destination = Sequence[0];
            if (!IsLoadable(destination)) return false;

            _step = 0;
            _hasStep = true;
            _isLoading = true;
            // 출발 씬이 없으므로 컷씬도 없다.
            PreviousScene = null;

            PendingScene = destination;
            SceneManager.LoadSceneAsync(LOADING_SCENE_NAME, LoadSceneMode.Additive);
            return true;
        }

        /// <summary>
        /// 로딩씬에서 호출한다. 기다리던 목적지 씬을 비동기로 불러오기 시작한다.
        /// 활성화를 막아 둔 채로 반환하므로, 호출한 쪽에서 allowSceneActivation을 true로 바꿔야 씬이 넘어간다.
        /// </summary>
        /// <returns>불러오기 작업. 기다리는 목적지가 없으면 null</returns>
        public static AsyncOperation LoadPendingSceneAsync()
        {
            if (!PendingScene.HasValue) return null;

            AsyncOperation operation = SceneManager.LoadSceneAsync((int)PendingScene.Value);
            operation.allowSceneActivation = false;
            operation.completed += _ =>
            {
                _isLoading = false;
                PendingScene = null;
            };
            return operation;
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
            PendingScene = null;
            OnBeforeSceneChange = null;
        }

        // 목적지와 로딩씬이 Build Settings에 등록돼 있는지 확인한다.
        // 로딩씬이 없으면 _isLoading이 풀리지 않고 멈추므로 상태를 바꾸기 전에 확인한다.
        private static bool IsLoadable(ESceneType destination)
        {
            int buildIndex = (int)destination;
            if (!Application.CanStreamedLevelBeLoaded(buildIndex))
            {
                Debug.LogError($"[SceneLoader] Build Settings {buildIndex}번에 {destination} 씬이 등록되지 않았습니다.");
                return false;
            }

            if (!Application.CanStreamedLevelBeLoaded(LOADING_SCENE_NAME))
            {
                Debug.LogError($"[SceneLoader] Build Settings에 {LOADING_SCENE_NAME} 씬이 등록되지 않았습니다.");
                return false;
            }

            return true;
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
