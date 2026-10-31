using UnityEngine;
using Unity.Cinemachine;
using Core;
using Facade.Player;
using Pet;

namespace UIandCinematic
{
    /// <summary>
    /// 대화 연출(펫·카메라)만 맡는 컴포넌트. 대화 진행은 모르고, UIManager가 시작·줄 바뀜·끝에 맞춰 부른다.
    /// 대화가 시작된 자리에서 펫을 플레이어 앞에 세워 플레이어를 보게 하고, 말하는 사람을 비추는 카메라로 전환한다.
    /// 대화가 끝나면 펫을 원래 자리로 돌려보내고 연출 카메라를 끈다.
    /// 대화 중에는 게임 시간이 멈추므로(GameManager.PauseGameTime), 그동안 플레이어·펫 Animator만 게임 시간과 상관없이 돌게 한다.
    /// CinemachineBrain은 게임 시간과 상관없이 돌도록 켜기만 하고 되돌리지 않는다(카메라 연출도 같은 설정을 쓰므로 서로 덮어쓰지 않게).
    /// 고정 카메라는 씬 오브젝트라 대화 SO에 넣을 수 없으므로 이 컴포넌트에 목록으로 두고, 대사의 CameraIndex로 고른다.
    /// </summary>
    public class CutsceneDirector : MonoBehaviour
    {
        #region Serialized Fields
        [Header("Speakers")]
        [Tooltip("대사의 SpeakerName과 똑같이 적는다")]
        [SerializeField] private string _playerSpeakerName = "코리";
        [SerializeField] private string _petSpeakerName = "비트";

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
        [Tooltip("대사의 CameraIndex가 0 이상이면 이 목록의 카메라를 쓴다. 이 씬의 모든 컷씬이 같이 쓰며, Priority는 플레이어 카메라(Base 0, Aim 10)보다 높게 둔다")]
        [SerializeField] private CinemachineCamera[] _fixedCameras;
        #endregion

        #region Private Fields
        // 대화 동안 옮겨 둔 펫 추종 위치와 원래 자리
        private Transform _petPivot;
        private Vector3 _petPivotLocalPosition;
        private Quaternion _petPivotLocalRotation;

        // 대화 동안 게임 시간이 멈춰도 돌도록 바꿔 둔 Animator와 원래 값
        private Animator _playerAnimator;
        private AnimatorUpdateMode _playerAnimatorUpdateMode;
        private Animator _petAnimator;
        private AnimatorUpdateMode _petAnimatorUpdateMode;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            // 작업하며 켜 둔 연출 카메라가 플레이어 카메라를 덮지 않도록 꺼 둔다.
            SetActiveCamera(null);
        }
        #endregion

        #region Public Methods
        // 펫을 플레이어 앞에 세우고, 지금 위치를 기준으로 말하는 사람 카메라를 놓는다.
        // 첫 줄의 카메라가 대화 시작과 동시에 정해지므로 대화를 띄우기 전에 부른다.
        public void BeginStaging()
        {
            PlayerFacade player = (GameManager.Instance != null) ? GameManager.Instance.CurrentPlayerFacade : null;
            if (player == null) return;

            // 대화 동안 게임 시간이 멈추므로, 카메라 전환과 플레이어 대기 애니메이션은 게임 시간과 상관없이 돌게 한다.
            // Brain은 되돌리지 않는다. 평소에는 게임 시간이 1이라 차이가 없다.
            CinemachineBrain brain = (CinemachineBrain.ActiveBrainCount > 0) ? CinemachineBrain.GetActiveBrain(0) : null;
            if (brain != null)
                brain.IgnoreTimeScale = true;

            _playerAnimator = player.GetComponentInChildren<Animator>();
            if (_playerAnimator != null) _playerAnimatorUpdateMode = SwitchToUnscaledTime(_playerAnimator);

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

            // 시간이 멈추면 PetFollower가 따라가지 않으므로 펫을 설 자리로 바로 옮기고 멈춰 선 애니메이션으로 둔다.
            PetFollower pet = petSpawner.Pet;
            if (pet != null)
            {
                pet.transform.SetPositionAndRotation(petPosition, _petPivot.rotation);

                if (pet.TryGetComponent(out PetAnimator petAnimator))
                    petAnimator.SetMoveSpeed(0.0f);

                _petAnimator = pet.GetComponentInChildren<Animator>();
                if (_petAnimator != null) _petAnimatorUpdateMode = SwitchToUnscaledTime(_petAnimator);
            }

            PlaceCamera(_playerShotCamera, petPosition + ToWorld(_playerShotOffset, right, forward), playerLookPosition);
            PlaceCamera(_petShotCamera, playerPosition + ToWorld(_petShotOffset, right, forward), petPosition);
        }

        // 펫을 원래 자리로 돌려보내고 연출 카메라를 끈다. 바꿔 둔 Animator도 원래대로 돌린다.
        public void EndStaging()
        {
            SetActiveCamera(null);

            if (_playerAnimator != null) _playerAnimator.updateMode = _playerAnimatorUpdateMode;
            if (_petAnimator != null) _petAnimator.updateMode = _petAnimatorUpdateMode;

            _playerAnimator = null;
            _petAnimator = null;

            if (_petPivot != null)
                _petPivot.SetLocalPositionAndRotation(_petPivotLocalPosition, _petPivotLocalRotation);

            _petPivot = null;
        }

        // 줄이 바뀔 때 그 줄의 말하는 사람·카메라 번호로 부른다. cameraIndex가 0 이상이면 고정 카메라, -1이면 말하는 사람 카메라를 켠다.
        public void ShowLine(string speakerName, int cameraIndex)
        {
            CinemachineCamera target = null;

            if ((_fixedCameras != null) && (cameraIndex >= 0) && (cameraIndex < _fixedCameras.Length))
                target = _fixedCameras[cameraIndex];
            else if (speakerName == _playerSpeakerName)
                target = _playerShotCamera;
            else if (speakerName == _petSpeakerName)
                target = _petShotCamera;

            SetActiveCamera(target);
        }

        // 연출 카메라를 모두 끈다. 펫은 그대로 둔다. 컷씬에서 카메라 키로 고른 카메라를 쓸 때 부른다.
        public void HideCameras()
        {
            SetActiveCamera(null);
        }
        #endregion

        #region Private Methods
        // target만 켜고 나머지 연출 카메라는 끈다. null이면 모두 꺼서 플레이어 카메라로 돌아간다.
        private void SetActiveCamera(CinemachineCamera target)
        {
            if (_playerShotCamera != null) _playerShotCamera.enabled = (_playerShotCamera == target);
            if (_petShotCamera != null) _petShotCamera.enabled = (_petShotCamera == target);

            if (_fixedCameras == null) return;

            foreach (CinemachineCamera fixedCamera in _fixedCameras)
            {
                if (fixedCamera != null)
                    fixedCamera.enabled = (fixedCamera == target);
            }
        }

        // 원래 updateMode를 돌려주고, 게임 시간과 상관없이 돌도록 UnscaledTime으로 바꾼다.
        private static AnimatorUpdateMode SwitchToUnscaledTime(Animator animator)
        {
            AnimatorUpdateMode originalMode = animator.updateMode;
            animator.updateMode = AnimatorUpdateMode.UnscaledTime;
            return originalMode;
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
