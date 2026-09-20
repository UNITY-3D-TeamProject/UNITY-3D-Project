using Scripts.Input;
using UnityEngine;

namespace Scripts.Mediator
{
    public class CharacterMediator : MonoBehaviour
    {
        [SerializeField] private PlayerInputComponent inputComponent;

        [SerializeField] private MoveMediator moveMediator;
        [SerializeField] private CameraMediator cameraMediator;

        private void OnEnable()
        {
            if (inputComponent)
            {
                inputComponent.SetRequestMove(moveMediator.CommandMove);
                inputComponent.SetRequestJump(moveMediator.CommandJump);
            }

            if (cameraMediator)
            {
                inputComponent.SetRequestLook(cameraMediator.CommandRotateCamera);
                moveMediator.SetReferenceFrame(cameraMediator.ReferenceFrame);
            }
        }
    }
}
