using System.Collections;
using UnityEngine;
using Unity.Cinemachine;
using Core;
using Core.Audio;
using Map.Common;
using Map.Gimmicks;
using UIandCinematic;

namespace Map.Feed
{
    /// <summary>완성 피드 샷에 맞춰 카드를 가속 회전·상승시키고, 클리어 대화 뒤 포탈을 연다.</summary>
    public class FeedUploadSequence : MonoBehaviour
    {
        #region Serialized Fields
        [Header("References")]
        [SerializeField] private Transform _card;
        [SerializeField] private CinemachineCamera _uploadCamera;
        [SerializeField] private GameObject _portal;
        [SerializeField] private TrailRenderer[] _trails;
        [SerializeField] private ParticleSystem _uploadParticles;
        [SerializeField] private ParticleSystem _completionBurst;
        [SerializeField] private SOAudioData _completionSound;

        [Header("Cutscene")]
        [SerializeField] private int _cutsceneId = 17;

        [Header("Upload")]
        [Tooltip("완성 피드를 먼저 보여 주는 시간. 카메라 블렌드가 포함된다")]
        [SerializeField, Min(0.0f)] private float _holdDuration = 1.0f;
        [SerializeField, Min(0.01f)] private float _riseDuration = 4.0f;
        [SerializeField, Min(0.0f)] private float _riseHeight = 18.0f;
        [Tooltip("초당 회전 각도. 시작은 느리고 끝으로 갈수록 빨라진다")]
        [SerializeField, Min(0.0f)] private float _startSpinSpeed = 20.0f;
        [SerializeField, Min(0.0f)] private float _endSpinSpeed = 720.0f;
        #endregion

        #region Private Fields
        private CinematicManager _manager;
        private Billboard _billboard;
        private Vector3 _startPosition;
        private Quaternion _startRotation;
        private Vector3 _startScale;
        private bool _wasBillboardEnabled;
        private bool _isRequested;
        private bool _isStarted;
        private bool _isUploading;
        #endregion

        #region Properties
        public bool IsUploaded { get; private set; }
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            Debug.Assert((_card != null) && (_uploadCamera != null) && (_portal != null),
                $"[{name}] 피드 업로드 참조가 누락되었습니다.");
            _billboard = _card.GetComponent<Billboard>();
            SetTrails(false);
        }

        private void OnEnable()
        {
            CinematicManager.OnShotStarted += HandleShotStarted;
            CinematicManager.OnPlayFinished += HandlePlayFinished;
        }

        private void Update()
        {
            if (!_isRequested || _isStarted) return;

            _manager = CinematicManager.Instance;
            if (_manager == null)
            {
                Debug.LogWarning($"[{name}] CinematicManager가 없어 출구 포탈을 엽니다.", this);
                _isRequested = false;
                _portal.SetActive(true);
                return;
            }

            GameManager gameManager = GameManager.Instance;
            ICutsceneController controller = CutsceneController.Current;
            if (_manager.IsPlaying || (gameManager == null) ||
                (gameManager.CurrentState != GameManager.GameState.Playing) ||
                (gameManager.CurrentPlayerFacade == null) || (controller == null) ||
                controller.IsDialoguePlaying || controller.IsPetGuideOpen) return;

            _manager.PlayCutscene(_cutsceneId);
            _isStarted = _manager.IsPlaying;
            if (!_isStarted)
            {
                Debug.LogWarning($"[{name}] 클리어 컷씬을 시작하지 못해 출구 포탈을 엽니다.", this);
                _isRequested = false;
                _portal.SetActive(true);
            }
        }

