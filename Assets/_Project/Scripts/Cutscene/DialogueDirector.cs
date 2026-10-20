using System;
using UnityEngine;
using Unity.Cinemachine;
using Core;
using Facade.Player;
using Pet;
using UI;
using UI.Dialogue;

namespace Cutscene
{
    /// <summary>
    /// 대화 연출을 맡는 씬 단위 컴포넌트. 씬에 하나 두고 DialogueCutscene들이 같이 쓴다.
    /// 대화가 시작된 자리에서 펫을 플레이어 앞에 세워 플레이어를 보게 하고, 말하는 사람을 비추는 카메라로 전환한다.
    /// 대화가 끝나면 펫을 원래 자리로 돌려보내고 연출 카메라를 끈다.
    /// </summary>
    public class DialogueDirector : MonoBehaviour
    {
        #region Serialized Fields
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
        #endregion

        #region Private Fields
        // 지금 재생 중인 대화의 고정 카메라와 종료 콜백
        private CinemachineCamera[] _fixedCameras;
        private Action _onFinished;
        // 대화 동안 옮겨 둔 펫 추종 위치와 원래 자리
        private Transform _petPivot;
        private Vector3 _petPivotLocalPosition;
        private Quaternion _petPivotLocalRotation;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            // 작업하며 켜 둔 연출 카메라가 플레이어 카메라를 덮지 않도록 꺼 둔다.
            SetActiveCamera(null);
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// 지금 플레이어가 있는 자리에서 대화 연출을 시작한다.
        /// </summary>
        /// <param name="sequence">재생할 대화</param>
        /// <param name="fixedCameras">대사의 CameraIndex가 0 이상일 때 쓸 고정 카메라. 없으면 null</param>
        /// <param name="onFinished">대화가 끝나 연출을 정리한 뒤 호출된다</param>
        /// <returns>시작했으면 true. UIManager가 없거나 이미 대화 중이면 false</returns>
        public bool Play(SODialogueSequence sequence, CinemachineCamera[] fixedCameras, Action onFinished)
        {
            UIManager uiManager = UIManager.Instance;

            if (uiManager == null)
            {
                Debug.LogWarning("[DialogueDirector] 씬에 UIManager가 없어 대화를 재생하지 못했습니다.", this);
                return false;
            }

            if (uiManager.IsDialoguePlaying) return false;

            _fixedCameras = fixedCameras;
            _onFinished = onFinished;

            // 첫 줄의 카메라가 대화 시작과 동시에 정해지므로 펫과 카메라를 먼저 배치한다.
            BeginStaging();

            if (!uiManager.PlayDialogue(sequence, HandleLineChanged, HandleFinished))
            {
                EndStaging();
                _onFinished = null;
                return false;
            }

            return true;
        }
        #endregion

        #region Private Methods
        // 펫을 플레이어 앞에 세우고, 지금 위치를 기준으로 말하는 사람 카메라를 놓는다.
        private void BeginStaging()
        {
            PlayerFacade player = (GameManager.Instance != null) ? GameManager.Instance.CurrentPlayerFacade : null;
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

        // 펫을 원래 자리로 돌려보내고 연출 카메라를 끈다.
        private void EndStaging()
        {
            SetActiveCamera(null);

            if (_petPivot != null)
                _petPivot.SetLocalPositionAndRotation(_petPivotLocalPosition, _petPivotLocalRotation);

            _petPivot = null;
            _fixedCameras = null;
        }

        // CameraIndex가 0 이상이면 고정 카메라, -1이면 말하는 사람 카메라를 켠다.
        private void HandleLineChanged(SDialogueLine line)
        {
            CinemachineCamera target = null;

            if ((_fixedCameras != null) && (line.CameraIndex >= 0) && (line.CameraIndex < _fixedCameras.Length))
                target = _fixedCameras[line.CameraIndex];
            else if (line.SpeakerName == _playerSpeakerName)
                target = _playerShotCamera;
            else if (line.SpeakerName == _petSpeakerName)
                target = _petShotCamera;

            SetActiveCamera(target);
        }

        private void HandleFinished()
        {
            EndStaging();

            Action onFinished = _onFinished;
            _onFinished = null;
            onFinished?.Invoke();
        }

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
