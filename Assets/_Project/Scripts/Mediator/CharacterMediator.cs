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
    /// // Awake 가 늦게 실행되어 다른 Mediator 에 대한 조정 실행
    [DefaultExecutionOrder(100)]
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
        private IMoveController _moveController;
        private IRotateController _rotateController;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            if(_moveController == null) _moveController = GetComponent<IMoveController>();
            if(_moveMediator) _moveMediator.MoveController = _moveController;
            
            if(_rotateController == null) _rotateController = GetComponent<IRotateController>();
            if(_cameraMediator) _cameraMediator.RotateController = _rotateController;
        }

        private void OnEnable()
        {
            if (_moveMediator && _cameraMediator)
            {
                _moveMediator.SetReferenceFrame(_cameraMediator.ReferenceFrame);
            }
        }

        private void OnDisable()
        {
            if (_moveMediator)
            {
                _moveMediator.SetReferenceFrame(null);
            }
        }
        #endregion
    }
}
