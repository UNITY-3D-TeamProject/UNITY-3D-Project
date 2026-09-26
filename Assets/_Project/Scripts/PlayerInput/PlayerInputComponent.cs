using System;
using UnityEngine;
using UnityEngine.InputSystem;
using Mediator.SubMediators;

// 주의: 이 네임스페이스는 UnityEngine.InputSystem.PlayerInput 클래스와 이름이 같다.
// 이 프로젝트 코드에서 해당 클래스를 쓸 때는 반드시 UnityEngine.InputSystem.PlayerInput 으로 완전 수식한다.
namespace PlayerInput
{
    public class PlayerInputComponent : MonoBehaviour, IMoveController, IRotateController
    {
        #region Events
        private event Action<Vector2> _onMoveRequested;
        private event Action<Vector2> _onLookRequested;
        private event Action _onJumpRequested;
        #endregion

        #region IMoveController
        public void SetMoveRequest(Action<Vector2> callback)
        {
            if (callback == null) return;
            _onMoveRequested = callback;
        }
        public void SetJumpRequest(Action callback)
        {
            if (callback == null) return;
            _onJumpRequested = callback;
        }
        public void ClearMoveRequest()
        {
            _onMoveRequested = null;
        }
        public void ClearJumpRequest()
        {
            _onJumpRequested = null;
        }
        #endregion
        
        #region IRotateController
        public void SetLookRequest(Action<Vector2> callback)
        {
            if (callback == null) return;
            _onLookRequested = callback;
        }
        public void ClearLookRequest()
        {
            _onLookRequested = null;
        }
        #endregion
        
        #region Input Messages
        // PlayerInput(Send Messages) 가 액션 이름("On" + 액션명)으로 호출하는 메서드들.
        // 직접 호출되지 않으므로 이름을 바꾸면 Input Actions 에셋의 액션 이름도 함께 바꿔야 한다.

        private void OnMove(InputValue value)
        {
            _onMoveRequested?.Invoke(value.Get<Vector2>());
        }

        private void OnLook(InputValue value)
        {
            _onLookRequested?.Invoke(value.Get<Vector2>());
        }

        private void OnJump(InputValue value)
        {
            _onJumpRequested?.Invoke();
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
        #endregion
    }
}
