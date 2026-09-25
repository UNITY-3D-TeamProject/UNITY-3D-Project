using UnityEngine;
using UnityEngine.Serialization;
using Controller;

namespace Mediator
{
    /// <summary>
    /// ICharacterController(입력/AI)의 요청을 MoveMediator / CameraMediator 에 연결하는 접착 컴포넌트.
    /// 같은 오브젝트의 ICharacterController 를 찾아 OnEnable 에서 콜백을 연결하고 OnDisable 에서 해제한다.
    /// 카메라가 있으면 카메라 피벗을 이동 기준 프레임으로 넘겨 카메라 방향 기준 이동이 되도록 한다.
    /// </summary>
    public class CharacterMediator : MonoBehaviour
    {
        #region Serialized Fields
        [Header("References")]
        [FormerlySerializedAs("moveMediator")]
        [SerializeField] private MoveMediator _moveMediator;
        [FormerlySerializedAs("cameraMediator")]
        [SerializeField] private CameraMediator _cameraMediator;
        #endregion

        #region Private Fields
        private ICharacterController _controller;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            if (_controller == null) _controller = GetComponent<ICharacterController>();
        }

        private void OnEnable()
        {
            if (_controller == null) return;

            if (_moveMediator)
            {
                _controller.SetMoveRequest(_moveMediator.CommandMove);
                _controller.SetJumpRequest(_moveMediator.CommandJump);
                if (_cameraMediator)
                {
                    _moveMediator.SetReferenceFrame(_cameraMediator.ReferenceFrame);
                }
            }

            if (_cameraMediator)
            {
                _controller.SetLookRequest(_cameraMediator.CommandRotateCamera);
            }
        }

        private void OnDisable()
        {
            if (_controller == null) return;

            _controller.ClearMoveRequest();
            _controller.ClearJumpRequest();
            _controller.ClearLookRequest();

            if (_moveMediator)
            {
                _moveMediator.SetReferenceFrame(null);
            }
        }
        #endregion
    }
}
