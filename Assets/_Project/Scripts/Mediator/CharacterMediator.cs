using Scripts.Controller;
using UnityEngine;

namespace Scripts.Mediator
{
    public class CharacterMediator : MonoBehaviour
    {
        [SerializeField] private MoveMediator moveMediator;
        [SerializeField] private CameraMediator cameraMediator;
        
        private ICharacterController _controller;

        private void Awake()
        {
            if(_controller == null) _controller = GetComponentInParent<ICharacterController>();
        }

        private void OnEnable()
        {
            if (_controller == null) return;

            if (moveMediator)
            {
                _controller.SetMoveRequest(moveMediator.CommandMove);
                _controller.SetJumpRequest(moveMediator.CommandJump);
                if (cameraMediator)
                {
                    moveMediator.SetReferenceFrame(cameraMediator.ReferenceFrame);
                }
            }

            if (cameraMediator)
            {
                _controller.SetLookRequest(cameraMediator.CommandRotateCamera);
            }
        }

        private void OnDisable()
        {
            if (_controller == null) return;

            if (moveMediator)
            {
                _controller.RemoveMoveRequest(moveMediator.CommandMove);
                _controller.RemoveJumpRequest(moveMediator.CommandJump);
                moveMediator.SetReferenceFrame(null);
            }
            
            if (cameraMediator)
            {
                _controller.RemoveLookRequest(cameraMediator.CommandRotateCamera);
            }
        }
    }
}
