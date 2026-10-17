using UnityEngine;
using Map.Gimmicks;
using SceneTransition;

namespace Map.Lobby
{
    /// <summary>
    /// 로비 앱 플랫폼의 네온 로고(Project/NeonLogo)를 열림/잠김 상태로 켠다.
    /// 포탈이 있으면 SceneLoader 순서상 지금 갈 수 있을 때만 밝게, 아니면 어둡게 깜빡인다.
    /// 포탈을 비워 두면 항상 상호작용 가능한 앱(음악·날씨·캘린더·피드)으로 보고 늘 밝게 둔다.
    /// 로비는 돌아올 때마다 다시 불러오므로 Start에서 한 번만 확인한다.
    /// </summary>
    public class AppPlatformNeon : MonoBehaviour
    {
        #region Constants
        private static readonly int LockedId = Shader.PropertyToID("_Locked");
        #endregion

        #region Serialized Fields
        [Header("Unlock")]
        [Tooltip("이 앱의 씬 포탈. 비워 두면 항상 열린 앱으로 본다.")]
        [SerializeField] private ScenePortal _portal;

        [Header("References")]
        [Tooltip("네온 로고·꺾쇠 렌더러 (4면, Frame, 윗면, 떠 있는 로고)")]
        [SerializeField] private Renderer[] _neonRenderers;
        #endregion

        #region Unity Lifecycle
        private void Start()
        {
            bool isOpen = (_portal == null) || SceneLoader.CanEnter(_portal.Destination);

            var block = new MaterialPropertyBlock();
            foreach (Renderer neonRenderer in _neonRenderers)
            {
                if (neonRenderer == null) continue;

                neonRenderer.GetPropertyBlock(block);
                block.SetFloat(LockedId, isOpen ? 0.0f : 1.0f);
                neonRenderer.SetPropertyBlock(block);
            }
        }
        #endregion
    }
}
