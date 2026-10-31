using System.Collections;
using System.Collections.Generic;
using Core;
using UIandCinematic;
using Unity.Cinemachine;
using UnityEngine;

namespace Map.Maze
{
    /// <summary>파일 미로 출구에서 안내, 음악 파일 전송, 로비 포탈 개방을 맡는다.</summary>
    public class FileMazeUpload : MonoBehaviour
    {
        #region Serialized Fields
        [SerializeField] private Collider _interactionCollider;
        [SerializeField] private SOCutscene _introCutscene;
        [SerializeField] private int _uploadCutsceneId = 34;
        #endregion

        #region Private Fields
        private Collider _portalCollider;
        private GameObject _portalVisual;
        private CinemachineCamera _camera;
        private CinematicManager _manager;
        private bool _isIntroPlaying;
        private bool _isIntroComplete;
        private bool _isUploadPlaying;
        private bool _isUploaded;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            Transform portal = transform.parent;
            _portalCollider = portal != null ? portal.GetComponent<Collider>() : null;
            Transform visual = portal != null ? portal.Find("Visual_PortalBasicBlue") : null;
            _portalVisual = visual != null ? visual.gameObject : null;
            if (_portalCollider != null) _portalCollider.enabled = false;
            if (_portalVisual != null) _portalVisual.SetActive(false);
            if (_interactionCollider != null) _interactionCollider.enabled = false;

            CreateCamera("FileMachineCamera", "FileUploadMachine",
                new Vector3(3.0f, 2.5f, -5.5f), new Vector3(0.0f, 1.0f, 0.5f));
            _camera = CreateCamera("FileUploadCamera", "FileUploadSequence",
                new Vector3(4.5f, 5.0f, -9.0f), new Vector3(0.0f, 4.0f, 0.0f));
            _camera.Lens.FieldOfView = 55.0f;
        }

        private void OnEnable()
        {
            CinematicManager.OnShotStarted += HandleShotStarted;
            CinematicManager.OnPlayFinished += HandlePlayFinished;
            CinematicManager.OnCutsceneStepStarted += HandleCutsceneStep;
        }

        private void OnDisable()
        {
            CinematicManager.OnShotStarted -= HandleShotStarted;
            CinematicManager.OnPlayFinished -= HandlePlayFinished;
            CinematicManager.OnCutsceneStepStarted -= HandleCutsceneStep;
            StopAllCoroutines();
        }
        #endregion

        #region Public Methods
        /// <summary>기계 상호작용으로 전송 샷을 시작한다.</summary>
        public void BeginUpload()
        {
            if (!_isIntroComplete || _isUploadPlaying || _isUploaded) return;
            _manager = CinematicManager.Instance;
            if (_manager == null || _manager.IsPlaying) return;
            _manager.PlayCutscene(_uploadCutsceneId);
            if (!_manager.IsPlaying) return;
            _isUploadPlaying = true;
            if (_interactionCollider != null) _interactionCollider.enabled = false;
        }
        #endregion

        #region Private Methods
        private CinemachineCamera CreateCamera(string cameraName, string key, Vector3 offset, Vector3 targetOffset)
        {
            var cameraObject = new GameObject(cameraName);
            cameraObject.transform.SetParent(transform, false);
            var camera = cameraObject.AddComponent<CinemachineCamera>();
            camera.Priority = 50;
            cameraObject.AddComponent<CinematicCamera>().SetKey(key);
            Vector3 position = transform.TransformPoint(offset);
            Vector3 target = transform.TransformPoint(targetOffset);
            cameraObject.transform.SetPositionAndRotation(position, Quaternion.LookRotation(target - position));
            return camera;
        }

        private void HandleCutsceneStep(SOCutscene cutscene, int index)
        {
            if (_isIntroComplete || cutscene != _introCutscene || index != 0) return;
            _manager = CinematicManager.Instance;
            _isIntroPlaying = _manager != null;
        }

        private void HandleShotStarted(CinematicManager manager, CinemachineCamera camera)
        {
            if (!_isUploadPlaying || manager != _manager || camera != _camera) return;
            StartCoroutine(CoUpload());
        }

        private void HandlePlayFinished(CinematicManager manager)
        {
            if (manager != _manager) return;
            if (_isIntroPlaying)
            {
                _isIntroPlaying = false;
                _isIntroComplete = true;
                if (_interactionCollider != null) _interactionCollider.enabled = true;
            }
            else if (_isUploadPlaying)
            {
                _isUploadPlaying = false;
                StopAllCoroutines();
                // 중단 시에는 포탈을 열지 않고 다시 조작할 수 있게 한다.
                if (!_isUploaded && _interactionCollider != null) _interactionCollider.enabled = true;
            }
        }

        private void ShowPortal()
        {
            _isUploaded = true;
            if (_portalVisual != null) _portalVisual.SetActive(true);
            if (_portalCollider != null) _portalCollider.enabled = true;
        }
        #endregion

        #region Coroutines
        private IEnumerator CoUpload()
        {
            FileMazeCutsceneStaging staging = FindAnyObjectByType<FileMazeCutsceneStaging>();
            if (staging == null) yield break;
            var cards = new List<GameObject>();
            for (int i = 0; i < 3; i++)
            {
                GameObject card = staging.CreateRewardFile(i);
                if (card == null) yield break;
                cards.Add(card);
            }
            float elapsed = 0f;
            const float DURATION = 5f;
            while (elapsed < DURATION)
            {
                GameManager gameManager = GameManager.Instance;
                if (gameManager != null && gameManager.CurrentState == GameManager.GameState.Playing)
                    elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsed / DURATION);
                float radius = Mathf.Lerp(1.1f, 0.08f, progress * progress);
                float angle = 25f * elapsed + 360f * progress * progress * progress;
                Vector3 center = transform.position + Vector3.up * (2.2f + 6f * progress * progress);
                for (int i = 0; i < cards.Count; i++)
                {
                    if (cards[i] == null) continue;
                    float radians = (angle + i * 120f) * Mathf.Deg2Rad;
                    cards[i].transform.position = center + new Vector3(Mathf.Cos(radians) * radius, 0f, Mathf.Sin(radians) * radius);
                    cards[i].transform.rotation = Quaternion.Euler(0f, -angle, 0f);
                    cards[i].transform.localScale = Vector3.one * Mathf.Lerp(1f, 0.08f, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.65f, 1f, progress)));
                }
                yield return null;
            }
            foreach (GameObject card in cards) if (card != null) Destroy(card);
            ShowPortal();
        }
        #endregion
    }
}
