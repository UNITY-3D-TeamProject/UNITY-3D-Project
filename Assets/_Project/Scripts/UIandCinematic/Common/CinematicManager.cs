using System;
using System.Collections;
using UnityEngine;
using Unity.Cinemachine;
using Core;
using Core.InputMap;

namespace UIandCinematic
{
    /// <summary>
    /// 씬의 카메라 연출(CinematicSequence)과 번호 표의 컷씬(SOCutscene)·카메라 연출(SOCameraShot)을 단계 순서대로 실행한다. 씬마다 하나 둔다.
    /// 연출 동안 InputMapController에 Cutscene 맵(조작 막기)을 요청하고, 샷 설정에 따라 게임 시간을 멈추거나 돌린다.
    /// 시작·끝을 이벤트로 알리면 UIManager가 받아 인게임 UI를 숨기고 보인다.
    /// 시간이 멈춰도 카메라 블렌드가 돌도록 CinemachineBrain을 게임 시간과 상관없이 돌린다. 이 설정은 대화(CutsceneDirector)도 쓰므로 되돌리지 않고,
    /// 샷마다 바꾼 기본 블렌드만 끝나면 원래대로 돌린다.
    /// 연출 중 플레이어는 무적이 아니므로, 시간이 흐르는 샷은 적이 없는 곳에서만 쓴다.
    /// </summary>
    public class CinematicManager : MonoBehaviour
    {
        #region Serialized Fields
        [Tooltip("PlayCutscene(번호)로 재생할 컷씬 목록")]
        [SerializeField] private SOCutsceneTable _cutsceneTable;
        [Tooltip("PlayCameraShot(번호)로 재생할 카메라 연출 목록")]
        [SerializeField] private SOCameraShotTable _cameraShotTable;
        #endregion

        #region Private Fields
        private bool _isPlaying;
        // 연출이 끝날 때 플레이어 카메라로 돌아가는 블렌드 시간(초)
        private float _endBlendSeconds;
        private Coroutine _playRoutine;
        // 발판 트리거(물리 콜백)에서 바로 코루틴을 시작하면 게임 시간을 멈춘 뒤 단계가 진행되지 않아, 다음 Update에서 시작한다.
        private IEnumerator _pendingSteps;
        private Coroutine _restoreBrainRoutine;
        private CinemachineCamera _activeShotCamera;

        // 연출 동안 기본 블렌드를 바꿔 둔 CinemachineBrain과 원래 값
        private CinemachineBrain _brain;
        private CinemachineBlendDefinition _brainDefaultBlend;
        #endregion

        #region Properties
        // 지금 씬의 CinematicManager. 배치하지 않은 씬이거나 Awake 전이면 null이다.
        public static CinematicManager Instance { get; private set; }

        // 연출이 재생 중이면 true.
        public bool IsPlaying => _isPlaying;
        #endregion

        #region Events
        // 연출이 시작·끝날 때 알린다. 씬마다 CinematicManager가 새로 생기므로 UIManager가 Awake 순서와 상관없이 구독할 수 있게 static으로 둔다.
        public static event Action<CinematicManager> OnPlayStarted;
        public static event Action<CinematicManager> OnPlayFinished;
        // 샷 카메라와 시간 설정이 적용된 뒤 알린다. 화면 속 연출의 시작 시점을 맞추는 데 쓴다.
        public static event Action<CinematicManager, CinemachineCamera> OnShotStarted;
        public static event Action<SOCutscene, int> OnCutsceneStepStarted;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            if ((Instance != null) && (Instance.gameObject.scene == gameObject.scene))
                Debug.LogError("[CinematicManager] 한 씬에 CinematicManager가 두 개 있습니다. 나중에 Awake된 쪽이 Instance가 됩니다.", this);

            Instance = this;
        }

        private void Update()
        {
            if (_pendingSteps == null) return;

            IEnumerator steps = _pendingSteps;
            _pendingSteps = null;
            _playRoutine = StartCoroutine(CoRun(steps));
        }

