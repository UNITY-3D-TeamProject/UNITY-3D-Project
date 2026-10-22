using System.Collections;
using UnityEngine;
using Unity.Cinemachine;
using Core.InputMap;
using UI;

namespace Core.Cinematic
{
    /// <summary>
    /// 씬의 카메라 연출(CinematicSequence)을 단계 순서대로 실행한다. 씬마다 하나 둔다.
    /// 연출 동안 InputMapController에 Dialogue 맵(조작 막기)을, UIManager에 인게임 UI 숨기기를 요청하고, 샷 설정에 따라 게임 시간을 멈추거나 돌린다.
    /// 시간이 멈춰도 카메라 블렌드가 돌도록 CinemachineBrain을 게임 시간과 상관없이 돌리고, 끝나면 모두 원래대로 돌린다.
    /// 연출 중 플레이어는 무적이 아니므로, 시간이 흐르는 샷은 적이 없는 곳에서만 쓴다.
    /// </summary>
    public class CinematicManager : MonoBehaviour
    {
        #region Constants
        private const string SEEN_KEY_PREFIX = "Cinematic.Seen.";
        #endregion

        #region Private Fields
        private CinematicSequence _currentSequence;
        private Coroutine _playRoutine;
        // 발판 트리거(물리 콜백)에서 바로 코루틴을 시작하면 게임 시간을 멈춘 뒤 단계가 진행되지 않아, 다음 Update에서 시작한다.
        private bool _shouldStartRoutine;
        private Coroutine _restoreBrainRoutine;
        private CinemachineCamera _activeShotCamera;

        // 연출 동안 바꿔 둔 CinemachineBrain과 원래 값
        private CinemachineBrain _brain;
        private bool _isBrainIgnoringTimeScale;
        private CinemachineBlendDefinition _brainDefaultBlend;
        #endregion

        #region Properties
        // 지금 씬의 CinematicManager. 배치하지 않은 씬이거나 Awake 전이면 null이다.
        public static CinematicManager Instance { get; private set; }

        // 연출이 재생 중이면 true.
        public bool IsPlaying => _currentSequence != null;
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
            if (!_shouldStartRoutine) return;

            _shouldStartRoutine = false;
            _playRoutine = StartCoroutine(CoPlay(_currentSequence));
        }

