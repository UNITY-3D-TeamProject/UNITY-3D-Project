using System.Collections;
using UnityEngine;

namespace UI.Credits
{
    /// <summary>
    /// 엔딩 크레딧 왼쪽 무대의 장면 하나. 무대 프리팹 Beats 아래 오브젝트에 붙인다.
    /// 장면이 시작될 때 무대가 먼저 시작 배치로 옮긴 뒤 CoPlay를 재생한다.
    /// 그래서 장면 순서를 바꿔도 앞 장면이 끝난 자리에 영향받지 않는다.
    /// 새 장면은 이 클래스를 상속하고 CoPlay만 작성한다.
    /// </summary>
    public abstract class EndingCastBeat : MonoBehaviour
    {
        #region Serialized Fields
        [Header("시작 배치 (무대 로컬)")]
        [SerializeField] private Vector3 _cameraPosition = new Vector3(0.0f, 1.2f, -6.5f);
        [SerializeField] private float _cameraPitch = 4.0f;

        [SerializeField] private Vector3 _playerPosition = Vector3.zero;
        [Tooltip("180 = 화면 보기, 90 = 오른쪽 보기, 270 = 왼쪽 보기")]
        [SerializeField] private float _playerYaw = EndingCastDirector.FACE_CAMERA_YAW;
        [Tooltip("시작할 때 틀 플레이어 애니메이터 상태 이름")]
        [SerializeField] private string _playerState = "Idle";

        [Tooltip("켜면 펫이 플레이어 머리 위에 앉아 따라다닌다 (Pet Position 무시)")]
        [SerializeField] private bool _isPetOnHead;
        [SerializeField] private Vector3 _petPosition = new Vector3(0.45f, 0.9f, 0.0f);
        [SerializeField] private float _petYaw = EndingCastDirector.FACE_CAMERA_YAW;
        [Tooltip("시작할 때 틀 펫 애니메이터 상태 이름")]
        [SerializeField] private string _petState = "R11_Idle";
        #endregion

        #region Public Methods
        public void PlaceAtStart(EndingCastDirector stage)
        {
            stage.StageCamera.localPosition = _cameraPosition;
            stage.StageCamera.localRotation = Quaternion.Euler(_cameraPitch, 0.0f, 0.0f);

            stage.Player.localPosition = _playerPosition;
            EndingCastDirector.Face(stage.Player, _playerYaw);
            EndingCastDirector.Play(stage.PlayerAnimator, _playerState);

            stage.IsPetOnHead = _isPetOnHead;
            if (!_isPetOnHead) stage.Pet.localPosition = _petPosition;
            EndingCastDirector.Face(stage.Pet, _petYaw);
            EndingCastDirector.Play(stage.PetAnimator, _petState);
        }
        #endregion

        #region Coroutines
        public abstract IEnumerator CoPlay(EndingCastDirector stage);
        #endregion
    }
}