        private void OnDisable()
        {
            CinematicManager.OnShotStarted -= HandleShotStarted;
            CinematicManager.OnPlayFinished -= HandlePlayFinished;
            StopAllCoroutines();
            SetTrails(false);
            if (_uploadParticles != null)
            {
                _uploadParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
            if (_completionBurst != null)
            {
                _completionBurst.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
            if (_isUploading)
            {
                _card.SetPositionAndRotation(_startPosition, _startRotation);
                _card.localScale = _startScale;
                if (_billboard != null) _billboard.enabled = _wasBillboardEnabled;
            }
            _isUploading = false;
            _isRequested = false;
            _isStarted = false;
        }
        #endregion

        #region Public Methods
        /// <summary>조립 완료 이벤트에서 부른다. 재생 중인 다른 연출이 있으면 끝날 때까지 기다린다.</summary>
        public void Begin()
        {
            if (_isRequested || IsUploaded) return;

            _portal.SetActive(false);
            _isRequested = true;
        }
        #endregion

        #region Private Methods
        private void HandleShotStarted(CinematicManager manager, CinemachineCamera camera)
        {
            if (!_isStarted || (manager != _manager) || (camera != _uploadCamera) ||
                _isUploading || IsUploaded) return;

            StartCoroutine(CoUpload());
        }

        private void HandlePlayFinished(CinematicManager manager)
        {
            if (!_isStarted || (manager != _manager)) return;

            // 중단(스킵)해도 미완성 업로드와 포탈이 남지 않도록 최종 상태로 정리한다.
            StopAllCoroutines();
            CompleteUpload(false);
            _isRequested = false;
            _isStarted = false;
            _portal.SetActive(true);
        }

        private void SetTrails(bool shouldEmit)
        {
            foreach (TrailRenderer trail in _trails)
            {
                if (trail == null) continue;
                trail.emitting = shouldEmit;
                if (!shouldEmit) trail.Clear();
            }
        }

        private void CompleteUpload(bool shouldPlayEffect)
        {
            if (IsUploaded) return;

            SetTrails(false);
            if (_uploadParticles != null) _uploadParticles.Stop();
            if (shouldPlayEffect)
            {
                if (_completionBurst != null)
                {
                    _completionBurst.transform.position = _card.position;
                    _completionBurst.Play();
                }
                GimmickUtility.PlayUi(_completionSound);
            }
            _card.gameObject.SetActive(false);
            if ((_billboard != null) && _isUploading) _billboard.enabled = _wasBillboardEnabled;
            _isUploading = false;
            IsUploaded = true;
        }
        #endregion

        #region Coroutines
        private IEnumerator CoUpload()
        {
            _isUploading = true;
            _startPosition = _card.position;
            _startRotation = _card.rotation;
            _startScale = _card.localScale;
            _wasBillboardEnabled = (_billboard != null) && _billboard.enabled;
            yield return new WaitForSeconds(_holdDuration);

            // 카메라가 자리를 잡은 뒤의 정면을 기준으로 회전을 시작한다.
            _startRotation = _card.rotation;
            if (_billboard != null) _billboard.enabled = false;
            SetTrails(true);
            if (_uploadParticles != null) _uploadParticles.Play();
            float elapsed = 0.0f;
            while (elapsed < _riseDuration)
            {
                float progress = Mathf.Clamp01(elapsed / _riseDuration);
                // 속도가 점점 커지는 상승과 회전. 각속도를 적분해 프레임 수에 관계없이 같은 회전을 만든다.
                float acceleration = progress * progress;
                float angle = (_startSpinSpeed * elapsed) +
                    ((_endSpinSpeed - _startSpinSpeed) * _riseDuration * progress * acceleration / 3.0f);
                _card.position = _startPosition + (Vector3.up * _riseHeight * acceleration);
                _card.rotation = Quaternion.AngleAxis(angle, Vector3.up) * _startRotation;
                float shrink = Mathf.SmoothStep(0.0f, 1.0f, Mathf.InverseLerp(0.65f, 1.0f, progress));
                _card.localScale = _startScale * Mathf.Lerp(1.0f, 0.08f, shrink);

                yield return null;
                elapsed += Time.deltaTime;
            }

            _card.position = _startPosition + (Vector3.up * _riseHeight);
            CompleteUpload(true);
        }
        #endregion
    }
}
