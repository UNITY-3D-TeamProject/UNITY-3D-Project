using System;
using UnityEngine;
using UnityEngine.InputSystem;
using Scripts.Controller;

namespace Scripts.PlayerInput
{
    public class PlayerInputComponent : MonoBehaviour, ICharacterController
    {
        private event Action<Vector2> OnMoveRequested;
        private event Action<Vector2> OnLookRequested;
        private event Action OnJumpRequested;

        private void OnMove(InputValue value)
        {
            OnMoveRequested?.Invoke(value.Get<Vector2>());
        }

        private void OnLook(InputValue value)
        {
            OnLookRequested?.Invoke(value.Get<Vector2>());
        }

        private void OnJump(InputValue value)
        {
            OnJumpRequested?.Invoke();
        }
    
        private void OnRoll(InputValue value)
        {
            Debug.Log("Roll");
        }
    
        private void OnAim(InputValue value)
        {
            Debug.Log("Aim");
        }
    
        private void OnFire(InputValue value)
        {
            Debug.Log("Fire");
        }
    
        private void OnInteract(InputValue value)
        {
            Debug.Log("Interact");
        }
    
        private void OnScan(InputValue value)
        {
            Debug.Log("Scan");
        }
    
        private void OnSpawnVehicle(InputValue value)
        {
            Debug.Log("SpawnVehicle");
        }

        public void SetMoveRequest(Action<Vector2> callback)
        {
            if(callback == null) return;
            OnMoveRequested = callback;
        }

        public void SetLookRequest(Action<Vector2> callback)
        {
            if(callback == null) return;
            OnLookRequested = callback;
        }

        public void SetJumpRequest(Action callback)
        {
            if(callback == null) return;
            OnJumpRequested = callback;
        }

        public void ClearMoveRequest()
        {
            OnMoveRequested = null;
        }

        public void ClearLookRequest()
        {
            OnLookRequested = null;
        }

        public void ClearJumpRequest()
        {
            OnJumpRequested = null;
        }
    }
}
