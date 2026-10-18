using UnityEngine;
using Unity.Cinemachine;
using Core;
using Core.Stage;
using Facade.Player;
using Pet;
using UI;

namespace Cutscene
{
    /// <summary>
    /// 스테이지가 시작되면 입장 대화를 한 번만 재생한다.
    /// 대화 중에는 펫을 플레이어 앞에 세워 플레이어를 보게 하고, 말하는 사람을 비추는 카메라로 전환한다.
    /// 본 기록은 PlayerPrefs에 대화 Id로 저장한다. (다시 보려면 Edit > Clear All PlayerPrefs)
    /// </summary>
    public class StageIntroDialogue : MonoBehaviour
    {
        #region Constants
        private const string SEEN_KEY_PREFIX = "DialogueSeen.";
        #endregion

        #region Serialized Fields
        [Header("References")]
        [SerializeField] private StageManager _stageManager;
        [SerializeField] private SODialogueSequence _sequence;
        [Tooltip("켜면 본 기록과 상관없이 시작할 때마다 재생한다 (테스트용)")]
        [SerializeField] private bool _shouldAlwaysPlay;

        [Header("Speakers")]
        [Tooltip("대사의 SpeakerName과 똑같이 적는다")]
        [SerializeField] private string _playerSpeakerName = "PLAYER";
        [SerializeField] private string _petSpeakerName = "PET";

        [Header("Pet Staging")]
        [Tooltip("대화 중 펫이 설 위치. 플레이어 기준 (오른쪽, 위, 앞) m. 펫은 이 자리에서 플레이어를 본다")]
        [SerializeField] private Vector3 _petStandOffset = new(0.0f, 1.2f, 1.8f);

        [Header("Speaker Cameras")]
        [Tooltip("CameraIndex가 -1인 줄에서 플레이어가 말할 때 켠다. 펫 뒤에서 플레이어 얼굴을 비춘다")]
        [SerializeField] private CinemachineCamera _playerShotCamera;
        [Tooltip("CameraIndex가 -1인 줄에서 펫이 말할 때 켠다. 플레이어 어깨 뒤에서 펫 얼굴을 비춘다")]
        [SerializeField] private CinemachineCamera _petShotCamera;
        [Tooltip("플레이어를 비추는 카메라 위치. 펫 위치 기준 (오른쪽, 위, 앞) m. 앞은 플레이어가 보는 방향")]
        [SerializeField] private Vector3 _playerShotOffset = new(0.5f, 0.1f, 0.9f);
        [Tooltip("펫을 비추는 카메라 위치. 플레이어 기준 (오른쪽, 위, 앞) m")]
        [SerializeField] private Vector3 _petShotOffset = new(0.7f, 1.6f, -1.0f);
        [Tooltip("플레이어를 비출 때 바라볼 높이 (플레이어 발 기준) m")]
        [SerializeField] private float _playerLookHeight = 1.2f;

        [Header("Fixed Cameras")]
        [Tooltip("대사의 CameraIndex가 0 이상이면 이 배열의 카메라를 쓴다. 시작할 때 모두 끄며, Priority는 플레이어 카메라(Base 0, Aim 10)보다 높게 둔다")]
        [SerializeField] private CinemachineCamera[] _cameras;
        #endregion

        #region Private Fields
        private bool _hasPlayed;
        // 대화 동안 옮겨 둔 펫 추종 위치와 원래 자리
        private Transform _petPivot;
        private Vector3 _petPivotLocalPosition;
        private Quaternion _petPivotLocalRotation;
        #endregion

        #region Properties
        private string SeenKey => SEEN_KEY_PREFIX + _sequence.Id;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            Debug.Assert(_stageManager != null, $"[{name}] StageManager가 연결되지 않았습니다.");
            Debug.Assert(_sequence != null, $"[{name}] 대화 SO가 연결되지 않았습니다.");

            // 작업하며 켜 둔 연출 카메라가 플레이어 카메라를 덮지 않도록 꺼 둔다.
            TurnOffCameras();
        }

        private void OnEnable()
        {
            if (_stageManager != null)
                _stageManager.OnStageStarted += TryPlay;
        }

        private void OnDisable()
        {
            if (_stageManager != null)
                _stageManager.OnStageStarted -= TryPlay;
        }

        private void Start()
        {
            // 이 컴포넌트가 구독하기 전에 스테이지가 이미 시작됐으면 여기서 재생한다.
            if ((_stageManager != null) && _stageManager.IsRunning)
                TryPlay();
        }
        #endregion