        private void OnDisable()
        {
            Stop();

            // 꺼진 뒤에는 코루틴이 돌지 않으므로 끝 블렌드를 기다리지 않고 바로 되돌리고 조작도 돌려준다.
            if (_restoreBrainRoutine != null)
            {
                StopCoroutine(_restoreBrainRoutine);
                _restoreBrainRoutine = null;
                ReleaseControl();
            }

            RestoreBrain();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }
        #endregion

        #region Public Methods
        // 연출을 처음부터 재생한다.
        // 이 매니저가 꺼져 있거나, 이미 연출·대화·펫 안내 중이거나, 설정창이 열려 있거나, 플레이어가 없으면 false.
        public bool Play(CinematicSequence sequence)
        {
            if (sequence == null) return false;

            return TryStart(
                sequence.EndBlendSeconds,
                CoPlay(sequence));
        }

        // 번호에 맞는 컷씬을 재생한다. 번호와 컷씬은 SOCutsceneTable에서 정한다.
        // 재생하지 못하는 조건은 Play와 같다. 발판(TriggerPlate) 같은 UnityEvent에서도 연결할 수 있도록 반환값이 없다.
        public void PlayCutscene(int id)
        {
            if ((_cutsceneTable == null) || !_cutsceneTable.TryGetCutscene(id, out SOCutscene cutscene))
            {
                Debug.LogWarning($"[CinematicManager] {id}번 컷씬이 테이블에 없습니다.", this);
                return;
            }

            TryStart(
                cutscene.EndBlendSeconds,
                CoPlayCutscene(cutscene));
        }

        // 번호에 맞는 카메라 연출을 재생한다. 번호와 카메라 연출은 SOCameraShotTable에서 정한다.
        // 재생하지 못하는 조건은 Play와 같다. 발판(TriggerPlate) 같은 UnityEvent에서도 연결할 수 있도록 반환값이 없다.
        public void PlayCameraShot(int id)
        {
            if ((_cameraShotTable == null) || !_cameraShotTable.TryGetCameraShot(id, out SOCameraShot cameraShot))
            {
                Debug.LogWarning($"[CinematicManager] {id}번 카메라 연출이 테이블에 없습니다.", this);
                return;
            }

            TryStart(
                cameraShot.EndBlendSeconds,
                CoPlayCameraShot(cameraShot));
        }

        // 연출을 끝까지 보지 않고 정리한다. 시간·입력·UI·카메라도 끝났을 때처럼 돌려놓는다.
        public void Stop()
        {
            if (!IsPlaying) return;

            _pendingSteps = null;

            // yield return으로 이어진 단계 코루틴까지 남지 않도록 모두 멈춘다.
            // 연출 중에는 Brain 복구 코루틴이 BeginBrainControl에서 이미 멈춰 있어 단계 코루틴만 남아 있다.
            StopAllCoroutines();
            _playRoutine = null;

            // 대화 단계 중이면 대화도 끝낸다. 대화는 자기 요청만 풀고, 연출의 시간·입력 요청과 UI 숨김은 Finish가 푼다.
            CutsceneController.Current?.StopDialogue();

            Finish();
        }
        #endregion

