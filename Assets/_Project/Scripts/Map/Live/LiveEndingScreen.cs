using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Video;
using Unity.Cinemachine;
using UIandCinematic;

namespace Map.Live
{
    /// <summary>
    /// 라이브 엔딩 스크린. 처음에는 하얀 화면만 띄우고, Play를 부르면 스크린을 비추는 컷씬을 재생한다.
    /// 컷씬 카메라가 스크린 카메라에 다 도착하면 영상을 틀고, 영상이 끝나면 컷씬을 멈춰 플레이어 카메라로 돌려보낸다.
    /// 컷씬의 스크린 샷은 영상보다 길게 잡아 두어야 한다(영상이 끝날 때 이 컴포넌트가 CinematicManager.Stop으로 끝낸다).
    /// </summary>
    public class LiveEndingScreen : MonoBehaviour
    {
        #region Serialized Fields
        [Header("Screen")]
        [SerializeField] private Renderer _screenRenderer;
        [Tooltip("영상 전에 띄우는 하얀 화면 머티리얼")]
        [SerializeField] private Material _idleMaterial;
        [Tooltip("영상을 그리는 머티리얼(VideoPlayer의 RenderTexture를 쓰는 것)")]
        [SerializeField] private Material _videoMaterial;
        [SerializeField] private VideoPlayer _videoPlayer;

        [Header("Cutscene")]
        [Tooltip("스크린으로 카메라를 옮기는 컷씬 번호(SOCutsceneTable의 Id)")]
        [SerializeField] private int _cutsceneId = 33;
        [Tooltip("스크린을 비추는 연출 카메라. 컷씬 카메라가 이 카메라에 다 도착하면 영상을 튼다")]
        [SerializeField] private CinemachineCamera _screenCamera;

        [Header("Events")]
        [Tooltip("영상이 끝났을 때 실행할 동작")]
        [SerializeField] private UnityEvent _onVideoFinished = new UnityEvent();
        #endregion

        #region Private Fields
        private bool _hasStarted;
        private bool _isVideoFinished;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            Debug.Assert(_screenRenderer != null, $"[{name}] Screen Renderer가 연결되지 않았습니다.");
            Debug.Assert(_videoPlayer != null, $"[{name}] VideoPlayer가 연결되지 않았습니다.");

            _videoPlayer.playOnAwake = false;
            _videoPlayer.isLooping = false;
            _videoPlayer.loopPointReached += HandleVideoFinished;
            _screenRenderer.sharedMaterial = _idleMaterial;
        }

        private void OnDestroy()
        {
            if (_videoPlayer != null)
                _videoPlayer.loopPointReached -= HandleVideoFinished;
        }
        #endregion

        #region Public Methods
        // 스크린 컷씬을 시작한다. 컷씬을 시작하지 못하면(다른 연출 중 등) 다음에 다시 부를 수 있다.
        public void Play()
        {
            if (_hasStarted) return;

            CinematicManager cinematicManager = CinematicManager.Instance;
            if (cinematicManager == null)
            {
                Debug.LogWarning($"[{name}] 씬에 CinematicManager가 없어 {_cutsceneId}번 컷씬을 재생하지 못했습니다.", this);
                return;
            }

            cinematicManager.PlayCutscene(_cutsceneId);
            if (!cinematicManager.IsPlaying) return;

            _hasStarted = true;
            StartCoroutine(CoPlayEnding(cinematicManager));
        }
        #endregion

        #region Private Methods
        private void HandleVideoFinished(VideoPlayer videoPlayer)
        {
            _isVideoFinished = true;
        }

        // 컷씬 카메라가 스크린 카메라로 넘어가 블렌드까지 끝났으면 true.
        private bool IsScreenCameraLive()
        {
            CinemachineBrain brain = (CinemachineBrain.ActiveBrainCount > 0) ? CinemachineBrain.GetActiveBrain(0) : null;
            if (brain == null) return false;

            return ((Object)brain.ActiveVirtualCamera == _screenCamera) && !brain.IsBlending;
        }
        #endregion

        #region Coroutines
        private IEnumerator CoPlayEnding(CinematicManager cinematicManager)
        {
            // 컷씬이 스크린 샷에 오기 전에 끝나면(중간에 멈춤 등) 영상을 틀지 않는다.
            while (!IsScreenCameraLive())
            {
                if (!cinematicManager.IsPlaying) yield break;
                yield return null;
            }

            _screenRenderer.sharedMaterial = _videoMaterial;
            _videoPlayer.Play();

            while (!_isVideoFinished)
                yield return null;

            if (cinematicManager.IsPlaying)
                cinematicManager.Stop();

            _onVideoFinished.Invoke();
        }
        #endregion
    }
}