        #region Private Methods
        private void TryPlay()
        {
            if (_hasPlayed || (_sequence == null)) return;
            if (!_shouldAlwaysPlay && (PlayerPrefs.GetInt(SeenKey, 0) == 1)) return;

            UIManager uiManager = UIManager.Instance;

            if (uiManager == null)
            {
                Debug.LogWarning($"[StageIntroDialogue] 씬에 UIManager가 없어 대화를 재생하지 못했습니다.", this);
                return;
            }

            // 첫 줄의 카메라가 대화 시작과 동시에 정해지므로 펫과 카메라를 먼저 배치한다.
            BeginStaging();

            if (!uiManager.PlayDialogue(_sequence, HandleLineChanged, HandleFinished))
            {
                EndStaging();
                Debug.LogWarning($"[StageIntroDialogue] {_sequence.Id} 대화를 시작하지 못했습니다.", this);
                return;
            }

            _hasPlayed = true;
        }

        // 펫을 플레이어 앞에 세우고, 지금 위치를 기준으로 말하는 사람 카메라를 놓는다.
        private void BeginStaging()
        {
            PlayerFacade player = GameManager.Instance != null ? GameManager.Instance.CurrentPlayerFacade : null;
            if (player == null) return;

            PetSpawner petSpawner = player.GetComponentInChildren<PetSpawner>(true);
            if ((petSpawner == null) || (petSpawner.Pivot == null)) return;

            _petPivot = petSpawner.Pivot;
            _petPivotLocalPosition = _petPivot.localPosition;
            _petPivotLocalRotation = _petPivot.localRotation;

            // 평소 펫 추종 위치의 앞쪽이 플레이어가 보는 방향이다.
            Vector3 forward = Vector3.ProjectOnPlane(_petPivot.forward, Vector3.up).normalized;
            if (forward.sqrMagnitude <= Mathf.Epsilon) forward = Vector3.ProjectOnPlane(player.transform.forward, Vector3.up).normalized;
            Vector3 right = Vector3.Cross(Vector3.up, forward);

            Vector3 playerPosition = player.transform.position;
            Vector3 petPosition = playerPosition + ToWorld(_petStandOffset, right, forward);
            Vector3 playerLookPosition = playerPosition + (Vector3.up * _playerLookHeight);

            // 펫은 추종 위치의 앞쪽을 보므로, 플레이어 쪽을 보게 돌려 둔다.
            _petPivot.SetPositionAndRotation(petPosition, Quaternion.LookRotation(-forward));

            PlaceCamera(_playerShotCamera, petPosition + ToWorld(_playerShotOffset, right, forward), playerLookPosition);
            PlaceCamera(_petShotCamera, playerPosition + ToWorld(_petShotOffset, right, forward), petPosition);
        }

        // 펫을 원래 자리로 돌려보낸다.
        private void EndStaging()
        {
            TurnOffCameras();

            if (_petPivot != null)
                _petPivot.SetLocalPositionAndRotation(_petPivotLocalPosition, _petPivotLocalRotation);

            _petPivot = null;
        }

        // CameraIndex가 0 이상이면 고정 카메라, -1이면 말하는 사람 카메라를 켠다.
        private void HandleLineChanged(SDialogueLine line)
        {
            CinemachineCamera target = null;

            if ((_cameras != null) && (line.CameraIndex >= 0) && (line.CameraIndex < _cameras.Length))
                target = _cameras[line.CameraIndex];
            else if (line.SpeakerName == _playerSpeakerName)
                target = _playerShotCamera;
            else if (line.SpeakerName == _petSpeakerName)
                target = _petShotCamera;

            SetActiveCamera(target);
        }

        private void HandleFinished()
        {
            EndStaging();
            PlayerPrefs.SetInt(SeenKey, 1);
            PlayerPrefs.Save();
        }

        // target만 켜고 나머지 연출 카메라는 끈다. null이면 모두 꺼서 플레이어 카메라로 돌아간다.
        private void SetActiveCamera(CinemachineCamera target)
        {
            if (_playerShotCamera != null) _playerShotCamera.enabled = (_playerShotCamera == target);
            if (_petShotCamera != null) _petShotCamera.enabled = (_petShotCamera == target);

            if (_cameras == null) return;

            foreach (CinemachineCamera fixedCamera in _cameras)
            {
                if (fixedCamera != null)
                    fixedCamera.enabled = (fixedCamera == target);
            }
        }

        private void TurnOffCameras()
        {
            SetActiveCamera(null);
        }

        private static Vector3 ToWorld(Vector3 offset, Vector3 right, Vector3 forward)
        {
            return (right * offset.x) + (Vector3.up * offset.y) + (forward * offset.z);
        }

        private static void PlaceCamera(CinemachineCamera camera, Vector3 position, Vector3 lookAt)
        {
            if (camera == null) return;

            camera.transform.SetPositionAndRotation(position, Quaternion.LookRotation(lookAt - position));
        }
        #endregion
    }
}
