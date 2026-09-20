using UnityEngine;
using UnityEngine.InputSystem;

namespace _Project.Scripts.Input
{
    public class PlayerInputComponent : MonoBehaviour
    {
        private void OnMove(InputValue value)
        {
            Vector2 input = value.Get<Vector2>();
            Debug.Log("Move");
        }
    
        private void OnLook(InputValue value)
        {
            Vector2 input = value.Get<Vector2>();
            Debug.Log("Look");
        }
    
        private void OnJump(InputValue value)
        {
            Debug.Log("Jump");
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
    }
}