        private void OnDisable()
        {
            Stop();

            // 꺼진 뒤에는 코루틴이 돌지 않으므로 끝 블렌드를 기다리지 않고 바로 되돌린다.
            if (_restoreBrainRoutine != null)
            {
                StopCoroutine(_restoreBrainRoutine);
                _restoreBrainRoutine = null;
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
        // 이미 연출·대화 중이거나, 한 번만 보는 연출을 이미 봤거나, UIManager가 연출을 시작할 수 없으면 false.
        public bool Play(CinematicSequence sequence)
        {
            if ((sequence == null) || IsPlaying) return false;

            UIManager uiManager = UIManager.Instance;
            if (uiManager == null)
            {
                Debug.LogWarning("[CinematicManager] 씬에 UIManager가 없어 연출을 재생하지 못했습니다.", this);
                return false;
            }

            if (sequence.ShouldPlayOnce && HasSeen(sequence)) return false;
            if (!uiManager.BeginCinematic()) return false;

            // 중간에 끊겨도 다시 보지 않도록 시작할 때 본 기록을 남긴다.
            if (sequence.ShouldPlayOnce)
                MarkSeen(sequence);

            _currentSequence = sequence;
            InputMapController.Instance?.RequestMap(this, EInputMapType.Dialogue);
            BeginBrainControl();
            _shouldStartRoutine = true;
            return true;
        }

        // 연출을 끝까지 보지 않고 정리한다. 시간·입력·UI·카메라도 끝났을 때처럼 돌려놓는다.
        public void Stop()
        {
            if (!IsPlaying) return;

            _shouldStartRoutine = false;

            if (_playRoutine != null)
            {
                StopCoroutine(_playRoutine);
                _playRoutine = null;
            }

            // 대화 단계 중이면 대화도 끝낸다. 연출 중이므로 UIManager는 시간·입력·UI를 돌려주지 않고 Finish가 돌려준다.
            if (UIManager.Instance != null)
                UIManager.Instance.StopDialogue();

            Finish();
        }
        #endregion

        #region Private Methods
        // 시간이 멈춰도 블렌드가 돌도록 Brain을 게임 시간과 상관없이 돌린다.
        private void BeginBrainControl()
        {
            // 앞 연출의 끝 블렌드를 기다리는 중이면 원래 값은 이미 들고 있으므로 다시 저장하지 않는다.
            if (_restoreBrainRoutine != null)
            {
                StopCoroutine(_restoreBrainRoutine);
                _restoreBrainRoutine = null;
            }
            else
            {
                _brain = (CinemachineBrain.ActiveBrainCount > 0) ? CinemachineBrain.GetActiveBrain(0) : null;
                if (_brain == null) return;

                _isBrainIgnoringTimeScale = _brain.IgnoreTimeScale;
                _brainDefaultBlend = _brain.DefaultBlend;
            }

            if (_brain != null)
                _brain.IgnoreTimeScale = true;
        }

        private void RestoreBrain()
        {
            if (_brain != null)
            {
                _brain.IgnoreTimeScale = _isBrainIgnoringTimeScale;
                _brain.DefaultBlend = _brainDefaultBlend;
            }

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

        // 연출을 마치고 샷 카메라·시간·입력·UI를 돌려준다. Brain 설정은 끝 블렌드가 끝난 뒤 되돌린다.
        private void Finish()
        {
            float endBlendSeconds = (_currentSequence != null) ? _currentSequence.EndBlendSeconds : 0.0f;
            _currentSequence = null;

            SetBlend(endBlendSeconds);
            SetShotCamera(null);

            if (GameManager.Instance != null)
                GameManager.Instance.ResumeGameTime(this);

            InputMapController.Instance?.ReleaseMap(this);

            if (UIManager.Instance != null)
                UIManager.Instance.EndCinematic();

            // Brain은 다음 프레임에 블렌드를 시작하므로, 블렌드 시간이 지난 뒤에 원래 기본 블렌드로 돌린다.
            if (isActiveAndEnabled)
                _restoreBrainRoutine = StartCoroutine(CoRestoreBrain(endBlendSeconds));
            else
                RestoreBrain();
        }

        private static bool HasSeen(CinematicSequence sequence)
        {
            return !string.IsNullOrEmpty(sequence.Id) && (PlayerPrefs.GetInt(SEEN_KEY_PREFIX + sequence.Id, 0) == 1);
        }

        private void MarkSeen(CinematicSequence sequence)
        {
            if (string.IsNullOrEmpty(sequence.Id))
            {
                Debug.LogWarning($"[CinematicManager] {sequence.name}의 Id가 비어 있어 본 기록을 남기지 못했습니다.", sequence);
                return;
            }

            PlayerPrefs.SetInt(SEEN_KEY_PREFIX + sequence.Id, 1);
            PlayerPrefs.Save();
        }
        #endregion

        #region Coroutines
        private IEnumerator CoPlay(CinematicSequence sequence)
        {
            for (int i = 0; i < sequence.StepCount; i++)
            {
                if (!sequence.TryGetStep(i, out SCinematicStep step)) continue;

                if (step.Type == ECinematicStepType.Shot)
                    yield return CoPlayShot(step);
                else
                    yield return CoPlayDialogue(step);
            }

            _playRoutine = null;
            Finish();
        }

        private IEnumerator CoPlayShot(SCinematicStep step)
        {
            if (step.Camera == null)
            {
                Debug.LogWarning("[CinematicManager] 카메라가 비어 있는 샷 단계를 건너뜁니다.", this);
                yield break;
            }

            SetBlend(step.BlendInSeconds);
            SetShotCamera(step.Camera);

            GameManager gameManager = GameManager.Instance;
            if (gameManager != null)
            {
                if (step.ShouldPauseTime)
                    gameManager.PauseGameTime(this);
                else
                    gameManager.ResumeGameTime(this);
            }

            float elapsed = 0.0f;
            while (elapsed < step.Duration)
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

            UIManager uiManager = UIManager.Instance;
            if (uiManager == null) yield break;

            // 테이블에 없거나 시작하지 못하면 IsDialoguePlaying이 false라 바로 다음 단계로 넘어간다.
            uiManager.PlayDialogue(step.DialogueId);

            while ((uiManager != null) && uiManager.IsDialoguePlaying)
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
        }
        #endregion
    }
}
