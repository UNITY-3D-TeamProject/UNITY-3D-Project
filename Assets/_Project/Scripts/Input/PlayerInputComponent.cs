using UnityEngine;
using UnityEngine.InputSystem;

namespace Scripts.Input
{
    public class PlayerInputComponent : MonoBehaviour
    {
        public delegate void RequestInputVector3(Vector3 amount);
        public delegate void RequestInputVector2(Vector2 amount);
        public delegate void RequestInputButton();

        private event RequestInputVector3 RequestMove;
        private event RequestInputVector2 RequestLook;
        private event RequestInputButton RequestJump;
        
        private void OnMove(InputValue value)
        {
            Vector2 input = value.Get<Vector2>();
            RequestMove?.Invoke(new Vector3(input.x, 0, input.y));
        }
    
        private void OnLook(InputValue value)
        {
            Vector2 input = value.Get<Vector2>();
            RequestLook?.Invoke(input);
        }
    
        private void OnJump(InputValue value)
        {
            RequestJump?.Invoke();
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

        public void SetRequestMove(RequestInputVector3 callback)
        {
            if (callback == null) return;
            RequestMove = callback;
        }

        public void SetRequestLook(RequestInputVector2 callback)
        {
            if (callback == null) return;
            RequestLook = callback;
        }
        
        public void SetRequestJump(RequestInputButton callback)
        {
            if (callback == null) return;
            RequestJump = callback;
        }
    }
}
