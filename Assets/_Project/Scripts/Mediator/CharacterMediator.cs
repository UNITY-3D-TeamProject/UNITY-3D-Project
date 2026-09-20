using Scripts.Input;
using UnityEngine;

namespace Scripts.Mediator
{
    public class CharacterMediator : MonoBehaviour
    {
        [SerializeField] private PlayerInputComponent inputComponent;
        
        [SerializeField] private MoveMediator moveMediator;

        private void OnEnable()
        {
            if (inputComponent)
            {
                inputComponent.SetRequestMove(moveMediator.CommandMove);
                inputComponent.SetRequestJump(moveMediator.CommandJump);
            }
        }
    }
}