        #region Private Methods
        // 연출을 시작한다. 단계(steps)는 다음 Update에서 돌기 시작한다.
        // 이 매니저가 꺼져 있거나, 이미 연출·대화·펫 안내 중이거나, 설정창이 열려 있거나, 플레이어가 없으면 false.
        private bool TryStart(
            float endBlendSeconds,
            IEnumerator steps)
        {
            if (IsPlaying) return false;

            // 꺼져 있으면 Update가 돌지 않아 연출이 시작되지 않은 채 조작만 막히므로 받지 않는다.
            if (!isActiveAndEnabled)
            {
                Debug.LogWarning("[CinematicManager] 꺼져 있어 연출을 재생하지 못했습니다.", this);
                return false;
            }

            ICutsceneController cutsceneController = CutsceneController.Current;
            if (cutsceneController == null)
            {
                Debug.LogWarning("[CinematicManager] 씬에 대화를 재생하는 쪽(UIManager)이 없어 연출을 재생하지 못했습니다.", this);
                return false;
            }

            GameManager gameManager = GameManager.Instance;
            if ((gameManager == null) ||
                (gameManager.CurrentState != GameManager.GameState.Playing) ||
                (gameManager.CurrentPlayerFacade == null))
                return false;

            if (cutsceneController.IsDialoguePlaying || cutsceneController.IsPetGuideOpen) return false;

            _isPlaying = true;
            _endBlendSeconds = endBlendSeconds;
            InputMapController.Instance?.RequestMap(this, EInputMapType.Cutscene);
            BeginBrainControl();
            _pendingSteps = steps;
            OnPlayStarted?.Invoke(this);
            return true;
        }

        // 시간이 멈춰도 블렌드가 돌도록 Brain을 게임 시간과 상관없이 돌리고, 원래 기본 블렌드를 저장해 둔다.
        // IgnoreTimeScale은 되돌리지 않는다. 평소에는 게임 시간이 1이라 차이가 없다.
        private void BeginBrainControl()
        {
            // 앞 연출의 끝 블렌드를 기다리는 중이면 원래 값은 이미 들고 있으므로 다시 저장하지 않는다.
            // 그 연출이 걸어 둔 시간 정지·조작 막기는 같은 요청자(this)라 새 연출이 이어받고, 새 연출이 끝날 때 푼다.
            if (_restoreBrainRoutine != null)
            {
                StopCoroutine(_restoreBrainRoutine);
                _restoreBrainRoutine = null;
            }
            else
            {
                _brain = (CinemachineBrain.ActiveBrainCount > 0) ? CinemachineBrain.GetActiveBrain(0) : null;
                if (_brain == null) return;

                _brainDefaultBlend = _brain.DefaultBlend;
            }

            if (_brain != null)
                _brain.IgnoreTimeScale = true;
        }

        // 샷마다 바꾼 기본 블렌드를 원래대로 돌린다.
        private void RestoreBrain()
        {
            if (_brain != null)
                _brain.DefaultBlend = _brainDefaultBlend;

            _brain = null;
        }

