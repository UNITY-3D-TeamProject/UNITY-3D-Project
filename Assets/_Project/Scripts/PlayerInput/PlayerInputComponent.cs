using System;
using UnityEngine;
using UnityEngine.InputSystem;
using Mediator.SubMediators;

// 주의: 이 네임스페이스는 UnityEngine.InputSystem.PlayerInput 클래스와 이름이 같다.
// 이 프로젝트 코드에서 해당 클래스를 쓸 때는 반드시 UnityEngine.InputSystem.PlayerInput 으로 완전 수식한다.
namespace PlayerInput
{
    public class PlayerInputComponent : MonoBehaviour, IMoveController, ICameraController, ISkillRequestController
    {
        #region Events
        private event Action<Vector2> _onMoveRequested;
        private event Action<Vector2> _onLookRequested;
        private event Action<bool> _onAimRequested;
        private event Action _onJumpRequested;
        private event Action<string> _requestExecuteSkill;
        private event Action<string> _requestStopSkill;
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
        
        #region ICameraController
        public void SetLookRequest(Action<Vector2> callback)
        {
            if (callback == null) return;
            _onLookRequested = callback;
        }
        public void ClearLookRequest()
        {
            _onLookRequested = null;
        }
        public void SetAimRequest(Action<bool> callback)
        {
            if (callback == null) return;
            _onAimRequested = callback;
        }
        public void ClearAimRequest()
        {
            _onAimRequested = null;
        }
        #endregion
        
        #region ISkillRequestController
        public void SetRequestExecuteSkill(Action<string> requestExecuteSkill)
        {
            if(requestExecuteSkill == null) return;
            _requestExecuteSkill = requestExecuteSkill;
        }

        public void ClearRequestExecuteSkill()
        {
            _requestExecuteSkill = null;
        }

        public void SetRequestStopSkill(Action<string> requestStopSkill)
        {
            if (requestStopSkill == null) return;
            _requestStopSkill = requestStopSkill;
        }

        public void ClearRequestStopSkill()
        {
            _requestStopSkill = null;
        }
        #endregion
        
        #region Input Messages
        // PlayerInput(Send Messages) 가 액션 이름("On" + 액션명)으로 호출하는 메서드들.
        // 직접 호출되지 않으므로 이름을 바꾸면 Input Actions 에셋의 액션 이름도 함께 바꿔야 한다.

        // Aim 액션은 Press And Release 로 설정되어 누를 때와 뗄 때 모두 호출된다.
        private void OnAim(InputValue value)
        {
            _onAimRequested?.Invoke(value.isPressed);
        }

        // Fire 액션은 Press And Release 로 설정되어 누를 때와 뗄 때 모두 호출된다.
        private void OnFire(InputValue value)
        {
            if (value.isPressed)
                _requestExecuteSkill?.Invoke("Attack");
            else
                _requestStopSkill?.Invoke("Attack");
        }
        
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
            _requestExecuteSkill?.Invoke("Roll");
        }

        private void OnInteract(InputValue value)
        {
            _requestExecuteSkill?.Invoke("Interact");
        }

        private void OnScan(InputValue value)
        {
            _requestExecuteSkill?.Invoke("Scan");
        }

        private void OnSpawnVehicle(InputValue value)
        {
            _requestExecuteSkill?.Invoke("SpawnVehicle");
        }
        #endregion
    }
}