        // 다음에 켜고 끄는 카메라로 넘어갈 블렌드 시간을 정한다. 0 이하면 바로 끊는다.
        private void SetBlend(float seconds)
        {
            if (_brain == null) return;

            _brain.DefaultBlend = (seconds > 0.0f)
                ? new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.EaseInOut, seconds)
                : new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.Cut, 0.0f);
        }

        // target만 켜고 앞 샷 카메라는 끈다. null이면 샷 카메라를 모두 꺼서 플레이어(또는 대화) 카메라로 돌아간다.
        private void SetShotCamera(CinemachineCamera target)
        {
            if ((_activeShotCamera != null) && (_activeShotCamera != target))
                _activeShotCamera.enabled = false;

            _activeShotCamera = target;

            if (_activeShotCamera != null)
                _activeShotCamera.enabled = true;
        }

        // 연출을 마치고 샷 카메라·UI를 돌려준다. Brain 기본 블렌드와 게임 시간·입력은 끝 블렌드가 끝난 뒤 돌려준다.
        private void Finish()
        {
            float endBlendSeconds = _endBlendSeconds;
            _isPlaying = false;

            SetBlend(endBlendSeconds);
            SetShotCamera(null);

            // 컷씬 대사로 세워 둔 펫과 말하는 사람 카메라를 돌려놓는다. 컷씬이 아니었으면 아무것도 하지 않는다.
            CutsceneController.Current?.EndCutscene();

            // 플레이어 카메라로 돌아오는 동안은 마지막 단계의 게임 시간 설정과 조작 막기를 그대로 둔다.
            // 시간이 흐르는 샷으로 끝났으면 블렌드 동안에도 시간이 흐르고, 끝 블렌드가 끝나면 ReleaseControl에서 둘 다 푼다.
            OnPlayFinished?.Invoke(this);

            // Brain은 다음 프레임에 블렌드를 시작하므로, 블렌드 시간이 지난 뒤에 원래 기본 블렌드로 돌리고 조작을 돌려준다.
            if (isActiveAndEnabled)
            {
                _restoreBrainRoutine = StartCoroutine(CoRestoreBrain(endBlendSeconds));
            }
            else
            {
                RestoreBrain();
                ReleaseControl();
            }
        }

        // 연출이 걸어 둔 게임 시간 정지와 조작 막기를 푼다.
        private void ReleaseControl()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.ResumeGameTime(this);

            InputMapController.Instance?.ReleaseMap(this);
        }

        // 키에 맞는 씬 카메라를 찾는다. 키가 비었으면 조용히, 씬에 없으면 경고를 남기고 null을 돌려준다.
        private CinemachineCamera FindCamera(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;
            if (CinematicCamera.TryFind(key, out CinemachineCamera camera)) return camera;

            Debug.LogWarning($"[CinematicManager] 키가 '{key}'인 연출 카메라(CinematicCamera)가 씬에 없습니다.", this);
            return null;
        }
        #endregion

        #region Coroutines
        // 단계를 끝까지 돌린 뒤 연출을 마친다.
        private IEnumerator CoRun(IEnumerator steps)
        {
            yield return steps;

            _playRoutine = null;
            Finish();
        }

        private IEnumerator CoPlay(CinematicSequence sequence)
        {
            for (int i = 0; i < sequence.StepCount; i++)
            {
                if (!sequence.TryGetStep(i, out SCinematicStep step)) continue;

                if (step.Type == ECinematicStepType.Shot)
                {
                    yield return CoPlayShot(
                        step.Camera,
                        step.Duration,
                        step.BlendInSeconds,
                        step.ShouldPauseTime);
                }
                else
                {
                    yield return CoPlayDialogue(step);
                }
            }
        }

        private IEnumerator CoPlayCutscene(SOCutscene cutscene)
        {
            for (int i = 0; i < cutscene.StepCount; i++)
            {
                if (!cutscene.TryGetStep(i, out SCutsceneStep step)) continue;

                OnCutsceneStepStarted?.Invoke(cutscene, i);

                if (step.Type == ECutsceneStepType.Line)
                {
                    bool isLastLine = (i == cutscene.StepCount - 1);
                    yield return CoPlayCutsceneLine(step, isLastLine);
                    continue;
                }

                // 카메라 단계 동안은 대사창과 말하는 사람 카메라를 끈다. 세워 둔 펫은 컷씬이 끝날 때 돌려놓는다.
                CutsceneController.Current?.HideCutsceneLine();

                if (step.CameraShot == null)
                {
                    Debug.LogWarning($"[CinematicManager] {cutscene.name}의 {i}번 단계에 카메라 연출이 비어 있어 건너뜁니다.", cutscene);
                    continue;
                }

                yield return CoPlayCameraShot(step.CameraShot);
            }
        }

        private IEnumerator CoPlayCameraShot(SOCameraShot cameraShot)
        {
            for (int i = 0; i < cameraShot.ShotCount; i++)
            {
                if (!cameraShot.TryGetShot(i, out SCameraShot shot)) continue;

                yield return CoPlayShot(
                    FindCamera(shot.CameraKey),
                    shot.Duration,
                    shot.BlendInSeconds,
                    shot.ShouldPauseTime);
            }
        }

        private IEnumerator CoPlayShot(
            CinemachineCamera camera,
            float duration,
            float blendInSeconds,
            bool shouldPauseTime)
        {
            if (camera == null)
            {
                Debug.LogWarning("[CinematicManager] 카메라가 비어 있는 샷 단계를 건너뜁니다.", this);
                yield break;
            }

            SetBlend(blendInSeconds);
            SetShotCamera(camera);

            GameManager gameManager = GameManager.Instance;
            if (gameManager != null)
            {
                if (shouldPauseTime)
                    gameManager.PauseGameTime(this);
                else
                    gameManager.ResumeGameTime(this);
            }

            OnShotStarted?.Invoke(this, camera);

            float elapsed = 0.0f;
            while (elapsed < duration)
            {
                yield return null;

                // 설정창이 열린 동안(Pause)은 샷 시간을 세지 않는다.
                if ((gameManager != null) && (gameManager.CurrentState != GameManager.GameState.Playing)) continue;

                elapsed += Time.unscaledDeltaTime;
            }
        }

        private IEnumerator CoPlayDialogue(SCinematicStep step)
        {
            // 대화는 말하는 사람 카메라를 쓰므로 샷 카메라를 끄고, 블렌드는 원래 기본값으로 둔다.
            if (_brain != null)
                _brain.DefaultBlend = _brainDefaultBlend;

            SetShotCamera(null);

            // 대화가 끝난 뒤 다음 단계가 시간을 정할 때까지 시간이 흐르지 않도록, 대화 단계 동안 연출도 시간 정지를 요청해 둔다.
            if (GameManager.Instance != null)
                GameManager.Instance.PauseGameTime(this);

            ICutsceneController cutsceneController = CutsceneController.Current;
            if (cutsceneController == null) yield break;

            // 테이블에 없거나 시작하지 못하면 IsDialoguePlaying이 false라 바로 다음 단계로 넘어간다.
            cutsceneController.PlayCinematicDialogue(step.DialogueId);

            // 대화 중에 대화 재생 쪽이 꺼져 등록이 풀리면 기다리지 않는다.
            while ((CutsceneController.Current == cutsceneController) && cutsceneController.IsDialoguePlaying)
                yield return null;
        }

        // 컷씬 대사 한 줄을 띄우고 E로 넘길 때까지 기다린다.
        private IEnumerator CoPlayCutsceneLine(SCutsceneStep step, bool isLastLine)
        {
            // 카메라 키가 있으면 그 카메라를, 없으면(또는 씬에 없으면) 말하는 사람 카메라를 쓴다. 블렌드는 원래 기본값으로 둔다.
            if (_brain != null)
                _brain.DefaultBlend = _brainDefaultBlend;

            CinemachineCamera camera = FindCamera(step.CameraKey);
            SetShotCamera(camera);

            // 기본은 시간 정지다. 움직이는 발판 등을 보여 주는 대사는 시간을 흐르게 할 수 있다.
            if (GameManager.Instance != null)
            {
                if (step.ShouldRunTime)
                    GameManager.Instance.ResumeGameTime(this);
                else
                    GameManager.Instance.PauseGameTime(this);
            }

            ICutsceneController cutsceneController = CutsceneController.Current;
            if (cutsceneController == null) yield break;

            bool isShown = cutsceneController.ShowCutsceneLine(
                step.SpeakerName,
                step.Portrait,
                step.Message,
                isLastLine,
                camera == null,
                step.IsPartnerTalk);
            if (!isShown) yield break;

            // 대사 중에 대화 재생 쪽이 꺼져 등록이 풀리면 기다리지 않는다.
            while ((CutsceneController.Current == cutsceneController) && cutsceneController.IsCutsceneLineWaiting)
                yield return null;
        }

        private IEnumerator CoRestoreBrain(float endBlendSeconds)
        {
            yield return null;

            float elapsed = 0.0f;
            while (elapsed < endBlendSeconds)
            {
                yield return null;
                elapsed += Time.unscaledDeltaTime;
            }

            _restoreBrainRoutine = null;
            RestoreBrain();
            ReleaseControl();
        }
        #endregion
    }
}
